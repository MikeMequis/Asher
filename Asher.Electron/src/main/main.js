import { app, BrowserWindow, dialog, ipcMain } from 'electron';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  getDiagnosticLogPath,
  initDiagnosticLogger,
  writeDiagnosticLog
} from './diagnostic-logger.js';
import { HostManager } from './host-manager.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/** @type {BrowserWindow | null} */
let mainWindow = null;
const hostManager = new HostManager();

function broadcast(channel, payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, payload);
    return true;
  }

  writeDiagnosticLog('warn', 'main', `broadcast skipped (no window): ${channel}`, payload);
  return false;
}

function broadcastHostStatus() {
  const status = {
    status: hostManager.status,
    message: hostManager.statusMessage
  };
  writeDiagnosticLog('info', 'main', 'broadcast host status', status);
  broadcast('host:status-changed', status);
}

function createWindow() {
  const preloadPath = path.join(__dirname, '..', 'preload', 'preload.cjs');
  writeDiagnosticLog('info', 'main', 'creating window', { preloadPath });

  mainWindow = new BrowserWindow({
    width: 720,
    height: 640,
    webPreferences: {
      preload: preloadPath,
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  });

  mainWindow.webContents.on('did-finish-load', () => {
    writeDiagnosticLog('info', 'main', 'renderer did-finish-load');
    broadcastHostStatus();
  });

  mainWindow.webContents.on('did-fail-load', (_event, errorCode, errorDescription, validatedURL) => {
    writeDiagnosticLog('error', 'main', 'renderer did-fail-load', {
      errorCode,
      errorDescription,
      validatedURL
    });
  });

  mainWindow.webContents.on('preload-error', (_event, preloadPathValue, error) => {
    writeDiagnosticLog('error', 'main', 'preload-error', {
      preloadPath: preloadPathValue,
      error: error?.message ?? String(error)
    });
  });

  mainWindow.webContents.on('console-message', (_event, level, message, line, sourceId) => {
    writeDiagnosticLog('info', 'renderer-console', message, { level, line, sourceId });
  });

  const indexPath = path.join(__dirname, '..', 'renderer', 'index.html');
  writeDiagnosticLog('info', 'main', 'loading renderer', { indexPath });
  mainWindow.loadFile(indexPath);
}

hostManager.on('status-changed', () => {
  broadcastHostStatus();
});

ipcMain.handle('asher:get-log-path', () => getDiagnosticLogPath());

ipcMain.handle('asher:log', (_event, { level, source, message, data }) => {
  const normalizedLevel = level === 'error' || level === 'warn' ? level : 'info';
  writeDiagnosticLog(normalizedLevel, source ?? 'renderer', message, data);
});

ipcMain.handle('host:get-status', () => {
  const status = {
    status: hostManager.status,
    message: hostManager.statusMessage,
    hostPath: hostManager.hostPath
  };
  writeDiagnosticLog('info', 'ipc', 'host:get-status', status);
  return status;
});

ipcMain.handle('host:start', async () => {
  writeDiagnosticLog('info', 'ipc', 'host:start requested', {
    currentStatus: hostManager.status
  });

  if (hostManager.status === 'ready') {
    const status = { status: hostManager.status, message: hostManager.statusMessage };
    writeDiagnosticLog('info', 'ipc', 'host:start already ready', status);
    return status;
  }

  try {
    await hostManager.start();
    const status = { status: hostManager.status, message: hostManager.statusMessage };
    writeDiagnosticLog('info', 'ipc', 'host:start completed', status);
    return status;
  } catch (err) {
    const status = {
      status: hostManager.status,
      message: err instanceof Error ? err.message : 'Failed to start host'
    };
    writeDiagnosticLog('error', 'ipc', 'host:start failed', status);
    return status;
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
  writeDiagnosticLog('info', 'ipc', `asher:invoke ${method}`, {
    trackProgress: Boolean(trackProgress),
    allowFailure: Boolean(allowFailure)
  });

  const client = hostManager.client;
  if (!client || hostManager.status !== 'ready') {
    const error = new Error('Host is not available. Wait for connection or restart the application.');
    writeDiagnosticLog('error', 'ipc', `asher:invoke blocked for ${method}`, {
      hostStatus: hostManager.status
    });
    throw error;
  }

  const progressEvents = [];
  /** @type {string | null} */
  let requestId = null;

  const result = await client.request(method, params, {
    allowFailure: allowFailure ?? false,
    onStarted: (id) => {
      requestId = id;
      broadcast('asher:operation-started', { method, requestId: id });
    },
    onProgress: trackProgress
      ? (progress) => {
          const payload = { method, requestId, progress };
          progressEvents.push(payload);
          broadcast('asher:progress', payload);
        }
      : undefined
  });

  writeDiagnosticLog('info', 'ipc', `asher:invoke ${method} completed`, {
    requestId,
    progressCount: progressEvents.length
  });

  return { requestId, result, progressCount: progressEvents.length };
});

app.whenReady().then(() => {
  const logPath = initDiagnosticLogger(app.getPath('userData'));
  writeDiagnosticLog('info', 'main', 'app ready', { logPath });
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  writeDiagnosticLog('info', 'main', 'window-all-closed');
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', async (event) => {
  writeDiagnosticLog('info', 'main', 'before-quit', { hostStatus: hostManager.status });
  if (hostManager.status === 'stopped' || hostManager.status === 'terminated') {
    return;
  }

  event.preventDefault();
  await hostManager.stop();
  app.exit(0);
});
