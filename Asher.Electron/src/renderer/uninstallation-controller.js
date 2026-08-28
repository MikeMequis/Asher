import { classifyError } from './errors.js';

/** @typedef {'idle' | 'confirming' | 'starting' | 'uninstalling' | 'cancelling' | 'completed' | 'failed' | 'cancelled'} UninstallState */

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

/**
 * @typedef {object} UninstallProgress
 * @property {number} percentage
 * @property {string} message
 * @property {string} details
 */

/**
 * Asher uninstallation flow via IAsherApplication uninstall + cancel JSONL methods.
 */
export class UninstallationController {
  /** @type {UninstallState} */
  #state = 'idle';
  /** @type {string | null} */
  #requestId = null;
  /** @type {UninstallProgress | null} */
  #progress = null;
  /** @type {object | null} */
  #result = null;
  /** @type {string | null} */
  #errorMessage = null;

  /** @param {ApplicationClient} client */
  constructor(client) {
    this.client = client;
    this.onChange = null;

    client.onOperationStarted(({ method, requestId }) => {
      if (method === 'uninstall' && this.#state === 'starting') {
        this.#requestId = requestId;
        this.#setState('uninstalling');
      }
    });

    client.onProgress(({ method, requestId, progress }) => {
      if (method !== 'uninstall' || requestId !== this.#requestId) {
        return;
      }

      this.#progress = {
        percentage: progress?.percentage ?? 0,
        message: progress?.message ?? '',
        details: progress?.details ?? ''
      };
      this.#notify();
    });
  }

  get state() {
    return this.#state;
  }

  get progress() {
    return this.#progress;
  }

  get result() {
    return this.#result;
  }

  get errorMessage() {
    return this.#errorMessage;
  }

  get canCancel() {
    return this.#state === 'uninstalling' && Boolean(this.#requestId);
  }

  #notify() {
    this.onChange?.();
  }

  #setState(state) {
    this.#state = state;
    this.#notify();
  }

  reset() {
    this.#state = 'idle';
    this.#requestId = null;
    this.#progress = null;
    this.#result = null;
    this.#errorMessage = null;
    this.#notify();
  }

  requestConfirmation() {
    this.#errorMessage = null;
    this.#setState('confirming');
  }

  cancelConfirmation() {
    this.#setState('idle');
  }

  /**
   * @param {string | null | undefined} gameFolderPath
   * @param {boolean} canUninstall
   * @returns {Promise<'completed' | 'failed' | 'cancelled'>}
   */
  async startUninstall(gameFolderPath, canUninstall) {
    if (!gameFolderPath?.trim()) {
      this.#errorMessage = 'No game folder is configured for uninstallation.';
      this.#setState('failed');
      return 'failed';
    }

    if (!canUninstall) {
      this.#errorMessage =
        'Asher cannot be uninstalled. The game may not be installed or no restorable backup was found.';
      this.#setState('failed');
      return 'failed';
    }

    this.#requestId = null;
    this.#progress = null;
    this.#result = null;
    this.#errorMessage = null;
    this.#setState('starting');

    try {
      const { result } = await this.client.invoke(
        'uninstall',
        { gameFolderPath },
        { trackProgress: true, allowFailure: true }
      );

      if (result?.error?.code === 'cancelled') {
        this.#errorMessage = result.error?.message ?? 'Uninstallation was cancelled.';
        this.#setState('cancelled');
        return 'cancelled';
      }

      const uninstallResult = result?.success === false ? result.result ?? result : result;

      if (!uninstallResult?.success) {
        this.#result = uninstallResult;
        this.#errorMessage =
          uninstallResult?.errorMessage ??
          uninstallResult?.message ??
          result?.error?.message ??
          'Uninstallation failed.';
        this.#setState('failed');
        return 'failed';
      }

      this.#result = uninstallResult;
      await this.#markUninstalled();
      this.#setState('completed');
      return 'completed';
    } catch (err) {
      const { kind, message } = classifyError(err);
      this.#errorMessage =
        kind === 'host'
          ? message
          : err instanceof Error && err.code === 'cancelled'
            ? 'Uninstallation was cancelled.'
            : message;
      this.#setState(err instanceof Error && err.code === 'cancelled' ? 'cancelled' : 'failed');
      return err instanceof Error && err.code === 'cancelled' ? 'cancelled' : 'failed';
    } finally {
      this.#requestId = null;
    }
  }

  async #markUninstalled() {
    const { result: settings } = await this.client.invoke('getSettings');
    await this.client.invoke('saveSettings', {
      ...settings,
      isInstalled: false,
      installationDate: null
    });
  }

  async cancelUninstall() {
    if (!this.canCancel || !this.#requestId) {
      return;
    }

    this.#setState('cancelling');

    try {
      await this.client.cancel(this.#requestId);
    } catch (err) {
      this.#errorMessage = classifyError(err).message;
      this.#setState('failed');
    }
  }
}
