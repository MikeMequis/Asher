import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { writeDiagnosticLog } from './diagnostic-logger.js';

/**
 * Escape a single-quoted PowerShell string literal.
 * @param {string} value
 */
function escapePsSingleQuoted(value) {
  return value.replace(/'/g, "''");
}

/**
 * Run a hidden PowerShell script after this process exits.
 * @param {string} scriptBody PowerShell statements (after wait loop)
 * @param {{ label?: string }} [options]
 */
function spawnHiddenPostQuitPowerShell(scriptBody, options = {}) {
  const label = options.label ?? 'asher-post-quit';
  const scriptPath = path.join(os.tmpdir(), `${label}-${process.pid}-${Date.now()}.ps1`);
  const pid = process.pid;

  const fullScript = [
    `$ErrorActionPreference = 'SilentlyContinue'`,
    `while (Get-Process -Id ${pid} -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 400 }`,
    scriptBody,
    `Remove-Item -LiteralPath '${escapePsSingleQuoted(scriptPath)}' -Force -ErrorAction SilentlyContinue`
  ].join('\r\n');

  fs.writeFileSync(scriptPath, fullScript, 'utf8');

  const child = spawn(
    'powershell.exe',
    ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', scriptPath],
    {
      detached: true,
      stdio: 'ignore',
      windowsHide: true
    }
  );
  child.unref();

  writeDiagnosticLog('info', 'post-quit', 'spawned hidden PowerShell helper', { scriptPath, label });
  return scriptPath;
}

/**
 * After quit: replace dest with source folder contents, then relaunch (Distribution updates).
 * @param {string} sourceDir unpacked update payload
 * @param {string} destDir app install folder
 * @param {string} appExePath
 */
export function scheduleReplaceAndRelaunch(sourceDir, destDir, appExePath) {
  const src = escapePsSingleQuoted(sourceDir);
  const dest = escapePsSingleQuoted(destDir);
  const exe = escapePsSingleQuoted(appExePath);
  const cwd = escapePsSingleQuoted(path.dirname(appExePath));

  spawnHiddenPostQuitPowerShell(
    [
      `New-Item -ItemType Directory -Force -Path '${dest}' | Out-Null`,
      `robocopy '${src}' '${dest}' /E /R:2 /W:1 /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null`,
      `Remove-Item -LiteralPath '${src}' -Recurse -Force -ErrorAction SilentlyContinue`,
      `Start-Process -FilePath '${exe}' -WorkingDirectory '${cwd}'`
    ].join('\r\n'),
    { label: 'asher-update-apply' }
  );
}
