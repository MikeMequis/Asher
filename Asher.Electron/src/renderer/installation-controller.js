import { classifyError } from './errors.js';

/** @typedef {'idle' | 'starting' | 'installing' | 'cancelling' | 'completed' | 'failed' | 'cancelled'} InstallState */

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

/**
 * @typedef {object} InstallProgress
 * @property {number} percentage
 * @property {string} message
 * @property {string} details
 */

/**
 * Asher installation flow via IAsherApplication install + cancel JSONL methods.
 */
export class InstallationController {
  /** @type {InstallState} */
  #state = 'idle';
  /** @type {string | null} */
  #requestId = null;
  /** @type {InstallProgress | null} */
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
      if (method === 'install' && this.#state === 'starting') {
        this.#requestId = requestId;
        this.#setState('installing');
      }
    });

    client.onProgress(({ method, requestId, progress }) => {
      if (method !== 'install' || requestId !== this.#requestId) {
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

  get requestId() {
    return this.#requestId;
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
    return this.#state === 'installing' && Boolean(this.#requestId);
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

  /**
   * @param {object} gameFolder
   * @returns {Promise<'completed' | 'failed' | 'cancelled'>}
   */
  async startInstall(gameFolder) {
    if (!gameFolder?.isValid) {
      this.#errorMessage = 'A valid game folder is required before installation.';
      this.#setState('failed');
      return 'failed';
    }

    this.#requestId = null;
    this.#progress = null;
    this.#result = null;
    this.#errorMessage = null;
    this.#setState('starting');

    const gameInfo = {
      path: gameFolder.path,
      version: gameFolder.version ?? '',
      isValid: true,
      source: gameFolder.source ?? '',
      hasPatchesFolder: Boolean(gameFolder.hasPatchesFolder),
      patchesFolderPath: gameFolder.patchesFolderPath ?? ''
    };

    try {
      const { result } = await this.client.invoke('install', gameInfo, {
        trackProgress: true,
        allowFailure: true
      });

      if (result?.success === false && result?.error?.code === 'cancelled') {
        this.#errorMessage = result.error?.message ?? 'Installation was cancelled.';
        this.#setState('cancelled');
        return 'cancelled';
      }

      const installResult = result?.success === false ? result.result ?? result : result;

      if (!installResult?.success) {
        this.#result = installResult;
        this.#errorMessage =
          installResult?.errorMessage ??
          installResult?.message ??
          result?.error?.message ??
          'Installation failed.';
        this.#setState('failed');
        return 'failed';
      }

      this.#result = installResult;
      await this.#markInstalled(gameFolder);
      this.#setState('completed');
      return 'completed';
    } catch (err) {
      const { kind, message } = classifyError(err);
      this.#errorMessage =
        kind === 'host'
          ? message
          : err instanceof Error && err.code === 'cancelled'
            ? 'Installation was cancelled.'
            : message;
      this.#setState(err instanceof Error && err.code === 'cancelled' ? 'cancelled' : 'failed');
      return err instanceof Error && err.code === 'cancelled' ? 'cancelled' : 'failed';
    } finally {
      this.#requestId = null;
    }
  }

  /**
   * @param {object} gameFolder
   */
  async #markInstalled(gameFolder) {
    await this.client.invoke('markInstalled', {
      gameFolderPath: gameFolder.path,
      gameVersion: gameFolder.version ?? ''
    });
  }

  async cancelInstall() {
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
