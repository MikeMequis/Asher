/**
 * Headless smoke test for installation flow (no GUI).
 * Run: node scripts/install-smoke-test.mjs
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

  const tempPath = path.join(os.tmpdir(), `asher-install-smoke-${Date.now()}`);
  let requestId = null;
  let progressCount = 0;

  try {
    const result = await client.request(
      'install',
      {
        path: tempPath,
        version: 'test',
        isValid: false,
        source: 'install-smoke'
      },
      {
        allowFailure: true,
        onStarted: (id) => {
          requestId = id;
        },
        onProgress: () => {
          progressCount++;
        }
      }
    );

    if (!requestId) {
      fail('install should emit requestId via onStarted');
    } else {
      pass(`install requestId (${requestId})`);
    }

    const failed =
      result?.success === false ||
      (result && typeof result === 'object' && result.success === false);

    if (!failed) {
      fail('install with invalid game info should fail');
    } else {
      pass('install failure path');
    }

    if (progressCount > 0) {
      pass(`install progress (${progressCount} events)`);
    } else {
      console.error('[WARN] install progress: no events (operation may have failed before reporting)');
    }
  } catch (err) {
    fail(`install: ${err.message}`);
  }

  try {
    const cancelResult = await client.request(
      'cancel',
      { targetRequestId: 'nonexistent-install-request' },
      { allowFailure: true }
    );

    if (cancelResult?.error?.code === 'not_found' || cancelResult?.success === false) {
      pass('cancel not_found path');
    } else {
      fail('cancel should return not_found for missing operation');
    }
  } catch (err) {
    fail(`cancel: ${err.message}`);
  }

  await host.stop();
  pass('host shutdown');

  console.error(failures === 0 ? '[install-smoke] all checks passed' : `[install-smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[install-smoke] fatal: ${err.message}`);
  process.exit(1);
});
