import { chromium } from "playwright";
const URL = "https://ievangelist.github.io/netlify-aspire-integration/?_=" + Date.now();
const b = await chromium.launch();
const ctx = await b.newContext({ viewport: { width: 1440, height: 1100 } });
for (const theme of ["dark", "light"]) {
  const p = await ctx.newPage();
  await p.goto(URL, { waitUntil: "networkidle" });
  await p.evaluate((t) => { document.documentElement.dataset.theme = t; }, theme);
  await p.waitForTimeout(500);
  await p.screenshot({ path: `tools/landing-${theme}.png`, fullPage: true });
  // Hover screenshots: framework pill + flow step
  await p.evaluate(() => { document.querySelector('.framework')?.dispatchEvent(new MouseEvent('mouseover', { bubbles: true })); });
  // (CSS :hover doesn't kick in via JS event; just take a non-hover full-page)
  await p.close();
}
// Check counts and basic geometry on the live page
const p2 = await ctx.newPage();
await p2.goto(URL, { waitUntil: "networkidle" });
const audit = await p2.evaluate(() => {
  const fws = Array.from(document.querySelectorAll('.framework')).map(a => a.getAttribute('aria-label'));
  const flow = Array.from(document.querySelectorAll('.flow__step')).map(li => li.querySelector('.flow__title')?.textContent);
  const stats = Array.from(document.querySelectorAll('.statrow__item')).map(it => ({
    hasIcon: !!it.querySelector('.statrow__icon'),
    label: it.querySelector('.statrow__label')?.textContent
  }));
  const h2s = Array.from(document.querySelectorAll('main h2')).map(h => ({
    text: h.textContent?.trim(),
    hasIcon: !!h.querySelector('.h2-icon')
  }));
  const install = !!document.querySelector('.install-note');
  return { fws, flow, stats, h2s, install };
});
console.log(JSON.stringify(audit, null, 2));
await b.close();
