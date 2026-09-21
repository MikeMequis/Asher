/**
 * Platform descriptor surfaced by the C# host via `getPlatformInfo`.
 * The renderer never embeds OS knowledge; it adapts UI capabilities from this data.
 *
 * @typedef {object} PlatformInfo
 * @property {string} kind                     'windows' | 'linux'
 * @property {string} gameExecutableName
 * @property {string} realGameExecutableName
 * @property {string} launcherExecutableName
 * @property {string} bootstrapLibraryName
 * @property {boolean} usesLauncherSwap
 * @property {boolean} supportsRecoveryHelper  Windows-only emergency uninstall script
 * @property {string} defaultGameFolderName
 */

/** @type {PlatformInfo} */
export const UNKNOWN_PLATFORM = {
  kind: 'unknown',
  gameExecutableName: '',
  realGameExecutableName: '',
  launcherExecutableName: '',
  bootstrapLibraryName: '',
  usesLauncherSwap: false,
  supportsRecoveryHelper: false,
  defaultGameFolderName: ''
};

/**
 * @param {PlatformInfo | null | undefined} platform
 * @returns {PlatformInfo}
 */
export function normalizePlatform(platform) {
  return platform ? { ...UNKNOWN_PLATFORM, ...platform } : { ...UNKNOWN_PLATFORM };
}

/**
 * @param {PlatformInfo | null | undefined} platform
 */
export function isWindows(platform) {
  return normalizePlatform(platform).kind === 'windows';
}

/**
 * @param {PlatformInfo | null | undefined} platform
 */
export function isLinux(platform) {
  return normalizePlatform(platform).kind === 'linux';
}

/**
 * Windows installs a launcher that replaces DustAET.exe; Linux leaves the native
 * executable in place and attaches through libasher_bootstrap.so.
 * @param {PlatformInfo | null | undefined} platform
 */
export function usesLauncherSwap(platform) {
  return normalizePlatform(platform).usesLauncherSwap;
}

/**
 * Whether the "Total exclusion" removal path (Uninstall-Asher.cmd) exists.
 * @param {PlatformInfo | null | undefined} platform
 */
export function supportsRecoveryHelper(platform) {
  return normalizePlatform(platform).supportsRecoveryHelper;
}
