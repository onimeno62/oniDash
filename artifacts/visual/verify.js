// Objective layout/visual verification for Milestone 01 (BOOTSTRAP.md):
// - runs at 1280x800, 1440x900, 1920x1080, 2560x1440 (plus compact + mobile widths)
// - asserts design-token application, theme switching, responsive shell behavior,
//   absence of horizontal overflow, and live API data rendering.
'use strict';

const path = require('path');
const { chromium } = require('playwright');

const BASE = 'http://localhost:5275';
const DESKTOP = [
  [1280, 800],
  [1440, 900],
  [1920, 1080],
  [2560, 1440],
];

let failures = 0;
function check(name, ok, detail = '') {
  if (ok) {
    console.log(`PASS ${name}${detail ? ` (${detail})` : ''}`);
  } else {
    failures += 1;
    console.log(`FAIL ${name}${detail ? ` (${detail})` : ''}`);
  }
}

async function metrics(page) {
  return page.evaluate(() => ({
    bodyBg: getComputedStyle(document.body).backgroundColor,
    bodyColor: getComputedStyle(document.body).color,
    font: getComputedStyle(document.body).fontFamily,
    theme: document.documentElement.dataset.theme,
    scrollWidth: document.scrollingElement.scrollWidth,
    innerWidth: window.innerWidth,
    sidebarDisplay: getComputedStyle(document.querySelector('aside')).display,
    sidebarWidth: document.querySelector('aside')
      ? document.querySelector('aside').getBoundingClientRect().width
      : 0,
    sidebarLabelVisible: (() => {
      const label = document.querySelector('aside nav span');
      if (!label) return false;
      return getComputedStyle(label).display !== 'none';
    })(),
    hamburgerVisible: (() => {
      const btn = document.querySelector('button[aria-label="Open navigation"]');
      if (!btn) return false;
      return getComputedStyle(btn).display !== 'none';
    })(),
    accentUsed: getComputedStyle(document.documentElement).getPropertyValue('--accent').trim(),
  }));
}

(async () => {
  const browser = await chromium.launch();

  for (const [width, height] of DESKTOP) {
    const page = await browser.newPage({ viewport: { width, height } });
    await page.goto(BASE + '/', { waitUntil: 'networkidle' });
    const m = await metrics(page);
    check(`${width}x${height}: dark theme applied`, m.theme === 'dark', m.theme);
    check(
      `${width}x${height}: tokenized dark background`,
      m.bodyBg === 'rgb(10, 12, 17)',
      m.bodyBg,
    );
    check(
      `${width}x${height}: primary text color`,
      m.bodyColor === 'rgb(238, 241, 247)',
      m.bodyColor,
    );
    check(`${width}x${height}: system UI font stack`, m.font.includes('ui-sans-serif'), m.font);
    check(
      `${width}x${height}: no horizontal overflow`,
      m.scrollWidth <= m.innerWidth,
      `${m.scrollWidth}/${m.innerWidth}`,
    );
    check(
      `${width}x${height}: full sidebar with labels`,
      m.sidebarWidth > 200 && m.sidebarLabelVisible,
      `${Math.round(m.sidebarWidth)}px, labels=${m.sidebarLabelVisible}`,
    );
    check(`${width}x${height}: hamburger hidden`, !m.hamburgerVisible);
    check(
      `${width}x${height}: accent token present`,
      m.accentUsed === '#7c6af5',
      m.accentUsed,
    );

    // Live API data flows through to the health page. Version must be semver-ish so
    // this regression suite does not need a bump every milestone.
    await page.goto(BASE + '/health', { waitUntil: 'networkidle' });
    const healthText = await page.textContent('body');
    const versionMatch = /0\.\d+\.\d+/.test(healthText);
    check(
      `${width}x${height}: health page shows live API data`,
      healthText.includes('Healthy') && versionMatch && healthText.includes('Ok'),
    );
    await page.close();
  }

  // Theme toggle round-trip (dark → light → dark) at 1440x900.
  let page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  await page.click('button[aria-label="Switch to light theme"]');
  let m = await metrics(page);
  check('toggle: light theme applied', m.theme === 'light');
  check('toggle: light background token', m.bodyBg === 'rgb(244, 246, 250)', m.bodyBg);
  check(
    'toggle: persisted to localStorage',
    (await page.evaluate(() => localStorage.getItem('onidash.theme'))) === 'light',
  );
  await page.click('button[aria-label="Switch to dark theme"]');
  m = await metrics(page);
  check('toggle: back to dark theme', m.theme === 'dark' && m.bodyBg === 'rgb(10, 12, 17)');
  await page.close();

  // Light theme reload persistence.
  page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.addInitScript(() => localStorage.setItem('onidash.theme', 'light'));
  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  m = await metrics(page);
  check('reload: light theme persists', m.theme === 'light' && m.bodyBg === 'rgb(244, 246, 250)');

  // Compact icon rail between 768 and 1023.
  await page.setViewportSize({ width: 900, height: 700 });
  await page.waitForTimeout(250);
  m = await metrics(page);
  check(
    '900x700: compact icon rail without labels',
    m.sidebarWidth > 60 && m.sidebarWidth < 90 && !m.sidebarLabelVisible,
    `${Math.round(m.sidebarWidth)}px, labels=${m.sidebarLabelVisible}`,
  );
  check('900x700: no horizontal overflow', m.scrollWidth <= m.innerWidth);
  await page.close();

  // Mobile: sidebar hidden, hamburger opens drawer.
  page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  m = await metrics(page);
  check('390x844: sidebar hidden', m.sidebarDisplay === 'none');
  check('390x844: hamburger visible', m.hamburgerVisible);
  check('390x844: no horizontal overflow', m.scrollWidth <= m.innerWidth);
  await page.click('button[aria-label="Open navigation"]');
  await page.waitForTimeout(350);
  const drawerLink = await page.textContent('body');
  check('390x844: drawer opens with navigation', drawerLink.includes('Dashboard'));
  await page.keyboard.press('Escape');
  await page.waitForTimeout(250);
  const drawerGone = await page.evaluate(
    () => !document.querySelector('div.fixed.inset-0'),
  );
  check('390x844: Escape closes drawer', drawerGone);
  await page.close();

  await browser.close();
  console.log(failures === 0 ? 'ALL CHECKS PASSED' : `${failures} CHECK(S) FAILED`);
  process.exit(failures === 0 ? 0 : 1);
})().catch((e) => {
  console.error('RUNNER FAILED:', e.message);
  process.exit(1);
});
