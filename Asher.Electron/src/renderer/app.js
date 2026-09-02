import { ApplicationClient } from './application-client.js';
import { ApplicationShell } from './application-shell.js';
import { logDiagnostic, showDiagnosticLogPath } from './diagnostic-log.js';
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
const statusBadge = document.getElementById('status-badge');
const statusMessage = document.getElementById('status-message');

const shellLoading = document.getElementById('shell-loading');
const shellLoadingTitle = document.getElementById('shell-loading-title');
const shellLoadingMessage = document.getElementById('shell-loading-message');
const hostErrorPanel = document.getElementById('host-error-panel');
const hostErrorMessage = document.getElementById('host-error-message');
const retryHostButton = document.getElementById('retry-host');
const appContent = document.getElementById('app-content');

const homeView = document.getElementById('home-view');
const homeLaunchSuccess = document.getElementById('home-launch-success');
const homeLaunchError = document.getElementById('home-launch-error');
const homeCardManager = document.getElementById('home-card-manager');
const homeCardSettings = document.getElementById('home-card-settings');
const homeCardLaunch = document.getElementById('home-card-launch');

const readyView = document.getElementById('ready-view');
const readySummary = document.getElementById('ready-summary');
const readyPath = document.getElementById('ready-path');
const readyVersion = document.getElementById('ready-version');
const readySource = document.getElementById('ready-source');
const readyMode = document.getElementById('ready-mode');
const openManagerButton = document.getElementById('open-manager');
const startInstallButton = document.getElementById('start-install');
const reconfigureButton = document.getElementById('reconfigure');

const installView = document.getElementById('install-view');
const installIdle = document.getElementById('install-idle');
const beginInstallButton = document.getElementById('begin-install');
const installActive = document.getElementById('install-active');
const installStatus = document.getElementById('install-status');
const installProgress = document.getElementById('install-progress');
const installDetails = document.getElementById('install-details');
const cancelInstallButton = document.getElementById('cancel-install');
const installCompleted = document.getElementById('install-completed');
const installSuccessMessage = document.getElementById('install-success-message');
const installContinueButton = document.getElementById('install-continue');
const installFailed = document.getElementById('install-failed');
const installFailureMessage = document.getElementById('install-failure-message');
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
const uninstallDetails = document.getElementById('uninstall-details');
const cancelUninstallButton = document.getElementById('cancel-uninstall');
const uninstallCompleted = document.getElementById('uninstall-completed');
const uninstallSuccessMessage = document.getElementById('uninstall-success-message');
const uninstallContinueButton = document.getElementById('uninstall-continue');
const uninstallFailed = document.getElementById('uninstall-failed');
const uninstallFailureMessage = document.getElementById('uninstall-failure-message');
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
const validateButton = document.getElementById('validate-path');
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
const settingsStatus = document.getElementById('settings-status');
const settingsError = document.getElementById('settings-error');
const settingsGamePath = document.getElementById('settings-game-path');
const settingsBrowse = document.getElementById('settings-browse');
const settingsPathStatus = document.getElementById('settings-path-status');
const settingsAutoLaunch = document.getElementById('settings-auto-launch');
const settingsBackup = document.getElementById('settings-backup');
const settingsLanguage = document.getElementById('settings-language');
const settingsTheme = document.getElementById('settings-theme');
const settingsCheckUpdates = document.getElementById('settings-check-updates');
const settingsUninstallCard = document.getElementById('settings-uninstall-card');
const settingsUninstallButton = document.getElementById('settings-uninstall');
const settingsResetButton = document.getElementById('settings-reset');
const settingsSaveButton = document.getElementById('settings-save');

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

  document.title = t('app.title');
}

function populateLanguageSelect() {
  settingsLanguage.innerHTML = '';
  for (const option of getLanguageOptions()) {
    const el = document.createElement('option');
    el.value = option.value;
    el.textContent = option.label;
    settingsLanguage.appendChild(el);
  }
}

function getInstallWizardStep() {
  if (shell.screen === 'install') {
    if (installation.state === 'completed') {
      return 'complete';
    }
    return 'installing';
  }

  if (shell.screen === 'setup') {
    const appState = shell.applicationState;
    if (appState?.isConfigured && !shell.isEditingSetup) {
      return 'welcome';
    }
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
        { id: 'home', label: t('nav.home'), icon: '&#8962;', screen: 'home', enabled: shell.canShowHome },
        {
          id: 'manager',
          label: t('nav.patchManager'),
          icon: '&#9881;',
          screen: 'manager',
          enabled: shell.canShowManager
        },
        { id: 'settings', label: t('nav.settings'), icon: '&#9881;', screen: 'settings', enabled: true }
      ]
    : [
        { id: 'welcome', label: t('nav.welcome'), icon: '&#9733;', screen: 'setup', step: 'welcome' },
        {
          id: 'gameDetection',
          label: t('nav.gameDetection'),
          icon: '&#128269;',
          screen: 'setup',
          step: 'gameDetection'
        },
        {
          id: 'installing',
          label: t('nav.installing'),
          icon: '&#8635;',
          screen: 'install',
          step: 'installing',
          enabled: shell.canShowManager
        },
        {
          id: 'complete',
          label: t('nav.complete'),
          icon: '&#10003;',
          screen: 'install',
          step: 'complete',
          enabled: installation.state === 'completed'
        }
      ];

  const currentStep = getInstallWizardStep();
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
      : item.enabled !== false &&
        (item.step === 'welcome' ||
          item.step === currentStep ||
          (item.step === 'gameDetection' && ['gameDetection', 'installing', 'complete'].includes(currentStep)) ||
          (item.step === 'installing' && ['installing', 'complete'].includes(currentStep)));

    button.classList.toggle('active', isActive);
    button.disabled = !isEnabled;
    button.innerHTML = `<span class="sidebar-nav-icon" aria-hidden="true">${item.icon}</span><span class="sidebar-nav-label">${item.label}</span>`;

    button.addEventListener('click', () => {
      if (item.screen === 'setup' && shell.applicationState?.isConfigured && !shell.isEditingSetup) {
        return;
      }
      shell.navigateTo(/** @type {import('./application-shell.js').AppScreen} */ (item.screen));
    });

    sidebarNav.appendChild(button);
  }
}

async function runInstall() {
  const folder = shell.applicationState?.folder;
  if (!folder) {
    return;
  }

  const outcome = await installation.startInstall(folder);
  await shell.handleInstallComplete(outcome);
}

async function runUninstall() {
  const appState = shell.applicationState;
  const gameFolderPath = appState?.folder?.path ?? appState?.settings?.gameFolderPath ?? '';
  const outcome = await uninstallation.startUninstall(gameFolderPath, appState?.canUninstall ?? false);
  await shell.handleUninstallComplete(outcome);
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
    await settings.loadFromHost();
    syncSettingsForm();
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

function renderShell() {
  applyLocalizedText();
  renderHostStatus();
  renderShellPhase();
  renderSidebar();
  renderHome();
  renderSetup();
  renderInstall();
  renderUninstall();
  renderManager();
  renderSettings();
}

function renderHostStatus() {
  const { status, message } = shell.hostStatus;
  statusBadge.textContent = status;
  statusBadge.className = `badge badge-${status}`;
  statusMessage.textContent = message ?? t('host.notConnected');
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
      install: 'app.subtitle.install',
      uninstall: 'app.subtitle.uninstall',
      home: 'app.subtitle.home',
      manager: 'app.subtitle.manager',
      settings: 'app.subtitle.settings',
      setup: 'app.subtitle.setup'
    };
    pageSubtitle.textContent = t(subtitles[shell.screen] ?? 'app.subtitle.setup');
  }

  homeView.hidden = true;
  readyView.hidden = true;
  setupView.hidden = true;
  managerView.hidden = true;
  settingsView.hidden = true;
  installView.hidden = true;
  uninstallView.hidden = true;

  if (phase !== 'ready') {
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

  const appState = shell.applicationState;
  if (appState?.isConfigured && appState.folder && !shell.isEditingSetup) {
    readyView.hidden = false;
    readyPath.textContent = appState.folder.path;
    readyVersion.textContent = appState.folder.version || '—';
    readySource.textContent = appState.folder.source || '—';
    readyMode.textContent =
      appState.mode === 'manager' ? t('ready.modeManager') : t('ready.modeWizard');
    readySummary.textContent =
      appState.mode === 'manager' ? t('ready.configured') : t('ready.needsInstall');
    startInstallButton.hidden = !appState.needsInstallation;
    openManagerButton.hidden = appState.needsInstallation;
  } else {
    setupView.hidden = false;
  }
}

function renderHome() {
  if (!shell.isApplicationReady || shell.screen !== 'home') {
    return;
  }

  homeCardLaunch.hidden = !shell.canLaunchGame;
  homeCardLaunch.disabled = !shell.canLaunchGame || launchGame.launching;

  homeLaunchSuccess.hidden = !launchGame.successMessage;
  homeLaunchSuccess.textContent = launchGame.successMessage ?? '';
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
    installStatus.textContent =
      state === 'cancelling' ? t('install.cancelling') : progress?.message || t('install.inProgress');
    installProgress.value = progress?.percentage ?? 0;
    installProgress.indeterminate = state === 'starting' && !progress;
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
    uninstallStatus.textContent =
      state === 'cancelling'
        ? t('uninstall.cancelling')
        : progress?.message || t('uninstall.inProgress');
    uninstallProgress.value = progress?.percentage ?? 0;
    uninstallProgress.indeterminate = state === 'starting' && !progress;
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

  if (shell.applicationState?.isConfigured && !shell.isEditingSetup) {
    return;
  }

  setupContextError.hidden = !shell.errorMessage || setupView.hidden;
  setupContextError.textContent = shell.errorMessage ?? '';

  autoDetectButton.disabled = !canInteractWithSetup();
  browseButton.disabled = !canInteractWithSetup();
  validateButton.disabled = !canInteractWithSetup() || !folderPathInput.value.trim();
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

function renderManager() {
  if (!shell.isApplicationReady || shell.screen !== 'manager') {
    return;
  }

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

  modList.innerHTML = '';

  if (manager.loadState !== 'loaded') {
    return;
  }

  for (const mod of manager.mods) {
    const item = document.createElement('li');
    item.className = 'mod-item';

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
    toggleLabel.className = 'mod-toggle';

    const checkbox = document.createElement('input');
    checkbox.type = 'checkbox';
    checkbox.checked = mod.isEnabled;
    checkbox.disabled = manager.togglingFileName !== null;
    checkbox.addEventListener('change', async () => {
      manager.clearToggleError();
      await manager.toggleMod(mod.fileName, checkbox.checked);
    });

    const toggleText = document.createElement('span');
    toggleText.textContent =
      manager.togglingFileName === mod.fileName
        ? t('manager.updating')
        : mod.isEnabled
          ? t('manager.enabled')
          : t('manager.disabled');

    toggleLabel.append(checkbox, toggleText);
    item.append(info, toggleLabel);
    modList.appendChild(item);
  }
}

function syncSettingsForm() {
  const draft = settings.draft;
  settingsGamePath.value = draft.gameFolderPath ?? '';
  settingsAutoLaunch.checked = Boolean(draft.autoLaunchEnabled);
  settingsBackup.checked = Boolean(draft.backupEnabled);
  settingsLanguage.value = draft.language ?? 'en-US';
  settingsTheme.value = draft.theme ?? 'Light';
  settingsCheckUpdates.checked = Boolean(draft.checkForUpdatesEnabled);
  settingsUninstallCard.hidden = !shell.canUninstall;
}

function renderSettings() {
  if (!shell.isApplicationReady || shell.screen !== 'settings') {
    return;
  }

  settingsSaveButton.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsResetButton.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsBrowse.disabled = settings.state === 'saving' || settings.state === 'loading';
  settingsUninstallCard.hidden = !shell.canUninstall;

  settingsStatus.hidden = settings.state !== 'saved';
  settingsStatus.textContent = settings.statusMessage ?? t('settings.saved');
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

  if (settings.state === 'saving') {
    settingsStatus.hidden = false;
    settingsStatus.className = 'setup-status status-validating';
    settingsStatus.textContent = t('settings.saving');
  }
}

async function saveSettingsAndApply() {
  const saved = await settings.save();
  if (!saved) {
    return;
  }

  applyLanguageFromSettings(settings.draft);
  applyThemeFromSettings(settings.draft);
  await shell.refreshApplicationState();
  settings.statusMessage = t('settings.saved');
  renderSettings();
}

homeCardManager.addEventListener('click', () => shell.navigateTo('manager'));
homeCardSettings.addEventListener('click', () => shell.navigateTo('settings'));
homeCardLaunch.addEventListener('click', async () => {
  launchGame.clearMessages();
  await launchGame.launchGame(shell.canLaunchGame);
});

openManagerButton.addEventListener('click', () => shell.navigateTo('manager'));
startInstallButton.addEventListener('click', () => shell.navigateTo('install'));
beginInstallButton.addEventListener('click', () => runInstall());
cancelInstallButton.addEventListener('click', () => installation.cancelInstall());
installContinueButton.addEventListener('click', () => shell.navigateTo(shell.canShowHome ? 'home' : 'manager'));
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
uninstallContinueButton.addEventListener('click', () => {
  uninstallation.reset();
  shell.navigateTo(shell.applicationState?.recommendedScreen ?? 'setup');
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

validateButton.addEventListener('click', () => setup.validatePath(folderPathInput.value));

saveButton.addEventListener('click', async () => {
  const saved = await setup.saveConfiguration();
  if (saved) {
    await shell.onConfigurationSaved();
  }
});

reconfigureButton.addEventListener('click', () => {
  setup.reset();
  folderPathInput.value = '';
  shell.beginSetupEditing();
});

refreshModsButton.addEventListener('click', async () => {
  await manager.loadMods();
  if (!shell.canShowManager) {
    await shell.handleConfigurationLost();
  }
});

settingsBrowse.addEventListener('click', () => settings.browsePath());
settingsAutoLaunch.addEventListener('change', () => {
  settings.updateDraft({ autoLaunchEnabled: settingsAutoLaunch.checked });
});
settingsBackup.addEventListener('change', () => {
  settings.updateDraft({ backupEnabled: settingsBackup.checked });
});
settingsLanguage.addEventListener('change', () => {
  settings.updateDraft({ language: settingsLanguage.value });
});
settingsTheme.addEventListener('change', () => {
  settings.updateDraft({ theme: settingsTheme.value });
  applyTheme(settingsTheme.value === 'Dark' ? 'Dark' : 'Light');
});
settingsCheckUpdates.addEventListener('change', () => {
  settings.updateDraft({ checkForUpdatesEnabled: settingsCheckUpdates.checked });
});
settingsResetButton.addEventListener('click', () => {
  settings.resetDraft();
  syncSettingsForm();
  applyTheme('Light');
});
settingsSaveButton.addEventListener('click', () => saveSettingsAndApply());

sidebarToggle.addEventListener('click', () => shell.toggleSidebar());

retryHostButton.addEventListener('click', async () => {
  retryHostButton.disabled = true;
  try {
    await shell.retryHost();
  } finally {
    retryHostButton.disabled = false;
  }
});

populateLanguageSelect();
applyLocalizedText();

await showDiagnosticLogPath(client);
await shell.start();

if (shell.applicationState?.settings) {
  applyLanguageFromSettings(shell.applicationState.settings);
  applyThemeFromSettings(shell.applicationState.settings);
  applyLocalizedText();
}
