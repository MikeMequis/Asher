/** @typedef {import('../preload/preload.js')} AsherApi */

/**
 * Thin renderer client over the preload bridge.
 */
export class ApplicationClient {
  /** @param {AsherApi} api */
  constructor(api) {
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
