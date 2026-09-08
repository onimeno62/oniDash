// Bootstraps a local NuGet folder feed by resolving the dependency closure of the
// given root packages and downloading their .nupkg files from nuget.org.
// Used in restricted-network environments where only node has TLS access.
// Usage: node fetch-nuget.js
'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');
const { spawnSync } = require('child_process');

const FEED = path.join(__dirname, 'nuget-feed');
const WORK = path.join(os.tmpdir(), 'onidash-nupkg-work');

const ROOTS = [
  ['Microsoft.EntityFrameworkCore.Sqlite', '10.0.0'],
  ['Microsoft.AspNetCore.Mvc.Testing', '10.0.0'],
  ['Microsoft.NET.Test.Sdk', '17.13.0'],
  ['xunit', '2.9.2'],
  ['xunit.core', '2.9.2'],
  ['xunit.assert', '2.9.2'],
  ['xunit.analyzers', '1.16.0'],
  ['Microsoft.TestPlatform.ObjectModel', '17.13.0'],
  ['xunit.runner.visualstudio', '2.8.2'],
];

const seen = new Map();
const queue = [...ROOTS];

function normalizeFloat(v) {
  if (!v) return null;
  let s = String(v).trim().replace(/[*xX]/g, '').replace(/\.$/, '').trim();
  return s || null;
}

function pickVersion(range) {
  if (!range) return null;
  const r = range.trim();
  const exact = r.match(/^\[\s*([^\s,\]]+)\s*\]$/);
  if (exact) return exact[1];
  if (r.includes(',')) {
    const parts = r.replace(/[[\]()\s]/g, '').split(',').filter(Boolean);
    if (!parts.length) return null;
    return normalizeFloat(parts[0]);
  }
  return normalizeFloat(r);
}

function frameworkScore(tfm) {
  const t = (tfm || '').toLowerCase().trim();
  if (!t) return 50; // group without target framework = always compatible fallback
  const candidates = t.split(';').map((s) => s.trim()).filter(Boolean);
  const scores = candidates.map((p) => {
    if (p === 'net10.0') return 0;
    const m = p.match(/^net(\d+)(?:\.(\d+))?$/);
    if (m) {
      const major = parseInt(m[1], 10);
      if (major >= 5 && major <= 9) return 10 - major; // net9.0 -> 1, net8.0 -> 2 ...
      if (major >= 10) return 90; // unlikely
      return 99; // net4x etc, incompatible with net10.0
    }
    if (p === 'netstandard2.1') return 20;
    if (p === 'netstandard2.0') return 21;
    if (p === 'netstandard1.6' || p === 'netstandard1.3') return 30;
    return 99;
  });
  return Math.min.apply(null, scores);
}

function parseDeps(nuspecText) {
  const deps = [];
  const depsBlock = nuspecText.match(/<dependencies\b[^>]*>([\s\S]*?)<\/dependencies>/);
  if (!depsBlock) return [];
  let body = depsBlock[1];

  const groupRe = /<group\b[^>]*?(?:targetFramework="([^"]*)")?[^>]*>([\s\S]*?)<\/group>/g;
  const depRe = /<dependency\s+id="([^"]+)"\s+version="([^"]*)"[^>]*\/?\s*>/g;
  let m;
  while ((m = groupRe.exec(body))) {
    const tfm = m[1] || '';
    let d;
    while ((d = depRe.exec(m[2]))) {
      deps.push({ id: d[1], range: d[2], score: frameworkScore(tfm) });
    }
  }

  // Bare <dependency> elements directly under <dependencies> (no group) are
  // always compatible (NuGet treats them as a fallback group).
  const withoutGroups = body.replace(groupRe, '');
  while ((m = depRe.exec(withoutGroups))) {
    deps.push({ id: m[1], range: m[2], score: 50 });
  }

  if (!deps.length) return [];
  const best = Math.min.apply(null, deps.map((x) => x.score));
  if (best >= 99) return []; // only incompatible groups
  return deps.filter((x) => x.score === best);
}

async function downloadInto(url, dest) {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
  fs.writeFileSync(dest, Buffer.from(await res.arrayBuffer()));
}

async function main() {
  fs.mkdirSync(FEED, { recursive: true });
  fs.mkdirSync(WORK, { recursive: true });

  while (queue.length) {
    const [id, version] = queue.shift();
    const key = id.toLowerCase();
    if (seen.has(key)) continue;
    seen.set(key, version);

    const dest = path.join(FEED, `${key}.${version}.nupkg`);
    if (!fs.existsSync(dest)) {
      const url = `https://api.nuget.org/v3-flatcontainer/${key}/${version}/${key}.${version}.nupkg`;
      await downloadInto(url, dest);
    }

    const extractDir = path.join(WORK, key);
    fs.rmSync(extractDir, { recursive: true, force: true });
    fs.mkdirSync(extractDir, { recursive: true });
    const tar = spawnSync('tar', ['-xf', dest, '-C', extractDir], { stdio: 'ignore' });
    if (tar.status !== 0) throw new Error(`tar failed on ${dest}`);

    const nuspecName = fs.readdirSync(extractDir).find((f) => f.toLowerCase().endsWith('.nuspec'));
    if (!nuspecName) throw new Error(`no nuspec in ${dest}`);
    const nuspecText = fs.readFileSync(path.join(extractDir, nuspecName), 'utf8');

    for (const dep of parseDeps(nuspecText)) {
      const v = pickVersion(dep.range);
      if (!v) {
        console.log(`WARN cannot resolve range ${dep.id} ${dep.range} (dep of ${id} ${version})`);
        continue;
      }
      if (!seen.has(dep.id.toLowerCase())) queue.push([dep.id, v]);
    }
    console.log(`OK ${id} ${version}`);
  }

  console.log(`DONE — ${seen.size} packages in ${FEED}`);
}

main().catch((e) => {
  console.error('FAILED:', e.cause ? e.cause.message || e.cause : e.message);
  process.exit(1);
});
