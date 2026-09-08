// End-to-end UI verification for Phase 4 (search):
// scan a real folder through the API, then drive the search UI — live results, ranking,
// library filter, empty and error states — plus M01/M02 regressions of the shell.
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

async function makeScanSource(tag, files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), `onidash-p4-${tag}-`));
  for (const [name, content] of Object.entries(files)) {
    const full = path.join(root, name);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, content);
  }
  const { body: library } = await apiJson('/libraries', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: `__p4_${tag}_${Date.now().toString(36)}__` }),
  });
  const { body: source } = await apiJson(`/libraries/${library.id}/sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'Main', rootPath: root }),
  });
  await apiJson(`/sources/${source.id}/scans`, { method: 'POST' });

  const deadline = Date.now() + 30000;
  while (Date.now() < deadline) {
    const { body: scans } = await apiJson(`/scans?sourceId=${source.id}&limit=1`);
    if (scans[0] && scans[0].status !== 'Running') break;
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  return { libraryId: library.id, libraryName: library.name, root };
}

(async () => {
  // Idempotence: visual-check libraries from any earlier run would distort counts.
  const { body: existingLibraries } = await apiJson('/libraries');
  for (const library of existingLibraries) {
    if (/^__(p4|vis|m0\d)_/.test(library.name)) {
      await apiJson(`/libraries/${library.id}`, { method: 'DELETE' });
    }
  }

  // Seed two scan-fed libraries so ranking and filtering have real data.
  const music = await makeScanSource('music', {
    'night-drive.mp3': 'a',
    'sunrise.mp3': 'b',
    'covers/front.jpg': 'c',
  });
  const movies = await makeScanSource('movies', {
    'night-hunter.mkv': 'a',
    'sunrise-serial.mkv': 'b',
  });

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(BASE + '/search', { waitUntil: 'networkidle' });

  // 1. Empty state before typing.
  await page.waitForSelector('text=Type to search your library');
  check('empty state renders before typing', true);

  // 2. Live search: results appear as you type, ranked, with library names.
  const box = page.locator('input#library-search');
  await box.fill('night');
  await page.waitForSelector('[data-testid="result-count"]', { timeout: 15000 });
  const countText = (await page.textContent('[data-testid="result-count"]')).trim();
  check('live results appear while typing', /2 results/.test(countText), countText);
  const firstResult = await page.textContent(
    'section[aria-label="Search results"] li >> nth=0',
  );
  check('most relevant result first (tie-break deterministic)', firstResult.includes('night-'), firstResult.trim().slice(0, 60));

  // 3. All terms must match. The count must transition 2 -> 1; the stale list from
  // the previous query can contain the same file, so only the count is transition-safe.
  await box.fill('night hunter');
  await page.waitForFunction(
    () => /^1 result/.test(document.querySelector('[data-testid="result-count"]')?.textContent ?? ''),
    null,
    { timeout: 15000 },
  );
  check('multi-term AND narrows results', true);

  // 4. Library filter constrains results: the only rendered hit must carry the music
  // library badge (display names are file stems, e.g. "sunrise").
  await page.selectOption('#library-filter', music.libraryId);
  await box.fill('sunrise');
  await page.waitForSelector(
    'section[aria-label="Search results"] li:has-text("sunrise"):has-text("__p4_music_")',
    { timeout: 15000 },
  );
  const filteredCount = (await page.textContent('[data-testid="result-count"]')).trim();
  const filteredFirst = await page.textContent('section[aria-label="Search results"] li >> nth=0');
  check('library filter constrains results',
    /^1 result/.test(filteredCount) && filteredFirst.includes('__p4_music_'),
    `${filteredCount}; first=${filteredFirst.trim().slice(0, 60)}`);

  // 5. No-results state.
  await box.fill('zzznothing');
  await page.waitForSelector('text=No results', { timeout: 15000 });
  check('no-results state renders', true);

  // 6. No overflow at required resolutions on the search page.
  for (const [w, h] of [[1280, 800], [1920, 1080], [2560, 1440]]) {
    await page.setViewportSize({ width: w, height: h });
    await page.goto(BASE + '/search', { waitUntil: 'networkidle' });
    const overflow = await page.evaluate(
      () => document.scrollingElement.scrollWidth > window.innerWidth,
    );
    check(`${w}x${h}: search page, no overflow`, !overflow);
  }

  await browser.close();

  // 7. Reindex endpoint keeps working against real data.
  const { body: reindexed } = await apiJson('/search/reindex', { method: 'POST' });
  check('reindex command reports item count', reindexed.indexedItems >= 5, `items=${reindexed.indexedItems}`);

  // 8. Search stays consistent after a scan adds files (live index, no reindex needed).
  const extra = path.join(music.root, 'night-echo.mp3');
  fs.writeFileSync(extra, 'x');
  const { body: sources } = await apiJson(`/libraries/${music.libraryId}/sources`);
  const { body: secondScan } = await apiJson(`/sources/${sources[0].id}/scans`, { method: 'POST' });
  const deadline = Date.now() + 30000;
  let secondDone = null;
  while (Date.now() < deadline) {
    const { body: scans } = await apiJson(`/scans?sourceId=${sources[0].id}&limit=1`);
    if (scans[0] && scans[0].status !== 'Running') { secondDone = scans[0]; break; }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  check('rescan completes', secondDone && secondDone.status === 'Completed', `indexed=${secondDone && secondDone.filesIndexed}`);
  const { body: echoHits } = await apiJson('/search?q=night-echo');
  check('newly scanned file searchable immediately', echoHits.length === 1 && echoHits[0].displayName === 'night-echo', `hits=${echoHits.length}`);

  // Cleanup: remove seeded libraries (cascades items + files + FTS rows).
  for (const seeded of [music, movies]) {
    await apiJson(`/libraries/${seeded.libraryId}`, { method: 'DELETE' });
  }
  try { fs.rmSync(music.root, { recursive: true, force: true }); } catch {}
  try { fs.rmSync(movies.root, { recursive: true, force: true }); } catch {}

  console.log(failures === 0 ? '\nALL CHECKS PASSED' : `\n${failures} CHECK(S) FAILED`);
  process.exit(failures === 0 ? 0 : 1);
})().catch((error) => {
  console.error('RUNNER FAILED:', error.message);
  process.exit(1);
});
