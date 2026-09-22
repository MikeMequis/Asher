/**
 * Builds the Windows host-side stack (SDK, patches, Runtime, Launcher, Host).
 *
 * Required projects abort the build on failure; optional patch projects are skipped
 * (e.g. GraphicsDeprofiler needs the XNA 4.0 GAC assemblies, which are not always present).
 *
 * Usage: node scripts/build-host.mjs [Debug|Release]
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const electronRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = path.resolve(electronRoot, '..');
const configuration = process.argv[2] === 'Release' ? 'Release' : 'Debug';
const platform = 'x86';

const steps = [
  { project: 'Asher.SDK/Asher.SDK.csproj', required: true },
  { project: 'Patches/Asher.Patching.DebugEnabler/Asher.Patching.DebugEnabler.csproj', required: false },
  { project: 'Patches/Asher.Patching.IntroSkipper/Asher.Patching.IntroSkipper.csproj', required: false },
  { project: 'Patches/Asher.Patching.GraphicsDeprofiler/Asher.Patching.GraphicsDeprofiler.csproj', required: false },
  { project: 'Patches/Asher.Patching.MuteVoiceActing/Asher.Patching.MuteVoiceActing.csproj', required: false },
  { project: 'Patches/Asher.Patching.OverheatDisabler/Asher.Patching.OverheatDisabler.csproj', required: false },
  { project: 'Asher.Runtime/Asher.Runtime.csproj', required: true },
  { project: 'Asher.Launcher/Asher.Launcher.csproj', required: true },
  { project: 'Asher.Host/Asher.Host.csproj', required: true }
];

let failed = false;
const skipped = [];

for (const step of steps) {
  const projectPath = path.join(repoRoot, step.project);
  console.error(`[build-host] ${step.required ? '' : '(optional) '}${step.project} [${configuration}|${platform}]`);
  const result = spawnSync('dotnet', ['build', projectPath, '-c', configuration, '-p:Platform=' + platform], {
    stdio: 'inherit'
  });

  if (result.status !== 0) {
    if (step.required) {
      failed = true;
      console.error(`[build-host] REQUIRED project failed: ${step.project}`);
    } else {
      skipped.push(step.project);
      console.error(`[build-host] optional project skipped: ${step.project}`);
    }
  }
}

if (skipped.length > 0) {
  console.error(`[build-host] optional projects skipped (build failed): ${skipped.join(', ')}`);
  console.error('[build-host] note: GraphicsDeprofiler needs XNA 4.0 in the GAC (see README addendum).');
}

if (failed) {
  console.error('[build-host] build failed: one or more required projects did not build.');
  process.exit(1);
}

console.error('[build-host] done.');
