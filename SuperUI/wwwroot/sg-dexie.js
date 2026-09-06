/**
 * sg-dexie.js - SuperUI Dexie.js Bridge
 * Wraps Dexie.js for IndexedDB management in Blazor.
 */

// Pinned version (never use @latest in a published package — non-reproducible builds + supply-chain risk).
// NOTE: Dexie is loaded lazily via dynamic import so that `import sg-dexie.js`
// itself never fails when offline / CDN blocked. All failures degrade to
// warnings (never uncaught throws that would surface as Blazor JSException).
const DEXIE_MJS_URL = 'https://unpkg.com/dexie@4.0.8/dist/dexie.mjs';

const _databases = new Map();
let _dexieCtor = null;

async function _ensureDexie() {
    if (_dexieCtor) return _dexieCtor;
    if (globalThis.Dexie) {
        _dexieCtor = globalThis.Dexie;
        return _dexieCtor;
    }
    try {
        const mod = await import(/* @vite-ignore */ DEXIE_MJS_URL);
        _dexieCtor = mod.default ?? mod.Dexie ?? globalThis.Dexie ?? null;
    } catch (e) {
        _dexieCtor = globalThis.Dexie ?? null;
    }
    if (!_dexieCtor) {
        throw new Error('Dexie.js unavailable (CDN blocked or offline)');
    }
    return _dexieCtor;
}

function _indexedDbAvailable() {
    try {
        return typeof window !== 'undefined' && !!window.indexedDB;
    } catch {
        return false;
    }
}

export async function initDb(dbName, schema) {
    if (_databases.has(dbName)) return true;

    if (!_indexedDbAvailable()) {
        console.warn(`[Dexie] IndexedDB unavailable — database '${dbName}' disabled (private mode / blocked storage?).`);
        return false;
    }

    let DexieCtor;
    try {
        DexieCtor = await _ensureDexie();
    } catch (e) {
        console.warn(`[Dexie] ${e?.message ?? e}`);
        return false;
    }

    try {
        const db = new DexieCtor(dbName);
        db.version(1).stores(schema);
        await db.open();
        _databases.set(dbName, db);
        console.log(`[Dexie] Database '${dbName}' initialized.`);
        return true;
    } catch (e) {
        // DOMException UnknownError / Internal error is typical when storage is
        // blocked, corrupt, or quota-exceeded. Never throw — storage is optional.
        console.warn(`[Dexie] Failed to open database '${dbName}': ${e?.message ?? e}`);
        return false;
    }
}

function _getDbOrWarn(dbName) {
    const db = _databases.get(dbName);
    if (!db) {
        console.warn(`[Dexie] Database '${dbName}' not initialized — operation skipped.`);
    }
    return db ?? null;
}

export async function add(dbName, tableName, item) {
    const db = _getDbOrWarn(dbName);
    if (!db) return null;
    try { return await db.table(tableName).add(item); }
    catch (e) { console.warn(`[Dexie] add failed: ${e?.message ?? e}`); return null; }
}

export async function bulkAdd(dbName, tableName, items) {
    const db = _getDbOrWarn(dbName);
    if (!db) return null;
    try { return await db.table(tableName).bulkAdd(items); }
    catch (e) { console.warn(`[Dexie] bulkAdd failed: ${e?.message ?? e}`); return null; }
}

export async function put(dbName, tableName, item) {
    const db = _getDbOrWarn(dbName);
    if (!db) return null;
    try { return await db.table(tableName).put(item); }
    catch (e) { console.warn(`[Dexie] put failed: ${e?.message ?? e}`); return null; }
}

export async function get(dbName, tableName, id) {
    const db = _getDbOrWarn(dbName);
    if (!db) return null;
    try { return await db.table(tableName).get(id); }
    catch (e) { console.warn(`[Dexie] get failed: ${e?.message ?? e}`); return null; }
}

export async function getAll(dbName, tableName) {
    const db = _getDbOrWarn(dbName);
    if (!db) return [];
    try { return await db.table(tableName).toArray(); }
    catch (e) { console.warn(`[Dexie] getAll failed: ${e?.message ?? e}`); return []; }
}

export async function query(dbName, tableName, filter) {
    const db = _getDbOrWarn(dbName);
    if (!db) return [];
    try {
        let collection = db.table(tableName);
        if (filter) {
            const entries = Object.entries(filter);
            for (let i = 0; i < entries.length; i++) {
                const [key, value] = entries[i];
                if (i === 0) {
                    collection = collection.where(key).equals(value);
                } else {
                    collection = collection.and(item => item[key] === value);
                }
            }
        }
        return await collection.toArray();
    } catch (e) { console.warn(`[Dexie] query failed: ${e?.message ?? e}`); return []; }
}

export async function remove(dbName, tableName, id) {
    const db = _getDbOrWarn(dbName);
    if (!db) return;
    try { return await db.table(tableName).delete(id); }
    catch (e) { console.warn(`[Dexie] remove failed: ${e?.message ?? e}`); }
}

export async function clearTable(dbName, tableName) {
    const db = _getDbOrWarn(dbName);
    if (!db) return;
    try { return await db.table(tableName).clear(); }
    catch (e) { console.warn(`[Dexie] clearTable failed: ${e?.message ?? e}`); }
}

export async function deleteDb(dbName) {
    try {
        if (_databases.has(dbName)) {
            const db = _databases.get(dbName);
            await db.delete();
            _databases.delete(dbName);
        } else if (globalThis.Dexie) {
            await globalThis.Dexie.delete(dbName);
        }
    } catch (e) { console.warn(`[Dexie] deleteDb failed: ${e?.message ?? e}`); }
}

export async function getTables(dbName) {
    const db = _databases.get(dbName);
    if (!db) return [];
    return db.tables.map(t => ({
        name: t.name,
        count: 0 // Will be updated if needed
    }));
}
