import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/**
 * Locate Asher.Host.exe for development.
 * Override with ASHER_HOST_PATH environment variable.
 */
export function resolveHostPath() {
  if (process.env.ASHER_HOST_PATH) {
    const resolved = path.resolve(process.env.ASHER_HOST_PATH);
    if (!fs.existsSync(resolved)) {
      throw new Error(`ASHER_HOST_PATH does not exist: ${resolved}`);
    }
    return resolved;
  }

  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const candidates = [
    path.join(repoRoot, 'Asher.Host', 'bin', 'x86', 'Debug', 'net8.0-windows', 'Asher.Host.exe'),
    path.join(repoRoot, 'Asher.Host', 'bin', 'x86', 'Release', 'net8.0-windows', 'Asher.Host.exe'),
    path.join(process.cwd(), 'Asher.Host', 'bin', 'x86', 'Debug', 'net8.0-windows', 'Asher.Host.exe')
  ];

  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) {
      return candidate;
    }
  }

  throw new Error(
    'Could not locate Asher.Host.exe. Build with:\n' +
      '  dotnet build Asher.Host/Asher.Host.csproj -c Debug -p:Platform=x86\n' +
      'Or set ASHER_HOST_PATH to the executable path.'
  );
}
