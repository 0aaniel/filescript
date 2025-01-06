import React from 'react';
import { createRoot } from 'react-dom/client'; // Import createRoot from react-dom/client
import App from './App'; // Assuming you have an App component

const container = document.getElementById('root'); // Ensure this ID matches your HTML
const root = createRoot(container); // Create a root using createRoot
root.render(<App />); // Use root.render instead of ReactDOM.render
