import { fetchApplicationState, mapApplicationError } from './application-state.js';
import { logDiagnostic } from './diagnostic-log.js';

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */
/** @typedef {import('./application-state.js').ApplicationState} ApplicationState */
/** @typedef {'booting' | 'connecting' | 'loading-app' | 'ready' | 'host-error'} ShellPhase */
/** @typedef {'welcome' | 'setup' | 'home' | 'manager' | 'settings' | 'install' | 'uninstall'} AppScreen */

/**
 * Central application shell — startup, host lifecycle, navigation, shared state.
 */
export class ApplicationShell {
  /** @type {ShellPhase} */
  #phase = 'booting';
  /** @type {{ status: string, message: string | null }} */
  #hostStatus = { status: 'stopped', message: null };
  /** @type {ApplicationState | null} */
  #applicationState = null;
  /** @type {AppScreen | null} */
  #screen = null;
  /** @type {string | null} */
  #errorMessage = null;
  /** @type {boolean} */
  #editingSetup = false;
  /** @type {(() => void) | null} */
  onChange = null;
  /** @type {((screen: AppScreen) => Promise<void>) | null} */
  onEnterScreen = null;

  /** @param {ApplicationClient} client */
  constructor(client) {
    this.client = client;
  }

  get phase() {
    return this.#phase;
  }

  get hostStatus() {
    return this.#hostStatus;
  }

  get applicationState() {
    return this.#applicationState;
  }

  get screen() {
    return this.#screen;
  }

  get errorMessage() {
    return this.#errorMessage;
  }

  get isHostReady() {
    return this.#hostStatus.status === 'ready';
  }

  get isApplicationReady() {
    return this.#phase === 'ready' && this.#applicationState !== null;
  }

  get canShowManager() {
    return Boolean(this.#applicationState?.isConfigured);
  }

  get needsInstallation() {
    return Boolean(this.#applicationState?.needsInstallation);
  }

  get canUninstall() {
    return Boolean(this.#applicationState?.canUninstall);
  }

  get isManagerMode() {
    return this.#applicationState?.mode === 'manager';
  }

  get canShowHome() {
    return this.isManagerMode && this.canShowManager;
  }

  get canLaunchGame() {
    return Boolean(this.#applicationState?.canLaunchGame);
  }

  get sidebarCollapsed() {
    return this.#sidebarCollapsed;
  }

  /** @type {boolean} */
  #sidebarCollapsed = false;

  get isEditingSetup() {
    return this.#editingSetup;
  }

  toggleSidebar() {
    this.#sidebarCollapsed = !this.#sidebarCollapsed;
    this.#notify();
  }

  #notify() {
    this.onChange?.();
  }

  /**
   * @param {ShellPhase} phase
   */
  #setPhase(phase) {
    this.#phase = phase;
    this.#notify();
  }

  async start() {
    if (!window.asher) {
      logDiagnostic('error', 'shell', 'preload bridge missing');
    }

    this.#setPhase('booting');
    this.client.onHostStatusChanged((status) => {
      void this.#handleHostStatus(status);
    });

    await this.#ensureHostConnected();
  }

  async #ensureHostConnected() {
    const initial = await this.client.getHostStatus();

    if (initial.status === 'ready') {
      await this.#handleHostStatus(initial);
      return;
    }

    this.#setPhase('connecting');
    const result = await this.client.startHost();
    await this.#handleHostStatus(result);
  }

  /**
   * @param {{ status: string, message?: string | null }} status
   */
  async #handleHostStatus(status) {
    this.#hostStatus = {
      status: status.status,
      message: status.message ?? null
    };
    this.#notify();

    if (status.status === 'starting') {
      this.#errorMessage = null;
      this.#setPhase('connecting');
      return;
    }

    if (status.status === 'ready') {
      if (this.#phase === 'ready' || this.#phase === 'loading-app') {
        return;
      }
      await this.#loadApplicationState({ reenterScreen: true });
      return;
    }

    if (status.status === 'error' || status.status === 'terminated') {
      this.#applicationState = null;
      this.#screen = null;
      this.#errorMessage =
        status.message ?? 'The Asher application host is not available.';
      this.#setPhase('host-error');
      return;
    }

    if (status.status === 'stopped') {
      this.#setPhase('booting');
    }
  }

  async #loadApplicationState(options = {}) {
    const reenterScreen = options.reenterScreen === true;
    this.#setPhase('loading-app');
    this.#errorMessage = null;

    logDiagnostic('info', 'shell', 'loadApplicationState start', {
      screen: this.#screen,
      reenterScreen
    });

    try {
      const state = await fetchApplicationState(this.client);
      if (state.settings?.gameFolderPath) {
        await this.client.relocateLogs(state.settings.gameFolderPath);
      }
      this.#applicationState = state;

      logDiagnostic('info', 'shell', 'loadApplicationState fetched', {
        mode: state.mode,
        needsInstallation: state.needsInstallation,
        settingsIsInstalled: state.settings?.isInstalled,
        gameFolderPath: state.settings?.gameFolderPath,
        canUninstall: state.canUninstall
      });

      if (
        !this.#screen ||
        (this.#screen === 'manager' && !state.isConfigured) ||
        (this.#screen === 'home' && !state.isConfigured) ||
        (this.#screen === 'welcome' && state.mode === 'manager' && state.isConfigured)
      ) {
        this.#screen = state.recommendedScreen;
      }

      this.#setPhase('ready');

      if (reenterScreen) {
        await this.#enterScreen(this.#screen);
      }
    } catch (err) {
      const { message } = mapApplicationError(this.client, err);
      logDiagnostic('error', 'shell', 'loadApplicationState() failed', {
        message,
        error: err instanceof Error ? err.message : String(err)
      });
      this.#applicationState = null;
      this.#screen = null;
      this.#errorMessage = message;
      this.#setPhase('host-error');
    }
  }

  async refreshApplicationState() {
    if (!this.isHostReady) {
      logDiagnostic('warn', 'shell', 'refreshApplicationState skipped — host not ready');
      return;
    }

    await this.#loadApplicationState({ reenterScreen: false });
  }

  /**
   * @param {AppScreen} screen
   * @param {{ force?: boolean }} [options]
   */
  async navigateTo(screen, options = {}) {
    if (!this.isApplicationReady) {
      return;
    }

    if (screen === 'home' && !this.canShowHome && !options.force) {
      this.#screen = this.canShowManager ? 'manager' : 'setup';
      this.#notify();
      await this.#enterScreen(this.#screen);
      return;
    }

    if (screen === 'settings' && !this.isApplicationReady && !options.force) {
      return;
    }

    if (screen === 'manager' && !this.canShowManager && !options.force) {
      this.#screen = 'setup';
      this.#notify();
      await this.#enterScreen('setup');
      return;
    }

    if (screen === 'install' && !this.canShowManager) {
      this.#screen = 'setup';
      this.#notify();
      await this.#enterScreen('setup');
      return;
    }

    if (screen === 'uninstall' && !this.canUninstall) {
      this.#screen = 'settings';
      this.#notify();
      await this.#enterScreen('settings');
      return;
    }

    if (screen === 'welcome' && this.isManagerMode && !options.force) {
      this.#screen = this.canShowHome ? 'home' : 'manager';
      this.#notify();
      await this.#enterScreen(this.#screen);
      return;
    }

    this.#screen = screen;
    this.#notify();
    await this.#enterScreen(screen);
  }

  /**
   * @param {AppScreen} screen
   */
  async #enterScreen(screen) {
    if (this.onEnterScreen) {
      await this.onEnterScreen(screen);
    }
  }

  async onConfigurationSaved() {
    this.#editingSetup = false;
    await this.refreshApplicationState();
    if (this.needsInstallation) {
      await this.navigateTo('install');
      return;
    }
    if (this.canShowManager) {
      await this.navigateTo(this.canShowHome ? 'home' : 'manager');
    }
  }

  beginSetupEditing() {
    this.#editingSetup = true;
    this.#screen = 'setup';
    this.#notify();
  }

  async returnToSetup(reason = null) {
    this.#screen = 'welcome';
    if (reason) {
      this.#errorMessage = reason;
    }
    this.#notify();
    await this.#enterScreen('welcome');
  }

  /**
   * Stay on the install Complete screen until the user finishes.
   * Defer mode refresh so the wizard sidebar remains until Finish.
   * @param {'completed' | 'failed' | 'cancelled'} outcome
   */
  async handleInstallComplete(outcome) {
    logDiagnostic('info', 'shell', 'handleInstallComplete', {
      outcome,
      mode: this.#applicationState?.mode
    });
    this.#notify();
  }

  /**
   * Finish the install wizard and enter manager mode.
   */
  async finishInstallation() {
    await this.refreshApplicationState();
    await this.navigateTo(this.canShowHome ? 'home' : 'manager');
  }

  /**
   * Stay on the uninstall Complete screen until the user continues.
   * Defer mode refresh so success feedback remains visible.
   * @param {'completed' | 'failed' | 'cancelled'} outcome
   */
  async handleUninstallComplete(outcome) {
    logDiagnostic('info', 'shell', 'handleUninstallComplete', { outcome });
    if (outcome === 'completed') {
      await this.refreshApplicationState();
    }
    this.#notify();
  }

  async retryHost() {
    this.#errorMessage = null;
    await this.#ensureHostConnected();
  }

  /**
   * Called by Manager when configuration is no longer valid.
   */
  async handleConfigurationLost(message) {
    await this.refreshApplicationState();
    if (!this.canShowManager) {
      await this.returnToSetup(
        message ?? 'Game configuration is no longer valid. Please set up the game folder again.'
      );
    }
  }
}
