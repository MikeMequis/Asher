import path from 'node:path';
import { app } from 'electron';

/**
 * Directory that contains the packaged Asher manager binary (Distribution / unpacked build).
 */
export function getAppInstallRoot() {
  return path.dirname(app.getPath('exe'));
}

/**
 * Packaged manager executable name for the current platform.
 */
export function getAppExecutableName() {
  return process.platform === 'win32' ? 'Asher.exe' : 'Asher';
}

/**
 * @param {string} installRoot
 */
export function getAppExecutablePath(installRoot = getAppInstallRoot()) {
  return path.join(installRoot, getAppExecutableName());
}
