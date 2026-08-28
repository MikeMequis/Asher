/**
 * Headless smoke test for Electron JSONL integration (no GUI).
 * Run: node scripts/smoke-test.mjs
 */
import os from 'node:os';
import path from 'node:path';
import { HostManager } from '../src/main/host-manager.js';

let failures = 0;

function fail(message) {
  console.error(`[FAIL] ${message}`);
  failures++;
}

function pass(message) {
  console.error(`[OK] ${message}`);
}

async function main() {
  const host = new HostManager();

  try {
    await host.start();
  } catch (err) {
    fail(`host start: ${err.message}`);
    process.exit(1);
  }

  if (host.status !== 'ready') {
    fail(`expected ready status, got ${host.status}`);
  } else {
    pass('host ready');
  }

  const client = host.client;
  if (!client) {
    fail('no jsonl client');
    process.exit(1);
  }

  try {
    const settings = await client.request('getSettings');
    if (!settings || typeof settings !== 'object') {
      fail('getSettings returned unexpected result');
    } else {
      pass('getSettings');
    }
  } catch (err) {
    fail(`getSettings: ${err.message}`);
  }

  try {
    const detection = await client.request('detectGameFolder');
    if (!detection || typeof detection !== 'object') {
      fail('detectGameFolder returned unexpected result');
    } else {
      pass('detectGameFolder');
    }
  } catch (err) {
    fail(`detectGameFolder: ${err.message}`);
  }

  try {
    const tempPath = path.join(os.tmpdir(), `asher-electron-smoke-${Date.now()}`);
    let progressCount = 0;

    const result = await client.request(
      'install',
      {
        path: tempPath,
        version: 'test',
        isValid: false,
        source: 'smoke-test',
        hasPatchesFolder: false,
        patchesFolderPath: ''
      },
      {
        allowFailure: true,
        onProgress: () => {
          progressCount++;
        }
      }
    );

    if (progressCount > 0) {
      pass(`install progress (${progressCount} events)`);
    } else {
      console.error('[WARN] install progress: no events (operation may have failed before reporting)');
    }

    if (result && typeof result === 'object' && ('result' in result || result.success === false)) {
      pass('install final response');
    } else {
      fail('install returned unexpected result');
    }
  } catch (err) {
    fail(`install probe: ${err.message}`);
  }

  try {
    await client.request('getApplicationMode');
    pass('second request after install (multiple requests)');
  } catch (err) {
    fail(`getApplicationMode: ${err.message}`);
  }

  await host.stop();

  if (host.status !== 'stopped') {
    fail(`expected stopped status after shutdown, got ${host.status}`);
  } else {
    pass('host shutdown');
  }

  console.error(failures === 0 ? '[smoke] all checks passed' : `[smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[smoke] fatal: ${err.message}`);
  process.exit(1);
});
