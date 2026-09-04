/**
 * Remove stale electron-builder output before packaging.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const distDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'dist');

if (fs.existsSync(distDir)) {
  fs.rmSync(distDir, { recursive: true, force: true });
  console.error(`[clean:dist] removed ${distDir}`);
} else {
  console.error('[clean:dist] nothing to remove');
}
