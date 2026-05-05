// Post-process DocFX HTML output to fix dangling parent-namespace breadcrumb links.
//
// DocFX renders namespace breadcrumbs by walking each segment, e.g.
//   <a href="Aspire.html">Aspire</a>.<a href="Aspire.Hosting.html">Hosting</a>
// but never generates a page for a parent namespace that has no public types
// directly in it (e.g. `Aspire`). This script rewrites those broken anchors as
// plain spans so we don't ship 404s.
//
// Usage: node tools/fix-docfx-breadcrumbs.mjs <site-dir>
import { readFile, writeFile, readdir } from "node:fs/promises";
import { join } from "node:path";

const siteDir = process.argv[2];
if (!siteDir) {
  console.error("usage: node fix-docfx-breadcrumbs.mjs <site-dir>");
  process.exit(2);
}

async function listHtml(dir) {
  const out = [];
  for (const e of await readdir(dir, { withFileTypes: true })) {
    const p = join(dir, e.name);
    if (e.isDirectory()) out.push(...(await listHtml(p)));
    else if (e.name.endsWith(".html")) out.push(p);
  }
  return out;
}

const files = await listHtml(siteDir);
const presentPages = new Set(files.map((p) => p.split(/[\\/]/).pop()));
const linkRe = /<a class="xref" href="([^"]+\.html)">([^<]+)<\/a>/g;

let totalFixed = 0;
for (const file of files) {
  const html = await readFile(file, "utf8");
  let touched = false;
  const fixed = html.replace(linkRe, (m, href, text) => {
    if (presentPages.has(href)) return m;
    touched = true;
    totalFixed++;
    return `<span class="xref">${text}</span>`;
  });
  if (touched) await writeFile(file, fixed);
}

console.log(`fix-docfx-breadcrumbs: rewrote ${totalFixed} dangling anchors across ${files.length} files`);
