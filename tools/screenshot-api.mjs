import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(__dirname, "screens");
mkdirSync(outDir, { recursive: true });

const base = process.argv[2] ?? "http://127.0.0.1:4321/netlify-aspire-integration";

const apiPaths = [
  ["/api/", "api-index"],
  ["/api/Aspire.Hosting.html", "api-namespace"],
  ["/api/Aspire.Hosting.JavaScriptHostingExtensions.html", "api-extensions"],
  ["/api/Aspire.Hosting.NetlifyDeployOptions.html", "api-options"],
];

const browser = await chromium.launch();

for (const theme of ["dark", "light"]) {
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: theme,
  });
  const page = await ctx.newPage();
  for (const [path, name] of apiPaths) {
    try {
      await page.goto(base + path, { waitUntil: "networkidle", timeout: 30000 });
      await page.waitForTimeout(500);
      const file = `${outDir}/${name}-${theme}.png`;
      await page.screenshot({ path: file, fullPage: true });
      console.log(`OK  ${theme}  ${path.padEnd(40)}  ${file}`);
    } catch (err) {
      console.log(`FAIL ${theme} ${path}: ${err.message}`);
    }
  }
  await ctx.close();
}

await browser.close();
