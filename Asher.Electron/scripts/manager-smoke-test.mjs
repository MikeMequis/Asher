/**
 * Headless smoke test for Mod Manager flow (no GUI).
 * Run: node scripts/manager-smoke-test.mjs
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
    const mods = await client.request('getMods');
    if (!Array.isArray(mods)) {
      fail('getMods should return an array');
    } else {
      pass(`getMods (${mods.length} mods)`);
    }
  } catch (err) {
    fail(`getMods: ${err.message}`);
  }

  try {
    const result = await client.request('setModEnabled', {
      fileName: 'nonexistent-mod.dll',
      enabled: false
    });

    if (result?.success) {
      fail('setModEnabled should fail for nonexistent mod');
    } else {
      pass('setModEnabled failure path');
    }
  } catch (err) {
    fail(`setModEnabled: ${err.message}`);
  }

  try {
    const modsAfterFailure = await client.request('getMods');
    if (!Array.isArray(modsAfterFailure)) {
      fail('getMods after toggle failure should still return array');
    } else {
      pass('getMods after failed toggle');
    }
  } catch (err) {
    fail(`getMods reload: ${err.message}`);
  }

  await host.stop();
  pass('host shutdown');

  console.error(failures === 0 ? '[manager-smoke] all checks passed' : `[manager-smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[manager-smoke] fatal: ${err.message}`);
  process.exit(1);
});
