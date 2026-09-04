import path from 'node:path';
import { createRequire } from 'node:module';
import { app } from 'electron';
import { writeDiagnosticLog } from './diagnostic-logger.js';
import {
  getAppInstallRoot,
  getManagerExecutablePath,
  getManagerFolderPath,
  isRunningFromGameManager,
  pathExists
} from './manager-paths.js';
import { relaunchManagerNow } from './post-quit-helper.js';

const require = createRequire(import.meta.url);

/**
 * Electron patches fs so app.asar looks like a directory. Copying with that
 * patch throws ENOTDIR. Use original-fs (real disk files) for deploy.
 */
function getOriginalFs() {
  try {
    return require('original-fs');
  } catch {
    return require('node:fs');
  }
}

/**
 * @param {string} sourceDir
 * @param {string} destDir
 */
function copyAppTree(sourceDir, destDir) {
  const fs = getOriginalFs();
  const previousNoAsar = process.noAsar;
  process.noAsar = true;

  try {
    fs.mkdirSync(path.dirname(destDir), { recursive: true });
    if (fs.existsSync(destDir)) {
      fs.rmSync(destDir, { recursive: true, force: true });
    }
    fs.cpSync(sourceDir, destDir, {
      recursive: true,
      force: true,
      errorOnExist: false
    });
  } finally {
    process.noAsar = previousNoAsar;
  }
}

/**
 * Copy the packaged app tree into game/Asher/Asher.App and quit into that copy.
 * Dev (unpackaged) skips deploy and returns { transitioned: false }.
 *
 * @param {string} gameFolderPath
 * @returns {Promise<{ transitioned: boolean, reason?: string, managerPath?: string }>}
 */
export async function transitionToInstalledManager(gameFolderPath) {
  if (!gameFolderPath || typeof gameFolderPath !== 'string') {
    return { transitioned: false, reason: 'missing_game_folder' };
  }

  if (!app.isPackaged) {
    writeDiagnosticLog('info', 'deploy', 'skip transition (dev / unpackaged)');
    return { transitioned: false, reason: 'not_packaged' };
  }

  const destDir = getManagerFolderPath(gameFolderPath);
  const managerExe = getManagerExecutablePath(destDir);

  if (isRunningFromGameManager(gameFolderPath)) {
    writeDiagnosticLog('info', 'deploy', 'already running from installed manager', { destDir });
    return { transitioned: false, reason: 'already_installed', managerPath: destDir };
  }

  const sourceDir = getAppInstallRoot();
  writeDiagnosticLog('info', 'deploy', 'copying manager into game folder', { sourceDir, destDir });

  copyAppTree(sourceDir, destDir);

  if (!pathExists(managerExe)) {
    throw new Error(`Manager deploy failed: missing ${managerExe}`);
  }

  relaunchManagerNow(managerExe);
  writeDiagnosticLog('info', 'deploy', 'spawned installed manager; quitting', { managerExe });

  setImmediate(() => {
    app.quit();
  });

  return { transitioned: true, managerPath: destDir };
}

/**
 * After uninstall from the in-game manager: do not delete Asher.App (would be
 * self-delete while Asher.exe is locked). Host already preserves this folder.
 * Stay running so the user can reinstall from the same UI.
 *
 * @param {string} gameFolderPath
 * @returns {{ scheduled: boolean, reason?: string }}
 */
export function scheduleSelfUninstallCleanup(gameFolderPath) {
  if (!app.isPackaged) {
    return { scheduled: false, reason: 'not_packaged' };
  }

  if (!isRunningFromGameManager(gameFolderPath)) {
    return { scheduled: false, reason: 'not_installed_location' };
  }

  writeDiagnosticLog('info', 'deploy', 'uninstall complete; preserving Asher.App for reinstall', {
    managerPath: getManagerFolderPath(gameFolderPath)
  });

  // Never quit or delete the running manager here.
  return { scheduled: false, reason: 'manager_preserved' };
}
