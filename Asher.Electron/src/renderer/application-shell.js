import { fetchApplicationState, mapApplicationError } from './application-state.js';
import { logDiagnostic } from './diagnostic-log.js';

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */
/** @typedef {import('./application-state.js').ApplicationState} ApplicationState */
/** @typedef {'booting' | 'connecting' | 'loading-app' | 'ready' | 'host-error'} ShellPhase */
/** @typedef {'setup' | 'manager' | 'install' | 'uninstall'} AppScreen */

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

  get canLaunchGame() {
    return Boolean(this.#applicationState?.canLaunchGame);
  }

  get isEditingSetup() {
    return this.#editingSetup;
  }

  #notify() {
    this.onChange?.();
  }

  /**
   * @param {ShellPhase} phase
   */
  #setPhase(phase) {
    logDiagnostic('info', 'shell', `phase -> ${phase}`, {
      hostStatus: this.#hostStatus.status
    });
    this.#phase = phase;
    this.#notify();
  }

  async start() {
    logDiagnostic('info', 'shell', 'start()');
    if (!window.asher) {
      logDiagnostic('error', 'shell', 'window.asher preload bridge is missing');
    }

    this.#setPhase('booting');
    this.client.onHostStatusChanged((status) => {
      void this.#handleHostStatus(status);
    });

    await this.#ensureHostConnected();
  }

  async #ensureHostConnected() {
    logDiagnostic('info', 'shell', 'ensureHostConnected() begin');
    const initial = await this.client.getHostStatus();
    logDiagnostic('info', 'shell', 'initial host status', initial);

    if (initial.status === 'ready') {
      await this.#handleHostStatus(initial);
      return;
    }

    this.#setPhase('connecting');
    const result = await this.client.startHost();
    logDiagnostic('info', 'shell', 'startHost() result', result);
    await this.#handleHostStatus(result);
  }

  /**
   * @param {{ status: string, message?: string | null }} status
   */
  async #handleHostStatus(status) {
    logDiagnostic('info', 'shell', 'handleHostStatus()', status);
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
      await this.#loadApplicationState();
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

  async #loadApplicationState() {
    logDiagnostic('info', 'shell', 'loadApplicationState() begin');
    this.#setPhase('loading-app');
    this.#errorMessage = null;

    try {
      const state = await fetchApplicationState(this.client);
      logDiagnostic('info', 'shell', 'application state loaded', {
        mode: state.mode,
        isConfigured: state.isConfigured,
        recommendedScreen: state.recommendedScreen,
        canLaunchGame: state.canLaunchGame
      });
      this.#applicationState = state;

      if (!this.#screen || (this.#screen === 'manager' && !state.isConfigured)) {
        this.#screen = state.recommendedScreen;
      }

      this.#setPhase('ready');
      await this.#enterScreen(this.#screen);
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
      return;
    }

    await this.#loadApplicationState();
  }

  /**
   * @param {AppScreen} screen
   * @param {{ force?: boolean }} [options]
   */
  async navigateTo(screen, options = {}) {
    if (!this.isApplicationReady) {
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
      this.#screen = 'manager';
      this.#notify();
      await this.#enterScreen('manager');
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
      await this.navigateTo('manager');
    }
  }

  beginSetupEditing() {
    this.#editingSetup = true;
    this.#screen = 'setup';
    this.#notify();
  }

  async returnToSetup(reason = null) {
    this.#screen = 'setup';
    if (reason) {
      this.#errorMessage = reason;
    }
    this.#notify();
    await this.#enterScreen('setup');
  }

  /**
   * @param {'completed' | 'failed' | 'cancelled'} outcome
   */
  async handleInstallComplete(outcome) {
    if (outcome === 'completed') {
      await this.refreshApplicationState();
      await this.navigateTo('manager');
      return;
    }
    this.#notify();
  }

  /**
   * @param {'completed' | 'failed' | 'cancelled'} outcome
   */
  async handleUninstallComplete(outcome) {
    if (outcome === 'completed') {
      await this.refreshApplicationState();
      const screen = this.#applicationState?.recommendedScreen ?? 'setup';
      await this.navigateTo(screen);
      return;
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
