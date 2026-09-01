import { ApplicationClient } from './application-client.js';
import { ApplicationShell } from './application-shell.js';
import { logDiagnostic, showDiagnosticLogPath } from './diagnostic-log.js';
import { GameSetupController } from './game-setup.js';
import { InstallationController } from './installation-controller.js';
import { LaunchGameController } from './launch-game.js';
import { ModManagerController } from './mod-manager.js';
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

const pageSubtitle = document.getElementById('page-subtitle');
const mainNav = document.getElementById('main-nav');
const navSetupButton = document.getElementById('nav-setup');
const navManagerButton = document.getElementById('nav-manager');

const statusBadge = document.getElementById('status-badge');
const statusMessage = document.getElementById('status-message');

const shellLoading = document.getElementById('shell-loading');
const shellLoadingTitle = document.getElementById('shell-loading-title');
const shellLoadingMessage = document.getElementById('shell-loading-message');
const hostErrorPanel = document.getElementById('host-error-panel');
const hostErrorMessage = document.getElementById('host-error-message');
const retryHostButton = document.getElementById('retry-host');
const appContent = document.getElementById('app-content');

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
const launchGameButton = document.getElementById('launch-game');
const managerLaunchSuccess = document.getElementById('manager-launch-success');
const managerLaunchError = document.getElementById('manager-launch-error');
const managerLoading = document.getElementById('manager-loading');
const managerEmpty = document.getElementById('manager-empty');
const managerError = document.getElementById('manager-error');
const managerToggleError = document.getElementById('manager-toggle-error');
const modList = document.getElementById('mod-list');
const managerStats = document.getElementById('manager-stats');
const activeCountEl = document.getElementById('active-count');
const totalCountEl = document.getElementById('total-count');
const uninstallAsherButton = document.getElementById('uninstall-asher');

shell.onChange = renderShell;
setup.onChange = renderSetup;
installation.onChange = renderInstall;
uninstallation.onChange = renderUninstall;
manager.onChange = renderManager;
launchGame.onChange = renderManager;

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

  if (screen === 'install' && installation.state === 'idle') {
    await runInstall();
    return;
  }

  if (screen === 'uninstall' && uninstallation.state === 'idle') {
    uninstallation.requestConfirmation();
  }
};

function renderShell() {
  renderHostStatus();
  renderShellPhase();
  renderNavigation();
  renderSetup();
  renderInstall();
  renderUninstall();
  renderManager();
}

function renderHostStatus() {
  const { status, message } = shell.hostStatus;
  statusBadge.textContent = status;
  statusBadge.className = `badge badge-${status}`;
  statusMessage.textContent = message ?? status;
}

function renderShellPhase() {
  const { phase } = shell;

  shellLoading.hidden = phase === 'ready' || phase === 'host-error';
  hostErrorPanel.hidden = phase !== 'host-error';
  appContent.hidden = phase !== 'ready';

  if (phase === 'booting' || phase === 'connecting') {
    shellLoadingTitle.textContent = 'Starting application';
    shellLoadingMessage.textContent = 'Connecting to Asher.Host...';
    pageSubtitle.textContent = 'Connecting...';
  } else if (phase === 'loading-app') {
    shellLoadingTitle.textContent = 'Loading application';
    shellLoadingMessage.textContent = 'Reading configuration from the application...';
    pageSubtitle.textContent = 'Loading...';
  } else if (phase === 'host-error') {
    hostErrorMessage.textContent =
      shell.errorMessage ?? 'The Asher application host is not available.';
    pageSubtitle.textContent = 'Disconnected';
  } else if (phase === 'ready') {
    if (shell.screen === 'install') {
      pageSubtitle.textContent = 'Installing Asher';
    } else if (shell.screen === 'uninstall') {
      pageSubtitle.textContent = 'Uninstalling Asher';
    } else {
      pageSubtitle.textContent = shell.screen === 'manager' ? 'Mod Manager' : 'Game setup';
    }
  }

  readyView.hidden = true;
  setupView.hidden = true;
  managerView.hidden = true;
  installView.hidden = true;
  uninstallView.hidden = true;

  if (phase !== 'ready') {
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

  const appState = shell.applicationState;
  if (appState?.isConfigured && appState.folder && !shell.isEditingSetup) {
    readyView.hidden = false;
    readyPath.textContent = appState.folder.path;
    readyVersion.textContent = appState.folder.version || '—';
    readySource.textContent = appState.folder.source || '—';
    readyMode.textContent = appState.mode === 'manager' ? 'Manager' : 'Install wizard';
    readySummary.textContent =
      appState.mode === 'manager'
        ? 'A valid Asher installation was found for this game folder.'
        : 'The game folder is configured. Install Asher to enable mod support.';
    startInstallButton.hidden = !appState.needsInstallation;
    openManagerButton.hidden = appState.needsInstallation;
  } else {
    setupView.hidden = false;
  }
}

function renderNavigation() {
  const showNav = shell.isApplicationReady;
  mainNav.hidden = !showNav;
  navManagerButton.disabled = !shell.canShowManager || shell.needsInstallation;

  navSetupButton.classList.toggle('nav-active', shell.screen === 'setup');
  navManagerButton.classList.toggle('nav-active', shell.screen === 'manager');
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
      state === 'cancelling'
        ? 'Cancelling installation...'
        : progress?.message || 'Installing...';
    installProgress.value = progress?.percentage ?? 0;
    installProgress.indeterminate = state === 'starting' && !progress;
    installDetails.hidden = !progress?.details;
    installDetails.textContent = progress?.details ?? '';
    cancelInstallButton.hidden = !installation.canCancel;
    cancelInstallButton.disabled = state === 'cancelling';
  }

  if (state === 'completed') {
    installSuccessMessage.textContent =
      installation.result?.message || 'Installation completed successfully.';
  }

  if (state === 'failed') {
    installFailureMessage.textContent = installation.errorMessage ?? 'Installation failed.';
  }

  if (state === 'cancelled') {
    installCancelledMessage.textContent =
      installation.errorMessage ?? 'Installation was cancelled.';
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
        ? 'Cancelling uninstallation...'
        : progress?.message || 'Uninstalling...';
    uninstallProgress.value = progress?.percentage ?? 0;
    uninstallProgress.indeterminate = state === 'starting' && !progress;
    uninstallDetails.hidden = !progress?.details;
    uninstallDetails.textContent = progress?.details ?? '';
    cancelUninstallButton.hidden = !uninstallation.canCancel;
    cancelUninstallButton.disabled = state === 'cancelling';
  }

  if (state === 'completed') {
    uninstallSuccessMessage.textContent =
      uninstallation.result?.message || 'Uninstallation completed successfully.';
  }

  if (state === 'failed') {
    uninstallFailureMessage.textContent = uninstallation.errorMessage ?? 'Uninstallation failed.';
  }

  if (state === 'cancelled') {
    uninstallCancelledMessage.textContent =
      uninstallation.errorMessage ?? 'Uninstallation was cancelled.';
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
      setupStatus.textContent = 'Validating game folder...';
      break;
    case 'valid':
      setupStatus.hidden = false;
      setupStatus.className = 'setup-status status-valid';
      setupStatus.textContent = `Valid game found — version ${setup.validatedFolder?.version || 'unknown'} (${setup.validatedFolder?.source || 'unknown source'})`;
      break;
    case 'invalid':
      setupError.hidden = false;
      setupError.textContent = setup.errorMessage ?? 'Invalid game folder.';
      break;
    case 'saving':
      setupStatus.hidden = false;
      setupStatus.className = 'setup-status status-validating';
      setupStatus.textContent = 'Saving configuration...';
      break;
    case 'error':
      setupError.hidden = false;
      setupError.textContent = setup.errorMessage ?? 'An unexpected error occurred.';
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
    !shell.isHostReady ||
    manager.togglingFileName !== null ||
    manager.loadState === 'loading' ||
    launchGame.launching;

  launchGameButton.hidden = !shell.canLaunchGame;
  launchGameButton.disabled =
    !shell.isHostReady || launchGame.launching || manager.togglingFileName !== null;

  managerLaunchSuccess.hidden = !launchGame.successMessage;
  managerLaunchSuccess.textContent = launchGame.successMessage ?? '';
  managerLaunchError.hidden = !launchGame.errorMessage;
  managerLaunchError.textContent = launchGame.errorMessage ?? '';

  uninstallAsherButton.hidden = !shell.canUninstall;

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
        ? 'Updating...'
        : mod.isEnabled
          ? 'Enabled'
          : 'Disabled';

    toggleLabel.append(checkbox, toggleText);
    item.append(info, toggleLabel);
    modList.appendChild(item);
  }
}

navSetupButton.addEventListener('click', () => shell.navigateTo('setup'));
navManagerButton.addEventListener('click', () => shell.navigateTo('manager'));
openManagerButton.addEventListener('click', () => shell.navigateTo('manager'));
startInstallButton.addEventListener('click', () => shell.navigateTo('install'));
beginInstallButton.addEventListener('click', () => runInstall());
cancelInstallButton.addEventListener('click', () => installation.cancelInstall());
installContinueButton.addEventListener('click', () => shell.navigateTo('manager'));
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

uninstallAsherButton.addEventListener('click', () => shell.navigateTo('uninstall'));
confirmUninstallButton.addEventListener('click', () => runUninstall());
cancelUninstallConfirmButton.addEventListener('click', () => {
  uninstallation.cancelConfirmation();
  shell.navigateTo('manager');
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

launchGameButton.addEventListener('click', async () => {
  launchGame.clearMessages();
  await launchGame.launchGame(shell.canLaunchGame);
});

retryHostButton.addEventListener('click', async () => {
  retryHostButton.disabled = true;
  try {
    await shell.retryHost();
  } finally {
    retryHostButton.disabled = false;
  }
});

await showDiagnosticLogPath(client);
await shell.start();
