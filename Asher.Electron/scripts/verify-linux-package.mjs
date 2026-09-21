/**
 * Verify the Linux package artifacts (tar.gz + AppImage) contain the expected
 * manager Host and Asher install-payload.
 *
 * Run after `npm run dist:linux`:
 *   node scripts/verify-linux-package.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const electronRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const distDir = path.join(electronRoot, 'dist');

const EXPECTED_ENTRIES = [
  'resources/asher-host/Asher.Host',
  'resources/asher-host/install-payload/libasher_bootstrap.so',
  'resources/asher-host/install-payload/Asher.Runtime.dll',
  'resources/asher-host/install-payload/Asher.SDK.dll',
  'resources/asher-host/install-payload/0Harmony.dll'
];

let failures = 0;

function fail(message) {
  console.error(`[FAIL] ${message}`);
  failures++;
}

function pass(message) {
  console.error(`[OK] ${message}`);
}

if (!fs.existsSync(distDir)) {
  fail(`dist directory not found: ${distDir}`);
  process.exit(1);
}

const entries = fs.readdirSync(distDir);
const tarName = entries.find((name) => /linux.*x64.*\.tar\.gz$/i.test(name));
const appImageName = entries.find((name) => /linux.*(?:x64|x86_64).*\.AppImage$/i.test(name));

if (!tarName) {
  fail('no Linux x64 tar.gz artifact found in dist/');
} else {
  pass(`tar.gz artifact: ${tarName}`);
}

if (!appImageName) {
  fail('no Linux x64 AppImage artifact found in dist/');
} else {
  const size = fs.statSync(path.join(distDir, appImageName)).size;
  if (size > 1024 * 1024) {
    pass(`AppImage artifact: ${appImageName} (${size} bytes)`);
  } else {
    fail(`AppImage artifact looks too small: ${appImageName} (${size} bytes)`);
  }
}

if (tarName) {
  const tarPath = path.join(distDir, tarName);
  const result = spawnSync('tar', ['-tzf', tarPath], { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });

  if (result.error || result.status !== 0) {
    fail(`could not list ${tarName}: ${result.error?.message ?? result.stderr ?? 'tar failed'}`);
  } else {
    const listing = result.stdout.split(/\r?\n/).filter(Boolean);

    for (const expected of EXPECTED_ENTRIES) {
      if (listing.some((entry) => entry.endsWith(expected))) {
        pass(`tar contains ${expected}`);
      } else {
        fail(`tar is missing ${expected}`);
      }
    }

    const hasMod = listing.some((entry) =>
      /resources\/asher-host\/install-payload\/DefaultMods\/Asher\.Patching\..+\.dll$/.test(entry));
    if (hasMod) {
      pass('tar contains DefaultMods/Asher.Patching.*.dll');
    } else {
      fail('tar is missing DefaultMods/Asher.Patching.*.dll');
    }
  }
}

console.error(failures === 0 ? '[verify-linux] all checks passed' : `[verify-linux] ${failures} failure(s)`);
process.exit(failures === 0 ? 0 : 1);
