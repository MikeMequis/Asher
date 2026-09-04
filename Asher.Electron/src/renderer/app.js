import { ApplicationClient } from './application-client.js';
import { ApplicationShell } from './application-shell.js';
import { bindActionBanner, showActionBanner } from './action-banner.js';
import { logDiagnostic, refreshDiagnosticLogFooter } from './diagnostic-log.js';
import { GameSetupController } from './game-setup.js';
import { InstallationController } from './installation-controller.js';
import { LaunchGameController } from './launch-game.js';
import {
  applyLanguageFromSettings,
  getLanguageOptions,
  onLanguageChange,
  t
} from './localization.js';
import { ModManagerController } from './mod-manager.js';
import { SettingsController } from './settings-controller.js';
import { applyTheme, applyThemeFromSettings } from './theme.js';
import { UninstallationController } from './uninstallation-controller.js';
import { applyDataIcons, iconHtml, NAV_ICONS } from './icons.js';

if (!window.asher) {
  logDiagnostic('error', 'renderer', 'window.asher preload bridge is missing');
}

window.addEventListener('error', (event) => {
  logDiagnostic('error', 'renderer', 'window error', {
    message: event.message,
    filename: event.filename,
    lineno: event.lineno,
    colno: event.colno
  });
});

window.addEventListener('unhandledrejection', (event) => {
  logDiagnostic('error', 'renderer', 'unhandled rejection', {
    reason: event.reason instanceof Error ? event.reason.message : String(event.reason)
  });
});

const client = new ApplicationClient(window.asher);
const shell = new ApplicationShell(client);
const setup = new GameSetupController(client);
const installation = new InstallationController(client);
const uninstallation = new UninstallationController(client);
const manager = new ModManagerController(client);
const launchGame = new LaunchGameController(client);
const settings = new SettingsController(client);

const appShell = document.querySelector('.app-shell');
const sidebar = document.getElementById('sidebar');
const sidebarNav = document.getElementById('sidebar-nav');
const sidebarToggle = document.getElementById('sidebar-toggle');

const pageSubtitle = document.getElementById('page-subtitle');
const hostChip = document.getElementById('host-chip');
const statusMessage = document.getElementById('status-message');
const actionBanner = document.getElementById('notification-host');
bindActionBanner(actionBanner);

const shellLoading = document.getElementById('shell-loading');
const shellLoadingTitle = document.getElementById('shell-loading-title');
const shellLoadingMessage = document.getElementById('shell-loading-message');
const hostErrorPanel = document.getElementById('host-error-panel');
const hostErrorMessage = document.getElementById('host-error-message');
const retryHostButton = document.getElementById('retry-host');
const appContent = document.getElementById('app-content');

const welcomeView = document.getElementById('welcome-view');
const welcomeBeginButton = document.getElementById('welcome-begin');

const homeView = document.getElementById('home-view');
const homeLaunchError = document.getElementById('home-launch-error');
const homeCardManager = document.getElementById('home-card-manager');
const homeCardSettings = document.getElementById('home-card-settings');
const homeCardLaunch = document.getElementById('home-card-launch');

const installView = document.getElementById('install-view');
const installIdle = document.getElementById('install-idle');
const beginInstallButton = document.getElementById('begin-install');
const installActive = document.getElementById('install-active');
const installStatus = document.getElementById('install-status');
const installProgress = document.getElementById('install-progress');
const installProgressRing = document.getElementById('install-progress-ring');
const installProgressPct = document.getElementById('install-progress-pct');
const installDetails = document.getElementById('install-details');
const cancelInstallButton = document.getElementById('cancel-install');
const installCompleted = document.getElementById('install-completed');
const installSuccessMessage = document.getElementById('install-success-message');
const installContinueButton = document.getElementById('install-continue');
const installAutoLaunch = document.getElementById('install-auto-launch');
const installFailed = document.getElementById('install-failed');
const installFailureMessage = document.getElementById('install-failure-message');
const installErrorDetails = document.getElementById('install-error-details');
const installErrorDetailsBody = document.getElementById('install-error-details-body');
const retryInstallButton = document.getElementById('retry-install');
const installBackSetupButton = document.getElementById('install-back-setup');
const installCancelled = document.getElementById('install-cancelled');
const installCancelledMessage = document.getElementById('install-cancelled-message');
const retryInstallCancelledButton = document.getElementById('retry-install-cancelled');
const installBackSetupCancelledButton = document.getElementById('install-back-setup-cancelled');

const uninstallView = document.getElementById('uninstall-view');
const uninstallConfirm = document.getElementById('uninstall-confirm');
const confirmUninstallButton = document.getElementById('confirm-uninstall');
const cancelUninstallConfirmButton = document.getElementById('cancel-uninstall-confirm');
const uninstallActive = document.getElementById('uninstall-active');
const uninstallStatus = document.getElementById('uninstall-status');
const uninstallProgress = document.getElementById('uninstall-progress');
const uninstallProgressRing = document.getElementById('uninstall-progress-ring');
const uninstallProgressPct = document.getElementById('uninstall-progress-pct');
const uninstallDetails = document.getElementById('uninstall-details');
const cancelUninstallButton = document.getElementById('cancel-uninstall');
const uninstallCompleted = document.getElementById('uninstall-completed');
const uninstallSuccessMessage = document.getElementById('uninstall-success-message');
const uninstallContinueButton = document.getElementById('uninstall-continue');
const uninstallFailed = document.getElementById('uninstall-failed');
const uninstallFailureMessage = document.getElementById('uninstall-failure-message');
const uninstallErrorDetails = document.getElementById('uninstall-error-details');
const uninstallErrorDetailsBody = document.getElementById('uninstall-error-details-body');
const retryUninstallButton = document.getElementById('retry-uninstall');
const uninstallBackManagerButton = document.getElementById('uninstall-back-manager');
const uninstallCancelled = document.getElementById('uninstall-cancelled');
const uninstallCancelledMessage = document.getElementById('uninstall-cancelled-message');
const retryUninstallCancelledButton = document.getElementById('retry-uninstall-cancelled');
const uninstallBackManagerCancelledButton = document.getElementById('uninstall-back-manager-cancelled');

const setupView = document.getElementById('setup-view');
const setupContextError = document.getElementById('setup-context-error');
const autoDetectButton = document.getElementById('auto-detect');
const browseButton = document.getElementById('browse-folder');
const folderPathInput = document.getElementById('folder-path');
const saveButton = document.getElementById('save-config');
const setupStatus = document.getElementById('setup-status');
const setupError = document.getElementById('setup-error');

const managerView = document.getElementById('manager-view');
const refreshModsButton = document.getElementById('refresh-mods');
const managerLoading = document.getElementById('manager-loading');
const managerEmpty = document.getElementById('manager-empty');
const managerError = document.getElementById('manager-error');
const managerToggleError = document.getElementById('manager-toggle-error');
const modList = document.getElementById('mod-list');
const managerStats = document.getElementById('manager-stats');
const activeCountEl = document.getElementById('active-count');
const totalCountEl = document.getElementById('total-count');

const settingsView = document.getElementById('settings-view');
const settingsError = document.getElementById('settings-error');
const settingsGamePath = document.getElementById('settings-game-path');
const settingsBrowse = document.getElementById('settings-browse');
const settingsPathStatus = document.getElementById('settings-path-status');
const settingsBackup = document.getElementById('settings-backup');
const settingsLanguage = document.getElementById('settings-language');
const settingsTheme = document.getElementById('settings-theme');
const settingsHostStatus = document.getElementById('settings-host-status');
const settingsUninstallCard = document.getElementById('settings-uninstall-card');
const settingsUninstallButton = document.getElementById('settings-uninstall');
const settingsResetButton = document.getElementById('settings-reset');
const settingsAppVersion = document.getElementById('settings-app-version');
const settingsCheckUpdatesButton = document.getElementById('settings-check-updates');
const settingsApplyUpdateButton = document.getElementById('settings-apply-update');
const settingsOpenReleaseButton = document.getElementById('settings-open-release');
const settingsUpdateStatus = document.getElementById('settings-update-status');

/** @type {{ status?: string, version?: string, downloadUrl?: string, releaseUrl?: string, canApplyInPlace?: boolean, message?: string } | null} */
let latestUpdateInfo = null;

shell.onChange = renderShell;
setup.onChange = renderSetup;
installation.onChange = renderInstall;
uninstallation.onChange = renderUninstall;
manager.onChange = renderManager;
launchGame.onChange = renderHome;
settings.onChange = renderSettings;

onLanguageChange(() => {
  applyLocalizedText();
  renderShell();
});

function applyLocalizedText() {
  document.querySelectorAll('[data-i18n]').forEach((el) => {
    const key = el.getAttribute('data-i18n');
    if (key) {
      el.textContent = t(key);
    }
  });

  document.querySelectorAll('[data-i18n-placeholder]').forEach((el) => {
    const key = el.getAttribute('data-i18n-placeholder');
    if (key) {
      el.setAttribute('placeholder', t(key));
    }
  });

  populateLanguageSelect();
  updateWindowTitle();
}

function updateWindowTitle() {
  if (!shell.isApplicationReady) {
    document.title = t('app.title');
    return;
  }

  document.title = shell.isManagerMode ? t('app.title.manager') : t('app.title.install');
}

/**
 * @param {SVGPathElement | null} ring
 * @param {HTMLElement | null} label
 * @param {number} percentage
 */
function setProgressRing(ring, label, percentage) {
  const clamped = Math.max(0, Math.min(100, Number(percentage) || 0));
  if (ring) {
    ring.setAttribute('stroke-dasharray', `${clamped}, 100`);
  }
  if (label) {
    label.textContent = `${Math.round(clamped)}%`;
  }
}

function populateLanguageSelect() {
  const selected = settingsLanguage.value || settings.draft?.language || 'en-US';
  settingsLanguage.innerHTML = '';
  for (const option of getLanguageOptions()) {
    const el = document.createElement('option');
    el.value = option.value;
    el.textContent = option.label;
    settingsLanguage.appendChild(el);
  }
  settingsLanguage.value = selected;
}

function getInstallWizardStep() {
  if (shell.screen === 'install') {
    if (installation.state === 'completed') {
      return 'complete';
    }
    return 'installing';
  }

  if (shell.screen === 'setup') {
    return 'gameDetection';
  }

  return 'welcome';
}

function renderSidebar() {
  const showNav = shell.isApplicationReady;
  sidebarNav.hidden = !showNav;
  appShell.classList.toggle('sidebar-collapsed', shell.sidebarCollapsed);

  if (!showNav) {
    sidebarNav.innerHTML = '';
    return;
  }

  const items = shell.isManagerMode
    ? [
        { id: 'home', label: t('nav.home'), icon: NAV_ICONS.home, screen: 'home', enabled: shell.canShowHome },
        {
          id: 'manager',
          label: t('nav.patchManager'),
          icon: NAV_ICONS.manager,
          screen: 'manager',
          enabled: shell.canShowManager
        },
        { id: 'settings', label: t('nav.settings'), icon: NAV_ICONS.settings, screen: 'settings', enabled: true }
      ]
    : [
        { id: 'welcome', label: t('nav.welcome'), icon: NAV_ICONS.welcome, screen: 'welcome', step: 'welcome' },
        {
          id: 'gameDetection',
          label: t('nav.gameDetection'),
          icon: NAV_ICONS.gameDetection,
          screen: 'setup',
          step: 'gameDetection'
        },
        {
          id: 'installing',
          label: t('nav.installing'),
          icon: NAV_ICONS.installing,
          screen: 'install',
          step: 'installing'
        },
        {
          id: 'complete',
          label: t('nav.complete'),
          icon: NAV_ICONS.complete,
          screen: 'install',
          step: 'complete'
        }
      ];

  const currentStep = getInstallWizardStep();
  const reached = {
    welcome: true,
    gameDetection: ['gameDetection', 'installing', 'complete'].includes(currentStep),
    installing: ['installing', 'complete'].includes(currentStep),
    complete: currentStep === 'complete'
  };

  sidebarNav.innerHTML = '';

  for (const item of items) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'sidebar-nav-item';
    button.dataset.screen = item.screen;

    const isActive = shell.isManagerMode
      ? shell.screen === item.screen
      : item.step === currentStep;

    const isEnabled = shell.isManagerMode
      ? item.enabled !== false
      : Boolean(item.step && reached[item.step]);

    button.classList.toggle('active', isActive);
    button.disabled = !isEnabled;
    button.innerHTML = `${iconHtml(item.icon, 'sidebar-nav-icon')}<span class="sidebar-nav-label">${item.label}</span>`;

    button.addEventListener('click', () => {
      shell.navigateTo(/** @type {import('./application-shell.js').AppScreen} */ (item.screen));
    });

    sidebarNav.appendChild(button);
  }
}

async function runInstall() {
  logDiagnostic('info', 'install', 'runInstall start', {
    screen: shell.screen,
    installState: installation.state,
    shellMode: shell.applicationState?.mode
  });

  await shell.refreshApplicationState();

  const savedPath =
    shell.applicationState?.settings?.gameFolderPath?.trim() ||
    shell.applicationState?.folder?.path ||
    '';

  if (!savedPath) {
    logDiagnostic('warn', 'install', 'runInstall aborted — no game path');
    return;
  }

  const { result: folder } = await client.invoke('getGameFolderInfo', { folderPath: savedPath });
  const { result: installed } = await client.invoke('isGameInstalled', { gameFolderPath: savedPath });

  logDiagnostic('info', 'install', 'runInstall preflight', {
    path: savedPath,
    folderValid: folder?.isValid,
    hostReportsInstalled: installed?.installed,
    markers: installed?.markers,
    shellMode: shell.applicationState?.mode,
    settingsIsInstalled: shell.applicationState?.settings?.isInstalled
  });

  if (!folder?.isValid) {
    logDiagnostic('warn', 'install', 'runInstall aborted — invalid folder from host');
    return;
  }

  const outcome = await installation.startInstall(folder);
  await shell.handleInstallComplete(outcome);

  logDiagnostic('info', 'install', 'runInstall finished', { outcome });

  if (outcome === 'completed') {
    showActionBanner('success', installation.result?.message || t('action.installSuccess'));
  } else if (outcome === 'failed') {
    showActionBanner('error', installation.errorMessage || t('action.installFailed'), {
      details: installation.errorDetails
    });
  }
}

async function runUninstall() {
  logDiagnostic('info', 'uninstall', 'runUninstall start', {
    screen: shell.screen,
    uninstallState: uninstallation.state,
    shellMode: shell.applicationState?.mode
  });

  await shell.refreshApplicationState();

  const appState = shell.applicationState;
  const gameFolderPath = appState?.folder?.path ?? appState?.settings?.gameFolderPath ?? '';

  const { result: installed } = await client.invoke('isGameInstalled', { gameFolderPath });
  logDiagnostic('info', 'uninstall', 'runUninstall preflight', {
    path: gameFolderPath,
    canUninstall: appState?.canUninstall,
    hostReportsInstalled: installed?.installed,
    markers: installed?.markers,
    settingsIsInstalled: appState?.settings?.isInstalled
  });

  const outcome = await uninstallation.startUninstall(
    gameFolderPath,
    appState?.canUninstall ?? false
  );
  await shell.handleUninstallComplete(outcome);

  logDiagnostic('info', 'uninstall', 'runUninstall finished', { outcome });
  await refreshDiagnosticLogFooter(client);

  if (outcome === 'completed') {
    showActionBanner('success', uninstallation.result?.message || t('action.uninstallSuccess'));
  } else if (outcome === 'failed') {
    showActionBanner('error', uninstallation.errorMessage || t('action.uninstallFailed'), {
      details: uninstallation.errorDetails
    });
  }
}

shell.onEnterScreen = async (screen) => {
  if (screen === 'manager') {
    await manager.loadMods();
    if (!shell.canShowManager) {
      await shell.handleConfigurationLost();
    }
    return;
  }

  if (screen === 'settings') {
    await settings.loadFromHost({ keepStatus: settings.state === 'saved' });
    syncSettingsForm();
    applyThemeFromSettings(settings.draft);
    await loadSettingsAppVersion();
    await refreshDiagnosticLogFooter(client);
    return;
  }

  if (screen === 'setup') {
    await prepareGameDetectionScreen();
    return;
  }

  if (screen === 'install' && installation.state === 'idle') {
    await runInstall();
    return;
  }

  if (screen === 'uninstall' && uninstallation.state === 'idle') {
    uninstallation.requestConfirmation();
  }
};

async function prepareGameDetectionScreen() {
  const existingPath =
    setup.validatedFolder?.path ||
    shell.applicationState?.folder?.path ||
    shell.applicationState?.settings?.gameFolderPath ||
    '';

  if (existingPath) {
    folderPathInput.value = existingPath;
    if (setup.state === 'idle' || setup.state === 'invalid' || setup.state === 'error') {
      await setup.validatePath(existingPath);
    }
    return;
  }

  if (setup.state === 'idle' && !folderPathInput.value.trim()) {
    await setup.autoDetect();
  }
}

function renderShell() {
  applyAppearanceFromShell();
  applyLocalizedText();
  renderHostStatus();
  renderShellPhase();
  renderSidebar();
  renderHome();
  renderSetup();
  renderInstall();
  renderUninstall();
  renderManager(true);
  renderSettings();
}

/** @type {string | null} */
let lastAppliedAppearanceKey = null;

function applyAppearanceFromShell() {
  const settings = shell.applicationState?.settings;
  if (!settings || shell.phase !== 'ready') {
    return;
  }

  const key = `${normalizeAppearanceValue(settings.theme)}|${normalizeAppearanceValue(settings.language)}`;
  if (key === lastAppliedAppearanceKey) {
    return;
  }

  lastAppliedAppearanceKey = key;
  applyLanguageFromSettings(settings);
  applyThemeFromSettings(settings);
}

function normalizeAppearanceValue(value) {
  return String(value ?? '').trim().toLowerCase();
}

function renderHostStatus() {
  const { status, message } = shell.hostStatus;
  const isReady = status === 'ready';

  hostChip.classList.toggle('host-chip-hidden', isReady);
  statusMessage.textContent = isReady ? '' : message ?? t('host.notConnected');
}

function renderShellPhase() {
  const { phase } = shell;

  shellLoading.hidden = phase === 'ready' || phase === 'host-error';
  hostErrorPanel.hidden = phase !== 'host-error';
  appContent.hidden = phase !== 'ready';

  if (phase === 'booting' || phase === 'connecting') {
    shellLoadingTitle.textContent = t('shell.starting');
    shellLoadingMessage.textContent = t('shell.connecting');
    pageSubtitle.textContent = t('app.subtitle.connecting');
  } else if (phase === 'loading-app') {
    shellLoadingTitle.textContent = t('shell.loading');
    shellLoadingMessage.textContent = t('shell.loadingConfig');
    pageSubtitle.textContent = t('app.subtitle.loading');
  } else if (phase === 'host-error') {
    hostErrorMessage.textContent = shell.errorMessage ?? t('shell.hostError');
    pageSubtitle.textContent = t('app.subtitle.disconnected');
  } else if (phase === 'ready') {
    const subtitles = {
      welcome: 'app.subtitle.welcome',
      install: 'app.subtitle.install',
      uninstall: 'app.subtitle.uninstall',
      home: 'app.subtitle.home',
      manager: 'app.subtitle.manager',
      settings: 'app.subtitle.settings',
      setup: 'app.subtitle.setup'
    };
    pageSubtitle.textContent = t(subtitles[shell.screen] ?? 'app.subtitle.setup');
    updateWindowTitle();
  }

  welcomeView.hidden = true;
  homeView.hidden = true;
  setupView.hidden = true;
  managerView.hidden = true;
  settingsView.hidden = true;
  installView.hidden = true;
  uninstallView.hidden = true;

  if (phase !== 'ready') {
    return;
  }

  if (shell.screen === 'welcome') {
    welcomeView.hidden = false;
    return;
  }

  if (shell.screen === 'home') {
    homeView.hidden = false;
    return;
  }

  if (shell.screen === 'install') {
    installView.hidden = false;
    return;
  }

  if (shell.screen === 'uninstall') {
    uninstallView.hidden = false;
    return;
  }

  if (shell.screen === 'manager') {
    managerView.hidden = false;
    return;
  }

  if (shell.screen === 'settings') {
    settingsView.hidden = false;
    return;
  }

  setupView.hidden = false;
}

function renderHome() {
  if (!shell.isApplicationReady || shell.screen !== 'home') {
    return;
  }

  homeCardLaunch.hidden = !shell.canLaunchGame;
  homeCardLaunch.disabled = !shell.canLaunchGame || launchGame.launching;

  homeLaunchError.hidden = !launchGame.errorMessage;
  homeLaunchError.textContent = launchGame.errorMessage ?? '';
}

function renderInstall() {
  if (!shell.isApplicationReady || shell.screen !== 'install') {
    return;
  }

  const state = installation.state;

  installIdle.hidden = state !== 'idle';
  installActive.hidden = !['starting', 'installing', 'cancelling'].includes(state);
  installCompleted.hidden = state !== 'completed';
  installFailed.hidden = state !== 'failed';
  installCancelled.hidden = state !== 'cancelled';

  if (['starting', 'installing', 'cancelling'].includes(state)) {
    const progress = installation.progress;
    const percentage = progress?.percentage ?? 0;
    installStatus.textContent =
      state === 'cancelling' ? t('install.cancelling') : progress?.message || t('install.inProgress');
    installProgress.value = percentage;
    installProgress.indeterminate = state === 'starting' && !progress;
    setProgressRing(installProgressRing, installProgressPct, percentage);
    installDetails.hidden = !progress?.details;
    installDetails.textContent = progress?.details ?? '';
    cancelInstallButton.hidden = !installation.canCancel;
    cancelInstallButton.disabled = state === 'cancelling';
  }

  if (state === 'completed') {
    installSuccessMessage.textContent = installation.result?.message || t('install.success');
  }

  if (state === 'failed') {
    installFailureMessage.textContent = installation.errorMessage ?? t('install.failed');
    const detailText =
      installation.errorDetails ||
      installation.result?.details ||
      installation.result?.errorMessage ||
      '';
    const showDetails = Boolean(detailText && detailText !== installFailureMessage.textContent);
    installErrorDetails.hidden = !showDetails;
    installErrorDetailsBody.textContent = detailText;
  }

  if (state === 'cancelled') {
    installCancelledMessage.textContent = installation.errorMessage ?? t('install.cancelled');
  }
}

function renderUninstall() {
  if (!shell.isApplicationReady || shell.screen !== 'uninstall') {
    return;
  }

  const state = uninstallation.state;

  uninstallConfirm.hidden = state !== 'confirming';
  uninstallActive.hidden = !['starting', 'uninstalling', 'cancelling'].includes(state);
  uninstallCompleted.hidden = state !== 'completed';
  uninstallFailed.hidden = state !== 'failed';
  uninstallCancelled.hidden = state !== 'cancelled';

  if (['starting', 'uninstalling', 'cancelling'].includes(state)) {
    const progress = uninstallation.progress;
    const percentage = progress?.percentage ?? 0;
    uninstallStatus.textContent =
      state === 'cancelling'
        ? t('uninstall.cancelling')
        : progress?.message || t('uninstall.inProgress');
    uninstallProgress.value = percentage;
    uninstallProgress.indeterminate = state === 'starting' && !progress;
    setProgressRing(uninstallProgressRing, uninstallProgressPct, percentage);
    uninstallDetails.hidden = !progress?.details;
    uninstallDetails.textContent = progress?.details ?? '';
    cancelUninstallButton.hidden = !uninstallation.canCancel;
    cancelUninstallButton.disabled = state === 'cancelling';
  }

  if (state === 'completed') {
    uninstallSuccessMessage.textContent = uninstallation.result?.message || t('uninstall.success');
  }

  if (state === 'failed') {
    uninstallFailureMessage.textContent = uninstallation.errorMessage ?? t('uninstall.failed');
    const detailText =
      uninstallation.errorDetails ||
      uninstallation.result?.details ||
      uninstallation.result?.errorMessage ||
      '';
    const showDetails = Boolean(detailText && detailText !== uninstallFailureMessage.textContent);
    uninstallErrorDetails.hidden = !showDetails;
    uninstallErrorDetailsBody.textContent = detailText;
  }

  if (state === 'cancelled') {
    uninstallCancelledMessage.textContent = uninstallation.errorMessage ?? t('uninstall.cancelled');
  }
}

function canInteractWithSetup() {
  return shell.isApplicationReady && !['validating', 'saving'].includes(setup.state);
}

function renderSetup() {
  if (!shell.isApplicationReady || shell.screen !== 'setup') {
    return;
  }

  setupContextError.hidden = !shell.errorMessage || setupView.hidden;
  setupContextError.textContent = shell.errorMessage ?? '';

  autoDetectButton.disabled = !canInteractWithSetup();
  browseButton.disabled = !canInteractWithSetup();
  saveButton.disabled = !canInteractWithSetup() || setup.state !== 'valid';

  setupStatus.hidden = true;
  setupError.hidden = true;
  setupStatus.textContent = '';
  setupError.textContent = '';

  if (setup.validatedFolder?.path) {
    folderPathInput.value = setup.validatedFolder.path;
  }

  switch (setup.state) {
    case 'validating':
      setupStatus.hidden = false;
      setupStatus.className = 'setup-status status-validating';
      setupStatus.textContent = t('setup.validating');
      break;
    case 'valid':
      setupStatus.hidden = false;
      setupStatus.className = 'setup-status status-valid';
      setupStatus.textContent = t('setup.valid', {
        version: setup.validatedFolder?.version || 'unknown',
        source: setup.validatedFolder?.source || 'unknown source'
      });
      break;
    case 'invalid':
      setupError.hidden = false;
      setupError.textContent = setup.errorMessage ?? t('setup.invalid');
      break;
    case 'saving':
      setupStatus.hidden = false;
      setupStatus.className = 'setup-status status-validating';
      setupStatus.textContent = t('setup.saving');
      break;
    case 'error':
      setupError.hidden = false;
      setupError.textContent = setup.errorMessage ?? t('common.error');
      break;
    default:
      break;
  }
}

/** @type {boolean} */
let suppressModToggleChange = false;

function renderManager(forceFull = false) {
  if (!shell.isApplicationReady || shell.screen !== 'manager') {
    return;
  }

  const hint = forceFull ? { scope: 'full', fileName: null } : manager.lastNotify;

  renderManagerChrome();

  if (manager.loadState !== 'loaded') {
    modList.innerHTML = '';
    return;
  }

  const needsFullList =
    hint.scope === 'full' || modList.children.length !== manager.mods.length;

  if (needsFullList) {
    renderModList();
    return;
  }

  if (hint.fileName) {
    updateModRow(hint.fileName);
  }
}

function renderManagerChrome() {
  refreshModsButton.disabled =
    !shell.isHostReady || manager.togglingFileName !== null || manager.loadState === 'loading';

  managerLoading.hidden = manager.loadState !== 'loading';
  managerEmpty.hidden = manager.loadState !== 'empty';
  managerError.hidden = manager.loadState !== 'error';
  modList.hidden = manager.loadState !== 'loaded';
  managerStats.hidden = manager.loadState !== 'loaded' && manager.loadState !== 'empty';

  managerError.textContent = manager.errorMessage ?? '';
  managerToggleError.hidden = !manager.toggleError;
  managerToggleError.textContent = manager.toggleError ?? '';

  activeCountEl.textContent = String(manager.activeCount);
  totalCountEl.textContent = String(manager.totalCount);
}

/**
 * @param {{ fileName: string, name: string, description: string, isEnabled: boolean }} mod
 * @returns {HTMLLIElement}
 */
function createModItem(mod) {
  const item = document.createElement('li');
  item.className = 'mod-item';
  item.dataset.fileName = mod.fileName;

  const info = document.createElement('div');
  info.className = 'mod-info';

  const title = document.createElement('div');
  title.className = 'mod-name';
  title.textContent = mod.name;

  const description = document.createElement('div');
  description.className = 'mod-description';
  description.textContent = mod.description || mod.fileName;

  info.append(title, description);

  const toggleLabel = document.createElement('label');
  toggleLabel.className = 'toggle-switch';

  const checkbox = document.createElement('input');
  checkbox.type = 'checkbox';
  checkbox.checked = mod.isEnabled;
  checkbox.setAttribute('role', 'switch');
  checkbox.setAttribute('aria-label', modStatusLabel(mod));

  const track = document.createElement('span');
  track.className = 'toggle-switch-track';
  track.setAttribute('aria-hidden', 'true');

  const thumb = document.createElement('span');
  thumb.className = 'toggle-switch-thumb';
  track.appendChild(thumb);

  toggleLabel.append(checkbox, track);
  item.append(info, toggleLabel);

  return item;
}

/**
 * @param {{ fileName: string, name: string, description: string, isEnabled: boolean }} mod
 */
function modStatusLabel(mod) {
  const isUpdating = manager.togglingFileName === mod.fileName;
  if (isUpdating) {
    return t('manager.updating');
  }

  return mod.isEnabled ? t('manager.enabled') : t('manager.disabled');
}

function renderModList() {
  modList.innerHTML = '';

  for (const mod of manager.mods) {
    const item = createModItem(mod);
    updateModRow(mod.fileName, item);
    modList.appendChild(item);
  }
}

/**
 * @param {string} fileName
 * @param {HTMLLIElement} [item]
 */
function updateModRow(fileName, item = null) {
  const mod = manager.mods.find((entry) => entry.fileName === fileName);
  if (!mod) {
    return;
  }

  const row =
    item ?? modList.querySelector(`.mod-item[data-file-name="${CSS.escape(fileName)}"]`);
  if (!(row instanceof HTMLLIElement)) {
    renderModList();
    return;
  }

  const checkbox = row.querySelector('input[type="checkbox"]');
  const toggleLabel = row.querySelector('.toggle-switch');
  if (!(checkbox instanceof HTMLInputElement) || !(toggleLabel instanceof HTMLLabelElement)) {
    return;
  }

  const isUpdating = manager.togglingFileName === fileName;

  if (!isUpdating && checkbox.checked !== mod.isEnabled) {
    suppressModToggleChange = true;
    checkbox.checked = mod.isEnabled;
    suppressModToggleChange = false;
  }

  checkbox.disabled = isUpdating;
  checkbox.setAttribute('aria-label', modStatusLabel(mod));
  toggleLabel.classList.toggle('toggle-switch-loading', isUpdating);
}

function syncSettingsForm() {
  const draft = settings.draft;
  settingsGamePath.value = draft.gameFolderPath ?? '';
  settingsBackup.checked = Boolean(draft.backupEnabled);
  settingsLanguage.value = draft.language ?? 'en-US';
  settingsTheme.value = draft.theme ?? 'Light';
  settingsUninstallCard.hidden = !shell.canUninstall;
  applyThemeFromSettings(draft);
}

function renderSettings() {
  if (!shell.isApplicationReady || shell.screen !== 'settings') {
    return;
  }

  settingsResetButton.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsBrowse.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsBackup.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsLanguage.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsTheme.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsUninstallCard.hidden = !shell.canUninstall;

  settingsError.hidden = settings.state !== 'error';
  settingsError.textContent = settings.errorMessage ?? '';

  settingsPathStatus.hidden = settings.pathState === 'idle';
  if (settings.pathState === 'validating') {
    settingsPathStatus.textContent = t('settings.pathValidating');
    settingsPathStatus.className = 'field-hint';
  } else if (settings.pathState === 'valid') {
    settingsPathStatus.textContent = t('settings.pathValid');
    settingsPathStatus.className = 'field-hint valid';
  } else if (settings.pathState === 'invalid') {
    settingsPathStatus.textContent = t('settings.pathInvalid');
    settingsPathStatus.className = 'field-hint invalid';
  }

  if (settingsHostStatus) {
    const { status, message } = shell.hostStatus;
    settingsHostStatus.textContent =
      status === 'ready' ? message ?? t('host.connected') : message ?? t('host.notConnected');
  }
}

async function loadSettingsAppVersion() {
  if (!settingsAppVersion) {
    return;
  }

  try {
    const version = await client.getAppVersion();
    settingsAppVersion.textContent = version ? t('settings.version', { version }) : '';
  } catch {
    settingsAppVersion.textContent = '';
  }
}

/**
 * @param {{ showToast?: boolean, toastKind?: 'success' | 'info', successMessage?: string }} [options]
 * @returns {Promise<boolean>}
 */
async function persistSettings(options = {}) {
  const { showToast = true, toastKind = 'success', successMessage } = options;

  if (settings.state === 'loading' || settings.state === 'saving') {
    return false;
  }

  const saved = await settings.save();
  if (!saved) {
    if (settings.errorMessage) {
      showActionBanner('error', settings.errorMessage);
    }
    renderSettings();
    return false;
  }

  applyLanguageFromSettings(settings.draft);
  applyThemeFromSettings(settings.draft);
  lastAppliedAppearanceKey = null;
  await shell.refreshApplicationState();
  await refreshDiagnosticLogFooter(client);
  renderSettings();

  if (showToast) {
    showActionBanner(toastKind, successMessage ?? t('settings.saved'));
  }

  return true;
}

welcomeBeginButton.addEventListener('click', () => shell.navigateTo('setup'));

homeCardManager.addEventListener('click', () => shell.navigateTo('manager'));
homeCardSettings.addEventListener('click', () => shell.navigateTo('settings'));
homeCardLaunch.addEventListener('click', async () => {
  launchGame.clearMessages();
  await launchGame.launchGame(shell.canLaunchGame);
  if (launchGame.successMessage) {
    showActionBanner('success', launchGame.successMessage);
  } else if (launchGame.errorMessage) {
    showActionBanner('error', launchGame.errorMessage);
  }
});

beginInstallButton.addEventListener('click', () => runInstall());
cancelInstallButton.addEventListener('click', () => installation.cancelInstall());
installContinueButton.addEventListener('click', async () => {
  const shouldAutoLaunch = Boolean(installAutoLaunch?.checked);
  installation.reset();
  await shell.finishInstallation();
  await shell.refreshApplicationState();

  if (shouldAutoLaunch) {
    launchGame.clearMessages();
    await launchGame.launchGame(shell.canLaunchGame);
    if (launchGame.errorMessage) {
      showActionBanner('error', launchGame.errorMessage);
    }
  }

  const gameFolderPath = shell.applicationState?.settings?.gameFolderPath;
  if (gameFolderPath) {
    const transition = await client.transitionToInstalledManager(gameFolderPath);
    if (transition?.transitioned) {
      return;
    }
    if (transition?.reason === 'error' && transition.message) {
      showActionBanner('error', transition.message);
    }
  }

  if (shouldAutoLaunch) {
    await client.minimizeWindow();
  }
});
retryInstallButton.addEventListener('click', async () => {
  installation.reset();
  await runInstall();
});
retryInstallCancelledButton.addEventListener('click', async () => {
  installation.reset();
  await runInstall();
});
installBackSetupButton.addEventListener('click', () => {
  installation.reset();
  shell.navigateTo('setup');
});
installBackSetupCancelledButton.addEventListener('click', () => {
  installation.reset();
  shell.navigateTo('setup');
});

settingsUninstallButton.addEventListener('click', () => shell.navigateTo('uninstall'));
confirmUninstallButton.addEventListener('click', () => runUninstall());
cancelUninstallConfirmButton.addEventListener('click', () => {
  uninstallation.cancelConfirmation();
  shell.navigateTo('settings');
});
cancelUninstallButton.addEventListener('click', () => uninstallation.cancelUninstall());
uninstallContinueButton.addEventListener('click', async () => {
  const gameFolderPath = shell.applicationState?.settings?.gameFolderPath;
  uninstallation.reset();

  if (gameFolderPath) {
    const cleanup = await client.scheduleSelfUninstallCleanup(gameFolderPath);
    if (cleanup?.scheduled) {
      return;
    }
  }

  await shell.refreshApplicationState();
  await shell.navigateTo(shell.applicationState?.recommendedScreen ?? 'welcome');
});
retryUninstallButton.addEventListener('click', async () => {
  uninstallation.requestConfirmation();
});
retryUninstallCancelledButton.addEventListener('click', async () => {
  uninstallation.requestConfirmation();
});
uninstallBackManagerButton.addEventListener('click', () => {
  uninstallation.reset();
  shell.navigateTo('manager');
});
uninstallBackManagerCancelledButton.addEventListener('click', () => {
  uninstallation.reset();
  shell.navigateTo('manager');
});

autoDetectButton.addEventListener('click', () => setup.autoDetect());

browseButton.addEventListener('click', async () => {
  const selected = await client.pickFolder();
  if (!selected) {
    return;
  }

  folderPathInput.value = selected;
  await setup.validatePath(selected);
});

saveButton.addEventListener('click', async () => {
  const saved = await setup.saveConfiguration();
  if (saved) {
    await shell.onConfigurationSaved();
    await refreshDiagnosticLogFooter(client);
  }
});

refreshModsButton.addEventListener('click', async () => {
  await manager.loadMods();
  if (!shell.canShowManager) {
    await shell.handleConfigurationLost();
  }
});

modList.addEventListener('change', async (event) => {
  if (suppressModToggleChange) {
    return;
  }

  const checkbox = event.target;
  if (!(checkbox instanceof HTMLInputElement) || checkbox.type !== 'checkbox') {
    return;
  }

  const row = checkbox.closest('.mod-item');
  if (!(row instanceof HTMLLIElement) || !row.dataset.fileName) {
    return;
  }

  const fileName = row.dataset.fileName;
  const mod = manager.mods.find((entry) => entry.fileName === fileName);
  if (!mod) {
    return;
  }

  manager.clearToggleError();

  const enabled = checkbox.checked;
  const patchName = mod.name || mod.fileName;

  await manager.toggleMod(fileName, enabled);

  if (manager.toggleError) {
    showActionBanner('error', manager.toggleError || t('action.modFailed'));
    return;
  }

  showActionBanner(
    'success',
    enabled
      ? t('manager.patchActive', { name: patchName })
      : t('manager.patchInactive', { name: patchName })
  );
});

settingsBrowse.addEventListener('click', async () => {
  await settings.browsePath();
  if (settings.pathState === 'valid') {
    await persistSettings({ showToast: false });
  }
});
settingsBackup.addEventListener('change', async () => {
  settings.updateDraft({ backupEnabled: settingsBackup.checked });
  await persistSettings({ showToast: false });
});
settingsLanguage.addEventListener('change', async () => {
  settings.updateDraft({ language: settingsLanguage.value });
  await persistSettings({ showToast: false });
});
settingsTheme.addEventListener('change', async () => {
  settings.updateDraft({ theme: settingsTheme.value });
  applyTheme(settingsTheme.value === 'Dark' ? 'Dark' : 'Light');
  await persistSettings({ showToast: false });
});
settingsResetButton.addEventListener('click', async () => {
  settings.resetDraft();
  syncSettingsForm();
  applyThemeFromSettings(settings.draft);
  await persistSettings({ toastKind: 'info', successMessage: t('settings.resetDone') });
});

/**
 * @param {{ status?: string, version?: string, downloadUrl?: string, releaseUrl?: string, canApplyInPlace?: boolean, message?: string, silent?: boolean }} info
 */
function applyUpdaterStatus(info) {
  latestUpdateInfo = info ?? null;
  if (!settingsUpdateStatus) {
    return;
  }

  const status = info?.status;
  if (!status || status === 'checking') {
    settingsUpdateStatus.hidden = status !== 'checking';
    settingsUpdateStatus.textContent = status === 'checking' ? t('settings.updateChecking') : '';
  } else if (status === 'up-to-date') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = t('settings.updateUpToDate');
  } else if (status === 'available') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = t('settings.updateAvailable', {
      version: info.version ?? ''
    });
  } else if (status === 'available-manual') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = t('settings.updateManual', {
      version: info.version ?? ''
    });
  } else if (status === 'downloading' || status === 'extracting') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = t('settings.updateDownloading');
  } else if (status === 'installing' || status === 'ready-to-install') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = t('settings.updateInstalling');
  } else if (status === 'unavailable') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = info.message || t('settings.updateUnavailable');
  } else if (status === 'error') {
    settingsUpdateStatus.hidden = false;
    settingsUpdateStatus.textContent = t('settings.updateError', {
      message: info.message || 'unknown'
    });
  } else {
    settingsUpdateStatus.hidden = true;
    settingsUpdateStatus.textContent = '';
  }

  if (settingsApplyUpdateButton) {
    settingsApplyUpdateButton.hidden = !(
      status === 'available' &&
      info?.canApplyInPlace &&
      info?.downloadUrl
    );
  }
  if (settingsOpenReleaseButton) {
    settingsOpenReleaseButton.hidden = !(
      (status === 'available' || status === 'available-manual') &&
      info?.releaseUrl
    );
  }

  if (!info?.silent && status === 'up-to-date') {
    showActionBanner('info', t('settings.updateUpToDate'));
  } else if (!info?.silent && status === 'available') {
    showActionBanner(
      'info',
      t('settings.updateAvailable', { version: info.version ?? '' })
    );
  } else if (!info?.silent && status === 'error' && info.message) {
    showActionBanner('error', t('settings.updateError', { message: info.message }));
  }
}

if (settingsCheckUpdatesButton) {
  settingsCheckUpdatesButton.addEventListener('click', async () => {
    settingsCheckUpdatesButton.disabled = true;
    try {
      const gameFolderPath = settings.draft?.gameFolderPath || shell.applicationState?.settings?.gameFolderPath;
      const result = await client.checkForUpdates({
        silent: false,
        gameFolderPath
      });
      applyUpdaterStatus(result);
    } finally {
      settingsCheckUpdatesButton.disabled = false;
    }
  });
}

if (settingsApplyUpdateButton) {
  settingsApplyUpdateButton.addEventListener('click', async () => {
    if (!latestUpdateInfo?.downloadUrl) {
      return;
    }
    const gameFolderPath =
      settings.draft?.gameFolderPath || shell.applicationState?.settings?.gameFolderPath;
    settingsApplyUpdateButton.disabled = true;
    try {
      const result = await client.downloadAndApplyUpdate({
        downloadUrl: latestUpdateInfo.downloadUrl,
        gameFolderPath
      });
      applyUpdaterStatus(result);
    } finally {
      settingsApplyUpdateButton.disabled = false;
    }
  });
}

if (settingsOpenReleaseButton) {
  settingsOpenReleaseButton.addEventListener('click', async () => {
    await client.openReleasePage(latestUpdateInfo?.releaseUrl);
  });
}

client.onUpdaterStatus((payload) => {
  applyUpdaterStatus(payload);
});

sidebarToggle.addEventListener('click', () => {
  shell.toggleSidebar();
});

retryHostButton.addEventListener('click', async () => {
  retryHostButton.disabled = true;
  try {
    await shell.retryHost();
  } finally {
    retryHostButton.disabled = false;
  }
});

populateLanguageSelect();
applyDataIcons();
applyLocalizedText();

await shell.start();
applyAppearanceFromShell();
applyLocalizedText();
await loadSettingsAppVersion();
