import { classifyError } from './errors.js';

/** @typedef {'loading' | 'loaded' | 'empty' | 'error'} ManagerLoadState */
/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

/**
 * @typedef {object} ManagedMod
 * @property {string} fileName
 * @property {string} name
 * @property {string} description
 * @property {boolean} isEnabled
 */

/**
 * @typedef {{ scope: 'full' | 'chrome', fileName?: string | null }} ManagerNotifyOptions
 */

/**
 * Mod Manager — loads and toggles mods via IAsherApplication JSONL methods.
 */
export class ModManagerController {
  /** @type {ManagerLoadState} */
  #loadState = 'loading';
  /** @type {ManagedMod[]} */
  #mods = [];
  /** @type {string | null} */
  #errorMessage = null;
  /** @type {string | null} */
  #togglingFileName = null;
  /** @type {string | null} */
  #toggleError = null;
  /** @type {ManagerNotifyOptions} */
  #lastNotify = { scope: 'full' };

  /** @param {ApplicationClient} client */
  constructor(client) {
    this.client = client;
    this.onChange = null;
  }

  get loadState() {
    return this.#loadState;
  }

  get mods() {
    return this.#mods;
  }

  get errorMessage() {
    return this.#errorMessage;
  }

  get togglingFileName() {
    return this.#togglingFileName;
  }

  get toggleError() {
    return this.#toggleError;
  }

  get lastNotify() {
    return this.#lastNotify;
  }

  get activeCount() {
    return this.#mods.filter((mod) => mod.isEnabled).length;
  }

  get totalCount() {
    return this.#mods.length;
  }

  /**
   * @param {ManagerNotifyOptions} [options]
   */
  #notify(options = { scope: 'chrome' }) {
    this.#lastNotify = {
      scope: options.scope,
      fileName: options.fileName ?? null
    };
    this.onChange?.();
  }

  /**
   * @param {ManagerLoadState} state
   */
  #setLoadState(state) {
    this.#loadState = state;
    this.#notify({ scope: 'full' });
  }

  async loadMods() {
    this.#errorMessage = null;
    this.#toggleError = null;
    this.#setLoadState('loading');

    try {
      const { result } = await this.client.invoke('getMods');
      const mods = Array.isArray(result) ? result : [];

      this.#mods = mods.map((mod) => ({
        fileName: mod.fileName ?? '',
        name: mod.name || mod.fileName || 'Unknown mod',
        description: mod.description ?? '',
        isEnabled: Boolean(mod.isEnabled)
      }));

      this.#setLoadState(this.#mods.length === 0 ? 'empty' : 'loaded');
    } catch (err) {
      this.#errorMessage = classifyError(err).message;
      this.#mods = [];
      this.#setLoadState('error');
    }
  }

  /**
   * @param {string} fileName
   * @param {boolean} enabled
   */
  async toggleMod(fileName, enabled) {
    const mod = this.#mods.find((item) => item.fileName === fileName);
    if (!mod || this.#togglingFileName) {
      return false;
    }

    const previous = mod.isEnabled;
    this.#toggleError = null;
    this.#togglingFileName = fileName;
    mod.isEnabled = enabled;
    this.#notify({ scope: 'chrome', fileName });

    try {
      const { result } = await this.client.invoke('setModEnabled', { fileName, enabled });

      if (!result?.success) {
        mod.isEnabled = previous;
        this.#toggleError = result?.errorMessage ?? 'Failed to update mod state.';
        return false;
      }

      return true;
    } catch (err) {
      mod.isEnabled = previous;
      this.#toggleError = classifyError(err).message;
      return false;
    } finally {
      this.#togglingFileName = null;
      this.#notify({ scope: 'chrome', fileName });
    }
  }

  clearToggleError() {
    if (!this.#toggleError) {
      return;
    }

    this.#toggleError = null;
    this.#notify({ scope: 'chrome' });
  }
}
