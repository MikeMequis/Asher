import { classifyError } from './errors.js';

/** @typedef {'idle' | 'validating' | 'valid' | 'invalid' | 'saving' | 'error'} SetupState */
/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

/**
 * Game setup operations — folder detect, validate, and persist.
 */
export class GameSetupController {
  /** @type {SetupState} */
  #state = 'idle';
  /** @type {object | null} */
  #validatedFolder = null;
  /** @type {string | null} */
  #errorMessage = null;

  /** @param {ApplicationClient} client */
  constructor(client) {
    this.client = client;
    this.onChange = null;
  }

  get state() {
    return this.#state;
  }

  get validatedFolder() {
    return this.#validatedFolder;
  }

  get errorMessage() {
    return this.#errorMessage;
  }

  #setState(state) {
    this.#state = state;
    this.onChange?.();
  }

  #setError(message) {
    this.#errorMessage = message;
    this.#setState('error');
  }

  #clearError() {
    this.#errorMessage = null;
  }

  reset() {
    this.#validatedFolder = null;
    this.#clearError();
    this.#setState('idle');
  }

  async autoDetect() {
    this.#clearError();
    this.#setState('validating');

    try {
      const { result } = await this.client.invoke('detectGameFolder');
      this.#applyValidationResult(result);
    } catch (err) {
      this.#setError(classifyError(err).message);
    }
  }

  /**
   * @param {string} folderPath
   */
  async validatePath(folderPath) {
    const trimmed = folderPath?.trim() ?? '';
    if (!trimmed) {
      this.#validatedFolder = null;
      this.#clearError();
      this.#setState('idle');
      return;
    }

    this.#clearError();
    this.#setState('validating');

    try {
      const { result } = await this.client.invoke('getGameFolderInfo', { folderPath: trimmed });
      this.#applyValidationResult(result);
    } catch (err) {
      this.#setError(classifyError(err).message);
    }
  }

  /**
   * @returns {Promise<boolean>}
   */
  async saveConfiguration() {
    if (!this.#validatedFolder?.isValid) {
      this.#errorMessage = 'Select a valid game folder before saving.';
      this.#setState('invalid');
      return false;
    }

    this.#clearError();
    this.#setState('saving');

    try {
      const { result: settings } = await this.client.invoke('getSettings');
      const updated = {
        ...settings,
        gameFolderPath: this.#validatedFolder.path,
        gameVersion: this.#validatedFolder.version ?? settings.gameVersion ?? ''
      };

      await this.client.invoke('saveSettings', updated);

      this.#setState('idle');
      return true;
    } catch (err) {
      this.#setError(classifyError(err).message);
      return false;
    }
  }

  /**
   * @param {object | null | undefined} folder
   */
  #applyValidationResult(folder) {
    this.#validatedFolder = folder ?? null;

    if (!folder?.path) {
      this.#setState('idle');
      return;
    }

    if (folder.isValid) {
      this.#setState('valid');
      return;
    }

    this.#errorMessage = 'Could not find DustAET.exe in this folder.';
    this.#setState('invalid');
  }
}
