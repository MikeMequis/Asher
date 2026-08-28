import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('asher', {
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
  pickFolder: () => ipcRenderer.invoke('dialog:pick-folder')
});
