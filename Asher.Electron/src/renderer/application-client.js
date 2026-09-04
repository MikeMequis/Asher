/** @typedef {Window['asher']} AsherApi */

/**
 * Thin renderer client over the preload bridge.
 */
export class ApplicationClient {
  /** @param {AsherApi | undefined} api */
  constructor(api) {
    if (!api) {
      throw new Error('Asher preload bridge is not available.');
    }

    this.api = api;
    this.#operationStartedUnsubscribe = this.api.onOperationStarted((payload) => {
      if (this.#operationStartedHandler) {
        this.#operationStartedHandler(payload);
      }
    });
    this.#progressUnsubscribe = this.api.onProgress((payload) => {
      if (this.#progressHandler) {
        this.#progressHandler(payload);
      }
    });
  }

  /** @type {((payload: { method: string, requestId: string }) => void) | null} */
  #operationStartedHandler = null;
  /** @type {((payload: { method: string, requestId: string, progress: object }) => void) | null} */
  #progressHandler = null;
  /** @type {(() => void) | null} */
  #operationStartedUnsubscribe = null;
  /** @type {(() => void) | null} */
  #progressUnsubscribe = null;

  /**
   * @param {(payload: { method: string, requestId: string }) => void} handler
   */
  onOperationStarted(handler) {
    this.#operationStartedHandler = handler;
  }

  /**
   * @param {(payload: { method: string, requestId: string, progress: object }) => void} handler
   */
  onProgress(handler) {
    this.#progressHandler = handler;
  }

  getHostStatus() {
    return this.api.getHostStatus();
  }

  getLogPath() {
    return this.api.getLogPath();
  }

  relocateLogs(gameFolderPath) {
    if (!this.api.relocateLogs) {
      return Promise.resolve(null);
    }

    return this.api.relocateLogs(gameFolderPath);
  }

  /**
   * @param {'info' | 'warn' | 'error'} level
   * @param {string} source
   * @param {string} message
   * @param {unknown} [data]
   */
  log(level, source, message, data) {
    if (this.api.log) {
      return this.api.log(level, source, message, data);
    }
    return Promise.resolve();
  }

  startHost() {
    return this.api.startHost();
  }

  onHostStatusChanged(callback) {
    return this.api.onHostStatusChanged(callback);
  }

  pickFolder() {
    return this.api.pickFolder();
  }

  getAppVersion() {
    if (!this.api.getAppVersion) {
      return Promise.resolve('0.1.0');
    }

    return this.api.getAppVersion();
  }

  minimizeWindow() {
    if (!this.api.minimizeWindow) {
      return Promise.resolve();
    }

    return this.api.minimizeWindow();
  }

  isPackaged() {
    if (!this.api.isPackaged) {
      return Promise.resolve(false);
    }
    return this.api.isPackaged();
  }

  isRunningFromManager(gameFolderPath) {
    if (!this.api.isRunningFromManager) {
      return Promise.resolve(false);
    }
    return this.api.isRunningFromManager(gameFolderPath);
  }

  transitionToInstalledManager(gameFolderPath) {
    if (!this.api.transitionToInstalledManager) {
      return Promise.resolve({ transitioned: false, reason: 'unavailable' });
    }
    return this.api.transitionToInstalledManager(gameFolderPath);
  }

  scheduleSelfUninstallCleanup(gameFolderPath) {
    if (!this.api.scheduleSelfUninstallCleanup) {
      return Promise.resolve({ scheduled: false, reason: 'unavailable' });
    }
    return this.api.scheduleSelfUninstallCleanup(gameFolderPath);
  }

  checkForUpdates(options) {
    if (!this.api.checkForUpdates) {
      return Promise.resolve({ status: 'unavailable' });
    }
    return this.api.checkForUpdates(options);
  }

  downloadAndApplyUpdate(params) {
    if (!this.api.downloadAndApplyUpdate) {
      return Promise.resolve({ status: 'error', message: 'Updater unavailable.' });
    }
    return this.api.downloadAndApplyUpdate(params);
  }

  openReleasePage(url) {
    if (!this.api.openReleasePage) {
      return Promise.resolve({ ok: false });
    }
    return this.api.openReleasePage(url);
  }

  onUpdaterStatus(callback) {
    if (!this.api.onUpdaterStatus) {
      return () => {};
    }
    return this.api.onUpdaterStatus(callback);
  }

  /**
   * @param {string} method
   * @param {object} [params]
   * @param {{ trackProgress?: boolean, allowFailure?: boolean }} [options]
   */
  invoke(method, params, options) {
    return this.api.invoke(method, params, options);
  }

  /**
   * @param {string} targetRequestId
   */
  cancel(targetRequestId) {
    return this.invoke('cancel', { targetRequestId }, { allowFailure: true });
  }
}
