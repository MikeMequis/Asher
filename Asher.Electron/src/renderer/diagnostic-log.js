/**
 * Renderer-side diagnostic logging forwarded to the main process log file.
 * @param {'info' | 'warn' | 'error'} level
 * @param {string} source
 * @param {string} message
 * @param {unknown} [data]
 */
export function logDiagnostic(level, source, message, data) {
  const api = window.asher;
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
export async function showDiagnosticLogPath(client) {
  const pathEl = document.getElementById('diagnostic-log-path');
  if (!pathEl) {
    return;
  }

  try {
    const logPath = await client.getLogPath();
    pathEl.textContent = logPath
      ? `Log file: ${logPath}`
      : 'Log file: unavailable until a game folder is configured';
  } catch (err) {
    pathEl.textContent = `Diagnostic log: ${err instanceof Error ? err.message : String(err)}`;
  }
}
