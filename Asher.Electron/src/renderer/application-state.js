import { classifyError } from './errors.js';
import { logDiagnostic } from './diagnostic-log.js';
import { normalizePlatform } from './platform.js';

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */
/** @typedef {import('./platform.js').PlatformInfo} PlatformInfo */

/**
 * @typedef {object} InstallState
 * @property {'notInstalled' | 'installed' | 'partial'} state
 * @property {boolean} canUninstall
 * @property {boolean} canRestore
 * @property {string} marker
 */

/**
 * @typedef {object} ApplicationState
 * @property {'manager' | 'installWizard'} mode
 * @property {object} settings
 * @property {object | null} folder
 * @property {PlatformInfo} platform
 * @property {InstallState | null} installState
 * @property {boolean} isConfigured
 * @property {boolean} needsInstallation
 * @property {boolean} canUninstall
 * @property {boolean} canRestore
 * @property {boolean} canLaunchGame
 * @property {'welcome' | 'setup' | 'home' | 'manager' | 'settings'} recommendedScreen
 */

/**
 * Load authoritative application state from the C# host.
 * @param {ApplicationClient} client
 * @returns {Promise<ApplicationState>}
 */
export async function fetchApplicationState(client) {
  const [{ result: modeResult }, { result: settings }, { result: platformResult }] = await Promise.all([
    client.invoke('getApplicationMode'),
    client.invoke('getSettings'),
    client.invoke('getPlatformInfo')
  ]);

  const mode = modeResult?.mode === 'manager' ? 'manager' : 'installWizard';
  const platform = normalizePlatform(platformResult);
  const savedPath = settings?.gameFolderPath?.trim() ?? '';

  let folder = null;
  if (savedPath) {
    const { result } = await client.invoke('getGameFolderInfo', { folderPath: savedPath });
    folder = result ?? null;
  }

  const isConfigured = Boolean(folder?.isValid);
  const needsInstallation = isConfigured && mode === 'installWizard';
  const hostReportsInstalled = mode === 'manager';
  const canLaunchGame = isConfigured && hostReportsInstalled;

  let installState = null;
  if (savedPath) {
    const { result } = await client.invoke('getInstallState', { gameFolderPath: savedPath });
    installState = result ?? null;
  }

  const canUninstall = Boolean(installState?.canUninstall);
  const canRestore = Boolean(installState?.canRestore);

  const recommendedScreen =
    mode === 'manager' && isConfigured
      ? 'home'
      : 'welcome';

  const state = {
    mode,
    settings: settings ?? {},
    folder,
    platform,
    installState,
    isConfigured,
    needsInstallation,
    canUninstall,
    canRestore,
    canLaunchGame,
    recommendedScreen
  };

  logDiagnostic('info', 'app-state', 'fetchApplicationState', {
    mode,
    platform: platform.kind,
    savedPath,
    installState: installState?.state,
    isConfigured,
    needsInstallation,
    canUninstall,
    canRestore,
    hostReportsInstalled,
    settingsIsInstalled: settings?.isInstalled
  });

  return state;
}

/**
 * @param {ApplicationClient} client
 * @param {unknown} err
 */
export function mapApplicationError(client, err) {
  void client;
  return classifyError(err);
}
