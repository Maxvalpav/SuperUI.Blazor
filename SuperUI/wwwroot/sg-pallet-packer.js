'use strict';

/**
 * Pallet Packer using Three.js (ES Modules)
 * @author SuperUI
 */

const _scenes = new Map();
const THREE_URL = 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';

let THREE = null;

async function _ensureThree() {
    if (THREE) return;
    try {
        THREE = await import(THREE_URL);
    } catch (e) {
        console.error('[SgPalletPacker] Failed to load Three.js module:', e);
        // Fallback to global if already loaded somehow
        if (window.THREE) THREE = window.THREE;
        else throw e;
    }
}

export async function init(dotNetRef, canvasEl, instanceId) {
    await _ensureThree();

    const renderer = new THREE.WebGLRenderer({ canvas: canvasEl, antialias: true, alpha: true });
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.setSize(canvasEl.clientWidth, canvasEl.clientHeight);
    renderer.shadowMap.enabled = true;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x0f172a); // Match demo background

    const camera = new THREE.PerspectiveCamera(45, canvasEl.clientWidth / canvasEl.clientHeight, 0.1, 5000);
    camera.position.set(300, 300, 300);
    camera.lookAt(0, 0, 0);

    // Lights
    scene.add(new THREE.AmbientLight(0xffffff, 0.7));
    const dir = new THREE.DirectionalLight(0xffffff, 0.8);
    dir.position.set(200, 500, 300);
    dir.castShadow = true;
    scene.add(dir);

    // Grid
    const grid = new THREE.GridHelper(1000, 50, 0x334155, 0x1e293b);
    scene.add(grid);

    // Simple Orbit Controls
    let isDragging = false, lastX = 0, lastY = 0, theta = 0.8, phi = 0.6, radius = 500;

    const updateCamera = () => {
        camera.position.set(
            radius * Math.sin(theta) * Math.cos(phi),
            radius * Math.sin(phi),
            radius * Math.cos(theta) * Math.cos(phi)
        );
        camera.lookAt(0, 0, 0);
    };

    canvasEl.addEventListener('mousedown', e => {
        isDragging = true;
        lastX = e.clientX;
        lastY = e.clientY;
    });

    function onMouseMove(e) {
        if (!isDragging) return;
        theta -= (e.clientX - lastX) * 0.01;
        phi -= (e.clientY - lastY) * 0.01;
        phi = Math.max(0.05, Math.min(Math.PI / 2 - 0.1, phi));
        lastX = e.clientX;
        lastY = e.clientY;
        updateCamera();
    }
    function onMouseUp() { isDragging = false; }
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);

    canvasEl.addEventListener('wheel', e => {
        radius = Math.max(100, Math.min(2000, radius + e.deltaY * 0.5));
        updateCamera();
        e.preventDefault();
    }, { passive: false });

    updateCamera();

    const inst = { renderer, scene, camera, boxMeshes: [], palletGroup: new THREE.Group(), _onMouseMove: onMouseMove, _onMouseUp: onMouseUp };
    scene.add(inst.palletGroup);
    _scenes.set(instanceId, inst);

    function animate() {
        if (!_scenes.has(instanceId)) return;
        requestAnimationFrame(animate);
        renderer.render(scene, camera);
    }
    animate();

    // Initial resize
    const resizeObserver = new ResizeObserver(() => {
        if (!canvasEl.clientWidth) return;
        camera.aspect = canvasEl.clientWidth / canvasEl.clientHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(canvasEl.clientWidth, canvasEl.clientHeight);
    });
    resizeObserver.observe(canvasEl);
    inst.resizeObserver = resizeObserver;
}

export function updatePallet(instanceId, pallet) {
    const inst = _scenes.get(instanceId);
    if (!inst) return;
    const { palletGroup } = inst;

    // Clear old pallet visualization
    while(palletGroup.children.length > 0) palletGroup.remove(palletGroup.children[0]);

    // Pallet base - more realistic wood material
    const palGeo = new THREE.BoxGeometry(pallet.width, 12, pallet.depth);
    const palMat = new THREE.MeshLambertMaterial({ 
        color: 0x8B4513,
        emissive: 0x221100,
        emissiveIntensity: 0.2
    });
    const palMesh = new THREE.Mesh(palGeo, palMat);
    palMesh.position.set(pallet.width / 2, -6, pallet.depth / 2);
    palMesh.receiveShadow = true;
    palletGroup.add(palMesh);

    // Pallet wireframe (bounds) - subtle blue
    const palWireGeo = new THREE.BoxGeometry(pallet.width, pallet.height, pallet.depth);
    const palWire = new THREE.LineSegments(
        new THREE.EdgesGeometry(palWireGeo),
        new THREE.LineBasicMaterial({ color: 0x3b82f6, transparent: true, opacity: 0.3 })
    );
    palWire.position.set(pallet.width / 2, pallet.height / 2, pallet.depth / 2);
    palletGroup.add(palWire);
}

export function render(instanceId, pallet, packedBoxes) {
    const inst = _scenes.get(instanceId);
    if (!inst) return;
    const { scene, boxMeshes } = inst;

    // Update pallet visualization
    updatePallet(instanceId, pallet);

    // Remove old boxes
    boxMeshes.forEach(m => scene.remove(m));
    boxMeshes.length = 0;

    // Boxes
    for (const pb of (packedBoxes || [])) {
        const b = pb.box;
        // Small offset to avoid z-fighting and add "natural" gaps
        const geo = new THREE.BoxGeometry(b.width - 0.4, b.height - 0.4, b.depth - 0.4);
        const color = parseInt((b.color || '#3b82f6').replace('#', ''), 16);
        
        // Use Standard material for better lighting
        const mat = new THREE.MeshStandardMaterial({ 
            color,
            roughness: 0.7,
            metalness: 0.1
        });
        const mesh = new THREE.Mesh(geo, mat);
        
        mesh.position.set(pb.x + b.width / 2, pb.y + b.height / 2, pb.z + b.depth / 2);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        scene.add(mesh);
        boxMeshes.push(mesh);

        // Edge highlights
        const wire = new THREE.LineSegments(
            new THREE.EdgesGeometry(geo),
            new THREE.LineBasicMaterial({ color: 0x000000, opacity: 0.1, transparent: true })
        );
        wire.position.copy(mesh.position);
        scene.add(wire);
        boxMeshes.push(wire);
    }
}

export function resetCamera(instanceId) {
    const inst = _scenes.get(instanceId);
    if (!inst) return;
    
    // Use the internal state if we want to reset the manual orbit logic
    // or just hard-reset the camera position.
    // Given the simple manual orbit logic in init(), we need to reset those vars.
    // However, they are scoped to init(). 
    // Let's just reset the camera directly for now.
    inst.camera.position.set(300, 300, 300);
    inst.camera.lookAt(0, 0, 0);
}

export function dispose(instanceId) {
    const inst = _scenes.get(instanceId);
    if (inst) {
        if (inst.resizeObserver) inst.resizeObserver.disconnect();
        // Remove window event listeners
        try { window.removeEventListener('mousemove', inst._onMouseMove); } catch {}
        try { window.removeEventListener('mouseup', inst._onMouseUp); } catch {}
        // Dispose Three.js geometries, materials, meshes
        inst.scene.traverse(obj => {
            if (obj.isMesh || obj.isLineSegments || obj.isLine) {
                if (obj.geometry) { try { obj.geometry.dispose(); } catch {} }
                if (obj.material) {
                    if (Array.isArray(obj.material)) obj.material.forEach(m => { try { m.dispose(); } catch {} });
                    else { try { obj.material.dispose(); } catch {} }
                }
            }
        });
        // Dispose box meshes
        inst.boxMeshes.forEach(m => {
            if (m.geometry) { try { m.geometry.dispose(); } catch {} }
            if (m.material) { try { m.material.dispose(); } catch {} }
        });
        inst.renderer.dispose();
    }
    _scenes.delete(instanceId);
}
