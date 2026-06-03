// Automated a11y audit with axe-core + Playwright
// Run: npm run test:a11y
// First time: npx playwright install chromium

import { chromium } from 'playwright';
import AxeBuilder from '@axe-core/playwright';
import { writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const OUT = join(__dirname, '..', '..', 'test-results', 'a11y');
const URLS = [
    // Adjust these to your demo app's routes
    process.env.A11Y_BASE_URL || 'http://localhost:5000',
];

async function audit(url) {
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
    const page = await context.newPage();

    try {
        await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 });
        console.log(`Auditing: ${url}`);

        const results = await new AxeBuilder({ page })
            .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'best-practice'])
            .analyze();

        mkdirSync(OUT, { recursive: true });

        // Save full report
        const reportPath = join(OUT, `axe-report-${Date.now()}.json`);
        writeFileSync(reportPath, JSON.stringify(results, null, 2));
        console.log(`Report saved: ${reportPath}`);

        // Print violations
        const { violations, passes } = results;
        if (violations.length === 0) {
            console.log(`\n  PASSED: 0 violations (${passes.length} checks passed)\n`);
        } else {
            console.log(`\n  FAILED: ${violations.length} violation(s):\n`);
            for (const v of violations) {
                console.log(`  [${v.impact}] ${v.help}`);
                console.log(`    ${v.helpUrl}`);
                console.log(`    Elements: ${v.nodes.length}`);
                console.log();
            }
        }

        return violations.length;
    } finally {
        await browser.close();
    }
}

// Main
const totalViolations = await audit(URLS[0]);
process.exit(totalViolations > 0 ? 1 : 0);
