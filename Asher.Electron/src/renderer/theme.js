/** @typedef {'Light' | 'Dark'} AppTheme */

/** @type {AppTheme} */
let currentTheme = 'Light';

/**
 * @param {string | null | undefined} theme
 * @returns {AppTheme}
 */
export function normalizeTheme(theme) {
  return theme === 'Dark' ? 'Dark' : 'Light';
}

/**
 * @param {AppTheme} theme
 */
export function applyTheme(theme) {
  currentTheme = theme;
  document.documentElement.dataset.theme = theme.toLowerCase();
}

/**
 * @returns {AppTheme}
 */
export function getTheme() {
  return currentTheme;
}

/**
 * Apply theme from persisted settings.
 * @param {object | null | undefined} settings
 */
export function applyThemeFromSettings(settings) {
  applyTheme(normalizeTheme(settings?.theme));
}
