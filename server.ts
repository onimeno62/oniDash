import express, { Request, Response } from 'express';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = 3000;

app.use(express.json());

// ==================== IN-MEMORY MOCK STORE ====================

interface Library {
  id: string;
  name: string;
  createdAtUtc: string;
}

interface LibrarySource {
  id: string;
  libraryId: string;
  name: string;
  rootPath: string;
  createdAtUtc: string;
}

const libraries: Library[] = [
  { id: 'lib-music', name: 'Music Vault', createdAtUtc: '2026-01-15T10:00:00Z' },
  { id: 'lib-cinema', name: 'Cinema & TV', createdAtUtc: '2026-01-16T12:30:00Z' },
  { id: 'lib-literature', name: 'Literature & Manga', createdAtUtc: '2026-01-18T15:45:00Z' },
];

const sources: LibrarySource[] = [
  { id: 'src-1', libraryId: 'lib-music', name: 'Local Flac Archive', rootPath: 'D:\\Media\\Music\\Lossless', createdAtUtc: '2026-01-15T10:05:00Z' },
  { id: 'src-2', libraryId: 'lib-cinema', name: '4K UltraHD Films', rootPath: 'D:\\Media\\Movies\\4K', createdAtUtc: '2026-01-16T12:35:00Z' },
  { id: 'src-3', libraryId: 'lib-literature', name: 'EPUB & CBZ Stash', rootPath: 'D:\\Media\\Books', createdAtUtc: '2026-01-18T15:50:00Z' },
];

const musicArtists = [
  { id: 'art-1', name: 'Radiohead' },
  { id: 'art-2', name: 'Miles Davis' },
  { id: 'art-3', name: 'Daft Punk' },
  { id: 'art-4', name: 'Nujabes' },
  { id: 'art-5', name: 'Chopin' },
];

const musicAlbums = [
  { id: 'alb-1', title: 'In Rainbows', artistName: 'Radiohead', year: 2007, hasCover: true },
  { id: 'alb-2', title: 'Kind of Blue', artistName: 'Miles Davis', year: 1959, hasCover: true },
  { id: 'alb-3', title: 'Discovery', artistName: 'Daft Punk', year: 2001, hasCover: true },
  { id: 'alb-4', title: 'Modal Soul', artistName: 'Nujabes', year: 2005, hasCover: true },
  { id: 'alb-5', title: 'Nocturnes', artistName: 'Chopin', year: 1832, hasCover: true },
];

const musicTracks = [
  { id: 'trk-1', mediaItemId: 'med-trk-1', title: 'Weird Fishes / Arpeggi', artistName: 'Radiohead', albumTitle: 'In Rainbows', albumId: 'alb-1', hasCover: true, trackNumber: 4, discNumber: 1, year: 2007, durationSeconds: 318, genre: 'Art Rock', rating: 5 },
  { id: 'trk-2', mediaItemId: 'med-trk-2', title: 'Reckoner', artistName: 'Radiohead', albumTitle: 'In Rainbows', albumId: 'alb-1', hasCover: true, trackNumber: 7, discNumber: 1, year: 2007, durationSeconds: 290, genre: 'Art Rock', rating: 5 },
  { id: 'trk-3', mediaItemId: 'med-trk-3', title: 'So What', artistName: 'Miles Davis', albumTitle: 'Kind of Blue', albumId: 'alb-2', hasCover: true, trackNumber: 1, discNumber: 1, year: 1959, durationSeconds: 562, genre: 'Modal Jazz', rating: 5 },
  { id: 'trk-4', mediaItemId: 'med-trk-4', title: 'Blue in Green', artistName: 'Miles Davis', albumTitle: 'Kind of Blue', albumId: 'alb-2', hasCover: true, trackNumber: 3, discNumber: 1, year: 1959, durationSeconds: 337, genre: 'Modal Jazz', rating: 4 },
  { id: 'trk-5', mediaItemId: 'med-trk-5', title: 'Digital Love', artistName: 'Daft Punk', albumTitle: 'Discovery', albumId: 'alb-3', hasCover: true, trackNumber: 3, discNumber: 1, year: 2001, durationSeconds: 298, genre: 'French House', rating: 5 },
  { id: 'trk-6', mediaItemId: 'med-trk-6', title: 'Something About Us', artistName: 'Daft Punk', albumTitle: 'Discovery', albumId: 'alb-3', hasCover: true, trackNumber: 9, discNumber: 1, year: 2001, durationSeconds: 231, genre: 'French House', rating: 5 },
  { id: 'trk-7', mediaItemId: 'med-trk-7', title: 'Feather', artistName: 'Nujabes', albumTitle: 'Modal Soul', albumId: 'alb-4', hasCover: true, trackNumber: 1, discNumber: 1, year: 2005, durationSeconds: 175, genre: 'Lo-Fi Hip Hop', rating: 5 },
  { id: 'trk-8', mediaItemId: 'med-trk-8', title: 'Luv(sic) Part 3', artistName: 'Nujabes', albumTitle: 'Modal Soul', albumId: 'alb-4', hasCover: true, trackNumber: 8, discNumber: 1, year: 2005, durationSeconds: 336, genre: 'Lo-Fi Hip Hop', rating: 5 },
  { id: 'trk-9', mediaItemId: 'med-trk-9', title: 'Nocturne Op. 9 No. 2 in E-Flat Major', artistName: 'Chopin', albumTitle: 'Nocturnes', albumId: 'alb-5', hasCover: true, trackNumber: 2, discNumber: 1, year: 1832, durationSeconds: 272, genre: 'Classical', rating: 5 },
];

const musicFavorites = [
  { id: 'fav-1', entityType: 'track', entityId: 'trk-1', createdAtUtc: '2026-02-01T12:00:00Z' },
  { id: 'fav-2', entityType: 'album', entityId: 'alb-1', createdAtUtc: '2026-02-01T12:05:00Z' },
  { id: 'fav-3', entityType: 'artist', entityId: 'art-1', createdAtUtc: '2026-02-01T12:10:00Z' },
];

const musicPlaylists = [
  { id: 'pl-1', name: 'Late Night Focus', description: 'Introspective jazz, art rock & ambient beats', isSmart: false, updatedAtUtc: '2026-02-10T22:15:00Z', smartQuery: null },
  { id: 'pl-2', name: 'Top Rated Essentials', description: '5-star gems across all genres', isSmart: true, updatedAtUtc: '2026-02-12T14:30:00Z', smartQuery: 'rating:5' },
];

const movies = [
  { id: 'mov-1', mediaItemId: 'med-mov-1', title: 'Spirited Away', year: 2001, durationSeconds: 7500, container: 'mkv', hasPoster: true, watched: true, watchProgressSeconds: 7500, watchedAtUtc: '2026-02-14T21:00:00Z' },
  { id: 'mov-2', mediaItemId: 'med-mov-2', title: 'Blade Runner 2049', year: 2017, durationSeconds: 9840, container: 'mkv', hasPoster: true, watched: false, watchProgressSeconds: 4200, watchedAtUtc: null },
  { id: 'mov-3', mediaItemId: 'med-mov-3', title: 'Interstellar', year: 2014, durationSeconds: 10140, container: 'mp4', hasPoster: true, watched: true, watchProgressSeconds: 10140, watchedAtUtc: '2026-01-20T23:30:00Z' },
  { id: 'mov-4', mediaItemId: 'med-mov-4', title: 'Princess Mononoke', year: 1997, durationSeconds: 8040, container: 'mkv', hasPoster: true, watched: false, watchProgressSeconds: 1800, watchedAtUtc: null },
  { id: 'mov-5', mediaItemId: 'med-mov-5', title: 'Dune: Part Two', year: 2024, durationSeconds: 9960, container: 'mkv', hasPoster: true, watched: false, watchProgressSeconds: null, watchedAtUtc: null },
];

const books = [
  { id: 'bk-1', mediaItemId: 'med-bk-1', title: 'Dune', author: 'Frank Herbert', year: 1965, pageCount: 688, format: 'EPUB', hasCover: true, read: false, progressPages: 312, progressPercent: 45, lastReadAtUtc: '2026-02-15T19:00:00Z', rating: 5, favorite: true, series: 'Dune Chronicles' },
  { id: 'bk-2', mediaItemId: 'med-bk-2', title: 'Neuromancer', author: 'William Gibson', year: 1984, pageCount: 271, format: 'EPUB', hasCover: true, read: true, progressPages: 271, progressPercent: 100, lastReadAtUtc: '2026-01-10T14:20:00Z', rating: 5, favorite: true, series: 'Sprawl Trilogy' },
  { id: 'bk-3', mediaItemId: 'med-bk-3', title: 'Norwegian Wood', author: 'Haruki Murakami', year: 1987, pageCount: 296, format: 'EPUB', hasCover: true, read: false, progressPages: 84, progressPercent: 28, lastReadAtUtc: '2026-02-08T22:10:00Z', rating: 4, favorite: false, series: null },
  { id: 'bk-4', mediaItemId: 'med-bk-4', title: 'Klara and the Sun', author: 'Kazuo Ishiguro', year: 2021, pageCount: 303, format: 'EPUB', hasCover: true, read: true, progressPages: 303, progressPercent: 100, lastReadAtUtc: '2026-01-25T18:40:00Z', rating: 4, favorite: false, series: null },
];

const mangaList = [
  { id: 'mng-1', title: 'Berserk', author: 'Kentaro Miura', status: 'reading' as const, chapterCount: 375, unreadCount: 15, readCount: 360, lastReadAtUtc: '2026-02-14T18:00:00Z', latestChapter: 'Ch. 375', rating: 5, favorite: true, inLibrary: true, progressPercent: 96, year: 1989 },
  { id: 'mng-2', title: 'Chainsaw Man', author: 'Tatsuki Fujimoto', status: 'reading' as const, chapterCount: 185, unreadCount: 3, readCount: 182, lastReadAtUtc: '2026-02-12T16:30:00Z', latestChapter: 'Ch. 185', rating: 5, favorite: true, inLibrary: true, progressPercent: 98, year: 2018 },
  { id: 'mng-3', title: 'Vinland Saga', author: 'Makoto Yukimura', status: 'reading' as const, chapterCount: 215, unreadCount: 10, readCount: 205, lastReadAtUtc: '2026-02-05T20:15:00Z', latestChapter: 'Ch. 215', rating: 5, favorite: true, inLibrary: true, progressPercent: 95, year: 2005 },
  { id: 'mng-4', title: 'One Piece', author: 'Eiichiro Oda', status: 'reading' as const, chapterCount: 1130, unreadCount: 2, readCount: 1128, lastReadAtUtc: '2026-02-15T11:00:00Z', latestChapter: 'Ch. 1130', rating: 5, favorite: true, inLibrary: true, progressPercent: 99, year: 1997 },
];

let windowsConfig = {
  launchOnStartup: true,
  minimizeToTray: true,
  closeToTray: false,
  globalMediaKeysEnabled: true,
  notificationsEnabled: true,
};

const notifications = [
  { title: 'Library Synchronized', message: 'Indexed 9 audio tracks and 5 video files in local vault.', timestampUtc: new Date(Date.now() - 3600000).toISOString() },
  { title: 'New Chapter Available', message: 'Chainsaw Man Ch. 185 is ready to read.', timestampUtc: new Date(Date.now() - 86400000).toISOString() },
];

// Helper: generate SVG placeholder images with rich artistic gradients, vinyl groove details and warm tones
function generateCoverSvg(title: string, subtitle?: string, accentColor = '#7c6af5') {
  // Deterministic palette based on title
  const hash = title.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0);
  const palettes = [
    { start: '#2a1b18', mid: '#4d2a22', end: '#140c0b', accent: '#e27b58', glow: 'rgba(226, 123, 88, 0.4)', text: '#fcebe6' }, // Warm amber/terracotta
    { start: '#192026', mid: '#253540', end: '#0c1317', accent: '#62b0d9', glow: 'rgba(98, 176, 217, 0.35)', text: '#e6f4fa' }, // Deep oceanic slate
    { start: '#261726', mid: '#3e1d3e', end: '#120b12', accent: '#d962b8', glow: 'rgba(217, 98, 184, 0.35)', text: '#fcecf7' }, // Velvet wine
    { start: '#1f1e18', mid: '#363420', end: '#10100a', accent: '#d4b755', glow: 'rgba(212, 183, 85, 0.35)', text: '#fcf8e8' }, // Golden olive/sand
    { start: '#181e22', mid: '#20322b', end: '#0a100d', accent: '#4ecba2', glow: 'rgba(78, 203, 162, 0.35)', text: '#e6faf3' }, // Emerald sage
    { start: '#211826', mid: '#332342', end: '#0e0b12', accent: '#9b7df5', glow: 'rgba(155, 125, 245, 0.4)', text: '#f3eefc' }, // Twilight violet
  ];
  const p = palettes[hash % palettes.length];

  return `<svg xmlns="http://www.w3.org/2000/svg" width="500" height="500" viewBox="0 0 500 500">
    <defs>
      <linearGradient id="bg-${hash}" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="${p.start}"/>
        <stop offset="50%" stop-color="${p.mid}"/>
        <stop offset="100%" stop-color="${p.end}"/>
      </linearGradient>
      <radialGradient id="glow-${hash}" cx="50%" cy="38%" r="65%">
        <stop offset="0%" stop-color="${p.glow}"/>
        <stop offset="100%" stop-color="rgba(0,0,0,0)"/>
      </radialGradient>
      <linearGradient id="shine-${hash}" x1="0%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="rgba(255,255,255,0.18)"/>
        <stop offset="100%" stop-color="rgba(255,255,255,0.01)"/>
      </linearGradient>
      <filter id="noise-${hash}">
        <feTurbulence type="fractalNoise" baseFrequency="0.75" numOctaves="3" result="noise"/>
        <feColorMatrix type="matrix" values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 0.04 0"/>
        <feComposite in="SourceGraphic" in2="noise" operator="over"/>
      </filter>
    </defs>

    <!-- Canvas -->
    <rect width="500" height="500" fill="url(#bg-${hash})" rx="24"/>
    <rect width="500" height="500" fill="url(#glow-${hash})" rx="24"/>

    <!-- Subtle vinyl circular grooves in the artwork center -->
    <circle cx="250" cy="210" r="160" fill="none" stroke="rgba(255,255,255,0.03)" stroke-width="1"/>
    <circle cx="250" cy="210" r="130" fill="none" stroke="rgba(255,255,255,0.04)" stroke-width="1"/>
    <circle cx="250" cy="210" r="100" fill="none" stroke="rgba(255,255,255,0.05)" stroke-width="1.5"/>
    <circle cx="250" cy="210" r="70" fill="none" stroke="${p.accent}" stroke-opacity="0.3" stroke-width="1.5"/>

    <!-- Geometric artistic disc & badge -->
    <circle cx="250" cy="210" r="50" fill="${p.start}" stroke="${p.accent}" stroke-width="2" filter="drop-shadow(0 8px 16px rgba(0,0,0,0.5))"/>
    <circle cx="250" cy="210" r="14" fill="${p.accent}"/>

    <!-- Specular rim highlight -->
    <rect x="0.5" y="0.5" width="499" height="499" rx="23.5" fill="none" stroke="url(#shine-${hash})" stroke-width="1"/>

    <!-- Editorial typographic layout -->
    <g transform="translate(40, 390)">
      <rect x="0" y="-36" width="38" height="4" rx="2" fill="${p.accent}"/>
      <text x="0" y="0" fill="${p.text}" font-family="system-ui, -apple-system, sans-serif" font-size="25" font-weight="700" letter-spacing="-0.03em">${escapeXml(title.length > 26 ? title.slice(0, 25) + '…' : title)}</text>
      ${subtitle ? `<text x="0" y="28" fill="${p.accent}" font-family="system-ui, -apple-system, sans-serif" font-size="14" font-weight="500" letter-spacing="0.04em" opacity="0.9">${escapeXml(subtitle.length > 32 ? subtitle.slice(0, 30) + '…' : subtitle)}</text>` : ''}
    </g>

    <!-- Top metadata watermark -->
    <text x="460" y="44" fill="rgba(255,255,255,0.25)" font-family="monospace" font-size="11" font-weight="600" text-anchor="end" letter-spacing="0.1em">LOSSLESS · HI-RES</text>
  </svg>`;
}

function escapeXml(str: string) {
  return str.replace(/[<>&'"]/g, (c) => {
    switch (c) {
      case '<': return '&lt;';
      case '>': return '&gt;';
      case '&': return '&amp;';
      case '\'': return '&apos;';
      case '"': return '&quot;';
      default: return c;
    }
  });
}

// ==================== API ROUTES ====================

// Health
app.get('/api/health', (req: Request, res: Response) => {
  res.json({
    status: 'Healthy',
    version: '1.0.0',
    databaseStatus: 'Ok',
    timestamp: new Date().toISOString(),
  });
});

// Libraries & Sources
app.get('/api/libraries', (req: Request, res: Response) => {
  res.json(libraries);
});

app.post('/api/libraries', (req: Request, res: Response) => {
  const { name } = req.body;
  const newLib = { id: `lib-${Date.now()}`, name: name || 'New Library', createdAtUtc: new Date().toISOString() };
  libraries.push(newLib);
  res.status(201).json(newLib);
});

app.get('/api/libraries/:id', (req: Request, res: Response) => {
  const lib = libraries.find(l => l.id === req.params.id);
  if (!lib) return res.status(404).json({ detail: 'Library not found' });
  res.json(lib);
});

app.put('/api/libraries/:id', (req: Request, res: Response) => {
  const lib = libraries.find(l => l.id === req.params.id);
  if (!lib) return res.status(404).json({ detail: 'Library not found' });
  if (req.body.name) lib.name = req.body.name;
  res.json(lib);
});

app.delete('/api/libraries/:id', (req: Request, res: Response) => {
  const idx = libraries.findIndex(l => l.id === req.params.id);
  if (idx !== -1) libraries.splice(idx, 1);
  res.status(204).end();
});

app.get('/api/libraries/:id/sources', (req: Request, res: Response) => {
  const libSources = sources.filter(s => s.libraryId === req.params.id);
  res.json(libSources);
});

app.post('/api/libraries/:id/sources', (req: Request, res: Response) => {
  const { name, rootPath } = req.body;
  const newSource = {
    id: `src-${Date.now()}`,
    libraryId: req.params.id,
    name: name || 'New Source',
    rootPath: rootPath || 'C:\\Media',
    createdAtUtc: new Date().toISOString(),
  };
  sources.push(newSource);
  res.status(201).json(newSource);
});

app.delete('/api/libraries/:id/sources/:sourceId', (req: Request, res: Response) => {
  const idx = sources.findIndex(s => s.id === req.params.sourceId);
  if (idx !== -1) sources.splice(idx, 1);
  res.status(204).end();
});

app.get('/api/libraries/:id/items', (req: Request, res: Response) => {
  const items = [
    ...musicTracks.map(t => ({ id: t.mediaItemId, displayName: `${t.artistName} - ${t.title}`, createdAtUtc: '2026-01-20T10:00:00Z' })),
    ...movies.map(m => ({ id: m.mediaItemId, displayName: `${m.title} (${m.year})`, createdAtUtc: '2026-01-22T14:00:00Z' })),
    ...books.map(b => ({ id: b.mediaItemId, displayName: `${b.author} - ${b.title}`, createdAtUtc: '2026-01-25T16:00:00Z' })),
  ];
  const page = parseInt(req.query.page as string) || 1;
  const pageSize = parseInt(req.query.pageSize as string) || 50;
  const start = (page - 1) * pageSize;
  const paged = items.slice(start, start + pageSize);
  res.json({
    items: paged,
    page,
    pageSize,
    totalCount: items.length,
  });
});

// Scans
app.post('/api/sources/:sourceId/scans', (req: Request, res: Response) => {
  const src = sources.find(s => s.id === req.params.sourceId);
  const scan = {
    scanId: `scan-${Date.now()}`,
    libraryId: src?.libraryId || 'lib-music',
    sourceId: req.params.sourceId,
    sourceName: src?.name || 'Local Source',
    status: 'Completed',
    phase: 'Done',
    filesDiscovered: 42,
    filesProcessed: 42,
    filesIndexed: 42,
    filesUpdated: 0,
    filesUnchanged: 42,
    filesMarkedMissing: 0,
    startedAtUtc: new Date(Date.now() - 3000).toISOString(),
    completedAtUtc: new Date().toISOString(),
    error: null,
  };
  res.json(scan);
});

app.get('/api/scans/:scanId', (req: Request, res: Response) => {
  res.json({
    scanId: req.params.scanId,
    libraryId: 'lib-music',
    sourceId: 'src-1',
    sourceName: 'Local Flac Archive',
    status: 'Completed',
    phase: 'Done',
    filesDiscovered: 42,
    filesProcessed: 42,
    filesIndexed: 42,
    filesUpdated: 0,
    filesUnchanged: 42,
    filesMarkedMissing: 0,
    startedAtUtc: new Date(Date.now() - 60000).toISOString(),
    completedAtUtc: new Date().toISOString(),
    error: null,
  });
});

app.post('/api/scans/:scanId/cancel', (req: Request, res: Response) => {
  res.json({ cancelled: true });
});

// Dashboard
app.get('/api/dashboard/continue', (req: Request, res: Response) => {
  res.json([
    { id: 'trk-1', title: 'Weird Fishes / Arpeggi', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-15T21:40:00Z', isFavorite: true },
    { id: 'mov-2', title: 'Blade Runner 2049', mediaType: 'Movie', extension: 'mkv', hasArtwork: true, timestamp: '2026-02-14T22:15:00Z', isFavorite: false },
    { id: 'bk-1', title: 'Dune', mediaType: 'Book', extension: 'epub', hasArtwork: true, timestamp: '2026-02-15T19:00:00Z', isFavorite: true },
    { id: 'mng-2', title: 'Chainsaw Man', mediaType: 'Manga', extension: 'cbz', hasArtwork: true, timestamp: '2026-02-12T16:30:00Z', isFavorite: true },
  ]);
});

app.get('/api/dashboard/recently-added', (req: Request, res: Response) => {
  res.json([
    { id: 'trk-5', title: 'Digital Love', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-15T10:00:00Z', isFavorite: true },
    { id: 'mov-5', title: 'Dune: Part Two', mediaType: 'Movie', extension: 'mkv', hasArtwork: true, timestamp: '2026-02-14T14:30:00Z', isFavorite: false },
    { id: 'trk-8', title: 'Luv(sic) Part 3', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-13T18:00:00Z', isFavorite: true },
    { id: 'bk-4', title: 'Klara and the Sun', mediaType: 'Book', extension: 'epub', hasArtwork: true, timestamp: '2026-02-11T11:00:00Z', isFavorite: false },
  ]);
});

app.get('/api/dashboard/recently-played', (req: Request, res: Response) => {
  res.json([
    { id: 'trk-1', title: 'Weird Fishes / Arpeggi', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-15T21:40:00Z', isFavorite: true },
    { id: 'trk-3', title: 'So What', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-15T20:10:00Z', isFavorite: true },
    { id: 'mov-1', title: 'Spirited Away', mediaType: 'Movie', extension: 'mkv', hasArtwork: true, timestamp: '2026-02-14T21:00:00Z', isFavorite: true },
  ]);
});

app.get('/api/dashboard/favorites', (req: Request, res: Response) => {
  res.json([
    { id: 'trk-1', title: 'Weird Fishes / Arpeggi', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-01T12:00:00Z', isFavorite: true },
    { id: 'trk-7', title: 'Feather', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-01T12:05:00Z', isFavorite: true },
    { id: 'mov-1', title: 'Spirited Away', mediaType: 'Movie', extension: 'mkv', hasArtwork: true, timestamp: '2026-02-01T12:10:00Z', isFavorite: true },
    { id: 'bk-1', title: 'Dune', mediaType: 'Book', extension: 'epub', hasArtwork: true, timestamp: '2026-02-01T12:15:00Z', isFavorite: true },
  ]);
});

app.get('/api/dashboard/activity', (req: Request, res: Response) => {
  res.json([
    { id: 'act-1', mediaItemId: 'med-trk-1', title: 'Weird Fishes / Arpeggi', mediaType: 'Music', action: 'Finished Listening', occurredAtUtc: new Date(Date.now() - 1800000).toISOString() },
    { id: 'act-2', mediaItemId: 'med-mov-2', title: 'Blade Runner 2049', mediaType: 'Movie', action: 'Paused at 1h 10m', occurredAtUtc: new Date(Date.now() - 86400000).toISOString() },
    { id: 'act-3', mediaItemId: 'med-bk-1', title: 'Dune', mediaType: 'Book', action: 'Bookmarked Page 312', occurredAtUtc: new Date(Date.now() - 172800000).toISOString() },
  ]);
});

app.get('/api/dashboard/recommendations', (req: Request, res: Response) => {
  res.json([
    { id: 'trk-7', title: 'Feather', mediaType: 'Music', extension: 'flac', hasArtwork: true, timestamp: '2026-02-15T00:00:00Z', isFavorite: true },
    { id: 'mov-4', title: 'Princess Mononoke', mediaType: 'Movie', extension: 'mkv', hasArtwork: true, timestamp: '2026-02-15T00:00:00Z', isFavorite: false },
    { id: 'bk-2', title: 'Neuromancer', mediaType: 'Book', extension: 'epub', hasArtwork: true, timestamp: '2026-02-15T00:00:00Z', isFavorite: true },
  ]);
});

// Music APIs
app.get('/api/music/artists', (req: Request, res: Response) => {
  res.json({ items: musicArtists, offset: 0, limit: 50, total: musicArtists.length });
});

app.get('/api/music/albums', (req: Request, res: Response) => {
  let list = musicAlbums;
  if (req.query.artistId) {
    const artist = musicArtists.find(a => a.id === req.query.artistId);
    if (artist) list = list.filter(alb => alb.artistName === artist.name);
  }
  res.json({ items: list, offset: 0, limit: 50, total: list.length });
});

app.get('/api/music/tracks', (req: Request, res: Response) => {
  let list = musicTracks;
  if (req.query.albumId) {
    list = list.filter(t => t.albumId === req.query.albumId);
  }
  if (req.query.artistId) {
    const artist = musicArtists.find(a => a.id === req.query.artistId);
    if (artist) list = list.filter(t => t.artistName === artist.name);
  }
  res.json({ items: list, offset: 0, limit: 50, total: list.length });
});

app.get('/api/music/favorites', (req: Request, res: Response) => {
  res.json(musicFavorites);
});

app.post('/api/music/favorites', (req: Request, res: Response) => {
  const { entityType, entityId } = req.body;
  if (!musicFavorites.some(f => f.entityType === entityType && f.entityId === entityId)) {
    musicFavorites.push({ id: `fav-${Date.now()}`, entityType, entityId, createdAtUtc: new Date().toISOString() });
  }
  res.status(200).json({ success: true });
});

app.delete('/api/music/favorites/:type/:id', (req: Request, res: Response) => {
  const idx = musicFavorites.findIndex(f => f.entityType === req.params.type && f.entityId === req.params.id);
  if (idx !== -1) musicFavorites.splice(idx, 1);
  res.status(200).json({ success: true });
});

app.get('/api/music/history', (req: Request, res: Response) => {
  res.json([
    { id: 'hist-1', trackId: 'trk-1', startedAtUtc: new Date(Date.now() - 3600000).toISOString(), completedAtUtc: new Date(Date.now() - 3300000).toISOString(), playedSeconds: 318, completionRatio: 1.0, source: 'local' },
    { id: 'hist-2', trackId: 'trk-3', startedAtUtc: new Date(Date.now() - 7200000).toISOString(), completedAtUtc: new Date(Date.now() - 6600000).toISOString(), playedSeconds: 562, completionRatio: 1.0, source: 'local' },
  ]);
});

app.post('/api/music/tracks/:id/play', (req: Request, res: Response) => {
  res.json({ recorded: true });
});

app.get('/api/music/tracks/:id/playback-state', (req: Request, res: Response) => {
  res.json({ positionSeconds: 45, completed: false, updatedAtUtc: new Date().toISOString() });
});

app.put('/api/music/tracks/:id/playback-state', (req: Request, res: Response) => {
  res.json({ success: true });
});

app.get('/api/music/playlists', (req: Request, res: Response) => {
  res.json(musicPlaylists);
});

app.get('/api/music/playlists/:id', (req: Request, res: Response) => {
  const pl = musicPlaylists.find(p => p.id === req.params.id);
  if (!pl) return res.status(404).json({ detail: 'Playlist not found' });
  res.json({
    ...pl,
    tracks: musicTracks.slice(0, 4).map(t => ({ ...t, itemId: `item-${t.id}` })),
  });
});

app.post('/api/music/playlists', (req: Request, res: Response) => {
  const newPl = {
    id: `pl-${Date.now()}`,
    name: req.body.name || 'New Playlist',
    description: req.body.description || null,
    isSmart: Boolean(req.body.isSmart),
    smartQuery: req.body.smartQuery || null,
    updatedAtUtc: new Date().toISOString(),
  };
  musicPlaylists.push(newPl);
  res.status(201).json(newPl);
});

app.patch('/api/music/playlists/:id', (req: Request, res: Response) => {
  const pl = musicPlaylists.find(p => p.id === req.params.id);
  if (!pl) return res.status(404).json({ detail: 'Playlist not found' });
  if (req.body.name) pl.name = req.body.name;
  if (req.body.description !== undefined) pl.description = req.body.description;
  pl.updatedAtUtc = new Date().toISOString();
  res.json(pl);
});

app.delete('/api/music/playlists/:id', (req: Request, res: Response) => {
  const idx = musicPlaylists.findIndex(p => p.id === req.params.id);
  if (idx !== -1) musicPlaylists.splice(idx, 1);
  res.status(204).end();
});

app.post('/api/music/playlists/:id/items', (req: Request, res: Response) => {
  res.status(200).json({ success: true });
});

app.delete('/api/music/playlists/:id/items/:itemId', (req: Request, res: Response) => {
  res.status(200).json({ success: true });
});

app.patch('/api/music/playlists/:id/items/reorder', (req: Request, res: Response) => {
  res.status(200).json({ success: true });
});

app.post('/api/music/playlists/:id/save-queue', (req: Request, res: Response) => {
  res.status(200).json({ added: (req.body.trackIds || []).length });
});

app.get('/api/music/collections/home', (req: Request, res: Response) => {
  res.json({
    continueListening: [{ trackId: 'trk-1', positionSeconds: 120, durationSeconds: 318, updatedAtUtc: new Date().toISOString() }],
    recentlyPlayed: musicTracks.slice(0, 4),
    recentlyAdded: musicTracks.slice(2, 6),
    mostPlayed: musicTracks.slice(0, 5),
    favorites: musicTracks.filter(t => t.rating === 5),
    topRated: musicTracks.filter(t => t.rating >= 4),
    neverPlayed: musicTracks.slice(6),
  });
});

app.get('/api/music/tracks/:id', (req: Request, res: Response) => {
  const track = musicTracks.find(t => t.id === req.params.id) || musicTracks[0];
  res.json(track);
});

app.get('/api/music/tracks/:id/metadata', (req: Request, res: Response) => {
  const track = musicTracks.find(t => t.id === req.params.id) || musicTracks[0];
  res.json({
    title: track.title,
    trackArtist: track.artistName,
    albumArtist: track.artistName,
    album: track.albumTitle,
    trackNumber: track.trackNumber,
    discNumber: track.discNumber,
    year: track.year,
    durationSeconds: track.durationSeconds,
    genre: track.genre,
  });
});

app.put('/api/music/tracks/:id/metadata', (req: Request, res: Response) => {
  const track = musicTracks.find(t => t.id === req.params.id);
  if (track) {
    if (req.body.title) track.title = req.body.title;
    if (req.body.trackArtist) track.artistName = req.body.trackArtist;
    if (req.body.album) track.albumTitle = req.body.album;
    if (req.body.genre) track.genre = req.body.genre;
  }
  res.json({ success: true });
});

app.get('/api/music/tracks/:id/rating', (req: Request, res: Response) => {
  const track = musicTracks.find(t => t.id === req.params.id);
  res.json({ rating: track?.rating ?? 0 });
});

app.put('/api/music/tracks/:id/rating', (req: Request, res: Response) => {
  const track = musicTracks.find(t => t.id === req.params.id);
  if (track) track.rating = req.body.rating ?? 0;
  res.json({ success: true });
});

app.get('/api/music/tracks/:id/lyrics', (req: Request, res: Response) => {
  res.json({
    text: "[00:15.00] In the deepest ocean\n[00:20.00] The bottom of the sea\n[00:25.00] Your eyes\n[00:28.00] They turn me",
    synchronized: true,
  });
});

app.post('/api/music/tracks/:id/lyrics', (req: Request, res: Response) => {
  res.json({ success: true });
});

app.get('/api/music/providers/lyrics/search', (req: Request, res: Response) => {
  res.json([
    {
      id: 'lyr-1',
      title: (req.query.track as string) || 'Weird Fishes',
      artist: (req.query.artist as string) || 'Radiohead',
      album: (req.query.album as string) || 'In Rainbows',
      synced: true,
      plainText: "In the deepest ocean\nThe bottom of the sea\nYour eyes\nThey turn me",
      syncedText: "[00:15.00] In the deepest ocean\n[00:20.00] The bottom of the sea",
      source: 'lrclib',
    },
  ]);
});

app.get('/api/music/tracks/:id/info', (req: Request, res: Response) => {
  const track = musicTracks.find(t => t.id === req.params.id) || musicTracks[0];
  res.json({
    id: track.id,
    title: track.title,
    artistName: track.artistName,
    album: track.albumTitle,
    rating: track.rating,
    path: `D:\\Media\\Music\\${track.artistName}\\${track.albumTitle}\\${track.title}.flac`,
    sizeBytes: 34500000,
    modifiedUtc: '2026-01-20T10:00:00Z',
    extension: 'flac',
  });
});

app.post('/api/music/tracks/:id/rename', (req: Request, res: Response) => {
  res.json({ success: true });
});

app.delete('/api/music/tracks/:id', (req: Request, res: Response) => {
  const idx = musicTracks.findIndex(t => t.id === req.params.id);
  if (idx !== -1) musicTracks.splice(idx, 1);
  res.json({ success: true });
});

app.get('/api/music/statistics/overview', (req: Request, res: Response) => {
  res.json({
    tracks: musicTracks.length,
    albums: musicAlbums.length,
    artists: musicArtists.length,
    playlists: musicPlaylists.length,
    favorites: musicFavorites.length,
    listeningSeconds: 42800,
  });
});

app.get('/api/music/health', (req: Request, res: Response) => {
  res.json({
    database: true,
    tracks: musicTracks.length,
    lastUpdatedUtc: new Date().toISOString(),
  });
});

app.get('/api/music/statistics/top-tracks', (req: Request, res: Response) => {
  res.json([
    { trackId: 'trk-1', plays: 48, playedSeconds: 15264 },
    { trackId: 'trk-5', plays: 35, playedSeconds: 10430 },
    { trackId: 'trk-3', plays: 28, playedSeconds: 15736 },
  ]);
});

app.get('/api/music/statistics/genres', (req: Request, res: Response) => {
  res.json([
    { genre: 'Art Rock', tracks: 2 },
    { genre: 'Modal Jazz', tracks: 2 },
    { genre: 'French House', tracks: 2 },
    { genre: 'Lo-Fi Hip Hop', tracks: 2 },
    { genre: 'Classical', tracks: 1 },
  ]);
});

app.post('/api/music/reindex', (req: Request, res: Response) => {
  res.json({ indexedTracks: musicTracks.length });
});

app.get('/api/music/albums/:id/cover', (req: Request, res: Response) => {
  const album = musicAlbums.find(a => a.id === req.params.id) || musicAlbums[0];
  const svg = generateCoverSvg(album.title, album.artistName ?? undefined, '#6366f1');
  res.setHeader('Content-Type', 'image/svg+xml');
  res.send(svg);
});

app.get('/api/music/tracks/:id/stream', (req: Request, res: Response) => {
  res.status(204).end();
});

// Movies APIs
app.get('/api/movies', (req: Request, res: Response) => {
  let list = movies;
  if (req.query.watched !== undefined) {
    const isWatched = req.query.watched === 'true';
    list = list.filter(m => m.watched === isWatched);
  }
  res.json(list);
});

app.get('/api/movies/continue', (req: Request, res: Response) => {
  const unwatchedInProgress = movies.filter(m => !m.watched && (m.watchProgressSeconds || 0) > 0);
  res.json(unwatchedInProgress);
});

app.get('/api/movies/:id/poster', (req: Request, res: Response) => {
  const movie = movies.find(m => m.id === req.params.id) || movies[0];
  const svg = generateCoverSvg(movie.title, `${movie.year}`, '#ec4899');
  res.setHeader('Content-Type', 'image/svg+xml');
  res.send(svg);
});

app.get('/api/movies/:id/stream', (req: Request, res: Response) => {
  res.status(204).end();
});

app.post('/api/movies/:id/progress', (req: Request, res: Response) => {
  const movie = movies.find(m => m.id === req.params.id);
  if (movie) movie.watchProgressSeconds = req.body.positionSeconds;
  res.json(movie || movies[0]);
});

app.post('/api/movies/:id/watched', (req: Request, res: Response) => {
  const movie = movies.find(m => m.id === req.params.id);
  if (movie) {
    movie.watched = req.body.watched;
    movie.watchedAtUtc = req.body.watched ? new Date().toISOString() : null;
  }
  res.json(movie || movies[0]);
});

app.post('/api/movies/reindex', (req: Request, res: Response) => {
  res.json({ indexedMovies: movies.length });
});

// Books APIs
app.get('/api/books', (req: Request, res: Response) => {
  let list = books;
  if (req.query.read !== undefined) {
    const isRead = req.query.read === 'true';
    list = list.filter(b => b.read === isRead);
  }
  if (req.query.author) {
    list = list.filter(b => b.author?.toLowerCase().includes((req.query.author as string).toLowerCase()));
  }
  res.json(list);
});

app.get('/api/books/continue', (req: Request, res: Response) => {
  const continueBooks = books.filter(b => !b.read && (b.progressPages || 0) > 0);
  res.json(continueBooks);
});

app.get('/api/books/authors', (req: Request, res: Response) => {
  const counts = new Map<string, number>();
  for (const b of books) {
    if (b.author) counts.set(b.author, (counts.get(b.author) || 0) + 1);
  }
  const result = Array.from(counts.entries()).map(([name, bookCount]) => ({ name, bookCount }));
  res.json(result);
});

app.get('/api/books/series', (req: Request, res: Response) => {
  const counts = new Map<string, number>();
  for (const b of books) {
    if (b.series) counts.set(b.series, (counts.get(b.series) || 0) + 1);
  }
  const result = Array.from(counts.entries()).map(([title, bookCount]) => ({ title, bookCount }));
  res.json(result);
});

app.get('/api/books/health', (req: Request, res: Response) => {
  res.json({
    totalBooks: books.length,
    missingCovers: 0,
    missingFiles: 0,
    missingMetadata: 0,
    generatedAtUtc: new Date().toISOString(),
  });
});

app.post('/api/books/reindex', (req: Request, res: Response) => {
  res.json({ indexedBooks: books.length });
});

app.post('/api/books/:id/read', (req: Request, res: Response) => {
  const book = books.find(b => b.id === req.params.id);
  if (book) book.read = req.body.read;
  res.json(book || books[0]);
});

app.post('/api/books/:id/favorite', (req: Request, res: Response) => {
  const book = books.find(b => b.id === req.params.id);
  if (book) book.favorite = req.body.favorite;
  res.json(book || books[0]);
});

app.post('/api/books/:id/progress', (req: Request, res: Response) => {
  const book = books.find(b => b.id === req.params.id);
  if (book) {
    book.progressPages = req.body.progressPages;
    if (book.pageCount) book.progressPercent = Math.round((book.progressPages / book.pageCount) * 100);
  }
  res.json(book || books[0]);
});

app.post('/api/books/:id/rating', (req: Request, res: Response) => {
  const book = books.find(b => b.id === req.params.id);
  if (book) book.rating = req.body.rating;
  res.json(book || books[0]);
});

app.get('/api/books/:id/manifest', (req: Request, res: Response) => {
  const book = books.find(b => b.id === req.params.id) || books[0];
  res.json({
    format: book.format || 'EPUB',
    totalPages: book.pageCount || 300,
    title: book.title,
    tableOfContents: [
      { title: 'Chapter 1', target: 'ch1', pageNumber: 1 },
      { title: 'Chapter 2', target: 'ch2', pageNumber: 45 },
      { title: 'Chapter 3', target: 'ch3', pageNumber: 98 },
    ],
  });
});

app.get('/api/books/:id/bookmarks', (req: Request, res: Response) => {
  res.json([
    { id: 'bm-1', pageNumber: 42, title: 'Crucial Quote', note: 'Interesting foreshadowing', createdAtUtc: '2026-02-01T12:00:00Z' },
  ]);
});

app.post('/api/books/:id/bookmarks', (req: Request, res: Response) => {
  res.status(201).json({
    id: `bm-${Date.now()}`,
    pageNumber: req.body.pageNumber || 1,
    title: req.body.title || null,
    note: req.body.note || null,
    createdAtUtc: new Date().toISOString(),
  });
});

app.delete('/api/books/:id/bookmarks/:bookmarkId', (req: Request, res: Response) => {
  res.status(204).end();
});

app.get('/api/books/:id/cover', (req: Request, res: Response) => {
  const book = books.find(b => b.id === req.params.id) || books[0];
  const svg = generateCoverSvg(book.title, book.author ?? undefined, '#10b981');
  res.setHeader('Content-Type', 'image/svg+xml');
  res.send(svg);
});

// Manga APIs
app.get('/api/manga/library', (req: Request, res: Response) => {
  res.json(mangaList);
});

app.get('/api/manga/continue-reading', (req: Request, res: Response) => {
  res.json(mangaList.slice(0, 3));
});

app.get('/api/manga/updates', (req: Request, res: Response) => {
  res.json([
    { id: 'upd-1', mangaId: 'mng-2', mangaTitle: 'Chainsaw Man', chapter: 'Ch. 185', publishedAtUtc: '2026-02-14T10:00:00Z', sourceName: 'MangaDex', read: false },
    { id: 'upd-2', mangaId: 'mng-4', mangaTitle: 'One Piece', chapter: 'Ch. 1130', publishedAtUtc: '2026-02-13T12:00:00Z', sourceName: 'MangaDex', read: false },
  ]);
});

app.get('/api/manga/categories', (req: Request, res: Response) => {
  res.json([
    { id: 'cat-1', name: 'Reading', count: 4, color: '#3b82f6' },
    { id: 'cat-2', name: 'Completed', count: 12, color: '#10b981' },
    { id: 'cat-3', name: 'Plan to Read', count: 25, color: '#f59e0b' },
  ]);
});

app.get('/api/manga/sources', (req: Request, res: Response) => {
  res.json([
    { id: 'src-mg-1', name: 'MangaDex', language: 'en', installed: true, enabled: true, mangaCount: 4 },
    { id: 'src-mg-2', name: 'MangaSee', language: 'en', installed: true, enabled: true, mangaCount: 2 },
  ]);
});

app.get('/api/manga/browse/popular', (req: Request, res: Response) => {
  res.json(mangaList);
});

app.get('/api/manga/search', (req: Request, res: Response) => {
  const q = ((req.query.q as string) || '').toLowerCase();
  const results = mangaList.filter(m => m.title.toLowerCase().includes(q) || m.author.toLowerCase().includes(q));
  res.json(results);
});

app.put('/api/manga/:id/favorite', (req: Request, res: Response) => {
  const manga = mangaList.find(m => m.id === req.params.id);
  if (manga) manga.favorite = req.body.favorite;
  res.json({ success: true });
});

app.put('/api/manga/:id/status', (req: Request, res: Response) => {
  const manga = mangaList.find(m => m.id === req.params.id);
  if (manga) manga.status = req.body.status;
  res.json({ success: true });
});

app.put('/api/manga/:id/progress', (req: Request, res: Response) => {
  res.json({ success: true });
});

app.post('/api/manga/:id/download', (req: Request, res: Response) => {
  res.json({ queued: true });
});

app.post('/api/manga/update', (req: Request, res: Response) => {
  res.json({ updated: true, newChaptersFound: 2 });
});

app.post('/api/manga/sources/install', (req: Request, res: Response) => {
  res.json({ success: true });
});

app.get('/api/manga/notifications', (req: Request, res: Response) => {
  res.json([
    { id: 'notif-1', mangaId: 'mng-2', mangaTitle: 'Chainsaw Man', chapterTitle: 'Chapter 185', chapterNumber: 185, read: false, createdAtUtc: new Date().toISOString() },
  ]);
});

app.post('/api/manga/notifications/:id/read', (req: Request, res: Response) => {
  res.json({ success: true });
});

app.get('/api/manga/:id/trackers', (req: Request, res: Response) => {
  res.json([]);
});

app.post('/api/manga/:id/trackers', (req: Request, res: Response) => {
  res.json({
    id: `trk-${Date.now()}`,
    mangaId: req.params.id,
    trackerName: req.body.trackerName || 'AniList',
    externalTrackingId: req.body.externalTrackingId || '12345',
    lastSyncedChapter: req.body.lastSyncedChapter || 1,
    status: req.body.status || 'reading',
    score: req.body.score || 0,
    lastSyncedAtUtc: new Date().toISOString(),
  });
});

app.post('/api/manga/sync', (req: Request, res: Response) => {
  res.json({ synced: 4 });
});

// Catalogue & TV
app.get('/api/catalogue/series', (req: Request, res: Response) => {
  res.json([
    { id: 'ser-1', libraryId: 'lib-cinema', title: 'Cowboy Bebop', year: 1998, seasonCount: 1, episodeCount: 26 },
    { id: 'ser-2', libraryId: 'lib-cinema', title: 'True Detective', year: 2014, seasonCount: 1, episodeCount: 8 },
  ]);
});

// Search
app.get('/api/search', (req: Request, res: Response) => {
  const q = ((req.query.q as string) || '').toLowerCase().trim();
  if (!q) return res.json([]);
  const results: Array<{ itemId: string; displayName: string; libraryId: string; libraryName: string }> = [];

  for (const t of musicTracks) {
    if (t.title.toLowerCase().includes(q) || (t.artistName && t.artistName.toLowerCase().includes(q))) {
      results.push({ itemId: t.id, displayName: `${t.artistName} - ${t.title}`, libraryId: 'lib-music', libraryName: 'Music Vault' });
    }
  }
  for (const m of movies) {
    if (m.title.toLowerCase().includes(q)) {
      results.push({ itemId: m.id, displayName: `${m.title} (${m.year})`, libraryId: 'lib-cinema', libraryName: 'Cinema & TV' });
    }
  }
  for (const b of books) {
    if (b.title.toLowerCase().includes(q) || (b.author && b.author.toLowerCase().includes(q))) {
      results.push({ itemId: b.id, displayName: `${b.author} - ${b.title}`, libraryId: 'lib-literature', libraryName: 'Literature & Manga' });
    }
  }
  for (const mg of mangaList) {
    if (mg.title.toLowerCase().includes(q)) {
      results.push({ itemId: mg.id, displayName: `${mg.title} (Manga)`, libraryId: 'lib-literature', libraryName: 'Literature & Manga' });
    }
  }
  res.json(results);
});

app.post('/api/search/reindex', (req: Request, res: Response) => {
  res.json({ indexedItems: musicTracks.length + movies.length + books.length + mangaList.length });
});

// Platform / Windows integrations
app.get('/api/platform/windows/config', (req: Request, res: Response) => {
  res.json(windowsConfig);
});

app.put('/api/platform/windows/config', (req: Request, res: Response) => {
  windowsConfig = { ...windowsConfig, ...req.body };
  res.json(windowsConfig);
});

app.get('/api/platform/windows/tray', (req: Request, res: Response) => {
  res.json({ visible: true });
});

app.post('/api/platform/windows/tray/visibility', (req: Request, res: Response) => {
  res.json({ visible: req.body.visible ?? true });
});

app.post('/api/platform/windows/notifications', (req: Request, res: Response) => {
  notifications.unshift({
    title: req.body.title || 'Notification',
    message: req.body.message || '',
    timestampUtc: new Date().toISOString(),
  });
  res.json({ success: true });
});

app.get('/api/platform/windows/notifications/history', (req: Request, res: Response) => {
  res.json(notifications);
});

app.get('/api/platform/windows/associations', (req: Request, res: Response) => {
  res.json({ extensions: ['.flac', '.mp3', '.mkv', '.mp4', '.epub', '.cbz'] });
});

app.post('/api/platform/windows/associations/register', (req: Request, res: Response) => {
  res.json({ extensions: req.body.extensions || ['.flac', '.mp3', '.mkv', '.mp4', '.epub', '.cbz'] });
});

app.post('/api/platform/windows/mediakeys', (req: Request, res: Response) => {
  res.json({ handled: true, action: req.body.action || 'play' });
});

app.get('/api/platform/windows/backup/export', (req: Request, res: Response) => {
  res.setHeader('Content-Type', 'application/json');
  res.setHeader('Content-Disposition', 'attachment; filename="onidash-backup.json"');
  res.json({
    version: '1.0.0',
    exportedAtUtc: new Date().toISOString(),
    libraries,
    sources,
    musicPlaylists,
  });
});

// ==================== VITE MIDDLEWARE / STATIC ASSETS ====================

async function start() {
  if (process.env.NODE_ENV !== 'production') {
    const { createServer: createViteServer } = await import('vite');
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: 'spa',
    });
    app.use(vite.middlewares);
  } else {
    const distPath = path.join(process.cwd(), 'dist');
    app.use(express.static(distPath));
    app.get('*', (req: Request, res: Response) => {
      res.sendFile(path.join(distPath, 'index.html'));
    });
  }

  app.listen(PORT, '0.0.0.0', () => {
    console.log(`oniDash server running at http://0.0.0.0:${PORT}`);
  });
}

start();
