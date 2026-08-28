/**
 * Headless smoke test for game setup flow (no GUI).
 * Run: node scripts/setup-smoke-test.mjs
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

  const client = host.client;
  if (!client) {
    fail('no jsonl client');
    process.exit(1);
  }

  let originalSettings;
  try {
    originalSettings = await client.request('getSettings');
    pass('getSettings');
  } catch (err) {
    fail(`getSettings: ${err.message}`);
  }

  try {
    const mode = await client.request('getApplicationMode');
    if (!mode || typeof mode.mode !== 'string') {
      fail('getApplicationMode returned unexpected shape');
    } else {
      pass(`getApplicationMode (${mode.mode})`);
    }
  } catch (err) {
    fail(`getApplicationMode: ${err.message}`);
  }

  try {
    const invalidPath = path.join(os.tmpdir(), 'asher-invalid-game-folder');
    const info = await client.request('getGameFolderInfo', { folderPath: invalidPath });
    if (info?.isValid) {
      fail('getGameFolderInfo should reject invalid folder');
    } else {
      pass('getGameFolderInfo invalid folder');
    }
  } catch (err) {
    fail(`getGameFolderInfo: ${err.message}`);
  }

  try {
    const probePath = path.join(os.tmpdir(), `asher-setup-persist-${Date.now()}`);
    const updated = {
      ...originalSettings,
      gameFolderPath: probePath,
      gameVersion: 'smoke-test'
    };

    await client.request('saveSettings', updated);
    const loaded = await client.request('getSettings');

    if (loaded?.gameFolderPath !== probePath) {
      fail('saveSettings persistence mismatch');
    } else {
      pass('saveSettings persistence');
    }

    await client.request('saveSettings', originalSettings);
    const restored = await client.request('getSettings');
    if (restored?.gameFolderPath !== originalSettings.gameFolderPath) {
      fail('settings restore failed');
    } else {
      pass('settings restore');
    }
  } catch (err) {
    fail(`settings persistence: ${err.message}`);
  }

  try {
    const detection = await client.request('detectGameFolder');
    if (!detection || typeof detection !== 'object') {
      fail('detectGameFolder returned unexpected result');
    } else {
      pass(`detectGameFolder (valid=${detection.isValid})`);
    }
  } catch (err) {
    fail(`detectGameFolder: ${err.message}`);
  }

  await host.stop();
  pass('host shutdown');

  console.error(failures === 0 ? '[setup-smoke] all checks passed' : `[setup-smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[setup-smoke] fatal: ${err.message}`);
  process.exit(1);
});
