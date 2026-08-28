import fs from 'node:fs';
import path from 'node:path';

/** @type {string | null} */
let logFilePath = null;

/**
 * @param {string} filePath
 */
export function setDiagnosticLogPath(filePath) {
  logFilePath = filePath;
}

/**
 * @param {'info' | 'warn' | 'error'} level
 * @param {string} source
 * @param {string} message
 * @param {unknown} [data]
 */
export function writeDiagnosticLog(level, source, message, data) {
  const entry = {
    ts: new Date().toISOString(),
    level,
    source,
    message,
    data: data ?? undefined
  };

  const line = `${JSON.stringify(entry)}\n`;
  const prefix = `[asher-electron][${source}] ${message}`;

  if (level === 'error') {
    console.error(prefix, data ?? '');
  } else if (level === 'warn') {
    console.warn(prefix, data ?? '');
  } else {
    console.log(prefix, data ?? '');
  }

  if (!logFilePath) {
    return;
  }

  try {
    fs.mkdirSync(path.dirname(logFilePath), { recursive: true });
    fs.appendFileSync(logFilePath, line, 'utf8');
  } catch {
    // Ignore file write failures; console remains available.
  }
}

export function getDiagnosticLogPath() {
  return logFilePath;
}

/**
 * Initialize file logging for the Electron main process.
 * @param {string} userDataDir
 */
export function initDiagnosticLogger(userDataDir) {
  const dir = userDataDir;
  fs.mkdirSync(dir, { recursive: true });
  const filePath = path.join(dir, 'asher-electron.log');
  setDiagnosticLogPath(filePath);

  writeDiagnosticLog('info', 'main', 'Diagnostic logger initialized', {
    logFilePath: filePath,
    cwd: process.cwd(),
    execPath: process.execPath,
    versions: process.versions
  });

  return filePath;
}
