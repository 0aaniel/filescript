// main.js
const { app, BrowserWindow } = require('electron');
const path = require('path');
const { spawn } = require('child_process');
const fetch = require('node-fetch'); // If needed (npm install node-fetch@2)
let mainWindow;
let backendProcess;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    }
  });

  mainWindow.loadFile(path.join(__dirname, 'public', 'index.html'));
  mainWindow.webContents.openDevTools(); // Open DevTools for debugging

  mainWindow.on('closed', () => {
    mainWindow = null;
    if (backendProcess) {
      backendProcess.kill();
    }
  });
}

app.whenReady().then(() => {
  createWindow();

  // --- Start .NET backend ------------------------------------------------
  // Path to your .csproj:
  const backendPath = path.join(__dirname, 'backend', 'Filescript.Backend', 'Filescript.Backend.csproj');
  console.log('Starting backend from:', backendPath);

  backendProcess = spawn('dotnet', ['run', '--project', backendPath], {
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: 'http://localhost:5001'
    },
    shell: true,
    stdio: 'inherit'
  });

  backendProcess.on('error', (err) => {
    console.error('Failed to start backend:', err);
    if (mainWindow) {
      mainWindow.webContents.send('backend-error', err.message);
    }
  });

  // Ping the backend after ~5 seconds to check if it's ready
  setTimeout(async () => {
    try {
      const response = await fetch('http://localhost:5001/api/health');
      if (response.ok) {
        console.log('Backend is healthy and responding');
        if (mainWindow) {
          mainWindow.webContents.send('backend-ready', true);
        }
      } else {
        throw new Error('Backend health check failed with non-200');
      }
    } catch (error) {
      console.error('Backend health check failed:', error);
      if (mainWindow) {
        mainWindow.webContents.send('backend-error', error.message);
      }
    }
  }, 5000);
  // -----------------------------------------------------------------------

  app.on('activate', function () {
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
