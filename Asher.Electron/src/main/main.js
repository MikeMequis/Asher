import { app, BrowserWindow, dialog, ipcMain, Menu } from 'electron';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  getDiagnosticLogPath,
  initDiagnosticLogger,
  relocateDiagnosticLogger,
  writeDiagnosticLog
} from './diagnostic-logger.js';
import { resolveGameFolderFromSettings } from './log-path-resolver.js';
import { HostManager } from './host-manager.js';
import {
  checkForUpdates,
  downloadAndApplyUpdate,
  initAutoUpdater,
  openReleasePage
} from './auto-updater.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/** @type {BrowserWindow | null} */
let mainWindow = null;
const hostManager = new HostManager();

function broadcast(channel, payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, payload);
    return true;
  }

  writeDiagnosticLog('warn', 'main', `broadcast skipped: ${channel}`);
  return false;
}

function broadcastHostStatus() {
  broadcast('host:status-changed', {
    status: hostManager.status,
    message: hostManager.statusMessage
  });
}

function tryRelocateLogsFromParams(params) {
  const gameFolderPath =
    typeof params?.gameFolderPath === 'string'
      ? params.gameFolderPath
      : typeof params?.path === 'string'
        ? params.path
        : null;

  if (gameFolderPath) {
    relocateDiagnosticLogger(gameFolderPath);
  }
}

function createWindow() {
  const preloadPath = path.join(__dirname, '..', 'preload', 'preload.cjs');

  mainWindow = new BrowserWindow({
    width: 720,
    height: 640,
    minWidth: 1000,
    minHeight: 700,
    autoHideMenuBar: true,
    show: false,
    backgroundColor: '#0d1117',
    webPreferences: {
      preload: preloadPath,
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  });

  mainWindow.webContents.on('did-finish-load', () => {
    broadcastHostStatus();
  });

  mainWindow.webContents.on('did-fail-load', (_event, errorCode, errorDescription, validatedURL) => {
    writeDiagnosticLog('error', 'main', 'renderer failed to load', {
      errorCode,
      errorDescription,
      validatedURL
    });
  });

  mainWindow.webContents.on('preload-error', (_event, preloadPathValue, error) => {
    writeDiagnosticLog('error', 'main', 'preload error', {
      preloadPath: preloadPathValue,
      error: error?.message ?? String(error)
    });
  });

  const indexPath = path.join(__dirname, '..', 'renderer', 'index.html');
  mainWindow.loadFile(indexPath);

  mainWindow.once('ready-to-show', () => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.show();
    }
  });
}

hostManager.on('status-changed', () => {
  broadcastHostStatus();
});

ipcMain.handle('asher:get-log-path', () => getDiagnosticLogPath());

ipcMain.handle('app:get-version', () => app.getVersion());

ipcMain.handle('updater:check', (_event, options) => checkForUpdates(options ?? {}));

ipcMain.handle('updater:download-and-apply', (_event, params) => downloadAndApplyUpdate(params ?? {}));

ipcMain.handle('updater:open-release', (_event, url) => openReleasePage(url));

ipcMain.handle('window:minimize', () => {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.minimize();
  }
});

ipcMain.handle('asher:relocate-logs', (_event, gameFolderPath) =>
  relocateDiagnosticLogger(gameFolderPath)
);

ipcMain.handle('asher:log', (_event, { level, source, message, data }) => {
  const normalizedLevel = level === 'error' || level === 'warn' ? level : 'info';
  writeDiagnosticLog(normalizedLevel, source ?? 'renderer', message, data);
});

ipcMain.handle('host:get-status', () => ({
  status: hostManager.status,
  message: hostManager.statusMessage,
  hostPath: hostManager.hostPath
}));

ipcMain.handle('host:start', async () => {
  if (hostManager.status === 'ready') {
    return { status: hostManager.status, message: hostManager.statusMessage };
  }

  try {
    await hostManager.start();
    return { status: hostManager.status, message: hostManager.statusMessage };
  } catch (err) {
    const message = err instanceof Error ? err.message : 'Failed to start host';
    writeDiagnosticLog('error', 'host', 'start failed', { message });
    return { status: hostManager.status, message };
  }
});

ipcMain.handle('dialog:pick-folder', async () => {
  const result = await dialog.showOpenDialog(mainWindow ?? undefined, {
    title: 'Select Dust: An Elysian Tail installation folder',
    properties: ['openDirectory']
  });

  if (result.canceled || result.filePaths.length === 0) {
    return null;
  }

  return result.filePaths[0];
});

ipcMain.handle('asher:invoke', async (_event, { method, params, trackProgress, allowFailure }) => {
  if (method === 'saveSettings' || method === 'markAsInstalled') {
    tryRelocateLogsFromParams(params);
  }

  const client = hostManager.client;
  if (!client || hostManager.status !== 'ready') {
    const error = new Error('Host is not available. Wait for connection or restart the application.');
    writeDiagnosticLog('error', 'ipc', `${method} blocked`, { hostStatus: hostManager.status });
    throw error;
  }

  /** @type {string | null} */
  let requestId = null;

  try {
    const result = await client.request(method, params, {
      allowFailure: allowFailure ?? false,
      onStarted: (id) => {
        requestId = id;
        broadcast('asher:operation-started', { method, requestId: id });
      },
      onProgress: trackProgress
        ? (progress) => {
            broadcast('asher:progress', { method, requestId, progress });
          }
        : undefined
    });

    if (method === 'getSettings' && result?.gameFolderPath) {
      relocateDiagnosticLogger(result.gameFolderPath);
    }

    if (method === 'markUninstalled') {
      relocateDiagnosticLogger(resolveGameFolderFromSettings());
    }

    return { requestId, result };
  } catch (err) {
    writeDiagnosticLog('error', 'ipc', `${method} failed`, {
      requestId,
      error: err instanceof Error ? err.message : String(err)
    });
    throw err;
  }
});

app.whenReady().then(() => {
  Menu.setApplicationMenu(null);
  initDiagnosticLogger();
  createWindow();
  initAutoUpdater(broadcast);

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', async (event) => {
  if (hostManager.status === 'stopped' || hostManager.status === 'terminated') {
    return;
  }

  event.preventDefault();
  await hostManager.stop();
  app.exit(0);
});
