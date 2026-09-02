/** @typedef {'en-US' | 'pt-BR' | 'es-ES'} SupportedLanguage */

/** @type {Record<SupportedLanguage, Record<string, string>>} */
const STRINGS = {
  'en-US': {
    'app.title': 'Asher',
    'app.subtitle.connecting': 'Connecting...',
    'app.subtitle.loading': 'Loading...',
    'app.subtitle.disconnected': 'Disconnected',
    'app.subtitle.setup': 'Game setup',
    'app.subtitle.home': 'Home',
    'app.subtitle.manager': 'Patch Manager',
    'app.subtitle.settings': 'Settings',
    'app.subtitle.install': 'Installing Asher',
    'app.subtitle.uninstall': 'Uninstalling Asher',
    'host.notConnected': 'Not connected',

    'shell.starting': 'Starting application',
    'shell.connecting': 'Connecting to Asher.Host...',
    'shell.loading': 'Loading application',
    'shell.loadingConfig': 'Reading configuration from the application...',
    'shell.unavailable': 'Application Unavailable',
    'shell.retry': 'Retry Connection',
    'shell.hostError': 'The Asher application host is not available.',

    'nav.welcome': 'Welcome',
    'nav.gameDetection': 'Game Detection',
    'nav.installing': 'Installing',
    'nav.complete': 'Complete',
    'nav.home': 'Home',
    'nav.patchManager': 'Patch Manager',
    'nav.settings': 'Settings',

    'home.title': 'Welcome to Asher',
    'home.description': 'Manage mods and launch your patched game from here.',
    'home.card.manager': 'Patch Manager',
    'home.card.managerDesc': 'Enable or disable individual mods',
    'home.card.settings': 'Settings',
    'home.card.settingsDesc': 'Preferences, game path, and uninstall',
    'home.card.launch': 'Launch Game',
    'home.card.launchDesc': 'Start the game with mods enabled',
    'home.launchSuccess': 'Game launch started.',
    'home.launchError': 'Failed to launch the game.',

    'setup.title': 'Locate Game Installation',
    'setup.help': 'Find the installation folder for Dust: An Elysian Tail.',
    'setup.autoDetect': 'Detect Automatically',
    'setup.browse': 'Browse...',
    'setup.validate': 'Validate',
    'setup.save': 'Save Configuration',
    'setup.validating': 'Validating game folder...',
    'setup.saving': 'Saving configuration...',
    'setup.invalid': 'Invalid game folder.',
    'setup.valid': 'Valid game found — version {version} ({source})',
    'setup.invalidExe': 'Could not find DustAET.exe in this folder.',
    'setup.pathPlaceholder': 'Game folder path',

    'ready.title': 'Game Configured',
    'ready.configured': 'A valid Asher installation was found for this game folder.',
    'ready.needsInstall': 'The game folder is configured. Install Asher to enable mod support.',
    'ready.folder': 'Folder',
    'ready.version': 'Version',
    'ready.source': 'Source',
    'ready.mode': 'Mode',
    'ready.modeManager': 'Manager',
    'ready.modeWizard': 'Install wizard',
    'ready.install': 'Install Asher',
    'ready.openManager': 'Open Patch Manager',
    'ready.reconfigure': 'Change Game Folder',

    'install.title': 'Installing Asher',
    'install.help': 'Preparing the game for mod support. Do not close the application during installation.',
    'install.start': 'Start Installation',
    'install.starting': 'Starting installation...',
    'install.inProgress': 'Installing...',
    'install.cancelling': 'Cancelling installation...',
    'install.cancel': 'Cancel',
    'install.success': 'Installation completed successfully.',
    'install.continue': 'Continue to Home',
    'install.failed': 'Installation failed.',
    'install.cancelled': 'Installation was cancelled.',
    'install.retry': 'Try Again',
    'install.backSetup': 'Back to Setup',

    'manager.title': 'Patch Manager',
    'manager.help': 'Enable or disable installed mods.',
    'manager.refresh': 'Refresh',
    'manager.loading': 'Loading mods...',
    'manager.empty': 'No mods found for the configured game folder.',
    'manager.active': 'Active',
    'manager.total': 'Total',
    'manager.enabled': 'Enabled',
    'manager.disabled': 'Disabled',
    'manager.updating': 'Updating...',

    'settings.title': 'Settings',
    'settings.description': 'Configure game path, preferences, and application behavior.',
    'settings.game': 'Game',
    'settings.gamePath': 'Game folder path',
    'settings.autoLaunch': 'Auto-launch game after install',
    'settings.backup': 'Create backup before install',
    'settings.application': 'Application',
    'settings.language': 'Language',
    'settings.theme': 'Theme',
    'settings.themeLight': 'Light',
    'settings.themeDark': 'Dark',
    'settings.checkUpdates': 'Check for updates',
    'settings.uninstall': 'Uninstall',
    'settings.uninstallDesc': 'Remove Asher from the game folder and restore original files where possible.',
    'settings.uninstallAction': 'Uninstall Asher',
    'settings.reset': 'Reset to Defaults',
    'settings.save': 'Save Settings',
    'settings.saving': 'Saving settings...',
    'settings.saved': 'Settings saved successfully.',
    'settings.resetDone': 'Settings reset to defaults.',
    'settings.pathValidating': 'Validating game folder...',
    'settings.pathValid': 'Valid game folder.',
    'settings.pathInvalid': 'Invalid game folder.',

    'uninstall.title': 'Uninstalling Asher',
    'uninstall.help': 'This will remove Asher from the configured game folder and restore the original game files where possible.',
    'uninstall.confirm': 'Are you sure you want to uninstall Asher from this game installation?',
    'uninstall.action': 'Uninstall',
    'uninstall.cancel': 'Cancel',
    'uninstall.starting': 'Starting uninstallation...',
    'uninstall.inProgress': 'Uninstalling...',
    'uninstall.cancelling': 'Cancelling uninstallation...',
    'uninstall.success': 'Uninstallation completed successfully.',
    'uninstall.continue': 'Continue',
    'uninstall.failed': 'Uninstallation failed.',
    'uninstall.cancelled': 'Uninstallation was cancelled.',
    'uninstall.retry': 'Try Again',
    'uninstall.backManager': 'Back to Patch Manager',

    'common.cancel': 'Cancel',
    'common.error': 'An unexpected error occurred.',
    'common.configLost': 'Game configuration is no longer valid. Please set up the game folder again.',
    'common.logFile': 'Log file: {path}'
  },
  'pt-BR': {
    'app.title': 'Asher',
    'app.subtitle.connecting': 'Conectando...',
    'app.subtitle.loading': 'Carregando...',
    'app.subtitle.disconnected': 'Desconectado',
    'app.subtitle.setup': 'Configuração do jogo',
    'app.subtitle.home': 'Início',
    'app.subtitle.manager': 'Gerenciador de Patches',
    'app.subtitle.settings': 'Configurações',
    'app.subtitle.install': 'Instalando Asher',
    'app.subtitle.uninstall': 'Desinstalando Asher',
    'host.notConnected': 'Não conectado',

    'shell.starting': 'Iniciando aplicativo',
    'shell.connecting': 'Conectando ao Asher.Host...',
    'shell.loading': 'Carregando aplicativo',
    'shell.loadingConfig': 'Lendo configuração do aplicativo...',
    'shell.unavailable': 'Aplicativo Indisponível',
    'shell.retry': 'Tentar Novamente',
    'shell.hostError': 'O host do aplicativo Asher não está disponível.',

    'nav.welcome': 'Boas-vindas',
    'nav.gameDetection': 'Detecção do Jogo',
    'nav.installing': 'Instalando',
    'nav.complete': 'Concluído',
    'nav.home': 'Início',
    'nav.patchManager': 'Gerenciador de Patches',
    'nav.settings': 'Configurações',

    'home.title': 'Bem-vindo ao Asher',
    'home.description': 'Gerencie mods e inicie seu jogo com patches a partir daqui.',
    'home.card.manager': 'Gerenciador de Patches',
    'home.card.managerDesc': 'Ativar ou desativar mods individualmente',
    'home.card.settings': 'Configurações',
    'home.card.settingsDesc': 'Preferências, caminho do jogo e desinstalação',
    'home.card.launch': 'Iniciar Jogo',
    'home.card.launchDesc': 'Iniciar o jogo com mods ativados',
    'home.launchSuccess': 'Inicialização do jogo iniciada.',
    'home.launchError': 'Falha ao iniciar o jogo.',

    'setup.title': 'Localizar Instalação do Jogo',
    'setup.help': 'Encontre a pasta de instalação de Dust: An Elysian Tail.',
    'setup.autoDetect': 'Detectar Automaticamente',
    'setup.browse': 'Procurar...',
    'setup.validate': 'Validar',
    'setup.save': 'Salvar Configuração',
    'setup.validating': 'Validando pasta do jogo...',
    'setup.saving': 'Salvando configuração...',
    'setup.invalid': 'Pasta do jogo inválida.',
    'setup.valid': 'Jogo válido encontrado — versão {version} ({source})',
    'setup.invalidExe': 'Não foi possível encontrar DustAET.exe nesta pasta.',
    'setup.pathPlaceholder': 'Caminho da pasta do jogo',

    'ready.title': 'Jogo Configurado',
    'ready.configured': 'Uma instalação válida do Asher foi encontrada para esta pasta.',
    'ready.needsInstall': 'A pasta do jogo está configurada. Instale o Asher para habilitar suporte a mods.',
    'ready.folder': 'Pasta',
    'ready.version': 'Versão',
    'ready.source': 'Origem',
    'ready.mode': 'Modo',
    'ready.modeManager': 'Gerenciador',
    'ready.modeWizard': 'Assistente de instalação',
    'ready.install': 'Instalar Asher',
    'ready.openManager': 'Abrir Gerenciador de Patches',
    'ready.reconfigure': 'Alterar Pasta do Jogo',

    'install.title': 'Instalando Asher',
    'install.help': 'Preparando o jogo para suporte a mods. Não feche o aplicativo durante a instalação.',
    'install.start': 'Iniciar Instalação',
    'install.starting': 'Iniciando instalação...',
    'install.inProgress': 'Instalando...',
    'install.cancelling': 'Cancelando instalação...',
    'install.cancel': 'Cancelar',
    'install.success': 'Instalação concluída com sucesso.',
    'install.continue': 'Continuar para o Início',
    'install.failed': 'Falha na instalação.',
    'install.cancelled': 'Instalação cancelada.',
    'install.retry': 'Tentar Novamente',
    'install.backSetup': 'Voltar à Configuração',

    'manager.title': 'Gerenciador de Patches',
    'manager.help': 'Ativar ou desativar mods instalados.',
    'manager.refresh': 'Atualizar',
    'manager.loading': 'Carregando mods...',
    'manager.empty': 'Nenhum mod encontrado para a pasta configurada.',
    'manager.active': 'Ativos',
    'manager.total': 'Total',
    'manager.enabled': 'Ativado',
    'manager.disabled': 'Desativado',
    'manager.updating': 'Atualizando...',

    'settings.title': 'Configurações',
    'settings.description': 'Configure o caminho do jogo, preferências e comportamento do aplicativo.',
    'settings.game': 'Jogo',
    'settings.gamePath': 'Caminho da pasta do jogo',
    'settings.autoLaunch': 'Iniciar jogo automaticamente após instalação',
    'settings.backup': 'Criar backup antes da instalação',
    'settings.application': 'Aplicativo',
    'settings.language': 'Idioma',
    'settings.theme': 'Tema',
    'settings.themeLight': 'Claro',
    'settings.themeDark': 'Escuro',
    'settings.checkUpdates': 'Verificar atualizações',
    'settings.uninstall': 'Desinstalar',
    'settings.uninstallDesc': 'Remover o Asher da pasta do jogo e restaurar os arquivos originais quando possível.',
    'settings.uninstallAction': 'Desinstalar Asher',
    'settings.reset': 'Restaurar Padrões',
    'settings.save': 'Salvar Configurações',
    'settings.saving': 'Salvando configurações...',
    'settings.saved': 'Configurações salvas com sucesso.',
    'settings.resetDone': 'Configurações restauradas para os padrões.',
    'settings.pathValidating': 'Validando pasta do jogo...',
    'settings.pathValid': 'Pasta do jogo válida.',
    'settings.pathInvalid': 'Pasta do jogo inválida.',

    'uninstall.title': 'Desinstalando Asher',
    'uninstall.help': 'Isso removerá o Asher da pasta do jogo e restaurará os arquivos originais quando possível.',
    'uninstall.confirm': 'Tem certeza de que deseja desinstalar o Asher desta instalação?',
    'uninstall.action': 'Desinstalar',
    'uninstall.cancel': 'Cancelar',
    'uninstall.starting': 'Iniciando desinstalação...',
    'uninstall.inProgress': 'Desinstalando...',
    'uninstall.cancelling': 'Cancelando desinstalação...',
    'uninstall.success': 'Desinstalação concluída com sucesso.',
    'uninstall.continue': 'Continuar',
    'uninstall.failed': 'Falha na desinstalação.',
    'uninstall.cancelled': 'Desinstalação cancelada.',
    'uninstall.retry': 'Tentar Novamente',
    'uninstall.backManager': 'Voltar ao Gerenciador de Patches',

    'common.cancel': 'Cancelar',
    'common.error': 'Ocorreu um erro inesperado.',
    'common.configLost': 'A configuração do jogo não é mais válida. Configure a pasta do jogo novamente.',
    'common.logFile': 'Arquivo de log: {path}'
  },
  'es-ES': {
    'app.title': 'Asher',
    'app.subtitle.connecting': 'Conectando...',
    'app.subtitle.loading': 'Cargando...',
    'app.subtitle.disconnected': 'Desconectado',
    'app.subtitle.setup': 'Configuración del juego',
    'app.subtitle.home': 'Inicio',
    'app.subtitle.manager': 'Gestor de Parches',
    'app.subtitle.settings': 'Ajustes',
    'app.subtitle.install': 'Instalando Asher',
    'app.subtitle.uninstall': 'Desinstalando Asher',
    'host.notConnected': 'No conectado',

    'shell.starting': 'Iniciando aplicación',
    'shell.connecting': 'Conectando a Asher.Host...',
    'shell.loading': 'Cargando aplicación',
    'shell.loadingConfig': 'Leyendo configuración de la aplicación...',
    'shell.unavailable': 'Aplicación No Disponible',
    'shell.retry': 'Reintentar Conexión',
    'shell.hostError': 'El host de la aplicación Asher no está disponible.',

    'nav.welcome': 'Bienvenida',
    'nav.gameDetection': 'Detección del Juego',
    'nav.installing': 'Instalando',
    'nav.complete': 'Completado',
    'nav.home': 'Inicio',
    'nav.patchManager': 'Gestor de Parches',
    'nav.settings': 'Ajustes',

    'home.title': 'Bienvenido a Asher',
    'home.description': 'Gestiona mods e inicia tu juego con parches desde aquí.',
    'home.card.manager': 'Gestor de Parches',
    'home.card.managerDesc': 'Activar o desactivar mods individualmente',
    'home.card.settings': 'Ajustes',
    'home.card.settingsDesc': 'Preferencias, ruta del juego y desinstalación',
    'home.card.launch': 'Iniciar Juego',
    'home.card.launchDesc': 'Iniciar el juego con mods activados',
    'home.launchSuccess': 'Inicio del juego iniciado.',
    'home.launchError': 'Error al iniciar el juego.',

    'setup.title': 'Localizar Instalación del Juego',
    'setup.help': 'Encuentra la carpeta de instalación de Dust: An Elysian Tail.',
    'setup.autoDetect': 'Detectar Automáticamente',
    'setup.browse': 'Examinar...',
    'setup.validate': 'Validar',
    'setup.save': 'Guardar Configuración',
    'setup.validating': 'Validando carpeta del juego...',
    'setup.saving': 'Guardando configuración...',
    'setup.invalid': 'Carpeta del juego no válida.',
    'setup.valid': 'Juego válido encontrado — versión {version} ({source})',
    'setup.invalidExe': 'No se pudo encontrar DustAET.exe en esta carpeta.',
    'setup.pathPlaceholder': 'Ruta de la carpeta del juego',

    'ready.title': 'Juego Configurado',
    'ready.configured': 'Se encontró una instalación válida de Asher para esta carpeta.',
    'ready.needsInstall': 'La carpeta del juego está configurada. Instala Asher para habilitar soporte de mods.',
    'ready.folder': 'Carpeta',
    'ready.version': 'Versión',
    'ready.source': 'Origen',
    'ready.mode': 'Modo',
    'ready.modeManager': 'Gestor',
    'ready.modeWizard': 'Asistente de instalación',
    'ready.install': 'Instalar Asher',
    'ready.openManager': 'Abrir Gestor de Parches',
    'ready.reconfigure': 'Cambiar Carpeta del Juego',

    'install.title': 'Instalando Asher',
    'install.help': 'Preparando el juego para soporte de mods. No cierres la aplicación durante la instalación.',
    'install.start': 'Iniciar Instalación',
    'install.starting': 'Iniciando instalación...',
    'install.inProgress': 'Instalando...',
    'install.cancelling': 'Cancelando instalación...',
    'install.cancel': 'Cancelar',
    'install.success': 'Instalación completada con éxito.',
    'install.continue': 'Continuar al Inicio',
    'install.failed': 'Error en la instalación.',
    'install.cancelled': 'Instalación cancelada.',
    'install.retry': 'Intentar de Nuevo',
    'install.backSetup': 'Volver a Configuración',

    'manager.title': 'Gestor de Parches',
    'manager.help': 'Activar o desactivar mods instalados.',
    'manager.refresh': 'Actualizar',
    'manager.loading': 'Cargando mods...',
    'manager.empty': 'No se encontraron mods para la carpeta configurada.',
    'manager.active': 'Activos',
    'manager.total': 'Total',
    'manager.enabled': 'Activado',
    'manager.disabled': 'Desactivado',
    'manager.updating': 'Actualizando...',

    'settings.title': 'Ajustes',
    'settings.description': 'Configura la ruta del juego, preferencias y comportamiento de la aplicación.',
    'settings.game': 'Juego',
    'settings.gamePath': 'Ruta de la carpeta del juego',
    'settings.autoLaunch': 'Iniciar juego automáticamente tras instalar',
    'settings.backup': 'Crear copia de seguridad antes de instalar',
    'settings.application': 'Aplicación',
    'settings.language': 'Idioma',
    'settings.theme': 'Tema',
    'settings.themeLight': 'Claro',
    'settings.themeDark': 'Oscuro',
    'settings.checkUpdates': 'Buscar actualizaciones',
    'settings.uninstall': 'Desinstalar',
    'settings.uninstallDesc': 'Eliminar Asher de la carpeta del juego y restaurar archivos originales cuando sea posible.',
    'settings.uninstallAction': 'Desinstalar Asher',
    'settings.reset': 'Restaurar Valores',
    'settings.save': 'Guardar Ajustes',
    'settings.saving': 'Guardando ajustes...',
    'settings.saved': 'Ajustes guardados correctamente.',
    'settings.resetDone': 'Ajustes restaurados a los valores predeterminados.',
    'settings.pathValidating': 'Validando carpeta del juego...',
    'settings.pathValid': 'Carpeta del juego válida.',
    'settings.pathInvalid': 'Carpeta del juego no válida.',

    'uninstall.title': 'Desinstalando Asher',
    'uninstall.help': 'Esto eliminará Asher de la carpeta del juego y restaurará los archivos originales cuando sea posible.',
    'uninstall.confirm': '¿Estás seguro de que deseas desinstalar Asher de esta instalación?',
    'uninstall.action': 'Desinstalar',
    'uninstall.cancel': 'Cancelar',
    'uninstall.starting': 'Iniciando desinstalación...',
    'uninstall.inProgress': 'Desinstalando...',
    'uninstall.cancelling': 'Cancelando desinstalación...',
    'uninstall.success': 'Desinstalación completada con éxito.',
    'uninstall.continue': 'Continuar',
    'uninstall.failed': 'Error en la desinstalación.',
    'uninstall.cancelled': 'Desinstalación cancelada.',
    'uninstall.retry': 'Intentar de Nuevo',
    'uninstall.backManager': 'Volver al Gestor de Parches',

    'common.cancel': 'Cancelar',
    'common.error': 'Ocurrió un error inesperado.',
    'common.configLost': 'La configuración del juego ya no es válida. Configura la carpeta del juego de nuevo.',
    'common.logFile': 'Archivo de registro: {path}'
  }
};

/** @type {SupportedLanguage} */
let currentLanguage = 'en-US';

/** @type {Set<() => void>} */
const listeners = new Set();

/**
 * @param {string} key
 * @param {Record<string, string>} [params]
 */
export function t(key, params = {}) {
  const table = STRINGS[currentLanguage] ?? STRINGS['en-US'];
  let text = table[key] ?? STRINGS['en-US'][key] ?? key;

  for (const [name, value] of Object.entries(params)) {
    text = text.replaceAll(`{${name}}`, value);
  }

  return text;
}

/** @returns {SupportedLanguage} */
export function getLanguage() {
  return currentLanguage;
}

/**
 * @param {string} language
 */
export function setLanguage(language) {
  const normalized = normalizeLanguage(language);
  if (normalized === currentLanguage) {
    return;
  }

  currentLanguage = normalized;
  for (const listener of listeners) {
    listener();
  }
}

/**
 * @param {() => void} listener
 */
export function onLanguageChange(listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/**
 * @param {string} language
 * @returns {SupportedLanguage}
 */
export function normalizeLanguage(language) {
  if (language === 'pt-BR' || language?.startsWith('pt')) {
    return 'pt-BR';
  }
  if (language === 'es-ES' || language?.startsWith('es')) {
    return 'es-ES';
  }
  return 'en-US';
}

/** @returns {{ value: SupportedLanguage, label: string }[]} */
export function getLanguageOptions() {
  return [
    { value: 'en-US', label: 'English' },
    { value: 'pt-BR', label: 'Português (Brasil)' },
    { value: 'es-ES', label: 'Español' }
  ];
}

/**
 * Apply language from persisted settings.
 * @param {object | null | undefined} settings
 */
export function applyLanguageFromSettings(settings) {
  if (settings?.language) {
    setLanguage(settings.language);
  }
}
