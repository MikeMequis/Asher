# PrepareDistribution.ps1
# Script para preparar distribuicao do Asher automaticamente
# Uso: .\PrepareDistribution.ps1 [-Configuration Release]

param(
    [string]$Configuration = "Release"
)

$OutputPath = ".\Distribution"

Write-Host "============================================" -ForegroundColor Green
Write-Host "  ASHER - PREPARACAO DE DISTRIBUICAO" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configuracao: $Configuration" -ForegroundColor Yellow
Write-Host "Pasta de saida: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Limpar pasta de distribuicao
if (Test-Path $OutputPath) {
    Write-Host "Limpando pasta de distribuicao existente..." -ForegroundColor Cyan
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# Caminhos base corretos
$AppPath = ".\Asher.App\bin\x86\$Configuration\net8.0-windows"
$UIPath = ".\Asher.UserInterface\bin\x86\$Configuration\net8.0-windows"
$LauncherPath = ".\Asher.Launcher\bin\x86\$Configuration\net472"
$RuntimePath = ".\Asher.Runtime\bin\x86\$Configuration"
$SDKPath = ".\Asher.SDK\bin\x86\$Configuration"

Write-Host "Verificando caminhos dos projetos..." -ForegroundColor Cyan
Write-Host ""

# Verificar se os paths existem
$pathsToCheck = @(
    @{Path=$AppPath; Name="Asher.App"; Priority="CRITICO"},
    @{Path=$UIPath; Name="Asher.UserInterface"; Priority="OPCIONAL"},
    @{Path=$LauncherPath; Name="Asher.Launcher"; Priority="CRITICO"},
    @{Path=$RuntimePath; Name="Asher.Runtime"; Priority="CRITICO"},
    @{Path=$SDKPath; Name="Asher.SDK"; Priority="CRITICO"}
)

$missingCritical = @()
$missingOptional = @()

foreach ($item in $pathsToCheck) {
    if (-not (Test-Path $item.Path)) {
        if ($item.Priority -eq "CRITICO") {
            $missingCritical += $item.Name
            Write-Host "  [ERRO] $($item.Name) - Pasta nao encontrada: $($item.Path)" -ForegroundColor Red
        } else {
            $missingOptional += $item.Name
            Write-Host "  [AVISO] $($item.Name) - Pasta nao encontrada (opcional)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  [OK] $($item.Name)" -ForegroundColor Green
    }
}

if ($missingCritical.Count -gt 0) {
    Write-Host ""
    Write-Host "ERRO: Projetos criticos nao foram compilados!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Execute 'Build Solution' no Visual Studio antes de rodar este script." -ForegroundColor Yellow
    Write-Host "Certifique-se de que a configuracao esta correta:" -ForegroundColor Yellow
    Write-Host "  - Platform: x86" -ForegroundColor White
    Write-Host "  - Configuration: $Configuration" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "Copiando arquivos principais..." -ForegroundColor Cyan
Write-Host ""

# Determinar qual pasta usar (Asher.App tem prioridade)
$mainAppPath = $AppPath
if (-not (Test-Path $AppPath)) {
    Write-Host "  Asher.App nao encontrado, usando Asher.UserInterface" -ForegroundColor Yellow
    $mainAppPath = $UIPath
}

# Copiar todos os arquivos da aplicacao principal
Write-Host "[1/7] Copiando aplicacao principal e dependencias..." -ForegroundColor Gray
if (Test-Path $mainAppPath) {
    Copy-Item -Path "$mainAppPath\*" -Destination $OutputPath -Recurse -Force
    $appFileCount = (Get-ChildItem -Path $OutputPath -File).Count
    Write-Host "      $appFileCount arquivos copiados de $(Split-Path $mainAppPath -Leaf)" -ForegroundColor DarkGray
} else {
    Write-Host "      [ERRO] Pasta da aplicacao principal nao encontrada!" -ForegroundColor Red
}

# Copiar Asher.Launcher.exe
Write-Host "[2/7] Copiando Asher.Launcher.exe..." -ForegroundColor Gray

# Tentar Release primeiro, depois Debug
$launcherExe = Join-Path $LauncherPath "Asher.Launcher.exe"
if (-not (Test-Path $launcherExe)) {
    # Tentar Debug se Release nao existir
    $LauncherPathDebug = ".\Asher.Launcher\bin\x86\Debug\net472"
    $launcherExe = Join-Path $LauncherPathDebug "Asher.Launcher.exe"
    if (Test-Path $launcherExe) {
        Write-Host "      [AVISO] Usando versao Debug do Launcher" -ForegroundColor Yellow
    }
}

if (Test-Path $launcherExe) {
    Copy-Item -Path $launcherExe -Destination $OutputPath -Force
    $launcherSize = [math]::Round((Get-Item $launcherExe).Length / 1KB, 2)
    Write-Host "      [OK] Asher.Launcher.exe ($launcherSize KB)" -ForegroundColor Green

    $launcherConfig = "$launcherExe.config"
    if (Test-Path $launcherConfig) {
        Copy-Item -Path $launcherConfig -Destination (Join-Path $OutputPath "Asher.Launcher.exe.config") -Force
        Write-Host "      [OK] Asher.Launcher.exe.config" -ForegroundColor Green
    } else {
        Write-Host "      [AVISO] Asher.Launcher.exe.config nao encontrado" -ForegroundColor Yellow
    }
} else {
    Write-Host "      [ERRO] Asher.Launcher.exe nao encontrado!" -ForegroundColor Red
    Write-Host "             Procurado em: $LauncherPath" -ForegroundColor DarkGray
}

# Copiar DLLs do Runtime e SDK
Write-Host "[3/7] Copiando Runtime e SDK..." -ForegroundColor Gray

# Runtime
$runtimeDlls = Get-ChildItem -Path $RuntimePath -Filter "Asher.Runtime.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($runtimeDlls) {
    Copy-Item -Path $runtimeDlls.FullName -Destination $OutputPath -Force
    $runtimeSize = [math]::Round($runtimeDlls.Length / 1KB, 2)
    Write-Host "      [OK] Asher.Runtime.dll ($runtimeSize KB)" -ForegroundColor Green
} else {
    Write-Host "      [ERRO] Asher.Runtime.dll nao encontrado em $RuntimePath" -ForegroundColor Red
}

# SDK
$sdkDlls = Get-ChildItem -Path $SDKPath -Filter "Asher.SDK.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($sdkDlls) {
    Copy-Item -Path $sdkDlls.FullName -Destination $OutputPath -Force
    $sdkSize = [math]::Round($sdkDlls.Length / 1KB, 2)
    Write-Host "      [OK] Asher.SDK.dll ($sdkSize KB)" -ForegroundColor Green
} else {
    Write-Host "      [ERRO] Asher.SDK.dll nao encontrado em $SDKPath" -ForegroundColor Red
}

# Procurar e copiar 0Harmony.dll
Write-Host "[4/7] Procurando 0Harmony.dll..." -ForegroundColor Gray

function Get-Net472HarmonyDll {
    $preferredPaths = @(
        ".\packages\Lib.Harmony.2.4.2\lib\net472\0Harmony.dll",
        ".\packages\Lib.Harmony.2.4.2\lib\net48\0Harmony.dll",
        ".\packages\Lib.Harmony.2.4.2\lib\net452\0Harmony.dll"
    )

    foreach ($path in $preferredPaths) {
        if (Test-Path $path) {
            return Get-Item $path
        }
    }

    return $null
}

# IMPORTANT: never pick the first recursive match - Lib.Harmony ships net10.0/net8.0 builds too.
$harmonyDll = Get-Net472HarmonyDll

if ($harmonyDll) {
    Copy-Item -Path $harmonyDll.FullName -Destination $OutputPath -Force
    $harmonySize = [math]::Round($harmonyDll.Length / 1KB, 2)
    Write-Host "      [OK] 0Harmony.dll copiado de $($harmonyDll.FullName) ($harmonySize KB)" -ForegroundColor Green
} else {
    # Tentar procurar no output da aplicacao
    $harmonyInApp = Join-Path $OutputPath "0Harmony.dll"
    if (Test-Path $harmonyInApp) {
        Write-Host "      [OK] 0Harmony.dll (ja presente via dependencias)" -ForegroundColor DarkGray
    } else {
        # Procurar recursivamente no bin do Launcher
        $harmonyInLauncher = Get-ChildItem -Path ".\Asher.Launcher\bin" -Filter "0Harmony.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($harmonyInLauncher) {
            Copy-Item -Path $harmonyInLauncher.FullName -Destination $OutputPath -Force
            Write-Host "      [OK] 0Harmony.dll copiado de Asher.Launcher\bin" -ForegroundColor Green
        } else {
            Write-Host "      [AVISO] 0Harmony.dll nao encontrado!" -ForegroundColor Red
            Write-Host "              Instale via NuGet: Lib.Harmony (versao compativel com .NET Framework 4.7.2)" -ForegroundColor Yellow
        }
    }
}

# Criar pasta DefaultMods
Write-Host "[5/7] Criando pasta DefaultMods..." -ForegroundColor Gray
$modsOutputPath = Join-Path $OutputPath "DefaultMods"
New-Item -ItemType Directory -Path $modsOutputPath -Force | Out-Null

# Copiar mods (se existirem)
Write-Host "[6/7] Copiando mods padrao..." -ForegroundColor Gray
$modProjects = @(
    "Asher.Patching.DebugEnabler",
    "Asher.Patching.IntroSkipper",
    "Asher.Patching.GraphicsDeprofiler"
)

$modsCopied = 0
foreach ($mod in $modProjects) {
    # Caminho direto do mod: \Asher\[ModName]\bin\x86\Release\
    $modPath = ".\$mod\bin\x86\$Configuration\$mod.dll"
    
    if (Test-Path $modPath) {
        Copy-Item -Path $modPath -Destination $modsOutputPath -Force
        $modSize = [math]::Round((Get-Item $modPath).Length / 1KB, 2)
        Write-Host "      [OK] $mod.dll ($modSize KB)" -ForegroundColor Green
        $modsCopied++
    } else {
        Write-Host "      [ - ] $mod.dll (nao encontrado, pulando)" -ForegroundColor DarkGray
    }
}

if ($modsCopied -eq 0) {
    Write-Host "      Nenhum mod padrao encontrado (opcional)" -ForegroundColor Yellow
}

# Criar README
Write-Host "[7/7] Criando documentacao..." -ForegroundColor Gray
$readmeContent = @"
================================================================
   ASHER - MOD MANAGER PARA DUST: AN ELYSIAN TAIL
================================================================

COMO USAR
---------------------------------------------------------
1. Execute Asher.exe (ou Asher.App.exe)
2. Siga o assistente de instalacao
3. O Asher detectara automaticamente a pasta do jogo
4. Apos a instalacao, execute o jogo normalmente pelo Steam/GOG

ESTRUTURA DE ARQUIVOS
---------------------------------------------------------
Asher.exe / Asher.App.exe    Instalador e gerenciador de mods
Asher.Launcher.exe            Launcher que sera copiado para o jogo
Asher.Runtime.dll             Runtime principal (.NET Framework 4.7.2)
Asher.SDK.dll                 SDK para desenvolvimento de mods
0Harmony.dll                  Biblioteca de patching (Harmony)
DefaultMods/                  Mods inclusos na instalacao

REQUISITOS
---------------------------------------------------------
- .NET 8.0 Runtime (para o instalador)
- .NET Framework 4.7.2 ou superior (para o jogo/launcher)
- Dust: An Elysian Tail instalado (Steam, GOG ou Humble Bundle)
- Windows 10 ou superior

IMPORTANTE
---------------------------------------------------------
- Um backup automatico do jogo sera criado durante a instalacao
- O jogo nao sera modificado permanentemente
- Voce pode desinstalar o Asher a qualquer momento
- Mods sao carregados apenas quando o jogo inicia pelo launcher

MODS INCLUSOS
---------------------------------------------------------
- DebugEnabler:        Ativa menu de debug (F12)
- IntroSkipper:        Pula a intro do jogo
- GraphicsDeprofiler:  Otimiza o profiler de graficos

ESTRUTURA APOS INSTALACAO NO JOGO
---------------------------------------------------------
GameFolder/
├── DustAET.exe              (Asher.Launcher - novo)
├── DustAET.real.exe         (executavel original renomeado)
└── Asher/
    ├── Asher.Runtime.dll
    ├── Asher.SDK.dll
    ├── 0Harmony.dll
    ├── Mods/                (mods ativos)
    │   └── disabled/        (mods desativados)
    ├── AsherLogs/
    ├── patches/
    ├── Asher.Backup/
    │   └── DustAET.exe
    └── Asher.App/           (gerenciador instalado no jogo)
        ├── Asher.App.exe
        ├── settings.json
        ├── Asher.Launcher.exe
        └── DefaultMods/

SUPORTE
---------------------------------------------------------
- Logs sao salvos em: [GameFolder]\Asher\AsherLogs\
- Visite o repositorio do projeto para reportar bugs
- Para problemas, inclua o arquivo de log mais recente

NOTAS DA VERSAO
---------------------------------------------------------
Versao: 1.0.0
Platform: x86
Configuration: $Configuration
Gerado em: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

(C) 2024 Asher Project
"@

Set-Content -Path (Join-Path $OutputPath "LEIA-ME.txt") -Value $readmeContent -Encoding UTF8
Write-Host "      [OK] LEIA-ME.txt criado" -ForegroundColor Green

# Resumo
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  DISTRIBUICAO PREPARADA COM SUCESSO!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "RESUMO:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pasta: $OutputPath" -ForegroundColor White
Write-Host ""
Write-Host "Arquivos principais:" -ForegroundColor Yellow

# Procurar executavel principal
$mainExe = Get-ChildItem -Path $OutputPath -Filter "*.exe" | Where-Object { $_.Name -like "Asher*" -and $_.Name -notlike "*Launcher*" } | Select-Object -First 1

$criticalFiles = @(
    @{Name=$mainExe.Name; Path=$mainExe.FullName},
    @{Name="Asher.Launcher.exe"; Path=(Join-Path $OutputPath "Asher.Launcher.exe")},
    @{Name="Asher.Runtime.dll"; Path=(Join-Path $OutputPath "Asher.Runtime.dll")},
    @{Name="Asher.SDK.dll"; Path=(Join-Path $OutputPath "Asher.SDK.dll")},
    @{Name="0Harmony.dll"; Path=(Join-Path $OutputPath "0Harmony.dll")}
)

foreach ($file in $criticalFiles) {
    if ($file.Path -and (Test-Path $file.Path)) {
        $fileItem = Get-Item $file.Path
        $sizeKB = [math]::Round($fileItem.Length / 1KB, 2)
        Write-Host "  [OK] $($file.Name) ($sizeKB KB)" -ForegroundColor Green
    } else {
        Write-Host "  [ERRO] $($file.Name) (FALTANDO!)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Mods padrao:" -ForegroundColor Yellow
$modsFiles = Get-ChildItem -Path $modsOutputPath -File -ErrorAction SilentlyContinue
if ($modsFiles) {
    foreach ($mod in $modsFiles) {
        $sizeKB = [math]::Round($mod.Length / 1KB, 2)
        Write-Host "  [OK] $($mod.Name) ($sizeKB KB)" -ForegroundColor Green
    }
} else {
    Write-Host "  (Nenhum mod encontrado)" -ForegroundColor DarkGray
}

Write-Host ""
$allFiles = Get-ChildItem -Path $OutputPath -File -Recurse
Write-Host "Total de arquivos: $($allFiles.Count)" -ForegroundColor White
$totalSizeMB = [math]::Round(($allFiles | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
Write-Host "Tamanho total: $totalSizeMB MB" -ForegroundColor White

Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Cyan
Write-Host "  1. Testar o executavel principal localmente" -ForegroundColor White
Write-Host "  2. Instalar em uma copia de teste do jogo" -ForegroundColor White
Write-Host "  3. Verificar se o jogo inicia corretamente" -ForegroundColor White
Write-Host "  4. Verificar logs em Asher\AsherLogs\" -ForegroundColor White
Write-Host ""

# Verificar problemas comuns
Write-Host "Verificacao de problemas comuns:" -ForegroundColor Yellow

$issues = @()
if (-not (Test-Path (Join-Path $OutputPath "Asher.Launcher.exe"))) {
    $issues += "Asher.Launcher.exe faltando - O jogo nao podera ser executado"
}
if (-not (Test-Path (Join-Path $OutputPath "Asher.Runtime.dll"))) {
    $issues += "Asher.Runtime.dll faltando - O runtime nao funcionara"
}
if (-not (Test-Path (Join-Path $OutputPath "0Harmony.dll"))) {
    $issues += "0Harmony.dll faltando - Patches nao serao aplicados"
}

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "ATENCAO: Problemas detectados:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  ! $issue" -ForegroundColor Red
    }
    Write-Host ""
} else {
    Write-Host "  Nenhum problema critico detectado" -ForegroundColor Green
}

# Abrir pasta no Explorer
Write-Host ""
Write-Host "Abrindo pasta de distribuicao..." -ForegroundColor Gray
Start-Process explorer.exe -ArgumentList $OutputPath

Write-Host ""
Write-Host "Pressione qualquer tecla para sair..." -ForegroundColor DarkGray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")