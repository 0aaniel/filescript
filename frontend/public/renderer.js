const API_BASE_URL = 'https://localhost:5001/api';  // Changed from https to http
let currentContainer = null;
let currentPath = '/';
let isBackendReady = false;

// Bootstrap Modal instances
let createContainerModal = null;
let createDirModal = null;

// Define the functions first
const FileScriptAppFunctions = {
    showCreateContainerModal: function() {
        try {
            if (!createContainerModal) {
                console.error('Modal not initialized');
                return;
            }
            document.getElementById('containerName').value = '';
            document.getElementById('containerPath').value = '';
            createContainerModal.show();
        } catch (error) {
            console.error('Error in showCreateContainerModal:', error);
        }
    },
    createContainer: async function() {
        try {
            if (!isBackendReady) {
                await waitForBackend();
            }
            const name = document.getElementById('containerName').value;
            const path = document.getElementById('containerPath').value;
            
            if (!name || !path) {
                alert('Please fill in all fields');
                return;
            }
        
            const response = await fetch(`https://localhost:5001/api/container/create`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({
                    containerName: name,
                    containerFilePath: path,
                    blockSize: 4096,
                    totalBlocks: 1000
                })
            });
        
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to create container');
            }
    
            const data = await response.json();
            createContainerModal.hide();
            await loadContainers();
            selectContainer(name);
        } catch (error) {
            console.error('Error creating container:', error);
            alert(error.message || 'Failed to create container');
        }
    },
    showCreateDirModal: function() {
        try {
            if (!createDirModal) {
                console.error('Modal not initialized');
                return;
            }
            document.getElementById('directoryName').value = '';
            createDirModal.show();
        } catch (error) {
            console.error('Error in showCreateDirModal:', error);
        }
    },
    showUploadFileModal: function() {
        alert('Upload file modal not yet implemented.');
    },
    refreshContent: function() {
        if (!currentContainer) {
            alert('No container selected.');
            return;
        }
        loadDirectoryContents(currentContainer, currentPath);
    },
    createDirectory: async function() {
        try {
            if (!currentContainer) {
                alert('Please select a container first');
                return;
            }
            const dirName = document.getElementById('directoryName').value;
            if (!dirName) {
                alert('Please enter a directory name');
                return;
            }
            const response = await fetch(`${API_BASE_URL}/container/${currentContainer}/directory/md`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({
                    directoryName: dirName,
                    path: currentPath
                })
            });
            if (!response.ok) {
                throw new Error('Failed to create directory');
            }
            createDirModal.hide();
            await loadDirectoryContents(currentContainer, currentPath);
        } catch (error) {
            console.error('Error creating directory:', error);
            alert(error.message);
        }
    },
    selectContainer: function(container) {
        currentContainer = container;
        currentPath = '/';
        document.getElementById('currentPath').textContent = currentPath;
        loadDirectoryContents(currentContainer, currentPath);
    },
    // ...other existing methods...
};

// Wait for DOM content to be loaded
document.addEventListener('DOMContentLoaded', () => {
    try {
        // Initialize Bootstrap modals
        createContainerModal = new bootstrap.Modal(document.getElementById('createContainerModal'));
        createDirModal = new bootstrap.Modal(document.getElementById('createDirModal'));

        if (currentContainer) {
            Object.assign(window.FileScriptApp, FileScriptAppFunctions);
        } else {
            console.error('currentContainer is not defined');
        }

        // Expose functions to window.FileScriptApp
        window.FileScriptApp = FileScriptAppFunctions; // Fix here

        // Load initial data
        loadContainers();



        // Attach event listeners
        document.getElementById('createContainerBtn')
                ?.addEventListener('click', () => FileScriptApp.showCreateContainerModal());
        document.getElementById('createDirBtn')
                ?.addEventListener('click', () => FileScriptApp.showCreateDirModal());
        document.getElementById('uploadFileBtn')
                ?.addEventListener('click', () => FileScriptApp.showUploadFileModal());
        document.getElementById('refreshBtn')
                ?.addEventListener('click', () => FileScriptApp.refreshContent());
        
        // Attach modal confirm button listeners
        document.getElementById('createContainerConfirmBtn')
                ?.addEventListener('click', () => FileScriptApp.createContainer());
        document.getElementById('createDirConfirmBtn')
                ?.addEventListener('click', () => FileScriptApp.createDirectory());
    } catch (error) {
        console.error('Error during initialization:', error);
    }
});

// Listen for backend status
window.electronAPI.onBackendReady((event, ready) => {
    isBackendReady = ready;
    if (ready) {
        loadContainers();
    }
});

async function waitForBackend(retries = 5, delay = 2000) {
    for (let i = 0; i < retries; i++) {
        try {
            const response = await fetch(`https://localhost:5001/api/health/ping`, {
                headers: {
                    'Accept': 'application/json'
                },
                method: 'GET',
                mode: 'cors',

            });
            if (response.ok) {
                isBackendReady = true;
                return true;
            }
        } catch (error) {
            console.log(`Backend not ready, attempt ${i + 1} of ${retries}`);
        }
        await new Promise(resolve => setTimeout(resolve, delay));
    }
    throw new Error('Backend failed to start');
}

async function loadContainers() {
    try {
        if (!isBackendReady) {
            await waitForBackend();
        }

        const response = await fetch(`https://localhost:5001/api/container/list`, {
            headers: {
                'Accept': 'application/json'
            },
            method: 'GET',
            mode: 'cors',
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        const containerList = document.getElementById('containerList');
        containerList.innerHTML = '';
        
        if (Array.isArray(data.containers)) {
            data.containers.forEach(container => {
                const div = document.createElement('div');
                div.className = `container-item ${currentContainer === container ? 'active' : ''}`;
                div.textContent = container;
                div.onclick = () => FileScriptApp.selectContainer(container);
                containerList.appendChild(div);
            });
        } else {
            console.error('Invalid response format:', data);
        }
    } catch (error) {
        console.error('Error loading containers:', error);
        if (error.message.includes('Backend failed to start')) {
            alert('Could not connect to the backend server. Please restart the application.');
        }
    }
}

async function loadDirectoryContents(container, path) {
    try {
        const response = await fetch(`${API_BASE_URL}/container/${container}/directory/ls`);
        if (!response.ok) {
            throw new Error('Failed to load directory contents');
        }
        const data = await response.json();
        const directoryList = document.getElementById('directoryList');
        const fileList = document.getElementById('fileList');
        directoryList.innerHTML = '';
        fileList.innerHTML = '';

        if (Array.isArray(data.directories)) {
            data.directories.forEach(dir => {
                const div = document.createElement('div');
                div.className = 'directory-item';
                div.textContent = dir;
                div.onclick = () => {
                    currentPath = currentPath.endsWith('/') ? currentPath + dir : currentPath + '/' + dir;
                    document.getElementById('currentPath').textContent = currentPath;
                    loadDirectoryContents(currentContainer, currentPath);
                };
                directoryList.appendChild(div);
            });
        }
        if (Array.isArray(data.files)) {
            data.files.forEach(file => {
                const div = document.createElement('div');
                div.className = 'file-item';
                div.textContent = file;
                fileList.appendChild(div);
            });
        }
    } catch (error) {
        console.error('Error loading directory contents:', error);
        alert('Could not load directory contents.');
    }
}

// ...rest of existing code...