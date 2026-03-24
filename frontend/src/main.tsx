import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './styles/auth.css'
// Import Excalidraw CSS - Vite will handle this at build time
import '@alkemio/excalidraw/index.css'
import App from './App.tsx'

const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Root element not found')
}

// Show loading message immediately
if (rootElement) {
  rootElement.innerHTML = '<div style="padding: 20px; text-align: center; font-family: sans-serif;">Loading application...</div>'
}

try {
  const root = createRoot(rootElement)
  root.render(
    <StrictMode>
      <App />
    </StrictMode>,
  )
} catch (error) {
  console.error('Failed to render React app:', error)
  if (rootElement) {
    const errorMessage = error instanceof Error ? error.stack || error.message : String(error)
    rootElement.innerHTML = `
      <div style="padding: 20px; font-family: sans-serif; max-width: 800px; margin: 20px auto;">
        <h1 style="color: #ef4444;">Error Loading Application</h1>
        <p>There was an error loading the application. Please check the browser console (F12) for details.</p>
        <details style="margin-top: 20px; border: 1px solid #ddd; padding: 10px; border-radius: 4px;">
          <summary style="cursor: pointer; font-weight: bold; padding: 5px;">Error Details (Click to expand)</summary>
          <pre style="background: #f0f0f0; padding: 10px; border-radius: 4px; overflow: auto; max-width: 100%; word-wrap: break-word; white-space: pre-wrap; margin-top: 10px;">${errorMessage}</pre>
        </details>
      </div>
    `
  }
}

// Catch any unhandled errors that might prevent React from mounting
window.addEventListener('error', (event) => {
  console.error('Unhandled error:', event.error)
  const rootEl = document.getElementById('root')
  if (rootEl && rootEl.innerHTML.trim() === '' || rootEl?.innerHTML.includes('Loading application')) {
    rootEl.innerHTML = `
      <div style="padding: 20px; font-family: sans-serif; max-width: 800px; margin: 20px auto;">
        <h1 style="color: #ef4444;">Application Error</h1>
        <p>An error occurred: ${event.error?.message || event.message || 'Unknown error'}</p>
        <p>Please check the browser console (F12) for more details.</p>
      </div>
    `
  }
})
