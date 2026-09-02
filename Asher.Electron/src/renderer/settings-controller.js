import { classifyError } from './errors.js';
import { normalizeLanguage } from './localization.js';
import { normalizeTheme } from './theme.js';

/** @typedef {'idle' | 'loading' | 'dirty' | 'saving' | 'saved' | 'error'} SettingsState */
/** @typedef {import('./application-client.js').ApplicationClient} ApplicationClient */

const DEFAULT_SETTINGS = {
  gameFolderPath: '',
  isInstalled: false,
  installationDate: null,
  gameVersion: '',
  firstRun: true,
  language: 'en-US',
  autoLaunchEnabled: true,
  backupEnabled: true,
  theme: 'Light',
  checkForUpdatesEnabled: true
};

/**
 * Settings screen — load, edit, save, and reset preferences.
 */
export class SettingsController {
  /** @type {SettingsState} */
  #state = 'idle';
  /** @type {object} */
  #draft = { ...DEFAULT_SETTINGS };
  /** @type {object | null} */
  #validatedFolder = null;
  /** @type {string | null} */
  #errorMessage = null;
  /** @type {string | null} */
  #statusMessage = null;
  /** @type {'idle' | 'validating' | 'valid' | 'invalid'} */
  #pathState = 'idle';

  /** @param {ApplicationClient} client */
  constructor(client) {
    this.client = client;
    this.onChange = null;
  }

  get state() {
    return this.#state;
  }

  get draft() {
    return this.#draft;
  }

  get errorMessage() {
    return this.#errorMessage;
  }

  get statusMessage() {
    return this.#statusMessage;
  }

  get pathState() {
    return this.#pathState;
  }

  get isDirty() {
    return this.#state === 'dirty';
  }

  #notify() {
    this.onChange?.();
  }

  #setState(state) {
    this.#state = state;
    this.#notify();
  }

  async loadFromHost() {
    this.#errorMessage = null;
    this.#statusMessage = null;
    this.#setState('loading');

    try {
      const { result: settings } = await this.client.invoke('getSettings');
      this.#draft = { ...DEFAULT_SETTINGS, ...settings };
      this.#validatedFolder = null;
      this.#pathState = 'idle';
      this.#setState('idle');
    } catch (err) {
      this.#errorMessage = classifyError(err).message;
      this.#setState('error');
    }
  }

  /**
   * @param {Partial<typeof DEFAULT_SETTINGS>} changes
   */
  updateDraft(changes) {
    this.#draft = { ...this.#draft, ...changes };
    this.#errorMessage = null;
    this.#statusMessage = null;
    this.#setState('dirty');
  }

  resetDraft() {
    this.#draft = { ...DEFAULT_SETTINGS };
    this.#validatedFolder = null;
    this.#pathState = 'idle';
    this.#errorMessage = null;
    this.#statusMessage = null;
    this.#setState('dirty');
  }

  async browsePath() {
    const selected = await this.client.pickFolder();
    if (!selected) {
      return;
    }

    this.updateDraft({ gameFolderPath: selected });
    await this.validatePath(selected);
  }

  /**
   * @param {string} folderPath
   */
  async validatePath(folderPath) {
    const trimmed = folderPath?.trim() ?? '';
    if (!trimmed) {
      this.#validatedFolder = null;
      this.#pathState = 'idle';
      this.#notify();
      return;
    }

    this.#pathState = 'validating';
    this.#notify();

    try {
      const { result } = await this.client.invoke('getGameFolderInfo', { folderPath: trimmed });
      this.#validatedFolder = result ?? null;
      this.#pathState = result?.isValid ? 'valid' : 'invalid';
      if (result?.isValid) {
        this.updateDraft({
          gameFolderPath: result.path,
          gameVersion: result.version ?? this.#draft.gameVersion
        });
        this.#setState('dirty');
      } else {
        this.#notify();
      }
    } catch (err) {
      this.#errorMessage = classifyError(err).message;
      this.#pathState = 'invalid';
      this.#notify();
    }
  }

  /**
   * @returns {Promise<boolean>}
   */
  async save() {
    if (this.#state === 'saving') {
      return false;
    }

    const trimmedPath = this.#draft.gameFolderPath?.trim() ?? '';
    if (trimmedPath && this.#pathState === 'invalid') {
      this.#errorMessage = 'Select a valid game folder before saving.';
      this.#setState('error');
      return false;
    }

    this.#errorMessage = null;
    this.#statusMessage = null;
    this.#setState('saving');

    try {
      const payload = {
        ...this.#draft,
        gameFolderPath: trimmedPath,
        language: normalizeLanguage(this.#draft.language),
        theme: normalizeTheme(this.#draft.theme)
      };

      await this.client.invoke('saveSettings', payload);
      this.#draft = payload;
      this.#setState('saved');
      return true;
    } catch (err) {
      this.#errorMessage = classifyError(err).message;
      this.#setState('error');
      return false;
    }
  }
}

export { DEFAULT_SETTINGS };
