/**
 * Headless smoke test for launch game flow (no GUI).
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
  let rejected = false;
  try {
    await client.request('launchGame');
    fail('launchGame should fail without installed game');
  } catch (err) {
    rejected = true;
    if (!err.message) {
      fail('launchGame failure should include a message');
    } else {
      pass(`launchGame failure path (${err.message})`);
    }
  }

  if (!rejected) {
    fail('launchGame did not reject');
  }
  } catch (err) {
    fail(`launchGame: ${err.message}`);
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
