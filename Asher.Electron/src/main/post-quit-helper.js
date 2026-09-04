import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { writeDiagnosticLog } from './diagnostic-logger.js';

/**
 * Escape a path for use inside a double-quoted cmd.exe string.
 * @param {string} value
 */
function escapeCmdPath(value) {
  return value.replace(/"/g, '""');
}

/**
 * Write and detach a .cmd that waits for this process to exit, then runs body lines.
 * @param {string[]} bodyLines cmd lines after the wait loop (no @echo off)
 * @param {{ label?: string }} [options]
 * @returns {string} path to the helper script
 */
export function spawnPostQuitHelper(bodyLines, options = {}) {
  const label = options.label ?? 'asher-post-quit';
  const scriptPath = path.join(os.tmpdir(), `${label}-${process.pid}-${Date.now()}.cmd`);
  const pid = process.pid;

  const lines = [
    '@echo off',
    'setlocal',
    `:wait_${pid}`,
    `tasklist /FI "PID eq ${pid}" 2>NUL | find "${pid}" >NUL`,
    'if not errorlevel 1 (',
    '  timeout /t 1 /nobreak >NUL',
    `  goto wait_${pid}`,
    ')',
    ...bodyLines,
    `del "%~f0" >NUL 2>&1`
  ];

  fs.writeFileSync(scriptPath, `${lines.join('\r\n')}\r\n`, 'utf8');

  const child = spawn('cmd.exe', ['/c', scriptPath], {
    detached: true,
    stdio: 'ignore',
    windowsHide: true
  });
  child.unref();

  writeDiagnosticLog('info', 'post-quit', 'spawned helper', { scriptPath, label });
  return scriptPath;
}

/**
 * After quit: start the installed manager executable.
 * @param {string} managerExePath
 */
export function scheduleRelaunch(managerExePath) {
  const exe = escapeCmdPath(managerExePath);
  const cwd = escapeCmdPath(path.dirname(managerExePath));
  spawnPostQuitHelper(
    [`start "" /D "${cwd}" "${exe}"`],
    { label: 'asher-relaunch' }
  );
}

/**
 * After quit: replace dest with source folder contents, then relaunch.
 * @param {string} sourceDir unpacked update payload
 * @param {string} destDir manager install folder
 * @param {string} managerExePath
 */
export function scheduleReplaceAndRelaunch(sourceDir, destDir, managerExePath) {
  const src = escapeCmdPath(sourceDir);
  const dest = escapeCmdPath(destDir);
  const exe = escapeCmdPath(managerExePath);
  const cwd = escapeCmdPath(path.dirname(managerExePath));

  spawnPostQuitHelper(
    [
      `if not exist "${dest}" mkdir "${dest}"`,
      `robocopy "${src}" "${dest}" /E /R:2 /W:1 /NFL /NDL /NJH /NJS /nc /ns /np`,
      `rmdir /s /q "${src}" >NUL 2>&1`,
      `start "" /D "${cwd}" "${exe}"`
    ],
    { label: 'asher-update-apply' }
  );
}

/**
 * After quit: delete the manager folder (self-uninstall cleanup).
 * @param {string} managerDir
 */
export function scheduleDeleteManagerFolder(managerDir) {
  const dest = escapeCmdPath(managerDir);
  spawnPostQuitHelper([`rmdir /s /q "${dest}" >NUL 2>&1`], { label: 'asher-self-uninstall' });
}
