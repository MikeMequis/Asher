import { classifyError } from './errors.js';
import { logDiagnostic } from './diagnostic-log.js';

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
  /** @type {string | null} */
  #errorDetails = null;

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

  get errorDetails() {
    return this.#errorDetails;
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
    this.#errorDetails = null;
    this.#notify();
  }

  /**
   * @param {object} gameFolder
   * @returns {Promise<'completed' | 'failed' | 'cancelled'>}
   */
  async startInstall(gameFolder) {
    if (!gameFolder?.isValid) {
      this.#errorMessage = 'A valid game folder is required before installation.';
      this.#errorDetails = null;
      this.#setState('failed');
      return 'failed';
    }

    this.#requestId = null;
    this.#progress = null;
    this.#result = null;
    this.#errorMessage = null;
    this.#errorDetails = null;
    this.#setState('starting');

    const gameInfo = {
      path: gameFolder.path,
      version: gameFolder.version ?? '',
      isValid: true,
      source: gameFolder.source ?? ''
    };

    try {
      logDiagnostic('info', 'install', 'startInstall invoking host', { path: gameFolder.path });

      const { result } = await this.client.invoke('install', gameInfo, {
        trackProgress: true,
        allowFailure: true
      });

      logDiagnostic('info', 'install', 'startInstall host response', {
        success: result?.success,
        message: result?.message ?? result?.result?.message,
        details: result?.details ?? result?.result?.details
      });

      if (result?.success === false && result?.error?.code === 'cancelled') {
        this.#errorMessage = result.error?.message ?? 'Installation was cancelled.';
        this.#errorDetails = null;
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
        this.#errorDetails = installResult?.details || null;
        this.#setState('failed');
        return 'failed';
      }

      this.#result = installResult;

      const { result: installed } = await this.client.invoke('isGameInstalled', {
        gameFolderPath: gameFolder.path
      });
      logDiagnostic('info', 'install', 'startInstall post-verify', {
        installed: installed?.installed,
        markers: installed?.markers
      });
      if (!installed?.installed) {
        this.#errorMessage =
          'Installation finished but Asher was not detected on disk. Try again.';
        this.#errorDetails = installResult?.details || installResult?.message || null;
        this.#setState('failed');
        return 'failed';
      }

      await this.#markInstalled(gameFolder);
      logDiagnostic('info', 'install', 'startInstall markInstalled complete');
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
      this.#errorDetails = null;
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
      this.#errorDetails = null;
      this.#setState('failed');
    }
  }
}
