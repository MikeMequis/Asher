import fs from 'node:fs';
import path from 'node:path';
import { app } from 'electron';
import { writeDiagnosticLog } from './diagnostic-logger.js';
import {
  getAppInstallRoot,
  getManagerExecutablePath,
  getManagerFolderPath,
  isRunningFromGameManager,
  pathExists
} from './manager-paths.js';
import { scheduleDeleteManagerFolder, scheduleRelaunch } from './post-quit-helper.js';

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

  fs.mkdirSync(path.dirname(destDir), { recursive: true });
  fs.cpSync(sourceDir, destDir, {
    recursive: true,
    force: true,
    errorOnExist: false
  });

  if (!pathExists(managerExe)) {
    throw new Error(`Manager deploy failed: missing ${managerExe}`);
  }

  scheduleRelaunch(managerExe);
  writeDiagnosticLog('info', 'deploy', 'scheduled relaunch; quitting', { managerExe });

  // Defer quit so the IPC response can flush.
  setImmediate(() => {
    app.quit();
  });

  return { transitioned: true, managerPath: destDir };
}

/**
 * If running from Asher.App, schedule folder deletion after quit.
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

  const managerDir = getManagerFolderPath(gameFolderPath);
  scheduleDeleteManagerFolder(managerDir);
  writeDiagnosticLog('info', 'deploy', 'scheduled self-uninstall cleanup', { managerDir });

  setImmediate(() => {
    app.quit();
  });

  return { scheduled: true };
}
