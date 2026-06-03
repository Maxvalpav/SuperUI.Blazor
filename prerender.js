const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 8080;
const ROOT = path.resolve(__dirname, 'publish/wwwroot');
const SITEMAP = path.join(ROOT, 'sitemap.xml');
const BASE = '/SuperUI.Blazor/';
const MIME = {
    '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css',
    '.png': 'image/png', '.svg': 'image/svg+xml', '.json': 'application/json',
    '.wasm': 'application/wasm', '.br': 'application/brotli', '.gz': 'application/gzip',
};

function serve(req, res) {
    let p = req.url.split('?')[0];
    if (p === '/') p = '/index.html';
    const file = path.join(ROOT, p);
    if (!file.startsWith(ROOT)) { res.writeHead(403); res.end(); return; }
    if (!fs.existsSync(file)) { res.writeHead(404); res.end('Not found'); return; }
    const ext = path.extname(file);
    res.writeHead(200, {
        'Content-Type': MIME[ext] || 'application/octet-stream',
        'Content-Encoding': ext === '.br' ? 'br' : ext === '.gz' ? 'gzip' : undefined,
        'Cache-Control': 'no-cache',
    });
    fs.createReadStream(file).pipe(res);
}

async function main() {
    const sitemap = fs.readFileSync(SITEMAP, 'utf-8');
    const locs = [...sitemap.matchAll(/<loc>(.*?)<\/loc>/g)].map(m => m[1]);

    const routes = locs
        .map(url => {
            const u = new URL(url);
            let p = u.pathname;
            if (p.startsWith(BASE)) p = p.slice(BASE.length - 1);
            if (p.endsWith('/')) p = p.slice(0, -1);
            return p || '/';
        })
        .filter((v, i, a) => a.indexOf(v) === i);

    console.log(`Found ${routes.length} unique routes to prerender`);

    const server = http.createServer(serve);
    await new Promise(r => server.listen(PORT, '127.0.0.1', r));
    console.log(`Server running on http://127.0.0.1:${PORT}`);

    let puppeteer;
    try {
        puppeteer = require('puppeteer');
    } catch {
        console.log('Installing puppeteer...');
        require('child_process').execSync('npm install puppeteer', { stdio: 'inherit', cwd: __dirname });
        puppeteer = require('puppeteer');
    }

    const browser = await puppeteer.launch({
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
    });

    const page = await browser.newPage();
    page.setDefaultNavigationTimeout(45000);

    let success = 0, fail = 0;

    for (const route of routes) {
        const url = `http://127.0.0.1:${PORT}${route === '/' ? '/' : route}`;
        const outDir = path.join(ROOT, route === '/' ? '' : route);
        const outPath = route === '/'
            ? path.join(ROOT, 'index.html')
            : path.join(outDir, 'index.html');

        process.stdout.write(`  ${route} → `);

        try {
            await page.goto(url, { waitUntil: 'networkidle0', timeout: 45000 });
            try { await page.waitForSelector('.sui-content', { timeout: 15000 }); } catch {}
            await new Promise(r => setTimeout(r, 2000));

            const html = await page.content();
            const bodySize = await page.evaluate(() =>
                document.querySelector('.sui-content')?.textContent?.length ?? 0
            );

            if (route !== '/') fs.mkdirSync(outDir, { recursive: true });
            fs.writeFileSync(outPath, html, 'utf-8');

            const kb = (Buffer.byteLength(html) / 1024).toFixed(1);
            process.stdout.write(`✓ ${kb} KB (body: ${bodySize} chars)\n`);
            success++;
        } catch (err) {
            process.stdout.write(`✗ ${err.message.slice(0, 60)}\n`);
            fail++;
        }
    }

    await browser.close();
    server.close();

    console.log(`\nPrerender done: ${success} OK, ${fail} failed`);
    if (fail > 0) process.exit(1);
}

main().catch(err => { console.error(err); process.exit(1); });
