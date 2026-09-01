/**
 * Verify install-payload is bundled next to Asher.Host.exe after dotnet build.
 * Run: node scripts/payload-smoke-test.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');

const hostDirs = [
  path.join(repoRoot, 'Asher.Host', 'bin', 'x86', 'Debug', 'net8.0-windows'),
  path.join(repoRoot, 'Asher.Host', 'bin', 'x86', 'Release', 'net8.0-windows')
];

/** @type {string | null} */
let hostDir = null;
for (const candidate of hostDirs) {
  if (fs.existsSync(path.join(candidate, 'Asher.Host.exe'))) {
    hostDir = candidate;
    break;
  }
}

let failures = 0;

function fail(message) {
  console.error(`[FAIL] ${message}`);
  failures++;
}

function pass(message) {
  console.error(`[OK] ${message}`);
}

if (!hostDir) {
  fail('Asher.Host.exe not found — run: dotnet build Asher.Host/Asher.Host.csproj -c Debug -p:Platform=x86');
  process.exit(1);
}

pass(`host output: ${hostDir}`);

const payloadDir = path.join(hostDir, 'install-payload');
if (!fs.existsSync(payloadDir)) {
  fail('install-payload directory missing');
} else {
  pass('install-payload directory');
}

const requiredFiles = [
  'Asher.Launcher.exe',
  'Asher.Runtime.dll',
  'Asher.SDK.dll',
  '0Harmony.dll'
];

for (const fileName of requiredFiles) {
  const filePath = path.join(payloadDir, fileName);
  if (!fs.existsSync(filePath)) {
    fail(`missing ${fileName}`);
  } else {
    pass(fileName);
  }
}

const defaultModsDir = path.join(payloadDir, 'DefaultMods');
if (!fs.existsSync(defaultModsDir)) {
  fail('DefaultMods directory missing');
} else {
  const modCount = fs.readdirSync(defaultModsDir).filter((name) => name.endsWith('.dll')).length;
  if (modCount === 0) {
    fail('DefaultMods has no DLLs — build patching projects or run PrepareDistribution.ps1 before Asher.Host');
  } else {
    pass(`DefaultMods (${modCount} dll(s))`);
  }
}

process.exit(failures === 0 ? 0 : 1);
