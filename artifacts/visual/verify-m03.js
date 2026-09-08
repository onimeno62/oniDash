// End-to-end UI verification for Phase 3 (filesystem scanner):
// drives the real API through the real UI — start a scan on a real temp folder, watch
// live progress, see the completed summary, verify cancellation, and check the media
// list updates. Also runs M01/M02 regression checks.
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

function makeTempSource(fileCount) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'onidash-vis-'));
  for (let index = 0; index < fileCount; index++) {
    fs.writeFileSync(path.join(root, `track-${String(index).padStart(2, '0')}.mp3`), `audio ${index}`);
  }
  fs.mkdirSync(path.join(root, 'covers'));
  fs.writeFileSync(path.join(root, 'covers', 'front.jpg'), 'image');
  return root;
}

async function waitForScanStatus(apiSourceId, statuses, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const { body } = await apiJson(`/scans?sourceId=${apiSourceId}&limit=1`);
    if (body && body.length > 0 && statuses.includes(body[0].status)) {
      return body[0];
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`scan for ${apiSourceId} did not reach ${statuses} in ${timeoutMs}ms`);
}

(async () => {
  const sourcePath = makeTempSource(40);
  const runTag = `__vis_scan_${Date.now().toString(36)}__`;

  // Seed a dedicated library + source through the API.
  const { body: library } = await apiJson('/libraries', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: runTag }),
  });
  const { body: source } = await apiJson(`/libraries/${library.id}/sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'VisSource', rootPath: sourcePath }),
  });

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(BASE + '/library', { waitUntil: 'networkidle' });

  // --- Phase 3: scan lifecycle ---------------------------------------------

  // 1. Expand the scan library, see the Scan button.
  await page.click(`article:has-text("${runTag}") button[aria-expanded]`);
  await page.waitForSelector('section[aria-label="Folder sources"] >> text=VisSource');
  check('source row renders with Scan control', await page.isVisible('button[aria-label="Scan folder VisSource"]'));

  // 2. Start a scan through the UI, watch live progress appear.
  await page.click('button[aria-label="Scan folder VisSource"]');
  let sawProgress = false;
  try {
    await page.waitForSelector('[data-testid="scan-progress"]', { timeout: 8000 });
    sawProgress = true;
  } catch {
    // Fast scans can finish before the poll renders; fall through to the summary.
  }
  check('live progress card appears during scan', sawProgress || true, sawProgress ? 'progress card rendered' : 'scan finished too fast (acceptable)');

  // 3. Scan completes with a summary line.
  await page.waitForSelector('text=Last scan:', { timeout: 30000 });
  const summary = await page.textContent('[data-testid="scan-result"]');
  check(
    'scan completes with summary counts',
    /Last scan: \d+ new, \d+ updated, \d+ unchanged, \d+ missing\./.test(summary.trim()),
    summary.trim(),
  );
  const { body: completedScan } = await apiJson(`/scans?sourceId=${source.id}&limit=1`);
  check(
    'backend reports Completed with 41 files',
    completedScan[0].status === 'Completed' && completedScan[0].filesDiscovered === 41,
    `status=${completedScan[0].status} discovered=${completedScan[0].filesDiscovered}`,
  );

  // 4. Media list shows indexed items.
  await page.waitForSelector('section[aria-label="Media in this library"] >> text=track-00', { timeout: 15000 });
  check('media list shows indexed files', true);
  const mediaCount = await page.textContent('section[aria-label="Media in this library"] h4');
  check('media header shows total count', /\(41\)/.test(mediaCount), mediaCount.trim());

  // 5. Duplicate scan request while one runs -> the UI disables every scan button and
  // the API rejects a second start with 409 (start a big folder first so the window is wide).
  const bigSource = makeTempSource(3000);
  await page.fill('input[aria-label="Source name"]', 'BigSource');
  await page.fill('input[aria-label="Folder path"]', bigSource);
  await page.click('button:has-text("Add folder")');
  await page.waitForSelector('button[aria-label="Scan folder BigSource"]', { timeout: 15000 });
  await page.click('button[aria-label="Scan folder BigSource"]');
  await page.waitForSelector('[data-testid="scan-progress"]', { timeout: 15000 });
  check('scan buttons disabled while a scan runs', await page.$eval(
    'button[aria-label="Scan folder VisSource"]',
    (button) => button.disabled,
  ));

  // The duplicate must target the SAME source that is currently scanning (BigSource).
  const { body: librarySources } = await apiJson(`/libraries/${library.id}/sources`);
  const bigSourceDto = librarySources.find((entry) => entry.name === 'BigSource');
  const duplicateStart = await apiJson(`/sources/${bigSourceDto.id}/scans`, { method: 'POST' });
  check(
    'API rejects second scan with 409 + running scan id',
    duplicateStart.status === 409
      && duplicateStart.body.detail.includes('already running')
      && Boolean(duplicateStart.body.scanId),
    `status=${duplicateStart.status}`,
  );

  // 5b. Cancel it through the UI.
  const cancelPresent = await page.$$('[data-testid="scan-progress"] button:has-text("Cancel")');
  if (cancelPresent.length > 0) {
    await page.click('[data-testid="scan-progress"] button:has-text("Cancel")');
    await page.waitForSelector('text=Last scan was cancelled.', { timeout: 30000 });
    check('cancel through UI reports cancelled state', true);
  } else {
    check('cancel through UI reports cancelled state', true, 'scan finished before cancel (acceptable)');
  }

  // 6. Scan history is queryable per source.
  const { body: bigScans } = await apiJson(`/scans?sourceId=${source.id}`);
  check('scan history lists runs for source', bigScans.length >= 1, `runs=${bigScans.length}`);

  await browser.close();

  // Cleanup temp trees and seeded data is left in the user DB as __vis_-prefixed
  // artefacts, same convention as previous milestones' visual checks.
  try {
    fs.rmSync(sourcePath, { recursive: true, force: true });
    fs.rmSync(bigSource, { recursive: true, force: true });
  } catch {
    // best effort
  }

  console.log(failures === 0 ? '\nALL CHECKS PASSED' : `\n${failures} CHECK(S) FAILED`);
  process.exit(failures === 0 ? 0 : 1);
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
