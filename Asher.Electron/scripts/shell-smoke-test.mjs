/**
 * Headless smoke test for application shell startup flow.
 * Run: node scripts/shell-smoke-test.mjs
 */
import { HostManager } from '../src/main/host-manager.js';
import { fetchApplicationState } from '../src/renderer/application-state.js';

let failures = 0;

function fail(message) {
  console.error(`[FAIL] ${message}`);
  failures++;
}

function pass(message) {
  console.error(`[OK] ${message}`);
}

/** @type {import('../src/renderer/application-client.js').ApplicationClient} */
const client = {
  invoke(method, params, options) {
    const host = globalThis.__shellHost;
    if (!host?.client) {
      return Promise.reject(new Error('Host is not available.'));
    }
    return host.client.request(method, params, options ?? {}).then((result) => ({ result }));
  }
};

async function main() {
  const host = new HostManager();
  globalThis.__shellHost = host;

  try {
    await host.start();
    pass('host ready');
  } catch (err) {
    fail(`host start: ${err.message}`);
    process.exit(1);
  }

  try {
    const state = await fetchApplicationState(client);
    if (!state.recommendedScreen) {
      fail('recommendedScreen missing');
    } else {
      pass(`application state (${state.recommendedScreen}, configured=${state.isConfigured})`);
    }
  } catch (err) {
    fail(`fetchApplicationState: ${err.message}`);
  }

  try {
    const mods = await client.invoke('getMods');
    if (!Array.isArray(mods.result)) {
      fail('getMods should return array via shell client');
    } else {
      pass(`getMods via shell path (${mods.result.length} mods)`);
    }
  } catch (err) {
    fail(`getMods: ${err.message}`);
  }

  await host.stop();
  pass('host shutdown');

  console.error(failures === 0 ? '[shell-smoke] all checks passed' : `[shell-smoke] ${failures} failure(s)`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error(`[shell-smoke] fatal: ${err.message}`);
  process.exit(1);
});
