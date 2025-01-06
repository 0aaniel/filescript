const { contextBridge, ipcRenderer } = require('electron');

// Provide a safe, controlled API to the renderer
contextBridge.exposeInMainWorld('electronAPI', {
    onBackendReady: (callback) => ipcRenderer.on('backend-ready', (event, ...args) => callback(...args)),
    onBackendError: (callback) => ipcRenderer.on('backend-error', (event, ...args) => callback(...args))
});

// Expose an empty FileScriptApp object so we can attach methods from renderer.js
contextBridge.exposeInMainWorld('FileScriptApp', {});
