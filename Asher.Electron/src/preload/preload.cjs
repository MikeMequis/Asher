const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('asher', {
  getLogPath: () => ipcRenderer.invoke('asher:get-log-path'),
  log: (level, source, message, data) =>
    ipcRenderer.invoke('asher:log', { level, source, message, data }),
  getHostStatus: () => ipcRenderer.invoke('host:get-status'),
  startHost: () => ipcRenderer.invoke('host:start'),
  invoke: (method, params, options) =>
    ipcRenderer.invoke('asher:invoke', {
      method,
      params,
      trackProgress: options?.trackProgress ?? false,
      allowFailure: options?.allowFailure ?? false
    }),
  onOperationStarted: (callback) => {
    const listener = (_event, data) => callback(data);
    ipcRenderer.on('asher:operation-started', listener);
    return () => ipcRenderer.removeListener('asher:operation-started', listener);
  },
  onHostStatusChanged: (callback) => {
    const listener = (_event, status) => callback(status);
    ipcRenderer.on('host:status-changed', listener);
    return () => ipcRenderer.removeListener('host:status-changed', listener);
  },
  onProgress: (callback) => {
    const listener = (_event, data) => callback(data);
    ipcRenderer.on('asher:progress', listener);
    return () => ipcRenderer.removeListener('asher:progress', listener);
  },
  pickFolder: () => ipcRenderer.invoke('dialog:pick-folder'),
  relocateLogs: (gameFolderPath) => ipcRenderer.invoke('asher:relocate-logs', gameFolderPath),
  getAppVersion: () => ipcRenderer.invoke('app:get-version'),
  checkForUpdates: (options) => ipcRenderer.invoke('updater:check', options),
  downloadAndApplyUpdate: (params) => ipcRenderer.invoke('updater:download-and-apply', params),
  openReleasePage: (url) => ipcRenderer.invoke('updater:open-release', url),
  onUpdaterStatus: (callback) => {
    const listener = (_event, data) => callback(data);
    ipcRenderer.on('updater:status', listener);
    return () => ipcRenderer.removeListener('updater:status', listener);
  },
  minimizeWindow: () => ipcRenderer.invoke('window:minimize')
});
