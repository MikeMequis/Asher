import { app, BrowserWindow, dialog, ipcMain } from 'electron';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { HostManager } from './host-manager.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/** @type {BrowserWindow | null} */
let mainWindow = null;
const hostManager = new HostManager();

function broadcast(channel, payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, payload);
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 720,
    height: 640,
    webPreferences: {
      preload: path.join(__dirname, '..', 'preload', 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  });

  mainWindow.loadFile(path.join(__dirname, '..', 'renderer', 'index.html'));
}

hostManager.on('status-changed', (status) => {
  broadcast('host:status-changed', status);
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
    return {
      status: hostManager.status,
      message: err.message ?? 'Failed to start host'
    };
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
  const client = hostManager.client;
  if (!client || hostManager.status !== 'ready') {
    throw new Error('Host is not available. Wait for connection or restart the application.');
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

  return { requestId, result, progressCount: progressEvents.length };
});

app.whenReady().then(async () => {
  createWindow();

  try {
    await hostManager.start();
  } catch {
    // Status already updated; renderer shows the error.
  }

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
