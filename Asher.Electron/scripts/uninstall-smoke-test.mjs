/**
 * Headless smoke test for uninstallation flow (no GUI).
 * Run: node scripts/uninstall-smoke-test.mjs
 */
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

  let requestId = null;
  let progressCount = 0;

  try {
    const result = await client.request(
      'uninstall',
      { gameFolderPath: 'C:\\nonexistent\\asher-uninstall-smoke' },
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
      fail('uninstall should emit requestId via onStarted');
    } else {
      pass(`uninstall requestId (${requestId})`);
    }

    const failed =
      result?.success === false ||
      (result && typeof result === 'object' && result.success === false);

    if (!failed) {
      fail('uninstall with invalid path should fail');
    } else {
      pass('uninstall failure path');
    }

    if (progressCount > 0) {
      pass(`uninstall progress (${progressCount} events)`);
    } else {
      console.error('[WARN] uninstall progress: no events (operation may have failed before reporting)');
    }
  } catch (err) {
    fail(`uninstall: ${err.message}`);
  }

  try {
    const installed = await client.request('isGameInstalled', {
      gameFolderPath: 'C:\\nonexistent\\asher-uninstall-smoke'
    });
    if (installed?.installed) {
      fail('isGameInstalled should be false for invalid path');
    } else {
      pass('isGameInstalled check');
    }
  } catch (err) {
    fail(`isGameInstalled: ${err.message}`);
  }

  try {
    const backup = await client.request('hasRestorableBackup', {
      gameFolderPath: 'C:\\nonexistent\\asher-uninstall-smoke'
    });
    if (backup?.hasBackup) {
      fail('hasRestorableBackup should be false for invalid path');
    } else {
      pass('hasRestorableBackup check');
    }
  } catch (err) {
    fail(`hasRestorableBackup: ${err.message}`);
  }

  await host.stop();
  pass('host shutdown');

  console.error(failures === 0 ? '[uninstall-smoke] all checks passed' : `[uninstall-smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[uninstall-smoke] fatal: ${err.message}`);
  process.exit(1);
});
