// Crawl the live docs site and report broken internal links + flag external 4xx/5xx.
// Usage:
//   node tools/linkcheck.mjs [base]
// Defaults to the GitHub Pages site.
import { JSDOM } from "jsdom";

const base = process.argv[2] ?? "https://ievangelist.github.io/netlify-aspire-integration";
const baseUrl = new URL(base.endsWith("/") ? base : base + "/");
const startPaths = ["/"];

const seen = new Map(); // url -> { status, contentType, refs:Set<string> }
const queue = [];
const broken = [];
const externalFailures = [];

function normalize(url, fromUrl) {
  try {
    const u = new URL(url, fromUrl);
    u.hash = "";
    // collapse trailing index.html
    if (u.pathname.endsWith("/index.html")) u.pathname = u.pathname.replace(/index\.html$/, "");
    return u;
  } catch {
    return null;
  }
}

function isInternal(u) {
  return u.origin === baseUrl.origin && u.pathname.startsWith(baseUrl.pathname);
}

async function head(url) {
  try {
    const r = await fetch(url, { method: "HEAD", redirect: "follow" });
    if (r.status === 405 || r.status === 403) {
      const r2 = await fetch(url, { method: "GET", redirect: "follow" });
      return { status: r2.status, contentType: r2.headers.get("content-type") ?? "", text: r2 };
    }
    return { status: r.status, contentType: r.headers.get("content-type") ?? "", text: null };
  } catch (e) {
    return { status: 0, contentType: "", error: String(e?.message ?? e) };
  }
}

async function get(url) {
  const r = await fetch(url, { redirect: "follow" });
  return { status: r.status, contentType: r.headers.get("content-type") ?? "", body: await r.text() };
}

for (const p of startPaths) queue.push(new URL(p === "/" ? "" : p.replace(/^\//, ""), baseUrl).toString());

while (queue.length > 0) {
  const url = queue.shift();
  if (seen.has(url)) continue;
  const u = new URL(url);
  const internal = isInternal(u);

  if (!internal) {
    // External: just HEAD-check (skip github API rate-limits etc by limiting to public docs hosts)
    const r = await head(url);
    seen.set(url, { status: r.status });
    if (r.status >= 400 || r.status === 0) externalFailures.push({ url, status: r.status, error: r.error });
    continue;
  }

  // Internal: GET, parse links if HTML.
  const r = await get(url);
  seen.set(url, { status: r.status, contentType: r.contentType });
  if (r.status >= 400) {
    broken.push({ url, status: r.status });
    continue;
  }
  if (!/text\/html/i.test(r.contentType)) continue;

  const dom = new JSDOM(r.body);
  const doc = dom.window.document;
  const anchors = [...doc.querySelectorAll("a[href]")];
  for (const a of anchors) {
    const href = a.getAttribute("href");
    if (!href || href.startsWith("javascript:") || href.startsWith("mailto:") || href.startsWith("#")) continue;
    const next = normalize(href, url);
    if (!next) continue;
    const nextStr = next.toString();
    if (!seen.has(nextStr)) queue.push(nextStr);
  }
}

const summary = {
  totalChecked: seen.size,
  internalBroken: broken.length,
  externalFailures: externalFailures.length,
};
console.log("=== link check summary ===");
console.log(JSON.stringify(summary, null, 2));
if (broken.length) {
  console.log("\n=== INTERNAL BROKEN ===");
  for (const b of broken) console.log(`  ${b.status}  ${b.url}`);
}
if (externalFailures.length) {
  console.log("\n=== EXTERNAL FAILURES ===");
  for (const b of externalFailures) console.log(`  ${b.status}  ${b.url}${b.error ? "  // " + b.error : ""}`);
}
process.exit(broken.length > 0 ? 1 : 0);
