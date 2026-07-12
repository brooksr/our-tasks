export function registerServiceWorker() {
  if (!('serviceWorker' in navigator) || import.meta.env.DEV) return;
  window.addEventListener('load', async () => {
    try {
      const registration = await navigator.serviceWorker.register(`${import.meta.env.BASE_URL}sw.js`, { scope: import.meta.env.BASE_URL });
      registration.addEventListener('updatefound', () => {
        const worker = registration.installing;
        worker?.addEventListener('statechange', () => {
          if (worker.state === 'installed' && navigator.serviceWorker.controller) {
            window.dispatchEvent(new CustomEvent('app-update-ready'));
          }
        });
      });
    } catch (error) { console.warn('[pwa] Service worker registration failed.', error); }
  });
}
