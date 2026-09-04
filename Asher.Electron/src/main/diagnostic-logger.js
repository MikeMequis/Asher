import fs from 'node:fs';
import path from 'node:path';
import {
  buildManagerLogFilePath,
  resolveGameLogsDir
} from './log-path-resolver.js';

/** @type {string | null} */
let logFilePath = null;

/**
 * @param {string} filePath
 */
export function setDiagnosticLogPath(filePath) {
  logFilePath = filePath;
}

/**
 * @param {unknown} data
 * @returns {string}
 */
function formatData(data) {
  if (data === undefined || data === null) {
    return '';
  }

  if (typeof data === 'string') {
    return data;
  }

  try {
    const serialized = JSON.stringify(data);
    return serialized === '{}' ? '' : serialized;
  } catch {
    return String(data);
  }
}

/**
 * @param {'info' | 'warn' | 'error'} level
 * @param {string} source
 * @param {string} message
 * @param {unknown} [data]
 */
export function writeDiagnosticLog(level, source, message, data) {
  const ts = new Date().toISOString().replace('T', ' ').slice(0, 19);
  const levelLabel = level.toUpperCase().padEnd(5);
  const extra = formatData(data);
  const line = extra
    ? `[${ts}] [${levelLabel}] [${source}] ${message} | ${extra}\n`
    : `[${ts}] [${levelLabel}] [${source}] ${message}\n`;

  const prefix = `[asher][${source}] ${message}`;
  if (level === 'error') {
    console.error(prefix, extra || '');
  } else if (level === 'warn') {
    console.warn(prefix, extra || '');
  } else {
    console.log(prefix, extra || '');
  }

  if (!logFilePath) {
    return;
  }

  try {
    fs.mkdirSync(path.dirname(logFilePath), { recursive: true });
    fs.appendFileSync(logFilePath, line, 'utf8');
  } catch {
    // Console remains available if file logging fails.
  }
}

export function getDiagnosticLogPath() {
  return logFilePath;
}

/**
 * Point file logging at the game's Asher/AsherLogs folder.
 * @param {string | null | undefined} gameFolderPath
 * @returns {string | null}
 */
export function relocateDiagnosticLogger(gameFolderPath) {
  const logsDir = resolveGameLogsDir(gameFolderPath);
  if (!logsDir) {
    return logFilePath;
  }

  fs.mkdirSync(logsDir, { recursive: true });

  const resolvedLogsDir = path.resolve(logsDir);
  if (logFilePath && path.resolve(logFilePath).startsWith(resolvedLogsDir)) {
    return logFilePath;
  }

  const previousPath = logFilePath;
  const nextPath = buildManagerLogFilePath(logsDir);
  setDiagnosticLogPath(nextPath);

  writeDiagnosticLog('info', 'main', 'Manager log file ready', {
    path: nextPath,
    previousPath: previousPath ?? undefined
  });

  return nextPath;
}

/**
 * Initialize manager logging under the configured game folder when available.
 * @returns {string | null}
 */
export function initDiagnosticLogger() {
  const logPath = relocateDiagnosticLogger(null);
  if (logPath) {
    return logPath;
  }

  writeDiagnosticLog(
    'warn',
    'main',
    'No game folder configured yet; file logging disabled until setup completes'
  );
  return null;
}
