/**
 * Material Symbols Outlined helpers (self-hosted font).
 * @param {string} name
 * @param {{ className?: string }} [options]
 * @returns {HTMLSpanElement}
 */
export function icon(name, options = {}) {
  const el = document.createElement('span');
  el.className = options.className
    ? `material-symbols-outlined ${options.className}`
    : 'material-symbols-outlined';
  el.setAttribute('aria-hidden', 'true');
  el.textContent = name;
  return el;
}

/**
 * @param {string} name
 * @param {string} [className]
 * @returns {string}
 */
export function iconHtml(name, className = '') {
  const cls = className ? `material-symbols-outlined ${className}` : 'material-symbols-outlined';
  return `<span class="${cls}" aria-hidden="true">${name}</span>`;
}

/** @type {Record<string, string>} */
export const NAV_ICONS = {
  home: 'home',
  manager: 'build',
  settings: 'settings',
  welcome: 'star',
  gameDetection: 'search',
  installing: 'sync',
  complete: 'check_circle'
};

/**
 * Replace [data-icon] placeholders with Material Symbols.
 * @param {ParentNode} [root]
 */
export function applyDataIcons(root = document) {
  root.querySelectorAll('[data-icon]').forEach((el) => {
    const name = el.getAttribute('data-icon');
    if (!name) {
      return;
    }

    el.textContent = name;
    el.classList.add('material-symbols-outlined');
    el.setAttribute('aria-hidden', 'true');
  });
}
