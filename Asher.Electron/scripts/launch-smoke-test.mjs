/**
 * Headless smoke test for launch preconditions (no GUI).
 * Run: node scripts/launch-smoke-test.mjs
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

  try {
    const resolved = await client.request('resolveGameFolderPath');
    if (resolved && typeof resolved === 'object') {
      pass(`resolveGameFolderPath contract (path=${resolved.path ?? 'none'})`);
    } else {
      fail('resolveGameFolderPath contract');
    }

    const installed = await client.request('isGameInstalled');
    if (typeof installed?.installed !== 'boolean') {
      fail('isGameInstalled contract');
    } else {
      pass('isGameInstalled contract');
    }

    if (installed?.installed) {
      pass('launchGame not exercised (Asher installed)');
    } else {
      try {
        await client.request('launchGame');
        fail('launchGame should reject when no launchable game is configured');
      } catch (err) {
        if (!err.message) {
          fail('launchGame failure should include a message');
        } else {
          pass(`launchGame failure path (${err.message})`);
        }
      }
    }
  } catch (err) {
    fail(`launch: ${err.message}`);
  }

  await host.stop();
  pass('host shutdown');

  console.error(failures === 0 ? '[launch-smoke] all checks passed' : `[launch-smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[launch-smoke] fatal: ${err.message}`);
  process.exit(1);
});
