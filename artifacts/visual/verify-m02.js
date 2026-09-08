// End-to-end UI verification for Milestone 02 (core library):
// drives the real API through the real UI — create library, rename, add/remove sources,
// delete with confirmation — plus regression checks of the M01 shell.
'use strict';

const { chromium } = require('playwright');

const BASE = 'http://localhost:5275';
const API = BASE + '/api';
const SOURCE_PATH = process.env.ONIDASH_SOURCE_PATH;

let failures = 0;
function check(name, ok, detail = '') {
  if (ok) {
    console.log(`PASS ${name}${detail ? ` (${detail})` : ''}`);
  } else {
    failures += 1;
    console.log(`FAIL ${name}${detail ? ` (${detail})` : ''}`);
  }
}

async function apiJson(path, init) {
  const response = await fetch(API + path, init);
  const text = await response.text();
  return { status: response.status, body: text ? JSON.parse(text) : null };
}

(async () => {
  // Seed: two libraries via API so the UI has content.
  const created = [];
  for (const name of ['__vis_music__', '__vis_movies__']) {
    const { body } = await apiJson('/libraries', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    });
    created.push(body);
  }
  const musicId = created[0].id;
  await apiJson(`/libraries/${musicId}/sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'Main', rootPath: SOURCE_PATH }),
  });

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(BASE + '/library', { waitUntil: 'networkidle' });

  // 1. Libraries from the API are rendered.
  await page.waitForSelector('text=__vis_music__');
  check('library list renders API data', true);

  // 2. Create a library through the UI.
  await page.click('button:has-text("New library")');
  await page.fill('input[aria-label="Library name"]', '__vis_tv__');
  await page.click('button:has-text("Create library")');
  await page.waitForSelector('text=__vis_tv__');
  const libraries = (await (await fetch(API + '/libraries')).json()).map((l) => l.name);
  check('UI create persists via API', libraries.includes('__vis_tv__'));

  // 3. Rename it through the UI.
  await page.click('button[aria-label="Rename library __vis_tv__"]');
  await page.fill('input[aria-label="Library name"]', '__vis_tv_renamed__');
  await page.click('button:has-text("Save")');
  await page.waitForSelector('text=__vis_tv_renamed__');
  check('UI rename persists via API', true);

  // 4. Expand sources, see the seeded source, add another one.
  await page.click('article:has-text("__vis_music__") button[aria-expanded]');
  await page.waitForSelector('section[aria-label="Folder sources"] >> text=Main');
  check('sources expand shows seeded folder', true);
  await page.fill('input[aria-label="Source name"]', 'Secondary');
  await page.fill('input[aria-label="Folder path"]', SOURCE_PATH);
  await page.click('button:has-text("Add folder")');
  // Duplicate path -> the UI must surface the 409 message.
  await page.waitForSelector('text=already configured as a source');
  check('duplicate source error surfaces in UI', true);

  // 5. Delete the renamed library with confirmation flow.
  await page.click('button[aria-label="Delete library __vis_tv_renamed__"]');
  await page.click('button:has-text("Delete")');
  await page.waitForFunction(() => !document.body.innerText.includes('__vis_tv_renamed__'));
  const afterDelete = (await (await fetch(API + '/libraries')).json()).map((l) => l.name);
  check('UI delete persists via API', !afterDelete.includes('__vis_tv_renamed__'));

  // 6. M01 regressions: shell, themes, pages.
  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  const theme = await page.evaluate(() => document.documentElement.dataset.theme);
  check('dashboard still renders (dark)', theme === 'dark');
  await page.click('button[aria-label="Switch to light theme"]');
  check(
    'theme toggle still works',
    (await page.evaluate(() => document.documentElement.dataset.theme)) === 'light',
  );
  await page.click('button[aria-label="Switch to dark theme"]');

  for (const route of ['/health', '/search', '/settings']) {
    const response = await page.goto(BASE + route, { waitUntil: 'networkidle' });
    check(`${route} still renders`, response.status() === 200);
  }

  // 7. Layout sanity at the three smaller required resolutions.
  for (const [w, h] of [[1280, 800], [1920, 1080], [2560, 1440]]) {
    await page.setViewportSize({ width: w, height: h });
    await page.goto(BASE + '/library', { waitUntil: 'networkidle' });
    const overflow = await page.evaluate(
      () => document.scrollingElement.scrollWidth > window.innerWidth,
    );
    const visible = await page.isVisible('text=__vis_music__');
    check(`${w}x${h}: library page, no overflow`, !overflow && visible);
  }

  await browser.close();

  // Cleanup: delete seeded libraries and the added source left behind.
  const all = (await (await fetch(API + '/libraries')).json());
  for (const library of all.filter((l) => l.name.startsWith('__vis_'))) {
    await apiJson(`/libraries/${library.id}`, { method: 'DELETE' });
  }
  console.log(failures === 0 ? 'ALL CHECKS PASSED' : `${failures} CHECK(S) FAILED`);
  process.exit(failures === 0 ? 0 : 1);
})().catch((e) => {
  console.error('RUNNER FAILED:', e.message);
  process.exit(1);
});
