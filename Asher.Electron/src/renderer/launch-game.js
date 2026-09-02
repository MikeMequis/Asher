import { classifyError } from './errors.js';
import { t } from './localization.js';

/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

/**
 * Launch game via IAsherApplication launchGame JSONL method.
 */
export class LaunchGameController {
  /** @type {boolean} */
  #launching = false;
  /** @type {string | null} */
  #errorMessage = null;
  /** @type {string | null} */
  #successMessage = null;

  /** @param {ApplicationClient} client */
  constructor(client) {
    this.client = client;
    this.onChange = null;
  }

  get launching() {
    return this.#launching;
  }

  get errorMessage() {
    return this.#errorMessage;
  }

  get successMessage() {
    return this.#successMessage;
  }

  #notify() {
    this.onChange?.();
  }

  clearMessages() {
    this.#errorMessage = null;
    this.#successMessage = null;
    this.#notify();
  }

  /**
   * @param {boolean} canLaunch
   * @returns {Promise<boolean>}
   */
  async launchGame(canLaunch) {
    if (this.#launching) {
      return false;
    }

    this.#errorMessage = null;
    this.#successMessage = null;

    if (!canLaunch) {
      this.#errorMessage = t('home.launchError');
      this.#notify();
      return false;
    }

    this.#launching = true;
    this.#notify();

    try {
      const { result } = await this.client.invoke('launchGame');

      if (result?.success) {
        this.#successMessage = t('home.launchSuccess');
        return true;
      }

      this.#errorMessage = result?.errorMessage ?? t('home.launchError');
      return false;
    } catch (err) {
      this.#errorMessage = classifyError(err).message;
      return false;
    } finally {
      this.#launching = false;
      this.#notify();
    }
  }
}
