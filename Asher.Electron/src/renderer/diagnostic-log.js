/**
 * Renderer-side diagnostic logging forwarded to the main process log file.
 */
import { t } from './localization.js';

/**
 * @param {'info' | 'warn' | 'error'} level
 * @param {string} source
 * @param {string} message
 * @param {unknown} [data]
 */
export function logDiagnostic(level, source, message, data) {
  const api = typeof window !== 'undefined' ? window.asher : undefined;
  if (api?.log) {
    void api.log(level, source, message, data);
    return;
  }

  const prefix = `[asher-electron][${source}] ${message}`;
  if (level === 'error') {
    console.error(prefix, data ?? '');
  } else if (level === 'warn') {
    console.warn(prefix, data ?? '');
  } else {
    console.log(prefix, data ?? '');
  }
}

/**
 * @param {import('./application-client.js').ApplicationClient} client
 */
export async function refreshDiagnosticLogFooter(client) {
  const pathEl = document.getElementById('diagnostic-log-path');
  if (!pathEl) {
    return null;
  }

  try {
    const logPath = await client.getLogPath();
    pathEl.textContent = logPath
      ? t('common.logFile', { path: logPath })
      : t('settings.logUnavailable');
    return logPath;
  } catch (err) {
    pathEl.textContent = t('settings.logError', {
      message: err instanceof Error ? err.message : String(err)
    });
    return null;
  }
}

/** @deprecated Use refreshDiagnosticLogFooter */
export async function showDiagnosticLogPath(client) {
  return refreshDiagnosticLogFooter(client);
}
