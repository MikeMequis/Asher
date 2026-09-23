[English](README.md) | **Português (Brasil)**

# 🧱 Plataforma de Modding Asher

**Asher** é uma plataforma de modding baseada em launcher para [*Dust: An Elysian Tail*](https://store.steampowered.com/app/236090/Dust_An_Elysian_Tail/). Aplica **patches de código em tempo de execução** (Harmony) e foi projetada para substituir conteúdo sem editar os arquivos do jogo — de forma segura, modular e reversível.

Inspirado em mod loaders como o **SMAPI**, o Asher prioriza uma ordem de inicialização explícita, controle do ciclo de vida em runtime e depuração limpa, em vez de injeção precoce frágil.

## O que inclui

- **Launcher para Windows** — substitui o `DustAET.exe` e controla a ordem de inicialização (o original é mantido como `DustAET.real.exe`)
- **Bootstrap para Linux** — o `DustAET` nativo se conecta via `libasher_bootstrap.so` (`LD_PRELOAD`); sem troca de launcher
- **Carregador de mods em runtime** — patching com Harmony nos estágios PreInit → Patch → Lifecycle
- **Gerenciador Electron** (`Asher.Electron` + `Asher.Host`) — instalação, Patch Manager, configurações e lançamento do jogo
- **SDK de mods** — interfaces para criar mods externos carregados de `Asher/Mods/`

## Início prático

1. Em `Asher.Electron/`, compile o host e inicie o gerenciador:
   ```bash
   npm install
   npm run build:host:debug   # Windows
   # ou: npm run build:host:linux   # Linux (publica build/linux-host)
   npm start
   ```
   No Linux, o Electron também precisa das bibliotecas de sistema (NSS/NSPR/ALSA, GTK e FUSE para o AppImage) — veja [dependências Linux](docs/Cross-Platform-Architecture.md#linux-dependencies).
2. Use **Setup** para detectar e salvar a pasta do jogo e depois **Install**.
3. Inicie o jogo pela **Steam** ou pelo botão **Launch Game** do gerenciador (no Linux, inicie pelo gerenciador; lançamento externo via Steam/atalho ainda não foi implementado).

### Adendo — XNA Framework (dependência de build)

`Asher.Runtime` e os projetos de patching referenciam os assemblies do **Microsoft XNA Framework 4.0** no GAC (o mesmo runtime que o Dust usa) no Windows. Sem eles, `npm run build:host` / builds de patching falham ou avisam sobre `Microsoft.Xna.Framework*` ausente.

1. Instale o **[Redistributable do XNA Framework 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=20914)** (ou o [4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598)).
2. Prefira o redistributable **x86** — o Asher usa `Platform=x86`.
3. Recompile: `cd Asher.Electron && npm run build:host:debug`.

Instalações Steam do Dust costumam já trazer esses assemblies. No Linux usa-se **Mono/FNA**; o requisito de GAC é só do Windows.

## Patches Incluídos

Cinco patches são criados por padrão. Cada um é um mod externo carregado em tempo de execução de `Asher/Mods/`:

- **Debug Menu Enabler** — `Tab` no menu de pausa abre o menu de depuração
- **Intro Skipper** — pula a classificação ESRB, telas de abertura e vídeos iniciais
- **Graphics Deprofiler** — contorna restrições de perfil de GPU HiDef
- **Mute Voice Acting** — silencia a dublagem mantendo outros SFX
- **Dust Storm Overheat Disabler** — impede o Dust Storm de superaquecer

## Build & Distribuição

Apenas um resumo; a página **Build & Distribution** do site é a referência completa.

```bash
cd Asher.Electron
npm run build:host         # backend Release (Host + runtime + patches)
npm run build:host:debug   # backend Debug para trabalhar na UI localmente
npm start                  # executa o gerenciador em desenvolvimento
npm run dist               # instalador NSIS + zip portátil + latest.yml + sincroniza Distribution/
npm run publish            # publica um GitHub Release (requer private/GH_TOKEN)
```

O usuário roda o instalador NSIS (`Asher-Setup-<version>.exe`) ou extrai o zip portátil (ou usa `Distribution/`) e instala na pasta do jogo. O gerenciador permanece em `Distribution`; a pasta do jogo recebe o runtime e o `Uninstall-Asher.cmd` ao lado do `DustAET.exe` para restauração de emergência.

O Linux é empacotado em um host Linux: `npm run dist:linux` / `npm run publish:linux` geram `Asher-<version>-linux-x86_64.AppImage`, `Asher-<version>-linux-x64.tar.gz` e `latest-linux.yml`. Atualizações no Linux são downloads manuais do GitHub Releases.

## Desenvolvimento assistido por IA

O Asher é desenvolvido com forte apoio de IA, principalmente via OpenCode. A IA ajuda na implementação, investigação, refatoração, testes, depuração e documentação. Arquitetura, direção técnica, escopo, validação e a decisão final sobre o que entra continuam sendo humanas. Consulte [❓ FAQ](https://mikesstash.com.br/asher/faq/) para uma resposta mais completa.

## Documentação

- **Site** — https://mikesstash.com.br/asher/ (guia do usuário, arquitetura, status, FAQ)
- [Arquitetura multiplataforma](docs/Cross-Platform-Architecture.md) — contratos de plataforma, layout/build/pacotes Linux
- [Bootstrap Linux](Asher.Linux/README.md) — entrada nativa via `LD_PRELOAD` + orquestração de patches
