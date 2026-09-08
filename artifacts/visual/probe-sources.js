'use strict';
const { chromium } = require('playwright');
const BASE = 'http://localhost:5275';
const API = BASE + '/api';
const SOURCE_PATH = process.env.ONIDASH_SOURCE_PATH;

(async () => {
  const { body: lib } = await fetch(API + '/libraries', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: '__probe__' }),
  }).then((r) => r.json()).then((b) => ({ body: b }));
  await fetch(`${API}/libraries/${lib.id}/sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'Main', rootPath: SOURCE_PATH }),
  });

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  page.on('response', (r) => {
    if (r.url().includes('/api/')) console.log('API', r.status(), r.url());
  });
  page.on('pageerror', (e) => console.log('PAGEERROR', e.message));
  await page.goto(BASE + '/library', { waitUntil: 'networkidle' });
  await page.waitForSelector('text=__probe__');
  await page.click('button[aria-expanded] >> nth=0');
  await page.waitForTimeout(1500);
  const section = await page.evaluate(() => {
    const el = document.querySelector('section[aria-label="Folder sources"]');
    return el ? el.innerText : 'NO SECTION RENDERED';
  });
  console.log('--- sources section ---');
  console.log(section);
  await browser.close();
  await fetch(`${API}/libraries/${lib.id}`, { method: 'DELETE' });
})().catch((e) => { console.error('FAILED:', e.message); process.exit(1); });
