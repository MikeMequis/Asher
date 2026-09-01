import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const RUNTIME_FOLDER = 'Asher';
const MANAGER_FOLDER = 'Asher.App';
const LOGS_FOLDER = 'AsherLogs';
const SETTINGS_FILE = 'settings.json';
const GAME_EXECUTABLE = 'DustAET.exe';
const REAL_GAME_EXECUTABLE = 'DustAET.real.exe';

/**
 * @param {string} filePath
 * @returns {Record<string, unknown> | null}
 */
function tryReadSettings(filePath) {
  try {
    if (!fs.existsSync(filePath)) {
      return null;
    }

    return JSON.parse(fs.readFileSync(filePath, 'utf8'));
  } catch {
    return null;
  }
}

/**
 * @param {string | null | undefined} gameFolderPath
 * @returns {boolean}
 */
export function isValidGameFolder(gameFolderPath) {
  if (!gameFolderPath?.trim()) {
    return false;
  }

  const gameFolder = gameFolderPath.trim();
  if (!fs.existsSync(gameFolder)) {
    return false;
  }

  return (
    fs.existsSync(path.join(gameFolder, GAME_EXECUTABLE)) ||
    (fs.existsSync(path.join(gameFolder, REAL_GAME_EXECUTABLE)) &&
      fs.existsSync(path.join(gameFolder, RUNTIME_FOLDER)))
  );
}

/**
 * @param {string} gameFolderPath
 * @returns {string}
 */
export function getGameLogsDir(gameFolderPath) {
  return path.join(gameFolderPath.trim(), RUNTIME_FOLDER, LOGS_FOLDER);
}

/**
 * @returns {string | null}
 */
export function resolveGameFolderFromSettings() {
  const settingsPaths = [
    path.join(process.cwd(), SETTINGS_FILE),
    path.join(path.dirname(process.execPath), SETTINGS_FILE),
    path.join(os.homedir(), 'AppData', 'Roaming', 'Asher', SETTINGS_FILE)
  ];

  for (const settingsPath of settingsPaths) {
    const settings = tryReadSettings(settingsPath);
    const gameFolderPath = typeof settings?.gameFolderPath === 'string'
      ? settings.gameFolderPath.trim()
      : '';

    if (isValidGameFolder(gameFolderPath)) {
      return gameFolderPath;
    }
  }

  for (const settingsPath of settingsPaths) {
    const settings = tryReadSettings(settingsPath);
    const gameFolderPath = typeof settings?.gameFolderPath === 'string'
      ? settings.gameFolderPath.trim()
      : '';

    if (!gameFolderPath) {
      continue;
    }

    const managerSettingsPath = path.join(
      gameFolderPath,
      RUNTIME_FOLDER,
      MANAGER_FOLDER,
      SETTINGS_FILE
    );
    const managerSettings = tryReadSettings(managerSettingsPath);
    const managerGameFolder = typeof managerSettings?.gameFolderPath === 'string'
      ? managerSettings.gameFolderPath.trim()
      : gameFolderPath;

    if (isValidGameFolder(managerGameFolder)) {
      return managerGameFolder;
    }
  }

  return null;
}

/**
 * @param {string | null | undefined} gameFolderPath
 * @returns {string | null}
 */
export function resolveGameLogsDir(gameFolderPath) {
  const resolvedGameFolder = gameFolderPath?.trim() || resolveGameFolderFromSettings();
  if (!resolvedGameFolder || !isValidGameFolder(resolvedGameFolder)) {
    return null;
  }

  return getGameLogsDir(resolvedGameFolder);
}

/**
 * @param {string} logsDir
 * @returns {string}
 */
export function buildManagerLogFilePath(logsDir) {
  const now = new Date();
  const pad = (value) => String(value).padStart(2, '0');
  const stamp = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  return path.join(logsDir, `manager_${stamp}.log`);
}
