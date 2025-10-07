// H2 Booking service worker – simpel cache + offline-fallback
const CACHE = 'h2booking-cache-v2';
const ASSET_URLS = [
    '/',              
    '/index.html',
    '/manifest.webmanifest', 
    '/favicon.png',
    '/icon-192.png',
    '/icon-512.png',
    '/css/app.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/Blazor.styles.css'
];

// Kun same-origin caches
const sameOrigin = (url) => new URL(url).origin === self.location.origin;

// Installer: cache assets (fortsæt selv ved fejl)

self.addEventListener('install', (event) => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE);

        // Cache each asset individually so one failure doesn't abort install
        const results = await Promise.allSettled(
            ASSET_URLS.map((u) => cache.add(u))
        );

        const failed = results.filter(r => r.status === 'rejected');
        if (failed.length) {
            // Log but continue (don’t break install)
            console.warn('SW: some assets failed to cache:', failed.length);
        }

        await self.skipWaiting();
    })());
});
// Aktivér: ryd gamle caches og tag kontrol

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        // Clean old caches
        const keys = await caches.keys();
        await Promise.all(keys.map(k => (k !== CACHE) && caches.delete(k)));
        await self.clients.claim();
    })());
});
// Fetch: håndter cache-strategier

self.addEventListener('fetch', (event) => {
    const req = event.request;
    const url = new URL(req.url);

    // Håndter aldrig /api/-kald
    if (url.pathname.startsWith('/api/')) return;

    // Ignorer cross-origin (lad netværket tage dem)
    if (!sameOrigin(req.url)) return;

    // Navigation: network-first, fallback til index.html
    if (req.mode === 'navigate') {
        event.respondWith((async () => {
            try {
                const fresh = await fetch(req);
                return fresh;
            } catch {
                const cache = await caches.open(CACHE);
                const fallback = await cache.match('/index.html');
                return fallback || new Response('Offline', { status: 503 });
            }
        })());
        return;
    }

    // Øvrige GET (same-origin): cache-first, derefter netværk og gem svaret til senere
    if (req.method === 'GET') {
        event.respondWith((async () => {
            const cached = await caches.match(req);
            if (cached) return cached;

            try {
                const res = await fetch(req);
                if (res && res.status === 200 && res.type === 'basic') {
                    const copy = res.clone();
                    const cache = await caches.open(CACHE);
                    cache.put(req, copy);
                }
                return res;
            } catch {
                return caches.match('/index.html');
            }
        })());
    }
});
