/**
 * Toast notifications stacked in the top-right corner.
 */

import { t } from './localization.js';

/** @type {HTMLElement | null} */
let hostEl = null;

/**
 * @param {HTMLElement | null} element
 */
export function bindActionBanner(element) {
  hostEl = element;
}

/**
 * @param {'success' | 'error' | 'info'} kind
 * @param {string} message
 * @param {{ details?: string | null, timeoutMs?: number }} [options]
 */
export function showActionBanner(kind, message, options = {}) {
  if (!hostEl || !message) {
    return;
  }

  const toast = document.createElement('div');
  toast.className = `action-banner action-banner-${kind}`;
  toast.setAttribute('role', kind === 'error' ? 'alert' : 'status');

  const details = options.details?.trim();

  const text = document.createElement('p');
  text.className = 'action-banner-message';
  text.textContent = message;
  toast.appendChild(text);

  if (details) {
    const detailEl = document.createElement('p');
    detailEl.className = 'action-banner-details';
    detailEl.textContent = details;
    toast.appendChild(detailEl);
  }

  const dismissButton = document.createElement('button');
  dismissButton.type = 'button';
  dismissButton.className = 'action-banner-dismiss';
  dismissButton.setAttribute('aria-label', t('common.dismiss'));
  dismissButton.textContent = '×';
  dismissButton.addEventListener('click', () => removeToast(toast));
  toast.appendChild(dismissButton);

  hostEl.appendChild(toast);

  const timeoutMs = options.timeoutMs ?? (kind === 'error' ? 8000 : 4500);
  if (timeoutMs > 0) {
    setTimeout(() => {
      removeToast(toast);
    }, timeoutMs);
  }
}

/**
 * @param {HTMLElement} toast
 */
function removeToast(toast) {
  if (!toast.isConnected) {
    return;
  }

  toast.classList.add('action-banner-exit');
  toast.addEventListener(
    'animationend',
    () => {
      toast.remove();
    },
    { once: true }
  );
}

export function hideActionBanner() {
  if (!hostEl) {
    return;
  }

  hostEl.querySelectorAll('.action-banner').forEach((toast) => {
    removeToast(/** @type {HTMLElement} */ (toast));
  });
}
