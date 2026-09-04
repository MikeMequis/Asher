import path from 'node:path';
import { app } from 'electron';

/**
 * Directory that contains the packaged Asher.exe (Distribution / unpacked build).
 */
export function getAppInstallRoot() {
  return path.dirname(app.getPath('exe'));
}

/**
 * @param {string} installRoot
 */
export function getAppExecutablePath(installRoot = getAppInstallRoot()) {
  return path.join(installRoot, 'Asher.exe');
}
