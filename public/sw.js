const CACHE = 'our-tasks-shell-v1';
const RUNTIME = 'our-tasks-runtime-v1';
const scopePath = new URL(self.registration.scope).pathname;
const shell = [scopePath, `${scopePath}manifest.webmanifest`, `${scopePath}icons/app-icon.svg`];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(shell)).then(() => self.skipWaiting()));
});
self.addEventListener('activate', (event) => {
  event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => ![CACHE, RUNTIME].includes(key)).map((key) => caches.delete(key)))).then(() => self.clients.claim()));
});
self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== location.origin) return;
  if (url.pathname.includes('/unity/house/Build/')) {
    event.respondWith(caches.open(RUNTIME).then(async (cache) => (await cache.match(request)) || fetch(request).then((response) => { if (response.ok) cache.put(request, response.clone()); return response; })));
    return;
  }
  event.respondWith(fetch(request).then((response) => { if (response.ok) caches.open(RUNTIME).then((cache) => cache.put(request, response.clone())); return response; }).catch(() => caches.match(request).then((cached) => cached || caches.match(scopePath))));
});
