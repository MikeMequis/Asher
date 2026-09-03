import { classifyError } from './errors.js';
import { normalizeLanguage, t } from './localization.js';
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
  theme: 'Light'
};

/** Preference fields restored by "Reset to Defaults". */
const PREFERENCE_DEFAULTS = {
  language: DEFAULT_SETTINGS.language,
  backupEnabled: DEFAULT_SETTINGS.backupEnabled,
  theme: DEFAULT_SETTINGS.theme
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

  /**
   * @param {string | null} message
   * @param {{ state?: SettingsState }} [options]
   */
  setStatusMessage(message, options = {}) {
    this.#statusMessage = message;
    this.#errorMessage = null;
    if (options.state) {
      this.#setState(options.state);
    } else if (message) {
      this.#setState('saved');
    } else {
      this.#notify();
    }
  }

  clearStatusMessage() {
    this.#statusMessage = null;
    if (this.#state === 'saved') {
      this.#setState('idle');
    } else {
      this.#notify();
    }
  }

  /**
   * @param {{ keepStatus?: boolean }} [options]
   */
  async loadFromHost(options = {}) {
    const keepStatus = Boolean(options.keepStatus);
    const preservedStatus = keepStatus ? this.#statusMessage : null;
    const wasSaved = keepStatus && this.#state === 'saved';

    this.#errorMessage = null;
    if (!keepStatus) {
      this.#statusMessage = null;
    }
    this.#setState('loading');

    try {
      const { result: settings } = await this.client.invoke('getSettings');
      this.#draft = { ...DEFAULT_SETTINGS, ...settings };
      this.#validatedFolder = null;
      this.#pathState = 'idle';

      if (wasSaved && preservedStatus) {
        this.#statusMessage = preservedStatus;
        this.#setState('saved');
      } else {
        this.#statusMessage = null;
        this.#setState('idle');
      }
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
    this.#draft = {
      ...this.#draft,
      ...PREFERENCE_DEFAULTS
    };
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
   * @param {string} [statusMessage]
   * @returns {Promise<boolean>}
   */
  async save(statusMessage) {
    if (this.#state === 'saving') {
      return false;
    }

    const trimmedPath = this.#draft.gameFolderPath?.trim() ?? '';
    if (trimmedPath && this.#pathState === 'invalid') {
      this.#errorMessage = t('settings.invalidPathSave');
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
      this.#statusMessage = statusMessage ?? null;
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
