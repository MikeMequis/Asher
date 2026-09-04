import fs from 'node:fs';
import path from 'node:path';
import { app } from 'electron';

export const RUNTIME_FOLDER_NAME = 'Asher';
export const MANAGER_FOLDER_NAME = 'Asher.App';

/**
 * Directory that contains the packaged Asher.exe (or Electron binary in dev).
 */
export function getAppInstallRoot() {
  return path.dirname(app.getPath('exe'));
}

/**
 * @param {string} gameFolderPath
 */
export function getManagerFolderPath(gameFolderPath) {
  return path.join(gameFolderPath, RUNTIME_FOLDER_NAME, MANAGER_FOLDER_NAME);
}

/**
 * True when the packaged exe lives under …/Asher/Asher.App (installed manager).
 */
export function isRunningFromInstalledManager() {
  const installRoot = path.resolve(getAppInstallRoot());
  const folder = path.basename(installRoot);
  const parent = path.basename(path.dirname(installRoot));
  return (
    folder.toLowerCase() === MANAGER_FOLDER_NAME.toLowerCase() &&
    parent.toLowerCase() === RUNTIME_FOLDER_NAME.toLowerCase()
  );
}

/**
 * @param {string} gameFolderPath
 */
export function isRunningFromGameManager(gameFolderPath) {
  if (!gameFolderPath || typeof gameFolderPath !== 'string') {
    return isRunningFromInstalledManager();
  }

  const installRoot = path.resolve(getAppInstallRoot());
  const managerRoot = path.resolve(getManagerFolderPath(gameFolderPath));
  return installRoot.toLowerCase() === managerRoot.toLowerCase();
}

/**
 * @param {string} targetPath
 */
export function getManagerExecutablePath(targetPath) {
  return path.join(targetPath, 'Asher.exe');
}

/**
 * @param {string} folderPath
 */
export function pathExists(folderPath) {
  try {
    return fs.existsSync(folderPath);
  } catch {
    return false;
  }
}
