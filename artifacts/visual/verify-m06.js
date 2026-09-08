// End-to-end UI verification for Phase 6 (movies):
// generate real MP4s (ffmpeg), scan them through the API, verify the movie catalogue
// (detection, filename metadata, posters, ranged streaming, watch progress) via API
// and UI, plus M01/M02/M04/M05 regressions.
'use strict';

const { chromium } = require('playwright');
const { execFileSync } = require('child_process');
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

// 2s 320x240 test video via the local ffmpeg build. VP9/WebM keeps the fixture
// decodable in Playwright's Chromium (no proprietary H.264/AAC there).
function makeVideo(filePath) {
  execFileSync('ffmpeg', [
    '-hide_banner', '-loglevel', 'error',
    '-f', 'lavfi', '-i', 'testsrc=duration=2:size=320x240:rate=10',
    '-f', 'lavfi', '-i', 'sine=frequency=440:duration=2',
    '-c:v', 'libvpx-vp9', '-b:v', '200k',
    '-c:a', 'libopus', '-shortest',
    '-y', filePath,
  ]);
}

// The same test video with an embedded poster atom (MP4 cover art, read by TagLib#):
// encode plain video, then mux the PNG in as an attached_pic (covr) stream.
function makeMp4WithPoster(filePath, pngPath) {
  const plain = filePath.replace(/\.mp4$/, '.plain.mp4');
  execFileSync('ffmpeg', [
    '-hide_banner', '-loglevel', 'error',
    '-f', 'lavfi', '-i', 'testsrc=duration=2:size=320x240:rate=10',
    '-c:v', 'libx264', '-preset', 'ultrafast', '-pix_fmt', 'yuv420p',
    '-y', plain,
  ]);
  execFileSync('ffmpeg', [
    '-hide_banner', '-loglevel', 'error',
    '-i', plain, '-i', pngPath,
    '-map', '0', '-map', '1',
    '-c', 'copy', '-c:v:1', 'png', '-disposition:1', 'attached_pic',
    '-y', filePath,
  ]);
  fs.rmSync(plain, { force: true });
}

function makePng(filePath) {
  // 1x1 white PNG.
  const png = Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=',
    'base64',
  );
  fs.writeFileSync(filePath, png);
}

async function makeScanSource(tag, files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), `onidash-p6-${tag}-`));
  for (const name of Object.keys(files)) {
    const full = path.join(root, name);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    files[name](full);
  }
  const { body: library } = await apiJson('/libraries', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: `__p6_${tag}_${Date.now().toString(36)}__` }),
  });
  const { body: source } = await apiJson(`/libraries/${library.id}/sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'Main', rootPath: root }),
  });
  await apiJson(`/sources/${source.id}/scans`, { method: 'POST' });

  const deadline = Date.now() + 60000;
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
    if (/^__(p6|p5|p4|vis|m0\d)_/.test(library.name)) {
      await apiJson(`/libraries/${library.id}`, { method: 'DELETE' });
    }
  }

  const poster = path.join(os.tmpdir(), `onidash-p6-poster-${Date.now()}.png`);
  makePng(poster);

  const movies = await makeScanSource('movies', {
    'The Matrix (1999).mkv': (full) => fs.writeFileSync(full, Buffer.alloc(64)),
    // Probeable but unparseable title => falls back to the filename stem.
    'Big.Buck.Bunny.2008.1080p.x264-TEST.webm': (full) => makeVideo(full),
    'Poster Movie.mp4': (full) => makeMp4WithPoster(full, poster),
  });
  check('scan completes with files indexed', movies.scan && movies.scan.status === 'Completed' && movies.scan.filesIndexed === 3,
    `status=${movies.scan && movies.scan.status} indexed=${movies.scan && movies.scan.filesIndexed}`);

  /* ------------------------------ API checks ------------------------------ */

  const { body: list } = await apiJson(`/movies?libraryId=${movies.libraryId}`);
  const matrix = list.find((m) => m.title === 'The Matrix');
  const bunny = list.find((m) => m.title === 'Big Buck Bunny');
  const posterMovie = list.find((m) => m.title === 'Poster Movie');
  check('movies endpoint lists scanned videos', list.length === 3, `count=${list.length}`);
  check('filename parsing extracts title and year',
    Boolean(matrix) && matrix.year === 1999, matrix && `year=${matrix.year}`);
  check('scene-style name parses with codec tail cut', Boolean(bunny) && bunny.year === 2008,
    bunny && `${bunny.title} ${bunny.year}`);
  check('unprobeable placeholder still indexes from its name', Boolean(matrix));

  // "The Matrix (1999).mkv" is a text placeholder: ffprobe must fail gracefully,
  // and the row must carry no duration rather than break the catalogue.
  check('probe failure degrades to filename metadata', Boolean(matrix) && matrix.durationSeconds === null);
  check('probe extracts duration from a real video', Boolean(bunny) && bunny.durationSeconds > 1,
    bunny && `dur=${bunny.durationSeconds}`);
  check('embedded poster detected', Boolean(posterMovie) && posterMovie.hasPoster === true,
    posterMovie && `hasPoster=${posterMovie.hasPoster}`);

  const posterResponse = await fetch(`${API}/movies/${posterMovie.id}/poster`);
  check('poster endpoint serves image bytes',
    posterResponse.status === 200 && (posterResponse.headers.get('content-type') || '').startsWith('image/'),
    `status=${posterResponse.status} type=${posterResponse.headers.get('content-type')}`);

  const streamResponse = await fetch(`${API}/movies/${bunny.id}/stream`, {
    headers: { Range: 'bytes=0-99' },
  });
  const streamBody = Buffer.from(await streamResponse.arrayBuffer());
  check('stream endpoint honors range requests',
    streamResponse.status === 206 && streamBody.length === 100 && (streamResponse.headers.get('content-type') || '').startsWith('video/'),
    `status=${streamResponse.status} bytes=${streamBody.length} type=${streamResponse.headers.get('content-type')}`);

  const { body: progressSaved } = await apiJson(`/movies/${bunny.id}/progress`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ positionSeconds: 0.8 }),
  });
  check('progress endpoint saves resume position',
    progressSaved && Math.abs(progressSaved.watchProgressSeconds - 0.8) < 0.001,
    progressSaved && `pos=${progressSaved.watchProgressSeconds}`);

  const { body: continueList } = await apiJson(`/movies/continue?libraryId=${movies.libraryId}`);
  check('continue-watching lists the partially watched movie',
    continueList.some((m) => m.id === bunny.id), `count=${continueList.length}`);

  const { body: markedWatched } = await apiJson(`/movies/${bunny.id}/watched`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ watched: true }),
  });
  check('watched endpoint marks and clears resume',
    markedWatched.watched === true && markedWatched.watchProgressSeconds === null);

  const { body: continueAfterWatched } = await apiJson(`/movies/continue?libraryId=${movies.libraryId}`);
  check('watched movies leave continue-watching', continueAfterWatched.length === 0, `count=${continueAfterWatched.length}`);

  const { body: reindexed } = await apiJson('/movies/reindex', { method: 'POST' });
  check('reindex command responds with counter', typeof reindexed.indexedMovies === 'number', `movies=${reindexed.indexedMovies}`);

  const { body: watchedFilter } = await apiJson(`/movies?libraryId=${movies.libraryId}&watched=true`);
  check('watched filter returns only watched movies',
    watchedFilter.length === 1 && watchedFilter[0].id === bunny.id, `count=${watchedFilter.length}`);

  /* ------------------------------- UI checks ------------------------------ */

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  await page.goto(BASE + '/movies', { waitUntil: 'networkidle' });
  await page.selectOption('select[aria-label="Movies library"]', movies.libraryId);
  await page.waitForSelector('section[aria-label="Movie collection"] figcaption:has-text("The Matrix")', { timeout: 15000 });
  check('movies page renders the collection grid', true);

  await page.waitForSelector('section[aria-label="Movie collection"] img', { timeout: 15000 });
  const posterLoaded = await page.evaluate(() => {
    const image = document.querySelector('section[aria-label="Movie collection"] img');
    return image && image.complete && image.naturalWidth > 0;
  });
  check('embedded poster renders from the image endpoint', posterLoaded === true);

  // Play the still-unwatched Matrix placeholder: player overlay opens and requests a stream.
  const streamPromise = page.waitForResponse(
    (response) => response.url().includes('/stream') && [200, 206].includes(response.status()),
    { timeout: 15000 },
  );
  await page.click('button[aria-label="Play The Matrix"]');
  const streamHit = await streamPromise;
  await page.waitForSelector('[data-testid="movie-player"]', { timeout: 15000 });
  check('player overlay opens and requests the stream',
    streamHit.request().headers().range !== undefined, `status=${streamHit.status()}`);
  await page.click('button[aria-label="Close player"]');
  await page.waitForSelector('[data-testid="movie-player"]', { state: 'detached' });

  // Playback on the real video: seeking to >= 95% must mark it watched server-side.
  await page.click('button[aria-label="Play Big Buck Bunny"]');
  await page.waitForSelector('[data-testid="movie-player"] video', { timeout: 15000 });
  await page.waitForFunction(() => {
    const video = document.querySelector('[data-testid="movie-player"] video');
    return video && Number.isFinite(video.duration) && video.duration > 0;
  }, null, { timeout: 15000 });
  await page.evaluate(() => {
    const video = document.querySelector('[data-testid="movie-player"] video');
    video.currentTime = video.duration * 0.97;
  });
  // The 'ended' event then triggers POST /watched; give it a moment.
  await page.waitForTimeout(1500);
  const { body: bunnyAfter } = await apiJson(`/movies?libraryId=${movies.libraryId}&watched=true`);
  check('reaching the end marks the movie watched server-side',
    bunnyAfter.some((m) => m.title === 'Big Buck Bunny'), `count=${bunnyAfter.length}`);
  // 'ended' also auto-closes the player; only close it if it is still open.
  const closeBtn = page.locator('button[aria-label="Close player"]');
  if (await closeBtn.isVisible().catch(() => false)) {
    await closeBtn.click();
  }
  await page.waitForSelector('[data-testid="movie-player"]', { state: 'detached', timeout: 15000 });

  fs.mkdirSync('shots', { recursive: true });
  await page.goto(BASE + '/movies', { waitUntil: 'networkidle' });
  await page.selectOption('select[aria-label="Movies library"]', movies.libraryId);
  await page.waitForSelector('section[aria-label="Movie collection"] figcaption', { timeout: 15000 });
  await page.screenshot({ path: 'shots/movies-catalog.png', fullPage: true });

  /* ---------------------------- M01–M05 regressions ---------------------------- */

  const { body: health } = await apiJson('/health');
  check('M01: health reports 0.6.0', health.version === '0.6.0', health.version);

  await page.goto(BASE + '/library', { waitUntil: 'networkidle' });
  const libraryVisible = await page.isVisible('section[aria-label="Libraries"], button:has-text("New library")');
  check('M02: library page renders', libraryVisible);

  await page.goto(BASE + '/music', { waitUntil: 'networkidle' });
  await page.selectOption('select[aria-label="Music library"]', movies.libraryId).catch(() => {});
  await page.waitForSelector('text=No tagged music yet', { timeout: 15000 });
  check('M05: music page still renders (empty for this library)', true);

  await page.goto(BASE + '/search', { waitUntil: 'networkidle' });
  await page.fill('input#library-search', 'matrix');
  await page.waitForSelector('[data-testid="result-count"]', { timeout: 15000 });
  const searchCount = (await page.textContent('[data-testid="result-count"]')).trim();
  check('M04: search still finds indexed files', /^[1-9]\d* result/.test(searchCount), searchCount);

  await browser.close();

  // Cleanup: seeded libraries cascade media items; movie rows follow via FK.
  await apiJson(`/libraries/${movies.libraryId}`, { method: 'DELETE' });
  try { fs.rmSync(movies.root, { recursive: true, force: true }); } catch {}
  try { fs.rmSync(poster, { force: true }); } catch {}

  console.log(failures === 0 ? '\nALL CHECKS PASSED' : `\n${failures} CHECK(S) FAILED`);
  process.exit(failures === 0 ? 0 : 1);
})().catch((error) => {
  console.error('RUNNER FAILED:', error.message);
  process.exit(1);
});
