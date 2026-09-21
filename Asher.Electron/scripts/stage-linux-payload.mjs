/**
 * Stage the Linux Asher payload for packaging.
 *
 * Reads the locally built Asher.Linux artifacts and produces:
 *   install-payload/
 *     libasher_bootstrap.so
 *     Asher.Runtime.dll
 *     Asher.SDK.dll
 *     0Harmony.dll
 *     DefaultMods/Asher.Patching.*.dll
 *
 * Deterministic and fail-fast: the destination is recreated and the script exits
 * non-zero when a required artifact is missing.
 *
 * Run: node scripts/stage-linux-payload.mjs --source ../Asher.Linux/out --dest build/linux-host/install-payload
 */
import fs from 'node:fs';
import path from 'node:path';

const REQUIRED_RUNTIME_FILES = [
  'libasher_bootstrap.so',
  'Asher.Runtime.dll',
  'Asher.SDK.dll',
  '0Harmony.dll'
];

const MOD_PREFIX = 'Asher.Patching.';
const MOD_EXTENSION = '.dll';

function fail(message) {
  console.error(`[stage-linux-payload] ${message}`);
  process.exit(1);
}

function parseArgs(argv) {
  const args = { source: null, dest: null };
  for (let i = 0; i < argv.length; i += 1) {
    if (argv[i] === '--source') {
      args.source = argv[i + 1];
      i += 1;
    } else if (argv[i] === '--dest') {
      args.dest = argv[i + 1];
      i += 1;
    }
  }
  return args;
}

const { source, dest } = parseArgs(process.argv.slice(2));
if (!source || !dest) {
  fail('usage: stage-linux-payload.mjs --source <Asher.Linux/out> --dest <install-payload>');
}

const sourceDir = path.resolve(source);
const destDir = path.resolve(dest);

if (!fs.existsSync(sourceDir)) {
  fail(`source directory not found: ${sourceDir}`);
}

for (const fileName of REQUIRED_RUNTIME_FILES) {
  if (!fs.existsSync(path.join(sourceDir, fileName))) {
    fail(`missing required runtime file: ${path.join(sourceDir, fileName)}`);
  }
}

const modsSourceDir = path.join(sourceDir, 'Mods');
if (!fs.existsSync(modsSourceDir)) {
  fail(`missing mods directory: ${modsSourceDir}`);
}

const mods = fs
  .readdirSync(modsSourceDir)
  .filter((name) => name.startsWith(MOD_PREFIX) && name.endsWith(MOD_EXTENSION))
  .sort();

if (mods.length === 0) {
  fail(`no ${MOD_PREFIX}*${MOD_EXTENSION} files found in ${modsSourceDir}`);
}

fs.rmSync(destDir, { recursive: true, force: true });
fs.mkdirSync(path.join(destDir, 'DefaultMods'), { recursive: true });

for (const fileName of REQUIRED_RUNTIME_FILES) {
  fs.copyFileSync(path.join(sourceDir, fileName), path.join(destDir, fileName));
}

for (const mod of mods) {
  fs.copyFileSync(path.join(modsSourceDir, mod), path.join(destDir, 'DefaultMods', mod));
}

console.error(`[stage-linux-payload] staged ${REQUIRED_RUNTIME_FILES.length} runtime file(s) and ${mods.length} mod(s)`);
console.error(`[stage-linux-payload] -> ${destDir}`);
