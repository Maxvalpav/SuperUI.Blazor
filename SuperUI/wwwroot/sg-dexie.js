/**
 * sg-dexie.js - SuperUI Dexie.js Bridge
 * Wraps Dexie.js for IndexedDB management in Blazor.
 */

// Pinned version (never use @latest in a published package — non-reproducible builds + supply-chain risk).
import 'https://unpkg.com/dexie@4.0.8/dist/dexie.js';

const _databases = new Map();

export async function initDb(dbName, schema) {
    if (_databases.has(dbName)) return;

    const db = new Dexie(dbName);
    db.version(1).stores(schema);
    await db.open();
    _databases.set(dbName, db);
    console.log(`[Dexie] Database '${dbName}' initialized.`);
}

export async function add(dbName, tableName, item) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).add(item);
}

export async function bulkAdd(dbName, tableName, items) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).bulkAdd(items);
}

export async function put(dbName, tableName, item) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).put(item);
}

export async function get(dbName, tableName, id) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).get(id);
}

export async function getAll(dbName, tableName) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).toArray();
}

export async function query(dbName, tableName, filter) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    
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
}

export async function remove(dbName, tableName, id) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).delete(id);
}

export async function clearTable(dbName, tableName) {
    const db = _databases.get(dbName);
    if (!db) throw new Error(`Database ${dbName} not found`);
    return await db.table(tableName).clear();
}

export async function deleteDb(dbName) {
    if (_databases.has(dbName)) {
        const db = _databases.get(dbName);
        await db.delete();
        _databases.delete(dbName);
    } else {
        await Dexie.delete(dbName);
    }
}

export async function getTables(dbName) {
    const db = _databases.get(dbName);
    if (!db) return [];
    return db.tables.map(t => ({
        name: t.name,
        count: 0 // Will be updated if needed
    }));
}
