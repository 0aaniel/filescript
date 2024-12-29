const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

let mainWindow;
let backendProcess;

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 800,
        height: 600,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false,
        },
    });

    mainWindow.loadURL('http://localhost:3000');

    mainWindow.on('closed', () => {
        mainWindow = null;
        if (backendProcess) {
            backendProcess.kill();
        }
    });
}

app.on('ready', () => {
    createWindow();

    backendProcess = spawn('dotnet', ['../backend/FileSystemSimulator.Backend/bin/Debug/net6.0/FileSystemSimulator.Backend.dll'], {
        cwd: path.join(__dirname, '..', 'backend', 'FileSystemSimulator.Backend'),
        shell: true,
        stdio: 'inherit',
    });

    backendProcess.on('close', (code) => {
        console.log(`Backend process exited with code ${code}`);
    });

    app.on('window-all-closed', () => {
        if (process.platform !== 'darwin') {
            app.quit();
        }
    });

    app.on('activate', () => {
        if (mainWindow === null) {
            createWindow();
        }
    });
});