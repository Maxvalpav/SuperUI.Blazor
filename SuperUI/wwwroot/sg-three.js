// SgThree - Three.js Integration Module for SuperUI Blazor
// Provides JS interop for SgThree component.

const _instances = new Map();
const _loaded    = new Set();

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (!url || _loaded.has(url)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) { _loaded.add(url); resolve(); return; }
        const s = document.createElement('script');
        s.src = url;
        s.onload  = () => { _loaded.add(url); resolve(); };
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

async function _ensureThree(sources) {
    // Load Three.js core first
    if (sources?.threeScript) await _loadScript(sources.threeScript);

    // Wait for window.THREE to be available
    let T = window.THREE;
    let n = 0;
    while (!T && n++ < 80) { await new Promise(r => setTimeout(r, 100)); T = window.THREE; }
    if (!T) throw new Error('Three.js not loaded');

    // Load add-ons after core is ready (they attach to window.THREE)
    const addons = [];
    if (sources?.orbitControls) addons.push(_loadScript(sources.orbitControls));
    if (sources?.gltfLoader)    addons.push(_loadScript(sources.gltfLoader));
    if (addons.length) await Promise.all(addons);

    return T;
}

// ── Colour helpers ────────────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

const PALETTE = ['#2563eb','#10b981','#f59e0b','#ef4444','#8b5cf6','#06b6d4','#84cc16','#ec4899','#f97316','#0ea5e9'];

// ── Raycaster helper ──────────────────────────────────────────────────────────

function _setupRaycaster(THREE, renderer, camera, scene, dotnetRef) {
    const raycaster = new THREE.Raycaster();
    const mouse     = new THREE.Vector2();
    const canvas    = renderer.domElement;

    canvas.addEventListener('click', (e) => {
        const rect = canvas.getBoundingClientRect();
        mouse.x =  ((e.clientX - rect.left) / rect.width)  * 2 - 1;
        mouse.y = -((e.clientY - rect.top)  / rect.height) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);
        const hits = raycaster.intersectObjects(scene.children, true);
        if (!hits.length) return;
        const obj = hits[0].object;
        const pt  = hits[0].point;
        const name = obj.userData?.sgName ?? obj.name ?? '';
        const data = obj.userData?.sgData ?? null;
        try {
            dotnetRef.invokeMethodAsync('OnObjectClickedAsync', { objectName: name, data, x: pt.x, y: pt.y, z: pt.z })?.catch(() => {});
        } catch {}
    });

    // Hover cursor
    canvas.addEventListener('mousemove', (e) => {
        const rect = canvas.getBoundingClientRect();
        mouse.x =  ((e.clientX - rect.left) / rect.width)  * 2 - 1;
        mouse.y = -((e.clientY - rect.top)  / rect.height) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);
        const hits = raycaster.intersectObjects(scene.children, true);
        canvas.style.cursor = hits.some(h => h.object.userData?.sgClickable) ? 'pointer' : 'default';
    });
}

// ── Base renderer / camera / controls setup ───────────────────────────────────

function _createBase(THREE, container, opts) {
    const w = container.clientWidth  || 400;
    const h = container.clientHeight || 300;

    // Renderer
    const renderer = new THREE.WebGLRenderer({ antialias: opts.antialias ?? true, alpha: false });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, opts.maxPixelRatio ?? 2));
    renderer.setSize(w, h);
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = opts.exposure ?? 1.0;
    if (opts.shadows) renderer.shadowMap.enabled = true;
    container.appendChild(renderer.domElement);

    // Camera
    let camera;
    if (opts.cameraType === 'Orthographic') {
        const aspect = w / h;
        camera = new THREE.OrthographicCamera(-aspect * 5, aspect * 5, 5, -5, 0.1, 1000);
    } else {
        camera = new THREE.PerspectiveCamera(opts.fov ?? 60, w / h, 0.1, 1000);
    }
    const cp = opts.cameraPosition ?? [5, 5, 5];
    camera.position.set(cp[0], cp[1], cp[2]);
    camera.lookAt(0, 0, 0);

    // Scene
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(opts.backgroundColor ?? '#1a1a2e');

    // Lights
    const ambient = new THREE.AmbientLight(0xffffff, opts.ambientIntensity ?? 0.4);
    scene.add(ambient);
    const dirLight = new THREE.DirectionalLight(0xffffff, opts.directionalIntensity ?? 1.0);
    dirLight.position.set(10, 20, 10);
    if (opts.shadows) { dirLight.castShadow = true; }
    scene.add(dirLight);

    // Helpers
    if (opts.showAxes) scene.add(new THREE.AxesHelper(5));
    if (opts.showGrid) scene.add(new THREE.GridHelper(20, 20, 0x444466, 0x333355));

    // Orbit controls
    let controls = null;
    if (opts.orbitControls !== false && window.THREE?.OrbitControls) {
        controls = new THREE.OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        controls.dampingFactor = 0.05;
        controls.autoRotate      = opts.autoRotate ?? false;
        controls.autoRotateSpeed = opts.autoRotateSpeed ?? 1.0;
    }

    return { renderer, camera, scene, controls };
}

// ── Scene builders ────────────────────────────────────────────────────────────

function _buildRotatingCube(THREE, scene) {
    const geo  = new THREE.BoxGeometry(2, 2, 2);
    const mat  = new THREE.MeshPhongMaterial({ color: 0x2563eb, shininess: 80 });
    const cube = new THREE.Mesh(geo, mat);
    cube.userData.sgName = 'cube';
    scene.add(cube);

    // Wireframe overlay
    const wf = new THREE.LineSegments(
        new THREE.EdgesGeometry(geo),
        new THREE.LineBasicMaterial({ color: 0x60a5fa, linewidth: 1 })
    );
    cube.add(wf);

    return (delta) => {
        cube.rotation.x += delta * 0.5;
        cube.rotation.y += delta * 0.8;
    };
}

function _buildSolarSystem(THREE, scene) {
    // Sun
    const sunGeo = new THREE.SphereGeometry(1.2, 32, 32);
    const sunMat = new THREE.MeshBasicMaterial({ color: 0xfbbf24 });
    const sun    = new THREE.Mesh(sunGeo, sunMat);
    sun.userData.sgName = 'Sun';
    scene.add(sun);

    // Point light from sun
    scene.add(new THREE.PointLight(0xfbbf24, 2, 50));

    const planets = [
        { name: 'Mercury', r: 0.25, dist: 2.5, speed: 4.1,  color: 0x9ca3af },
        { name: 'Venus',   r: 0.45, dist: 3.8, speed: 1.6,  color: 0xfde68a },
        { name: 'Earth',   r: 0.5,  dist: 5.5, speed: 1.0,  color: 0x3b82f6 },
        { name: 'Mars',    r: 0.35, dist: 7.2, speed: 0.53, color: 0xef4444 },
        { name: 'Jupiter', r: 0.9,  dist: 10,  speed: 0.08, color: 0xd97706 },
    ];

    const pivots = planets.map(p => {
        const pivot  = new THREE.Object3D();
        const geo    = new THREE.SphereGeometry(p.r, 24, 24);
        const mat    = new THREE.MeshPhongMaterial({ color: p.color });
        const mesh   = new THREE.Mesh(geo, mat);
        mesh.position.x = p.dist;
        mesh.userData.sgName = p.name;
        mesh.userData.sgClickable = true;
        pivot.add(mesh);
        scene.add(pivot);

        // Orbit ring
        const ring = new THREE.Line(
            new THREE.BufferGeometry().setFromPoints(
                Array.from({ length: 65 }, (_, i) => {
                    const a = (i / 64) * Math.PI * 2;
                    return new THREE.Vector3(Math.cos(a) * p.dist, 0, Math.sin(a) * p.dist);
                })
            ),
            new THREE.LineBasicMaterial({ color: 0x334155, transparent: true, opacity: 0.4 })
        );
        scene.add(ring);

        return { pivot, speed: p.speed };
    });

    return (delta) => {
        sun.rotation.y += delta * 0.2;
        pivots.forEach(({ pivot, speed }) => { pivot.rotation.y += delta * speed * 0.3; });
    };
}

function _buildParticleField(THREE, scene) {
    const count = 4000;
    const pos   = new Float32Array(count * 3);
    const col   = new Float32Array(count * 3);
    const colors = [new THREE.Color(0x2563eb), new THREE.Color(0x8b5cf6), new THREE.Color(0x06b6d4)];

    for (let i = 0; i < count; i++) {
        pos[i * 3]     = (Math.random() - 0.5) * 40;
        pos[i * 3 + 1] = (Math.random() - 0.5) * 40;
        pos[i * 3 + 2] = (Math.random() - 0.5) * 40;
        const c = colors[Math.floor(Math.random() * colors.length)];
        col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b;
    }

    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    geo.setAttribute('color',    new THREE.BufferAttribute(col, 3));
    const mat = new THREE.PointsMaterial({ size: 0.12, vertexColors: true, transparent: true, opacity: 0.85 });
    const pts = new THREE.Points(geo, mat);
    scene.add(pts);

    return (delta) => {
        pts.rotation.y += delta * 0.05;
        pts.rotation.x += delta * 0.02;
    };
}

function _buildWaveSurface(THREE, scene) {
    const W = 40, H = 40, SEG = 80;
    const geo = new THREE.PlaneGeometry(W, H, SEG, SEG);
    geo.rotateX(-Math.PI / 2);
    const mat = new THREE.MeshPhongMaterial({
        color: 0x0ea5e9, wireframe: false,
        transparent: true, opacity: 0.85,
        side: THREE.DoubleSide, shininess: 120,
    });
    const mesh = new THREE.Mesh(geo, mat);
    scene.add(mesh);

    // Wireframe overlay
    const wfMat = new THREE.MeshBasicMaterial({ color: 0x38bdf8, wireframe: true, transparent: true, opacity: 0.15 });
    scene.add(new THREE.Mesh(geo, wfMat));

    let t = 0;
    const pos = geo.attributes.position;
    const origY = new Float32Array(pos.count);
    for (let i = 0; i < pos.count; i++) origY[i] = pos.getY(i);

    return (delta) => {
        t += delta;
        for (let i = 0; i < pos.count; i++) {
            const x = pos.getX(i), z = pos.getZ(i);
            pos.setY(i, origY[i] + Math.sin(x * 0.4 + t * 1.5) * 0.6 + Math.cos(z * 0.3 + t) * 0.4);
        }
        pos.needsUpdate = true;
        geo.computeVertexNormals();
    };
}

function _buildBarChart3D(THREE, scene, data) {
    if (!data || !data.length) return () => {};
    const maxVal = Math.max(...data.map(d => d.value));
    const groups = [...new Set(data.map(d => d.group || 'default'))];
    const labels = [...new Set(data.map(d => d.label))];
    const barW = 0.7, gap = 0.3, groupGap = 0.5;
    const groupW = groups.length * (barW + gap) + groupGap;

    labels.forEach((lbl, li) => {
        groups.forEach((grp, gi) => {
            const item = data.find(d => d.label === lbl && (d.group || 'default') === grp);
            if (!item) return;
            const h = Math.max(0.05, (item.value / maxVal) * 5);
            const geo = new THREE.BoxGeometry(barW, h, barW);
            const col = new THREE.Color(PALETTE[gi % PALETTE.length]);
            const mat = new THREE.MeshPhongMaterial({ color: col, shininess: 60 });
            const mesh = new THREE.Mesh(geo, mat);
            mesh.position.set(
                li * groupW + gi * (barW + gap) - (labels.length * groupW) / 2,
                h / 2,
                0
            );
            mesh.userData.sgName = `${lbl} / ${grp}`;
            mesh.userData.sgData = JSON.stringify({ label: lbl, group: grp, value: item.value });
            mesh.userData.sgClickable = true;
            scene.add(mesh);
        });
    });

    // Floor
    const floor = new THREE.Mesh(
        new THREE.PlaneGeometry(labels.length * groupW + 2, 8),
        new THREE.MeshPhongMaterial({ color: 0x1e293b })
    );
    floor.rotation.x = -Math.PI / 2;
    scene.add(floor);

    return () => {};
}

// ── Cell texture helper ───────────────────────────────────────────────────────

function _makeCellTexture(THREE, status, cell) {
    const W = 128, H = 64;
    const cv  = document.createElement('canvas');
    cv.width  = W; cv.height = H;
    const ctx = cv.getContext('2d');

    // Background — transparent (the mesh colour shows through)
    ctx.clearRect(0, 0, W, H);

    if (status === 'Occupied') {
        // Quantity badge — bottom-right corner
        const qty = cell.quantity;
        if (qty != null) {
            const text  = String(qty);
            const badgeW = Math.min(W - 4, text.length * 14 + 16);
            const badgeH = 22;
            const bx = W - badgeW - 3;
            const by = H - badgeH - 3;

            // Badge background
            ctx.fillStyle = 'rgba(0,0,0,0.72)';
            ctx.beginPath();
            ctx.roundRect(bx, by, badgeW, badgeH, 4);
            ctx.fill();

            // Quantity text
            ctx.fillStyle = '#ffffff';
            ctx.font = `bold ${text.length > 3 ? 11 : 13}px sans-serif`;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(text, bx + badgeW / 2, by + badgeH / 2);
        }

        // SKU label — top strip
        if (cell.label) {
            const sku = cell.label.length > 12 ? cell.label.slice(0, 12) + '…' : cell.label;
            ctx.fillStyle = 'rgba(0,0,0,0.55)';
            ctx.fillRect(0, 0, W, 18);
            ctx.fillStyle = '#a5f3fc';
            ctx.font = '9px monospace';
            ctx.textAlign = 'left';
            ctx.textBaseline = 'middle';
            ctx.fillText(sku, 4, 9);
        }
    } else if (status === 'Reserved') {
        // Diagonal stripes
        ctx.strokeStyle = 'rgba(251,191,36,0.25)';
        ctx.lineWidth = 3;
        for (let x = -H; x < W + H; x += 14) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x + H, H);
            ctx.stroke();
        }
    } else if (status === 'Blocked') {
        // X mark
        ctx.strokeStyle = 'rgba(239,68,68,0.4)';
        ctx.lineWidth = 4;
        ctx.lineCap = 'round';
        ctx.beginPath(); ctx.moveTo(12, 12); ctx.lineTo(W - 12, H - 12); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(W - 12, 12); ctx.lineTo(12, H - 12); ctx.stroke();
    }

    return new THREE.CanvasTexture(cv);
}

function _makeCellMaterials(THREE, status, cell) {
    const color = CELL_COLORS[status]  ?? CELL_COLORS.Empty;
    const emiss = CELL_EMISSIVE[status] ?? CELL_EMISSIVE.Empty;
    const tex   = _makeCellTexture(THREE, status, cell);

    // 6 faces: right, left, top, bottom, front(+Z), back(-Z)
    // We put the texture only on the front face (index 4)
    return [
        new THREE.MeshPhongMaterial({ color, emissive: emiss, shininess: 50, transparent: status === 'Empty', opacity: status === 'Empty' ? 0.55 : 1.0 }), // right
        new THREE.MeshPhongMaterial({ color, emissive: emiss, shininess: 50, transparent: status === 'Empty', opacity: status === 'Empty' ? 0.55 : 1.0 }), // left
        new THREE.MeshPhongMaterial({ color, emissive: emiss, shininess: 50, transparent: status === 'Empty', opacity: status === 'Empty' ? 0.55 : 1.0 }), // top
        new THREE.MeshPhongMaterial({ color, emissive: emiss, shininess: 50, transparent: status === 'Empty', opacity: status === 'Empty' ? 0.55 : 1.0 }), // bottom
        new THREE.MeshPhongMaterial({ color, emissive: emiss, shininess: 50, map: tex, transparent: status === 'Empty', opacity: status === 'Empty' ? 0.55 : 1.0 }), // front
        new THREE.MeshPhongMaterial({ color, emissive: emiss, shininess: 50, transparent: status === 'Empty', opacity: status === 'Empty' ? 0.55 : 1.0 }), // back
    ];
}

const CELL_COLORS = {
    Empty:    0x1a3a5c,
    Occupied: 0x15803d,
    Reserved: 0xb45309,
    Blocked:  0xb91c1c,
};
const CELL_EMISSIVE = {
    Empty:    0x0a1a2e,
    Occupied: 0x052e16,
    Reserved: 0x431407,
    Blocked:  0x450a0a,
};
const CELL_HOVER_COLOR    = 0xfbbf24;
const CELL_HOVER_EMISSIVE = 0x78350f;

// Rack geometry constants
const CELL_W = 0.9, CELL_D = 0.75, CELL_H = 0.55;
const CELL_GAP_X = 0.08, CELL_GAP_Y = 0.12;
const RACK_SPACING = 3.2;   // gap between rack rows
const POST_R = 0.04, POST_H_EXTRA = 0.3;
const BEAM_H = 0.06, BEAM_D = 0.06;

function _buildWarehouse(THREE, scene, layout, dotnetRef) {
    const rows   = layout.rows    ?? ['A','B','C','D'];
    const cols   = layout.columns ?? 10;
    const levels = layout.levels  ?? 3;
    const cells  = layout.cells   ?? [];

    const cellMap = {};
    cells.forEach(c => { cellMap[c.id] = c; });

    // Materials
    const postMat  = new THREE.MeshPhongMaterial({ color: 0x475569, shininess: 60 });
    const beamMat  = new THREE.MeshPhongMaterial({ color: 0xf97316, shininess: 80 });
    const floorMat = new THREE.MeshPhongMaterial({ color: 0x0f172a, shininess: 10 });
    const stripeMat= new THREE.MeshPhongMaterial({ color: 0xfbbf24, shininess: 5 });

    const rackW = cols * (CELL_W + CELL_GAP_X) - CELL_GAP_X;
    const rackH = levels * (CELL_H + CELL_GAP_Y) - CELL_GAP_Y + POST_H_EXTRA;
    const totalW = rows.length * (rackW + RACK_SPACING) - RACK_SPACING;

    // ── Centre offset so the whole warehouse is centred at (0,0,0) ──
    const offsetX = -totalW / 2;

    rows.forEach((row, ri) => {
        const rackX = offsetX + ri * (rackW + RACK_SPACING);

        // ── Vertical posts ──
        const postGeo = new THREE.CylinderGeometry(POST_R, POST_R, rackH, 8);
        const postPositionsX = [0];
        for (let c = 2; c <= cols; c += 2) postPositionsX.push(c * (CELL_W + CELL_GAP_X) - CELL_GAP_X / 2);
        postPositionsX.push(rackW);

        postPositionsX.forEach(px => {
            [-CELL_D / 2, CELL_D / 2].forEach(pz => {
                const post = new THREE.Mesh(postGeo, postMat);
                post.position.set(rackX + px, rackH / 2, pz);
                scene.add(post);
            });
        });

        // ── Horizontal beams per level ──
        for (let lv = 0; lv <= levels; lv++) {
            const beamY = lv * (CELL_H + CELL_GAP_Y) - CELL_GAP_Y / 2 + CELL_H / 2;
            const beamGeo = new THREE.BoxGeometry(rackW, BEAM_H, BEAM_D);
            [-CELL_D / 2, CELL_D / 2].forEach(bz => {
                const beam = new THREE.Mesh(beamGeo, beamMat);
                beam.position.set(rackX + rackW / 2, beamY, bz);
                scene.add(beam);
            });
        }

        // ── Cells ──
        for (let col = 1; col <= cols; col++) {
            for (let lv = 1; lv <= levels; lv++) {
                const id     = `${row}-${String(col).padStart(2,'0')}-${lv}`;
                const cell   = cellMap[id] ?? { id, row, column: col, level: lv, status: 'Empty' };
                const status = cell.status ?? 'Empty';
                const color  = CELL_COLORS[status]   ?? CELL_COLORS.Empty;
                const emiss  = CELL_EMISSIVE[status]  ?? CELL_EMISSIVE.Empty;

                const cx = rackX + (col - 1) * (CELL_W + CELL_GAP_X) + CELL_W / 2;
                const cy = (lv - 1) * (CELL_H + CELL_GAP_Y) + CELL_H / 2 + CELL_GAP_Y / 2;

                const geo  = new THREE.BoxGeometry(CELL_W, CELL_H, CELL_D);
                const mats = _makeCellMaterials(THREE, status, cell);
                const mesh = new THREE.Mesh(geo, mats);
                mesh.position.set(cx, cy, 0);

                mesh.userData.sgName      = id;
                mesh.userData.sgData      = JSON.stringify(cell);
                mesh.userData.sgClickable = true;
                mesh.userData._origColor  = CELL_COLORS[status]  ?? CELL_COLORS.Empty;
                mesh.userData._origEmiss  = CELL_EMISSIVE[status] ?? CELL_EMISSIVE.Empty;
                mesh.userData._status     = status;
                mesh.userData._THREE      = THREE;
                scene.add(mesh);

                if (status !== 'Empty') {
                    const edges = new THREE.LineSegments(
                        new THREE.EdgesGeometry(geo),
                        new THREE.LineBasicMaterial({ color: 0x64748b, transparent: true, opacity: 0.4 })
                    );
                    mesh.add(edges);
                }
            }
        }

        // ── Row label sprite ──
        const canvas = document.createElement('canvas');
        canvas.width = 160; canvas.height = 80;
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, 160, 80);
        ctx.fillStyle = 'rgba(15,23,42,0.75)';
        ctx.beginPath(); ctx.roundRect(4, 4, 152, 72, 8); ctx.fill();
        ctx.fillStyle = '#f1f5f9';
        ctx.font = 'bold 48px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(row, 80, 42);
        const tex = new THREE.CanvasTexture(canvas);
        const spr = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, transparent: true }));
        spr.scale.set(1.6, 0.8, 1);
        spr.position.set(rackX + rackW / 2, rackH + 0.6, 0);
        scene.add(spr);
    });

    // ── Floor ──
    const floorDepth = CELL_D * 2 + 4;
    const floor = new THREE.Mesh(
        new THREE.PlaneGeometry(totalW + 6, floorDepth),
        floorMat
    );
    floor.rotation.x = -Math.PI / 2;
    floor.position.set(0, -0.01, 0);
    scene.add(floor);

    // Safety stripes between racks
    rows.forEach((_, ri) => {
        if (ri === rows.length - 1) return;
        const sx = offsetX + ri * (rackW + RACK_SPACING) + rackW + RACK_SPACING / 2;
        const stripe = new THREE.Mesh(
            new THREE.PlaneGeometry(0.25, floorDepth),
            stripeMat
        );
        stripe.rotation.x = -Math.PI / 2;
        stripe.position.set(sx, 0.001, 0);
        scene.add(stripe);
    });

    // ── Ceiling lights ──
    const ceilY = rackH + 1.5;
    rows.forEach((_, ri) => {
        const lx = offsetX + ri * (rackW + RACK_SPACING) + rackW / 2;
        const light = new THREE.PointLight(0xfff8e7, 0.8, rackW * 2.5);
        light.position.set(lx, ceilY, 0);
        scene.add(light);
        const fixtureGeo = new THREE.BoxGeometry(rackW * 0.6, 0.08, 0.2);
        const fixtureMat = new THREE.MeshBasicMaterial({ color: 0xfef9c3 });
        const fixture = new THREE.Mesh(fixtureGeo, fixtureMat);
        fixture.position.set(lx, ceilY - 0.05, 0);
        scene.add(fixture);
    });

    // Return scene centre so initThree can aim the camera at it
    return { animateFn: () => {}, centerX: 0, centerY: rackH / 2, centerZ: 0 };
}

function _setupRaycasterWithHover(THREE, renderer, camera, scene, dotnetRef) {
    const raycaster = new THREE.Raycaster();
    const mouse     = new THREE.Vector2();
    const canvas    = renderer.domElement;
    let   hovered   = null;

    canvas.addEventListener('click', (e) => {
        const rect = canvas.getBoundingClientRect();
        mouse.x =  ((e.clientX - rect.left) / rect.width)  * 2 - 1;
        mouse.y = -((e.clientY - rect.top)  / rect.height) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);
        const hits = raycaster.intersectObjects(scene.children, true);
        const hit  = hits.find(h => h.object.userData?.sgClickable);
        if (!hit) return;
        const obj = hit.object;
        const pt  = hit.point;
        try {
            dotnetRef.invokeMethodAsync('OnObjectClickedAsync', {
                objectName: obj.userData.sgName ?? '',
                data:       obj.userData.sgData ?? null,
                x: pt.x, y: pt.y, z: pt.z,
            })?.catch(() => {});
        } catch {}
    });

    canvas.addEventListener('mousemove', (e) => {
        const rect = canvas.getBoundingClientRect();
        mouse.x =  ((e.clientX - rect.left) / rect.width)  * 2 - 1;
        mouse.y = -((e.clientY - rect.top)  / rect.height) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);
        const hits = raycaster.intersectObjects(scene.children, true);
        const hit  = hits.find(h => h.object.userData?.sgClickable);
        const mesh = hit ? hit.object : null;

        if (hovered && hovered !== mesh) {
            // Restore all faces
            const mats = Array.isArray(hovered.material) ? hovered.material : [hovered.material];
            mats.forEach(m => {
                m.color.setHex(hovered.userData._origColor ?? 0x1a3a5c);
                m.emissive.setHex(hovered.userData._origEmiss ?? 0x0a1a2e);
            });
            hovered = null;
        }
        if (mesh && mesh !== hovered) {
            hovered = mesh;
            const mats = Array.isArray(mesh.material) ? mesh.material : [mesh.material];
            mats.forEach(m => {
                m.color.setHex(CELL_HOVER_COLOR);
                m.emissive.setHex(CELL_HOVER_EMISSIVE);
            });
        }
        canvas.style.cursor = mesh ? 'pointer' : 'default';
    });

    canvas.addEventListener('mouseleave', () => {
        if (hovered) {
            const mats = Array.isArray(hovered.material) ? hovered.material : [hovered.material];
            mats.forEach(m => {
                m.color.setHex(hovered.userData._origColor ?? 0x1a3a5c);
                m.emissive.setHex(hovered.userData._origEmiss ?? 0x0a1a2e);
            });
            hovered = null;
        }
    });
}

function _buildGlobe(THREE, scene) {
    // Globe sphere
    const geo  = new THREE.SphereGeometry(3, 48, 48);
    const mat  = new THREE.MeshPhongMaterial({ color: 0x1e40af, wireframe: false, shininess: 30, transparent: true, opacity: 0.85 });
    const globe = new THREE.Mesh(geo, mat);
    scene.add(globe);

    // Wireframe overlay
    const wfMat = new THREE.MeshBasicMaterial({ color: 0x3b82f6, wireframe: true, transparent: true, opacity: 0.2 });
    scene.add(new THREE.Mesh(geo, wfMat));

    // Animated arcs (great-circle segments)
    const arcData = [
        { from: [55.75, 37.62], to: [40.71, -74.01] },  // Moscow → New York
        { from: [51.51, -0.13], to: [35.68, 139.69] },  // London → Tokyo
        { from: [48.85, 2.35],  to: [-33.87, 151.21] }, // Paris → Sydney
        { from: [1.35, 103.82], to: [19.43, -99.13] },  // Singapore → Mexico City
    ];

    const arcMeshes = arcData.map(({ from, to }) => {
        const pts = [];
        for (let t = 0; t <= 1; t += 0.02) {
            const lat = from[0] + (to[0] - from[0]) * t;
            const lon = from[1] + (to[1] - from[1]) * t;
            const phi   = (90 - lat) * (Math.PI / 180);
            const theta = (lon + 180) * (Math.PI / 180);
            const r = 3.05 + Math.sin(t * Math.PI) * 0.5;
            pts.push(new THREE.Vector3(
                -r * Math.sin(phi) * Math.cos(theta),
                r * Math.cos(phi),
                r * Math.sin(phi) * Math.sin(theta)
            ));
        }
        const line = new THREE.Line(
            new THREE.BufferGeometry().setFromPoints(pts),
            new THREE.LineBasicMaterial({ color: 0x34d399, transparent: true, opacity: 0.7 })
        );
        return line;
    });
    arcMeshes.forEach(a => scene.add(a));

    let t = 0;
    return (delta) => {
        t += delta;
        globe.rotation.y += delta * 0.1;
        arcMeshes.forEach((arc, i) => {
            arc.material.opacity = 0.4 + 0.4 * Math.sin(t * 1.5 + i * 1.2);
        });
    };
}

// ── Main dispatcher ───────────────────────────────────────────────────────────

function _buildFactory(THREE, scene) {
    // Floor
    const floorMat = new THREE.MeshPhongMaterial({ color: 0x1e293b, shininess: 5 });
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(40, 20), floorMat);
    floor.rotation.x = -Math.PI / 2;
    scene.add(floor);

    // Floor grid lines
    scene.add(new THREE.GridHelper(40, 20, 0x334155, 0x1e293b));

    // Machine colors
    const machineMat  = (c) => new THREE.MeshPhongMaterial({ color: c, shininess: 80 });
    const panelMat    = new THREE.MeshPhongMaterial({ color: 0x0f172a, shininess: 20 });
    const screenMat   = new THREE.MeshBasicMaterial({ color: 0x22d3ee });
    const warningMat  = new THREE.MeshBasicMaterial({ color: 0xfbbf24 });
    const dangerMat   = new THREE.MeshBasicMaterial({ color: 0xef4444 });
    const okMat       = new THREE.MeshBasicMaterial({ color: 0x22c55e });

    const machines = [
        { id: 'CNC-01',   x: -14, z: -5, color: 0x1d4ed8, status: 'ok',      label: 'CNC-01\nАктивен' },
        { id: 'CNC-02',   x: -14, z:  5, color: 0x1d4ed8, status: 'ok',      label: 'CNC-02\nАктивен' },
        { id: 'PRESS-01', x:  -5, z: -5, color: 0x7c3aed, status: 'warning', label: 'PRESS-01\nТО' },
        { id: 'PRESS-02', x:  -5, z:  5, color: 0x7c3aed, status: 'ok',      label: 'PRESS-02\nАктивен' },
        { id: 'WELD-01',  x:   5, z: -5, color: 0xb45309, status: 'error',   label: 'WELD-01\nОшибка' },
        { id: 'WELD-02',  x:   5, z:  5, color: 0xb45309, status: 'ok',      label: 'WELD-02\nАктивен' },
        { id: 'ROBOT-01', x:  14, z:  0, color: 0x0f766e, status: 'ok',      label: 'ROBOT-01\nАктивен' },
    ];

    const machineGroups = [];

    machines.forEach(m => {
        const g = new THREE.Group();
        g.position.set(m.x, 0, m.z);

        // Body
        const body = new THREE.Mesh(new THREE.BoxGeometry(2.2, 2.0, 1.8), machineMat(m.color));
        body.position.y = 1.0;
        g.add(body);

        // Control panel
        const panel = new THREE.Mesh(new THREE.BoxGeometry(2.0, 0.8, 0.1), panelMat);
        panel.position.set(0, 1.8, 0.95);
        g.add(panel);

        // Screen
        const screen = new THREE.Mesh(new THREE.PlaneGeometry(1.2, 0.5), screenMat);
        screen.position.set(0, 1.9, 1.01);
        g.add(screen);

        // Status light
        const sLight = new THREE.Mesh(
            new THREE.SphereGeometry(0.12, 8, 8),
            m.status === 'ok' ? okMat : m.status === 'warning' ? warningMat : dangerMat
        );
        sLight.position.set(0.7, 2.1, 1.01);
        g.add(sLight);

        // Point light from status indicator
        const ptLight = new THREE.PointLight(
            m.status === 'ok' ? 0x22c55e : m.status === 'warning' ? 0xfbbf24 : 0xef4444,
            0.4, 3
        );
        ptLight.position.set(m.x + 0.7, 2.5, m.z + 1);
        scene.add(ptLight);

        // Legs
        [[-0.8, -0.8], [0.8, -0.8], [-0.8, 0.8], [0.8, 0.8]].forEach(([lx, lz]) => {
            const leg = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.06, 0.3, 6), machineMat(0x475569));
            leg.position.set(lx, 0.15, lz);
            g.add(leg);
        });

        // Label sprite
        const cv = document.createElement('canvas'); cv.width = 256; cv.height = 128;
        const ctx = cv.getContext('2d');
        ctx.fillStyle = 'rgba(15,23,42,0.85)';
        ctx.beginPath(); ctx.roundRect(4, 4, 248, 120, 8); ctx.fill();
        ctx.fillStyle = '#f1f5f9'; ctx.font = 'bold 28px sans-serif';
        ctx.textAlign = 'center';
        const lines = m.label.split('\n');
        lines.forEach((l, i) => ctx.fillText(l, 128, 44 + i * 36));
        const spr = new THREE.Sprite(new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(cv), transparent: true }));
        spr.scale.set(2.5, 1.25, 1);
        spr.position.set(0, 3.2, 0);
        g.add(spr);

        g.userData.sgName      = m.id;
        g.userData.sgData      = JSON.stringify({ id: m.id, status: m.status });
        g.userData.sgClickable = true;
        scene.add(g);
        machineGroups.push({ g, status: m.status });
    });

    // Conveyor belt (series of rollers)
    const conveyorY = 0.55;
    const beltMat = new THREE.MeshPhongMaterial({ color: 0x374151, shininess: 30 });
    const rollerMat = new THREE.MeshPhongMaterial({ color: 0x6b7280, shininess: 60 });
    const frameMat  = new THREE.MeshPhongMaterial({ color: 0xf97316, shininess: 40 });

    // Belt frame
    const beltFrame = new THREE.Mesh(new THREE.BoxGeometry(28, 0.15, 0.8), frameMat);
    beltFrame.position.set(0, conveyorY - 0.1, 0);
    scene.add(beltFrame);

    // Belt surface
    const belt = new THREE.Mesh(new THREE.BoxGeometry(27.5, 0.08, 0.6), beltMat);
    belt.position.set(0, conveyorY, 0);
    scene.add(belt);

    // Rollers
    const rollers = [];
    for (let rx = -13; rx <= 13; rx += 1.2) {
        const roller = new THREE.Mesh(new THREE.CylinderGeometry(0.12, 0.12, 0.7, 10), rollerMat);
        roller.rotation.z = Math.PI / 2;
        roller.position.set(rx, conveyorY + 0.04, 0);
        scene.add(roller);
        rollers.push(roller);
    }

    // Boxes on conveyor
    const boxColors = [0x2563eb, 0x16a34a, 0xd97706, 0x7c3aed];
    const boxes = [];
    for (let b = 0; b < 5; b++) {
        const box = new THREE.Mesh(
            new THREE.BoxGeometry(0.5, 0.5, 0.5),
            new THREE.MeshPhongMaterial({ color: boxColors[b % boxColors.length], shininess: 40 })
        );
        box.position.set(-12 + b * 5.5, conveyorY + 0.3, 0);
        scene.add(box);
        boxes.push({ mesh: box, offset: b * 5.5 });
    }

    // Ceiling lights
    for (let lx = -12; lx <= 12; lx += 8) {
        const cl = new THREE.PointLight(0xfff8e7, 0.6, 12);
        cl.position.set(lx, 6, 0);
        scene.add(cl);
        const fix = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.1, 0.3), new THREE.MeshBasicMaterial({ color: 0xfef9c3 }));
        fix.position.set(lx, 5.95, 0);
        scene.add(fix);
    }

    let t = 0;
    return (delta) => {
        t += delta;
        // Animate rollers
        rollers.forEach(r => { r.rotation.y += delta * 3; });
        // Move boxes
        boxes.forEach(b => {
            b.mesh.position.x += delta * 1.5;
            if (b.mesh.position.x > 14) b.mesh.position.x = -14;
        });
        // Pulse warning/error lights
        machineGroups.forEach(({ g, status }) => {
            if (status !== 'ok') {
                const sLight = g.children.find(c => c.geometry?.type === 'SphereGeometry');
                if (sLight) sLight.material.opacity = 0.5 + 0.5 * Math.sin(t * 4);
            }
        });
    };
}

function _buildPipeline(THREE, scene) {
    const pipeMat   = (c) => new THREE.MeshPhongMaterial({ color: c, shininess: 90, metalness: 0.8 });
    const valveMat  = new THREE.MeshPhongMaterial({ color: 0xef4444, shininess: 60 });
    const sensorMat = new THREE.MeshBasicMaterial({ color: 0x22d3ee });
    const flowMat   = new THREE.MeshBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.6 });
    const floorMat  = new THREE.MeshPhongMaterial({ color: 0x0f172a });

    // Floor
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(30, 14), floorMat);
    floor.rotation.x = -Math.PI / 2;
    scene.add(floor);

    const R = 0.18; // pipe radius

    // Helper: horizontal pipe segment
    function hPipe(x, y, z, len, color = 0x64748b) {
        const m = new THREE.Mesh(new THREE.CylinderGeometry(R, R, len, 12), pipeMat(color));
        m.rotation.z = Math.PI / 2;
        m.position.set(x, y, z);
        scene.add(m);
    }
    function vPipe(x, y, z, len, color = 0x64748b) {
        const m = new THREE.Mesh(new THREE.CylinderGeometry(R, R, len, 12), pipeMat(color));
        m.position.set(x, y, z);
        scene.add(m);
    }
    function elbow(x, y, z) {
        const m = new THREE.Mesh(new THREE.SphereGeometry(R * 1.1, 12, 12), pipeMat(0x475569));
        m.position.set(x, y, z);
        scene.add(m);
    }
    function valve(x, y, z, open = true) {
        const body = new THREE.Mesh(new THREE.CylinderGeometry(R * 1.8, R * 1.8, 0.3, 8), valveMat);
        body.rotation.z = Math.PI / 2;
        body.position.set(x, y, z);
        scene.add(body);
        const handle = new THREE.Mesh(new THREE.BoxGeometry(0.08, 0.5, 0.08), pipeMat(0x374151));
        handle.position.set(x, y + 0.4, z);
        handle.rotation.z = open ? 0 : Math.PI / 2;
        scene.add(handle);
    }
    function tank(x, y, z, h, color, label) {
        const body = new THREE.Mesh(new THREE.CylinderGeometry(0.9, 0.9, h, 16), pipeMat(color));
        body.position.set(x, y + h / 2, z);
        scene.add(body);
        const top = new THREE.Mesh(new THREE.SphereGeometry(0.9, 16, 8, 0, Math.PI * 2, 0, Math.PI / 2), pipeMat(color));
        top.position.set(x, y + h, z);
        scene.add(top);
        // Level indicator
        const lvl = new THREE.Mesh(new THREE.CylinderGeometry(0.85, 0.85, h * 0.6, 16), new THREE.MeshPhongMaterial({ color: 0x0ea5e9, transparent: true, opacity: 0.4 }));
        lvl.position.set(x, y + h * 0.3, z);
        scene.add(lvl);
        // Label
        const cv = document.createElement('canvas'); cv.width = 200; cv.height = 80;
        const ctx = cv.getContext('2d');
        ctx.fillStyle = 'rgba(15,23,42,0.8)'; ctx.beginPath(); ctx.roundRect(2,2,196,76,6); ctx.fill();
        ctx.fillStyle = '#f1f5f9'; ctx.font = 'bold 26px sans-serif'; ctx.textAlign = 'center';
        ctx.fillText(label, 100, 48);
        const spr = new THREE.Sprite(new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(cv), transparent: true }));
        spr.scale.set(2, 0.8, 1);
        spr.position.set(x, y + h + 1.2, z);
        scene.add(spr);
        return body;
    }
    function sensor(x, y, z) {
        const s = new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.2, 0.2), sensorMat);
        s.position.set(x, y, z);
        scene.add(s);
    }

    // Tanks
    tank(-10, 0, 0, 3.5, 0x1d4ed8, 'Резервуар A');
    tank( 10, 0, 0, 3.0, 0x15803d, 'Резервуар B');
    tank(  0, 0, 4, 2.5, 0x7c3aed, 'Смеситель');

    // Main pipeline: A → mixer
    hPipe(-8.5, 1.5, 0, 3);
    elbow(-7, 1.5, 0);
    hPipe(-5.5, 1.5, 0, 3, 0x2563eb);
    valve(-4, 1.5, 0, true);
    hPipe(-2.5, 1.5, 0, 3, 0x2563eb);
    elbow(-1, 1.5, 0);
    vPipe(-1, 2.5, 0, 2, 0x2563eb);
    elbow(-1, 3.5, 0);
    hPipe(-0.5, 3.5, 2, 3, 0x2563eb);
    elbow(1, 3.5, 4);
    vPipe(1, 2.5, 4, 2);

    // Pipeline: B → mixer
    hPipe(8.5, 1.5, 0, 3);
    elbow(7, 1.5, 0);
    hPipe(5.5, 1.5, 0, 3, 0x16a34a);
    valve(4, 1.5, 0, false);
    hPipe(2.5, 1.5, 0, 3, 0x16a34a);
    elbow(1, 1.5, 0);
    vPipe(1, 2.5, 0, 2, 0x16a34a);
    elbow(1, 3.5, 0);
    hPipe(1, 3.5, 2, 3, 0x16a34a);

    // Output from mixer
    vPipe(0, 0.5, 4, 1);
    elbow(0, 0, 4);
    hPipe(3, 0.5, 4, 6, 0x7c3aed);
    valve(6, 0.5, 4, true);
    hPipe(8, 0.5, 4, 4, 0x7c3aed);

    // Sensors
    sensor(-4, 1.9, 0); sensor(4, 1.9, 0); sensor(6, 0.9, 4);

    // Ceiling lights
    [[-8,0],[0,0],[8,0],[0,4]].forEach(([lx,lz]) => {
        const cl = new THREE.PointLight(0xfff8e7, 0.7, 10);
        cl.position.set(lx, 6, lz);
        scene.add(cl);
    });

    // Animated flow particles
    const flowParticles = [];
    const flowPaths = [
        { pts: [[-8.5,1.5,0],[-1,1.5,0],[-1,3.5,0],[1,3.5,4],[1,2.5,4]], color: 0x38bdf8 },
        { pts: [[8.5,1.5,0],[1,1.5,0],[1,3.5,0],[1,3.5,4]],               color: 0x4ade80 },
        { pts: [[0,0,4],[8,0.5,4]],                                         color: 0xa78bfa },
    ];
    flowPaths.forEach(({ pts, color }) => {
        for (let i = 0; i < 6; i++) {
            const p = new THREE.Mesh(new THREE.SphereGeometry(0.07, 6, 6), new THREE.MeshBasicMaterial({ color }));
            scene.add(p);
            flowParticles.push({ mesh: p, pts, t: i / 6 });
        }
    });

    return (delta) => {
        flowParticles.forEach(fp => {
            fp.t = (fp.t + delta * 0.3) % 1;
            const pts = fp.pts;
            const totalSeg = pts.length - 1;
            const pos = fp.t * totalSeg;
            const seg = Math.min(Math.floor(pos), totalSeg - 1);
            const frac = pos - seg;
            const a = pts[seg], b = pts[seg + 1];
            fp.mesh.position.set(
                a[0] + (b[0] - a[0]) * frac,
                a[1] + (b[1] - a[1]) * frac,
                a[2] + (b[2] - a[2]) * frac
            );
        });
    };
}

function _buildScene(THREE, scene, sceneType, warehouseLayout, barData) {
    switch (sceneType) {
        case 'Warehouse':     return () => {};  // handled separately
        case 'Factory':       return _buildFactory(THREE, scene);
        case 'Pipeline':      return _buildPipeline(THREE, scene);
        case 'BarChart3D':    return _buildBarChart3D(THREE, scene, barData);
        case 'RotatingCube':  return _buildRotatingCube(THREE, scene);
        case 'ParticleField': return _buildParticleField(THREE, scene);
        default:              return () => {};
    }
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initThree(dotnetRef, containerRef, instanceId, sceneType, opts, warehouseLayout, barData, sources) {
    await disposeThree(instanceId);

    const THREE = await _ensureThree(sources);
    const { renderer, camera, scene, controls } = _createBase(THREE, containerRef, opts ?? {});

    let animateFn;
    let sceneCenter = new THREE.Vector3(0, 0, 0);

    if (sceneType === 'Warehouse') {
        const result = _buildWarehouse(THREE, scene, warehouseLayout ?? {}, dotnetRef);
        animateFn   = result.animateFn;
        sceneCenter = new THREE.Vector3(result.centerX, result.centerY, result.centerZ);

        // Aim camera and orbit target at warehouse centre
        const rows  = (warehouseLayout?.rows  ?? ['A','B','C','D']).length;
        const cols  = warehouseLayout?.columns ?? 10;
        const lvls  = warehouseLayout?.levels  ?? 3;
        const rackW = cols * (CELL_W + CELL_GAP_X) - CELL_GAP_X;
        const rackH = lvls * (CELL_H + CELL_GAP_Y) - CELL_GAP_Y + POST_H_EXTRA;
        const totalW = rows * (rackW + RACK_SPACING) - RACK_SPACING;
        const dist   = Math.max(totalW, rackH * 3) * 0.9;

        camera.position.set(0, rackH * 1.8, dist * 0.7);
        camera.lookAt(0, rackH / 2, 0);

        if (controls) {
            controls.target.set(0, rackH / 2, 0);
            controls.update();
        }
    } else {
        animateFn = _buildScene(THREE, scene, sceneType, warehouseLayout, barData);
    }

    _setupRaycasterWithHover(THREE, renderer, camera, scene, dotnetRef);

    let lastTime = performance.now();
    let rafId    = 0;

    function animate() {
        rafId = requestAnimationFrame(animate);
        const now   = performance.now();
        const delta = Math.min((now - lastTime) / 1000, 0.1);
        lastTime = now;
        if (animateFn) animateFn(delta);
        if (controls) controls.update();
        renderer.render(scene, camera);
    }
    animate();

    // Resize observer
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf2 = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf2);
            raf2 = requestAnimationFrame(() => {
                const w = containerRef.clientWidth  || 400;
                const h = containerRef.clientHeight || 300;
                renderer.setSize(w, h);
                if (camera.isPerspectiveCamera) { camera.aspect = w / h; camera.updateProjectionMatrix(); }
            });
        });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { renderer, camera, scene, controls, rafId, ro, THREE });
}

export function updateWarehouseCells(instanceId, cells) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const { scene, THREE } = inst;

    cells.forEach(cell => {
        const mesh = scene.children.find(c => c.userData?.sgName === cell.id && c.isMesh);
        if (!mesh) return;

        const status = cell.status ?? 'Empty';
        const color  = CELL_COLORS[status]  ?? CELL_COLORS.Empty;
        const emiss  = CELL_EMISSIVE[status] ?? CELL_EMISSIVE.Empty;

        // Dispose old materials
        const oldMats = Array.isArray(mesh.material) ? mesh.material : [mesh.material];
        oldMats.forEach(m => { if (m.map) m.map.dispose(); m.dispose(); });

        // Create new materials with updated texture
        mesh.material = _makeCellMaterials(THREE, status, cell);

        // Update metadata
        mesh.userData._origColor = color;
        mesh.userData._origEmiss = emiss;
        mesh.userData._status    = status;
        mesh.userData.sgData     = JSON.stringify(cell);

        // Update edge lines visibility
        const edges = mesh.children.find(c => c.isLineSegments);
        if (status === 'Empty' && edges) {
            mesh.remove(edges);
        } else if (status !== 'Empty' && !edges) {
            const eg = new THREE.LineSegments(
                new THREE.EdgesGeometry(mesh.geometry),
                new THREE.LineBasicMaterial({ color: 0x64748b, transparent: true, opacity: 0.4 })
            );
            mesh.add(eg);
        }
    });
}

export function setCameraPosition(instanceId, x, y, z) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.camera.position.set(x, y, z);
    // Keep looking at the current orbit target
    if (inst.controls) {
        inst.camera.lookAt(inst.controls.target);
        inst.controls.update();
    } else {
        inst.camera.lookAt(0, 0, 0);
    }
}

export function resetCamera(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    if (inst.controls) {
        inst.controls.reset();
    } else {
        inst.camera.position.set(5, 5, 5);
        inst.camera.lookAt(0, 0, 0);
    }
}

export function takeScreenshot(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.renderer.render(inst.scene, inst.camera);
    const url = inst.renderer.domElement.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = url; a.download = `scene-${Date.now()}.png`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
}

export async function disposeThree(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    cancelAnimationFrame(inst.rafId);
    try { inst.ro?.disconnect(); } catch {}
    try { inst.controls?.dispose(); } catch {}
    try {
        inst.scene.traverse(obj => {
            if (obj.geometry) obj.geometry.dispose();
            if (obj.material) {
                if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose());
                else obj.material.dispose();
            }
        });
    } catch {}
    try {
        const canvas = inst.renderer.domElement;
        inst.renderer.dispose();
        canvas.parentElement?.removeChild(canvas);
    } catch {}
    _instances.delete(instanceId);
}
