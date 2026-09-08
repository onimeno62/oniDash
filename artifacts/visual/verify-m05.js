// End-to-end UI verification for Phase 5 (music):
// generate real tagged MP3s, scan them through the API, verify the music catalogue
// (artists/albums/covers/ranged streaming) via API and UI, plus M01–M04 regressions.
'use strict';

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const os = require('os');

const BASE = 'http://localhost:5275';
const API = BASE + '/api';

let failures = 0;
function check(name, ok, detail = '') {
  if (ok) {
    console.log(`PASS ${name}${detail ? ` (${detail})` : ''}`);
  } else {
    failures += 1;
    console.log(`FAIL ${name}${detail ? ` (${detail})` : ''}`);
  }
}

async function apiJson(pathname, init) {
  const response = await fetch(API + pathname, init);
  const text = await response.text();
  return { status: response.status, body: text ? JSON.parse(text) : null };
}

/* ------------------------- MP3 generation (ID3v2.3) ------------------------- */

function syncsafe(n) {
  return [(n >> 21) & 0x7f, (n >> 14) & 0x7f, (n >> 7) & 0x7f, n & 0x7f];
}

function textFrame(id, value) {
  const payload = Buffer.concat([Buffer.from([0x00]), Buffer.from(value, 'latin1')]);
  return makeFrame(id, payload);
}

function apicFrame(pngBytes) {
  const payload = Buffer.concat([
    Buffer.from([0x00]),
    Buffer.from('image/png\0', 'latin1'),
    Buffer.from([0x03]), // front cover
    Buffer.from([0x00]), // empty description
    pngBytes,
  ]);
  return makeFrame('APIC', payload);
}

function makeFrame(id, payload) {
  const header = Buffer.alloc(10);
  header.write(id, 0, 'latin1');
  header.writeUInt32BE(payload.length, 4); // v2.3: plain big-endian size
  header[8] = 0;
  header[9] = 0;
  return Buffer.concat([header, payload]);
}

function id3v2Tag(frames) {
  const body = Buffer.concat(frames);
  return Buffer.concat([Buffer.from('ID3', 'latin1'), Buffer.from([0x03, 0x00, 0x00]), Buffer.from(syncsafe(body.length)), body]);
}

// ~2s of silent MPEG-1 Layer III audio (78 x 417-byte frames).
function silentAudio() {
  const frame = Buffer.alloc(417);
  frame[0] = 0xff;
  frame[1] = 0xfb; // MPEG-1 Layer III, no CRC
  frame[2] = 0x90; // 128 kbps, 44.1 kHz
  frame[3] = 0xc4; // joint stereo
  return Buffer.concat(Array.from({ length: 78 }, () => frame));
}

const ONE_BY_ONE_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
  'base64',
);

function taggedMp3({ title, artist, albumArtist, album, track, year }) {
  const frames = [
    textFrame('TIT2', title),
    textFrame('TPE1', artist),
    textFrame('TALB', album),
    textFrame('TRCK', String(track)),
    textFrame('TYER', String(year)),
  ];
  if (albumArtist) frames.push(textFrame('TPE2', albumArtist));
  if (title === 'Nightcall') frames.push(apicFrame(ONE_BY_ONE_PNG));
  return Buffer.concat([id3v2Tag(frames), silentAudio()]);
}

async function makeScanSource(tag, files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), `onidash-p5-${tag}-`));
  for (const [name, content] of Object.entries(files)) {
    const full = path.join(root, name);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, content);
  }
  const { body: library } = await apiJson('/libraries', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: `__p5_${tag}_${Date.now().toString(36)}__` }),
  });
  const { body: source } = await apiJson(`/libraries/${library.id}/sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'Main', rootPath: root }),
  });
  await apiJson(`/sources/${source.id}/scans`, { method: 'POST' });

  const deadline = Date.now() + 30000;
  let done = null;
  while (Date.now() < deadline) {
    const { body: scans } = await apiJson(`/scans?sourceId=${source.id}&limit=1`);
    if (scans[0] && scans[0].status !== 'Running') { done = scans[0]; break; }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  return { libraryId: library.id, libraryName: library.name, root, scan: done };
}

(async () => {
  // Idempotence: earlier-run libraries would distort catalogue counts.
  const { body: existingLibraries } = await apiJson('/libraries');
  for (const library of existingLibraries) {
    if (/^__(p5|p4|vis|m0\d)_/.test(library.name)) {
      await apiJson(`/libraries/${library.id}`, { method: 'DELETE' });
    }
  }

  const music = await makeScanSource('music', {
    '01 - One More Time.mp3': taggedMp3({ title: 'One More Time', artist: 'Daft Punk', album: 'Discovery', track: 1, year: 2001 }),
    '02 - Aerodynamic.mp3': taggedMp3({ title: 'Aerodynamic', artist: 'Daft Punk', album: 'Discovery', track: 2, year: 2001 }),
    'Nightcall.mp3': taggedMp3({ title: 'Nightcall', artist: 'Kavinsky', album: 'OutRun', track: 1, year: 2013 }),
  });
  check('scan completes with files indexed', music.scan && music.scan.status === 'Completed' && music.scan.filesIndexed === 3,
    `status=${music.scan && music.scan.status} indexed=${music.scan && music.scan.filesIndexed}`);

  /* ------------------------------ API checks ------------------------------ */

  const { body: artists } = await apiJson(`/music/artists?libraryId=${music.libraryId}`);
  check('artists endpoint lists scanned artists',
    artists.map((a) => a.name).sort().join('|') === 'Daft Punk|Kavinsky', artists.map((a) => a.name).join(', '));

  const { body: albums } = await apiJson(`/music/albums?libraryId=${music.libraryId}`);
  const discovery = albums.find((a) => a.title === 'Discovery');
  const outrun = albums.find((a) => a.title === 'OutRun');
  check('albums grouped by album tag', Boolean(discovery) && Boolean(outrun), albums.map((a) => a.title).join(', '));
  check('embedded cover detected', Boolean(outrun) && outrun.hasCover === true && Boolean(discovery) && discovery.hasCover === false);

  const coverResponse = await fetch(`${API}/music/albums/${outrun.id}/cover`);
  check('cover endpoint serves image bytes',
    coverResponse.status === 200 && (coverResponse.headers.get('content-type') || '').startsWith('image/'),
    `status=${coverResponse.status} type=${coverResponse.headers.get('content-type')}`);

  const { body: tracks } = await apiJson(`/music/tracks?libraryId=${music.libraryId}`);
  const nightcall = tracks.find((t) => t.title === 'Nightcall');
  check('tracks endpoint lists all indexed tracks', tracks.length === 3, `count=${tracks.length}`);
  check('track duration comes from the file', Boolean(nightcall) && nightcall.durationSeconds > 0, `dur=${nightcall && nightcall.durationSeconds}`);

  const { body: albumTracks } = await apiJson(`/music/tracks?libraryId=${music.libraryId}&albumId=${discovery.id}`);
  check('album filter returns ordered album tracks',
    albumTracks.map((t) => t.title).join('|') === 'One More Time|Aerodynamic',
    albumTracks.map((t) => t.title).join(', '));

  const streamResponse = await fetch(`${API}/music/tracks/${nightcall.id}/stream`, {
    headers: { Range: 'bytes=0-99' },
  });
  const streamBody = Buffer.from(await streamResponse.arrayBuffer());
  check('stream endpoint honors range requests',
    streamResponse.status === 206 && streamBody.length === 100 && (streamResponse.headers.get('content-type') || '').startsWith('audio/'),
    `status=${streamResponse.status} bytes=${streamBody.length} type=${streamResponse.headers.get('content-type')}`);

  const { body: reindexed } = await apiJson('/music/reindex', { method: 'POST' });
  check('reindex command responds with counter', typeof reindexed.indexedTracks === 'number', `tracks=${reindexed.indexedTracks}`);

  /* ------------------------------- UI checks ------------------------------ */

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  await page.goto(BASE + '/music', { waitUntil: 'networkidle' });
  await page.selectOption('select[aria-label="Music library"]', music.libraryId);
  await page.waitForSelector('button:has-text("Daft Punk")', { timeout: 15000 });
  check('music page renders artist chips', true);

  await page.waitForSelector('section[aria-label="Albums"] img', { timeout: 15000 });
  const coverLoaded = await page.evaluate(() => {
    const image = document.querySelector('section[aria-label="Albums"] img');
    return image && image.complete && image.naturalWidth > 0;
  });
  check('album cover renders from embedded art', coverLoaded === true);

  // Artist filter narrows the view to one album.
  await page.click('button:has-text("Kavinsky")');
  await page.waitForSelector('section[aria-label="Albums"] figcaption:has-text("OutRun")', { timeout: 15000 });
  check('artist chip filters albums', true);

  // Playback: clicking play must actually fetch the ranged stream.
  const streamPromise = page.waitForResponse(
    (response) => response.url().includes('/stream') && [200, 206].includes(response.status()),
    { timeout: 15000 },
  );
  await page.click('button[aria-label="Play Nightcall"]');
  const streamHit = await streamPromise;
  await page.waitForSelector('[data-testid="player-bar"]', { timeout: 15000 });
  const barText = await page.textContent('[data-testid="player-bar"]');
  check('player bar opens and requests the stream',
    barText.includes('Nightcall') && streamHit.request().headers().range !== undefined,
    `range=${streamHit.request().headers().range} status=${streamHit.status()}`);

  // Overflow checks on the music page at the required resolutions.
  for (const [w, h] of [[1280, 800], [1920, 1080], [2560, 1440]]) {
    await page.setViewportSize({ width: w, height: h });
    await page.goto(BASE + '/music', { waitUntil: 'networkidle' });
    await page.selectOption('select[aria-label="Music library"]', music.libraryId);
    await page.waitForSelector('button:has-text("Daft Punk")', { timeout: 15000 });
    const overflow = await page.evaluate(
      () => document.scrollingElement.scrollWidth > window.innerWidth,
    );
    check(`${w}x${h}: music page, no overflow`, !overflow);
  }

  fs.mkdirSync('shots', { recursive: true });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(BASE + '/music', { waitUntil: 'networkidle' });
  await page.selectOption('select[aria-label="Music library"]', music.libraryId);
  await page.waitForSelector('section[aria-label="Tracks"] li', { timeout: 15000 });
  await page.screenshot({ path: 'shots/music-catalog.png', fullPage: true });

  /* ---------------------------- M01–M04 regressions ---------------------------- */

  const { body: health } = await apiJson('/health');
  check('M01: health reports a current semver', /^0\.\d+\.\d+$/.test(health.version), health.version);

  await page.goto(BASE + '/library', { waitUntil: 'networkidle' });
  const libraryVisible = await page.isVisible('section[aria-label="Libraries"], button:has-text("New library")');
  check('M02: library page renders', libraryVisible);

  await page.goto(BASE + '/search', { waitUntil: 'networkidle' });
  await page.fill('input#library-search', 'night');
  await page.waitForSelector('[data-testid="result-count"]', { timeout: 15000 });
  const searchCount = (await page.textContent('[data-testid="result-count"]')).trim();
  check('M04: search still finds indexed files', /^[1-9]\d* result/.test(searchCount), searchCount);

  await browser.close();

  // Cleanup: seeded libraries cascade media items; music rows follow via FK.
  await apiJson(`/libraries/${music.libraryId}`, { method: 'DELETE' });
  try { fs.rmSync(music.root, { recursive: true, force: true }); } catch {}

  console.log(failures === 0 ? '\nALL CHECKS PASSED' : `\n${failures} CHECK(S) FAILED`);
  process.exit(failures === 0 ? 0 : 1);
})().catch((error) => {
  console.error('RUNNER FAILED:', error.message);
  process.exit(1);
});
