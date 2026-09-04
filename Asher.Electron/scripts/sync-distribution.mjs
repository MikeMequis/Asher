/**
 * Sync electron-builder unpacked output to repo-root Distribution/.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const electronRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = path.resolve(electronRoot, '..');
const unpackedDir = path.join(electronRoot, 'dist', 'win-ia32-unpacked');
const distributionDir = path.join(repoRoot, 'Distribution');

if (!fs.existsSync(unpackedDir)) {
  console.error(`[sync-distribution] missing unpacked build: ${unpackedDir}`);
  process.exit(1);
}

if (fs.existsSync(distributionDir)) {
  fs.rmSync(distributionDir, { recursive: true, force: true });
}

fs.cpSync(unpackedDir, distributionDir, { recursive: true });
console.error(`[sync-distribution] synced ${unpackedDir} -> ${distributionDir}`);
