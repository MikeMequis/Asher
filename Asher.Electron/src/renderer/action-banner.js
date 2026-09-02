/**
 * Lightweight global action banner for success/error feedback across screens.
 */

/** @type {HTMLElement | null} */
let bannerEl = null;
/** @type {ReturnType<typeof setTimeout> | null} */
let hideTimer = null;

/**
 * @param {HTMLElement | null} element
 */
export function bindActionBanner(element) {
  bannerEl = element;
}

/**
 * @param {'success' | 'error' | 'info'} kind
 * @param {string} message
 * @param {{ details?: string | null, timeoutMs?: number }} [options]
 */
export function showActionBanner(kind, message, options = {}) {
  if (!bannerEl || !message) {
    return;
  }

  if (hideTimer) {
    clearTimeout(hideTimer);
    hideTimer = null;
  }

  bannerEl.hidden = false;
  bannerEl.className = `action-banner action-banner-${kind}`;
  bannerEl.setAttribute('role', kind === 'error' ? 'alert' : 'status');

  const details = options.details?.trim();
  bannerEl.innerHTML = '';

  const text = document.createElement('p');
  text.className = 'action-banner-message';
  text.textContent = message;
  bannerEl.appendChild(text);

  if (details) {
    const detailEl = document.createElement('p');
    detailEl.className = 'action-banner-details';
    detailEl.textContent = details;
    bannerEl.appendChild(detailEl);
  }

  const timeoutMs = options.timeoutMs ?? (kind === 'error' ? 8000 : 4500);
  if (timeoutMs > 0) {
    hideTimer = setTimeout(() => {
      hideActionBanner();
    }, timeoutMs);
  }
}

export function hideActionBanner() {
  if (hideTimer) {
    clearTimeout(hideTimer);
    hideTimer = null;
  }

  if (!bannerEl) {
    return;
  }

  bannerEl.hidden = true;
  bannerEl.textContent = '';
  bannerEl.className = 'action-banner';
}
