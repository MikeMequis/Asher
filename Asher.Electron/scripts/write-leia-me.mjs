/**
 * Generates Distribution/LEIA-ME.txt (the manager readme shipped with the packaged app).
 *
 * Run directly to regenerate into the repo-root Distribution/:
 *   node scripts/write-leia-me.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const electronRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = path.resolve(electronRoot, '..');
const packageJson = JSON.parse(fs.readFileSync(path.join(electronRoot, 'package.json'), 'utf8'));

/**
 * @param {string} version
 * @returns {string}
 */
export function buildLeiaMe(version) {
  return `================================================================
   ASHER - MOD MANAGER PARA DUST: AN ELYSIAN TAIL
================================================================

COMO USAR
---------------------------------------------------------
1. Execute Asher.exe
2. Siga o assistente de instalacao
3. O Asher detecta automaticamente a pasta do jogo
4. Apos instalar, inicie o jogo pelo gerenciador ou pelo Steam/GOG

CONTEUDO DESTA PASTA
---------------------------------------------------------
Asher.exe     Gerenciador (instalador + mod manager)
resources/    Electron, Asher.Host (backend) e payload de instalacao

REQUISITOS
---------------------------------------------------------
- Windows 10 ou superior
- .NET 8.0 Runtime
- Dust: An Elysian Tail instalado (Steam, GOG ou Humble Bundle)

IMPORTANTE
---------------------------------------------------------
- Um backup do executavel do jogo e criado na instalacao
- O jogo nao e modificado permanentemente; a desinstalacao restaura o original
- Mods sao carregados quando o jogo inicia pelo launcher

MODS INCLUSOS
---------------------------------------------------------
- DebugEnabler:        Ativa o menu de debug (Tab)
- IntroSkipper:        Pula a intro do jogo
- GraphicsDeprofiler:  Bypassa restricoes de perfil HiDef
- MuteVoiceActing:     Silencia as vozes dos dialogos
- OverheatDisabler:    Desativa o superaquecimento do Dust Storm

ESTRUTURA APOS INSTALACAO NO JOGO
---------------------------------------------------------
GameFolder/
  DustAET.exe              (Asher Launcher)
  DustAET.real.exe         (executavel original renomeado)
  Uninstall-Asher.cmd      (recuperacao sem o gerenciador)
  Asher/
    Asher.Runtime.dll
    Asher.SDK.dll
    0Harmony.dll
    Mods/                  (mods ativos)
    AsherLogs/             (logs do runtime)

SUPORTE
---------------------------------------------------------
- Logs: [GameFolder]\\Asher\\AsherLogs\\
- Repositorio do projeto: reporte bugs com o log mais recente

Versao: ${version}
`;
}

/**
 * @param {string} destDir
 * @param {string} [version]
 * @returns {string}
 */
export function writeLeiaMe(destDir, version = packageJson.version) {
  fs.mkdirSync(destDir, { recursive: true });
  const target = path.join(destDir, 'LEIA-ME.txt');
  fs.writeFileSync(target, buildLeiaMe(version), 'utf8');
  return target;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const target = writeLeiaMe(path.join(repoRoot, 'Distribution'));
  console.error(`[leia-me] wrote ${target}`);
}
