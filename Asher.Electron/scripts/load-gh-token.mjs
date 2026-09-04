/**
 * Load GH_TOKEN from repo-root private/GH_TOKEN for electron-builder publish.
 * Prints nothing sensitive; exits non-zero if missing.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const electronRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const tokenPath = path.resolve(electronRoot, '..', 'private', 'GH_TOKEN');

if (!fs.existsSync(tokenPath)) {
  console.error(`[publish] missing token file: ${tokenPath}`);
  console.error('[publish] create private/GH_TOKEN with a GitHub personal access token (repo scope).');
  process.exit(1);
}

const token = fs.readFileSync(tokenPath, 'utf8').trim();
if (!token) {
  console.error(`[publish] token file is empty: ${tokenPath}`);
  process.exit(1);
}

const args = process.argv.slice(2);
if (args.length === 0) {
  console.error('[publish] usage: node scripts/load-gh-token.mjs <command> [args...]');
  process.exit(1);
}

const [command, ...commandArgs] = args;
const result = spawnSync(command, commandArgs, {
  stdio: 'inherit',
  shell: process.platform === 'win32',
  env: {
    ...process.env,
    GH_TOKEN: token
  },
  cwd: electronRoot
});

process.exit(result.status ?? 1);
