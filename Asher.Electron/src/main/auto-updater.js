import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { createWriteStream } from 'node:fs';
import { Readable } from 'node:stream';
import { pipeline } from 'node:stream/promises';
import { app, shell } from 'electron';
import { writeDiagnosticLog } from './diagnostic-logger.js';
import { getAppInstallRoot, getManagerExecutablePath, isRunningFromGameManager, isRunningFromInstalledManager } from './manager-paths.js';
import { scheduleReplaceAndRelaunch } from './post-quit-helper.js';

const GITHUB_OWNER = 'MikeMequis';
const GITHUB_REPO = 'Asher';
const RELEASES_LATEST = `https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPO}/releases/latest`;

/** @type {((channel: string, payload: unknown) => boolean) | null} */
let broadcast = null;

/**
 * @param {(channel: string, payload: unknown) => boolean} broadcastFn
 */
export function initAutoUpdater(broadcastFn) {
  broadcast = broadcastFn;

  if (!app.isPackaged) {
    writeDiagnosticLog('info', 'updater', 'disabled (unpackaged)');
    return;
  }

  setTimeout(() => {
    void checkForUpdates({ silent: true }).catch((err) => {
      writeDiagnosticLog('warn', 'updater', 'background check failed', {
        error: err instanceof Error ? err.message : String(err)
      });
    });
  }, 8_000);
}

function emit(status, extra = {}) {
  const payload = { status, ...extra };
  writeDiagnosticLog('info', 'updater', status, extra);
  if (broadcast) {
    broadcast('updater:status', payload);
  }
  return payload;
}

function compareSemver(a, b) {
  const pa = String(a).replace(/^v/i, '').split('.').map((n) => Number.parseInt(n, 10) || 0);
  const pb = String(b).replace(/^v/i, '').split('.').map((n) => Number.parseInt(n, 10) || 0);
  const len = Math.max(pa.length, pb.length);
  for (let i = 0; i < len; i += 1) {
    const d = (pa[i] ?? 0) - (pb[i] ?? 0);
    if (d !== 0) {
      return d > 0 ? 1 : -1;
    }
  }
  return 0;
}

/**
 * @param {{ name?: string, browser_download_url?: string }} asset
 */
function isWindowsZipAsset(asset) {
  const name = asset?.name?.toLowerCase() ?? '';
  return name.endsWith('.zip') && (name.includes('win') || name.includes('ia32') || name.includes('asher'));
}

async function fetchLatestRelease() {
  const response = await fetch(RELEASES_LATEST, {
    headers: {
      Accept: 'application/vnd.github+json',
      'User-Agent': 'Asher-Manager'
    }
  });

  if (!response.ok) {
    throw new Error(`GitHub releases request failed (${response.status})`);
  }

  return response.json();
}

/**
 * @param {string} url
 * @param {string} destPath
 */
async function downloadFile(url, destPath) {
  const response = await fetch(url, {
    headers: { 'User-Agent': 'Asher-Manager' },
    redirect: 'follow'
  });

  if (!response.ok || !response.body) {
    throw new Error(`Download failed (${response.status})`);
  }

  const nodeStream = Readable.fromWeb(/** @type {any} */ (response.body));
  await pipeline(nodeStream, createWriteStream(destPath));
}

/**
 * Extract a zip using PowerShell Expand-Archive (Windows).
 * @param {string} zipPath
 * @param {string} destDir
 */
async function extractZip(zipPath, destDir) {
  const { spawnSync } = await import('node:child_process');
  fs.mkdirSync(destDir, { recursive: true });
  const result = spawnSync(
    'powershell.exe',
    [
      '-NoProfile',
      '-Command',
      `Expand-Archive -LiteralPath '${zipPath.replace(/'/g, "''")}' -DestinationPath '${destDir.replace(/'/g, "''")}' -Force`
    ],
    { encoding: 'utf8' }
  );

  if (result.status !== 0) {
    throw new Error(result.stderr || result.stdout || 'Expand-Archive failed');
  }
}

/**
 * electron-builder zip often contains a single top-level folder (e.g. win-ia32-unpacked).
 * @param {string} extractRoot
 */
function resolveUnpackedAppDir(extractRoot) {
  const exeDirect = path.join(extractRoot, 'Asher.exe');
  if (fs.existsSync(exeDirect)) {
    return extractRoot;
  }

  const entries = fs.readdirSync(extractRoot, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory()) {
      continue;
    }
    const candidate = path.join(extractRoot, entry.name);
    if (fs.existsSync(path.join(candidate, 'Asher.exe'))) {
      return candidate;
    }
  }

  throw new Error('Downloaded update zip does not contain Asher.exe');
}

/**
 * @param {{ silent?: boolean, gameFolderPath?: string | null }} [options]
 */
export async function checkForUpdates(options = {}) {
  const silent = Boolean(options.silent);

  if (!app.isPackaged) {
    return emit('unavailable', { message: 'Updates are only available in packaged builds.', silent });
  }

  emit('checking', { silent });

  try {
    const release = await fetchLatestRelease();
    const remoteVersion = String(release.tag_name || release.name || '').replace(/^v/i, '');
    if (!remoteVersion) {
      throw new Error('Latest release has no version tag');
    }

    if (compareSemver(remoteVersion, app.getVersion()) <= 0) {
      return emit('up-to-date', { version: app.getVersion(), silent });
    }

    const assets = Array.isArray(release.assets) ? release.assets : [];
    const zipAsset =
      assets.find(isWindowsZipAsset) ?? assets.find((a) => a?.name?.toLowerCase().endsWith('.zip'));
    if (!zipAsset?.browser_download_url) {
      const releaseUrl =
        release.html_url || `https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}/releases/latest`;
      return emit('available-manual', {
        version: remoteVersion,
        releaseUrl,
        silent,
        message: 'A new version is available. Download the zip from GitHub Releases.'
      });
    }

    return emit('available', {
      version: remoteVersion,
      downloadUrl: zipAsset.browser_download_url,
      assetName: zipAsset.name,
      releaseUrl: release.html_url,
      canApplyInPlace:
        Boolean(options.gameFolderPath && isRunningFromGameManager(options.gameFolderPath)) ||
        isRunningFromInstalledManager(),
      silent
    });
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    return emit('error', { message, silent });
  }
}

/**
 * Download the zip update and schedule replace+relaunch when running from Asher.App.
 * @param {{ downloadUrl: string, gameFolderPath?: string }} params
 */
export async function downloadAndApplyUpdate(params) {
  const { downloadUrl, gameFolderPath } = params;

  if (!app.isPackaged) {
    return emit('error', { message: 'Updates are only available in packaged builds.' });
  }

  const canApply =
    (gameFolderPath && isRunningFromGameManager(gameFolderPath)) || isRunningFromInstalledManager();
  if (!canApply) {
    return emit('error', {
      message: 'In-place updates require running the manager from the game Asher.App folder.'
    });
  }

  if (!downloadUrl) {
    return emit('error', { message: 'Missing download URL.' });
  }

  emit('downloading');

  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'asher-update-'));
  const zipPath = path.join(tempRoot, 'update.zip');
  const extractDir = path.join(tempRoot, 'extracted');

  try {
    await downloadFile(downloadUrl, zipPath);
    emit('extracting');
    await extractZip(zipPath, extractDir);
    const unpacked = resolveUnpackedAppDir(extractDir);

    const destDir = getAppInstallRoot();
    const managerExe = getManagerExecutablePath(destDir);

    emit('ready-to-install');
    scheduleReplaceAndRelaunch(unpacked, destDir, managerExe);

    setImmediate(() => {
      app.quit();
    });

    return emit('installing', { message: 'Applying update and restarting…' });
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    try {
      fs.rmSync(tempRoot, { recursive: true, force: true });
    } catch {
      // best effort
    }
    return emit('error', { message });
  }
}

/**
 * Open the GitHub releases page in the default browser.
 * @param {string} [url]
 */
export async function openReleasePage(url) {
  const href =
    typeof url === 'string' && url.startsWith(`https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}/`)
      ? url
      : `https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}/releases/latest`;
  await shell.openExternal(href);
  return { ok: true };
}
