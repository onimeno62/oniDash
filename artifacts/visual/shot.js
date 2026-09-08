// Visual verification for Milestone 01: screenshots at the resolutions required by
// BOOTSTRAP.md (1280x800, 1440x900, 1920x1080, 2560x1440) plus theme/state variants.
// Requires the API serving the SPA on http://localhost:5275.
'use strict';

const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const BASE = 'http://localhost:5275';
const OUT = path.join(__dirname, 'shots');
const VIEWPORTS = [
  [1280, 800],
  [1440, 900],
  [1920, 1080],
  [2560, 1440],
];

async function shot(page, url, file, settleMs = 500) {
  await page.goto(BASE + url, { waitUntil: 'networkidle' });
  await page.waitForTimeout(settleMs);
  await page.screenshot({ path: path.join(OUT, file) });
  console.log('shot', file);
}

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const browser = await chromium.launch();

  for (const [width, height] of VIEWPORTS) {
    const page = await browser.newPage({ viewport: { width, height } });
    await shot(page, '/', `dashboard-${width}x${height}-dark.png`);
    await page.close();
  }

  let page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.addInitScript(() => localStorage.setItem('onidash.theme', 'dark'));
  await shot(page, '/health', 'health-1440x900-dark.png');
  await shot(page, '/settings', 'settings-1440x900-dark.png');
  await page.close();

  page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.addInitScript(() => localStorage.setItem('onidash.theme', 'light'));
  await shot(page, '/', 'dashboard-1440x900-light.png');
  await shot(page, '/health', 'health-1440x900-light.png');
  await shot(page, '/search', 'search-1440x900-light.png');
  await page.close();

  page = await browser.newPage({ viewport: { width: 900, height: 700 } });
  await shot(page, '/', 'dashboard-900x700-iconrail.png');
  await page.close();

  page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  await shot(page, '/', 'mobile-390x844-dashboard.png');
  await page.click('button[aria-label="Open navigation"]');
  await page.waitForTimeout(450);
  await page.screenshot({ path: path.join(OUT, 'mobile-390x844-drawer.png') });
  console.log('shot mobile-390x844-drawer.png');
  await page.close();

  await browser.close();
  console.log('DONE');
})().catch((e) => {
  console.error('FAILED:', e.message);
  process.exit(1);
});
