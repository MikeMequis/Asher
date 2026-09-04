import { classifyError } from './errors.js';
import { logDiagnostic } from './diagnostic-log.js';

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

/**
 * @typedef {object} ApplicationState
 * @property {'manager' | 'installWizard'} mode
 * @property {object} settings
 * @property {object | null} folder
 * @property {boolean} isConfigured
 * @property {boolean} needsInstallation
 * @property {boolean} canUninstall
 * @property {boolean} canLaunchGame
 * @property {'welcome' | 'setup' | 'home' | 'manager' | 'settings'} recommendedScreen
 */

/**
 * Load authoritative application state from the C# host.
 * @param {ApplicationClient} client
 * @returns {Promise<ApplicationState>}
 */
export async function fetchApplicationState(client) {
  const [{ result: modeResult }, { result: settings }] = await Promise.all([
    client.invoke('getApplicationMode'),
    client.invoke('getSettings')
  ]);

  const mode = modeResult?.mode === 'manager' ? 'manager' : 'installWizard';
  const savedPath = settings?.gameFolderPath?.trim() ?? '';

  let folder = null;
  if (savedPath) {
    const { result } = await client.invoke('getGameFolderInfo', { folderPath: savedPath });
    folder = result ?? null;
  }

  const isConfigured = Boolean(folder?.isValid);
  const needsInstallation = isConfigured && mode === 'installWizard';

  let canUninstall = false;
  let canLaunchGame = false;
  let hostReportsInstalled = false;
  if (savedPath && mode === 'manager') {
    const { result: installed } = await client.invoke('isGameInstalled', { gameFolderPath: savedPath });
    hostReportsInstalled = Boolean(installed?.installed);
    canLaunchGame = isConfigured && hostReportsInstalled;

    if (hostReportsInstalled) {
      const { result: backup } = await client.invoke('hasRestorableBackup', { gameFolderPath: savedPath });
      canUninstall = Boolean(backup?.hasBackup);
    }
  }

  const recommendedScreen =
    mode === 'manager' && isConfigured
      ? 'home'
      : 'welcome';

  const state = {
    mode,
    settings: settings ?? {},
    folder,
    isConfigured,
    needsInstallation,
    canUninstall,
    canLaunchGame,
    recommendedScreen
  };

  logDiagnostic('info', 'app-state', 'fetchApplicationState', {
    mode,
    savedPath,
    isConfigured,
    needsInstallation,
    canUninstall,
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
