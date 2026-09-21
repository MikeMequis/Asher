/**
 * Platform capability + install-state contract checks (no GUI).
 * Run: node scripts/platform-smoke-test.mjs
 */
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { HostManager } from '../src/main/host-manager.js';
import {
  isLinux,
  isWindows,
  normalizePlatform,
  supportsRecoveryHelper,
  usesLauncherSwap
} from '../src/renderer/platform.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

let failures = 0;

function fail(message) {
  console.error(`[FAIL] ${message}`);
  failures++;
}

function pass(message) {
  console.error(`[OK] ${message}`);
}

const linuxDto = {
  kind: 'linux',
  usesLauncherSwap: false,
  supportsRecoveryHelper: false,
  bootstrapLibraryName: 'libasher_bootstrap.so'
};
const windowsDto = {
  kind: 'windows',
  usesLauncherSwap: true,
  supportsRecoveryHelper: true,
  bootstrapLibraryName: ''
};

if (isLinux(linuxDto) && !usesLauncherSwap(linuxDto) && !supportsRecoveryHelper(linuxDto)) {
  pass('linux capability model');
} else {
  fail('linux capability model');
}

if (isWindows(windowsDto) && usesLauncherSwap(windowsDto) && supportsRecoveryHelper(windowsDto)) {
  pass('windows capability model');
} else {
  fail('windows capability model');
}

const unknown = normalizePlatform(null);
if (!isLinux(unknown) && !isWindows(unknown) && !supportsRecoveryHelper(unknown)) {
  pass('unknown platform normalizes safely');
} else {
  fail('unknown platform normalizes safely');
}

const rendererDir = path.join(__dirname, '..', 'src', 'renderer');
const offenders = fs
  .readdirSync(rendererDir)
  .filter((name) => name.endsWith('.js'))
  .filter((name) => fs.readFileSync(path.join(rendererDir, name), 'utf8').includes('hasRestorableBackup'));

if (offenders.length === 0) {
  pass('renderer does not derive capabilities from hasRestorableBackup');
} else {
  fail(`renderer still references hasRestorableBackup: ${offenders.join(', ')}`);
}

const host = new HostManager();
try {
  await host.start();
} catch (err) {
  fail(`host start: ${err.message}`);
  process.exit(1);
}

try {
  const platform = await host.client.request('getPlatformInfo');
  if (platform && (platform.kind === 'windows' || platform.kind === 'linux')) {
    pass(`getPlatformInfo kind=${platform.kind}`);
  } else {
    fail('getPlatformInfo shape');
  }

  if (platform.supportsRecoveryHelper === (platform.kind === 'windows')) {
    pass('supportsRecoveryHelper matches platform kind');
  } else {
    fail('supportsRecoveryHelper does not match platform kind');
  }

  if (platform.usesLauncherSwap === (platform.kind === 'windows')) {
    pass('usesLauncherSwap matches platform kind');
  } else {
    fail('usesLauncherSwap does not match platform kind');
  }

  if (Boolean(platform.bootstrapLibraryName) === (platform.kind === 'linux')) {
    pass('bootstrapLibraryName matches platform kind');
  } else {
    fail('bootstrapLibraryName does not match platform kind');
  }

  const probePath = path.join(os.tmpdir(), `asher-platform-smoke-${Date.now()}`);
  const state = await host.client.request('getInstallState', { gameFolderPath: probePath });

  if (
    state &&
    state.state === 'notInstalled' &&
    state.canUninstall === false &&
    state.canRestore === false &&
    state.marker === ''
  ) {
    pass('getInstallState notInstalled shape');
  } else {
    fail(`getInstallState unexpected shape: ${JSON.stringify(state)}`);
  }

  if (!platform.usesLauncherSwap && state.canRestore === false) {
    pass('canRestore false without launcher swap');
  } else if (platform.usesLauncherSwap) {
    pass('canRestore gated on launcher swap (windows)');
  } else {
    fail('canRestore set without launcher swap');
  }
} finally {
  await host.stop();
}

console.error(failures === 0 ? '[platform-smoke] all checks passed' : `[platform-smoke] ${failures} failure(s)`);
process.exit(failures === 0 ? 0 : 1);
