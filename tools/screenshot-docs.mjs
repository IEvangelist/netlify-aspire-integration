import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(__dirname, "screens");
mkdirSync(outDir, { recursive: true });

const base = process.argv[2] ?? "https://ievangelist.github.io/netlify-aspire-integration";
const targets = [
  ["home", "/"],
  ["quickstart", "/guides/quickstart/"],
  ["api", "/api/"],
  ["frameworks-angular", "/frameworks/angular/"],
  ["guides-install", "/guides/install/"],
];

const viewports = [
  { name: "desktop", width: 1440, height: 900 },
  { name: "mobile", width: 390, height: 844 },
];

const browser = await chromium.launch();
for (const { name: vp, width, height } of viewports) {
  const ctx = await browser.newContext({
    viewport: { width, height },
    deviceScaleFactor: 1,
    colorScheme: "dark",
  });
  const page = await ctx.newPage();
  for (const [name, path] of targets) {
    const url = base + path;
    try {
      await page.goto(url, { waitUntil: "networkidle", timeout: 30000 });
      await page.waitForTimeout(1500);
      const file = `${outDir}/${name}-${vp}.png`;
      await page.screenshot({ path: file, fullPage: true });
      console.log(`OK  ${vp.padEnd(8)} ${path.padEnd(36)} ${file}`);
    } catch (err) {
      console.log(`ERR ${vp.padEnd(8)} ${path.padEnd(36)} ${err.message}`);
    }
  }
  await ctx.close();
}
await browser.close();
