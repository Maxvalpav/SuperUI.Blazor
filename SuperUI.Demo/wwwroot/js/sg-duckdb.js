// sg-duckdb.js — DuckDB-Wasm interop for SuperUI Demo
// Pattern: https://rud.is/drop/2024-02-19/vanilla/

const DUCKDB_ESM = 'https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.29.0/+esm';
const OPFS_PATH  = 'opfs://superui-demo.duckdb';
const DB_FNAME   = 'superui-demo.duckdb';   // virtual FS filename for export/import

let _duckdb      = null;   // duckdb module
let _db          = null;   // AsyncDuckDB instance
let _conn        = null;   // active connection
let _initPromise = null;
let _opfsActive  = false;  // whether OPFS is currently in use

// ── Init ──────────────────────────────────────────────────────────────────────

export async function init(useOpfs = false) {
    // If already running with the right mode, reuse
    if (_conn && _opfsActive === useOpfs) return { ok: true, alreadyInit: true, opfs: _opfsActive };
    // If mode changed, tear down first
    if (_conn) await _teardown();

    _initPromise = _doInit(useOpfs);
    return _initPromise;
}

async function _doInit(useOpfs) {
    try {
        if (!_duckdb) {
            _duckdb = await import(DUCKDB_ESM);
        }
        const CDN_BUNDLES = _duckdb.getJsDelivrBundles();
        const bundle      = await _duckdb.selectBundle(CDN_BUNDLES);

        const workerUrl = URL.createObjectURL(
            new Blob([`importScripts("${bundle.mainWorker}");`], { type: 'text/javascript' })
        );
        const worker = new Worker(workerUrl);
        const logger = new _duckdb.ConsoleLogger(_duckdb.LogLevel.WARNING);
        _db = new _duckdb.AsyncDuckDB(logger, worker);

        // Try OPFS if requested; fall back to in-memory on error
        let actualOpfs = false;
        if (useOpfs && 'storage' in navigator && 'getDirectory' in navigator.storage) {
            try {
                await _db.instantiate(bundle.mainModule, bundle.pthreadWorker);
                _conn = await _db.connect();
                // Open / create the OPFS-backed database file
                await _conn.query(`ATTACH IF NOT EXISTS '${OPFS_PATH}' AS opfsdb`);
                await _conn.query(`USE opfsdb`);
                actualOpfs = true;
            } catch (e) {
                // OPFS failed — fall through to in-memory
                try { await _conn?.close(); } catch {}
                try { await _db.terminate(); } catch {}
                _conn = null;
                _db   = new _duckdb.AsyncDuckDB(logger, new Worker(workerUrl));
                await _db.instantiate(bundle.mainModule, bundle.pthreadWorker);
                _conn = await _db.connect();
            }
        } else {
            await _db.instantiate(bundle.mainModule, bundle.pthreadWorker);
            _conn = await _db.connect();
        }

        URL.revokeObjectURL(workerUrl);
        _opfsActive  = actualOpfs;
        _initPromise = null;
        return { ok: true, opfs: actualOpfs };
    } catch (e) {
        _initPromise = null;
        return { ok: false, error: String(e) };
    }
}

async function _teardown() {
    try { await _conn?.close(); } catch {}
    try { await _db?.terminate(); } catch {}
    _conn = null;
    _db   = null;
    _opfsActive = false;
}

// ── Status ────────────────────────────────────────────────────────────────────

export function getStatus() {
    return { connected: !!_conn, opfs: _opfsActive };
}

// ── Query → JSON rows ─────────────────────────────────────────────────────────

// Arrow type names that need special handling
const RE_DECIMAL = /^Decimal/i;
const RE_DATE    = /^Date/i;
const RE_TIME    = /^Time/i;
const RE_TS      = /^Timestamp/i;

function arrowValueToJs(v, typeName) {
    if (v === null || v === undefined) return null;

    // BigInt → number (INTEGER, BIGINT, HUGEINT)
    if (typeof v === 'bigint') return Number(v);

    // Decimal128 → Arrow.js returns it as a string like "10267800" scaled by precision
    // We get the actual string representation from Arrow directly via toString()
    if (RE_DECIMAL.test(typeName)) {
        // Arrow Decimal proxy: has a toString() that gives the correct decimal string
        return typeof v === 'object' && v !== null ? String(v) : v;
    }

    // Date32 → Arrow.js gives milliseconds since epoch as a number
    if (RE_DATE.test(typeName)) {
        if (typeof v === 'number') {
            // Arrow Date32 stores days since epoch, but JS receives it as ms
            return new Date(v).toISOString().slice(0, 10); // "YYYY-MM-DD"
        }
        if (v instanceof Date) return v.toISOString().slice(0, 10);
        // Already a string
        if (typeof v === 'string') return v.slice(0, 10);
        return String(v);
    }

    // Timestamp → ms since epoch
    if (RE_TS.test(typeName)) {
        if (typeof v === 'number' || typeof v === 'bigint') {
            return new Date(Number(v)).toISOString().replace('T', ' ').slice(0, 19);
        }
        if (v instanceof Date) return v.toISOString().replace('T', ' ').slice(0, 19);
        return String(v);
    }

    // Time → keep as-is (usually a number of ms/us)
    if (RE_TIME.test(typeName)) {
        return typeof v === 'bigint' ? Number(v) : v;
    }

    return v;
}

export async function query(sql) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const result = await _conn.query(sql);
        const schema = result.schema.fields.map(f => ({
            name: f.name,
            type: f.type.toString(),
        }));
        const rows = result.toArray().map(row => {
            const obj = {};
            schema.forEach(col => {
                obj[col.name] = arrowValueToJs(row[col.name], col.type);
            });
            return obj;
        });
        return { ok: true, schema, rows, rowCount: rows.length };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Execute (DDL / DML) ───────────────────────────────────────────────────────

export async function execute(sql) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        await _conn.query(sql);
        return { ok: true };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── List tables ───────────────────────────────────────────────────────────────

export async function listTables() {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const result = await _conn.query('SHOW TABLES');
        const rows = result.toArray().map(r => r.name ?? r[Object.keys(r)[0]]);
        return { ok: true, tables: rows };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── List databases ────────────────────────────────────────────────────────────

export async function listDatabases() {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const result = await _conn.query('SELECT database_name, path, type FROM duckdb_databases()');
        const dbs = result.toArray().map(r => ({
            name: r.database_name ?? r[Object.keys(r)[0]],
            path: r.path ?? '',
            type: r.type ?? '',
        }));
        return { ok: true, databases: dbs };
    } catch (e) {
        return { ok: true, databases: [{ name: 'memory', path: '', type: 'memory' }] };
    }
}

// ── Switch active database ────────────────────────────────────────────────────

export async function switchDatabase(dbName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        await _conn.query(`USE "${dbName}"`);
        // Return tables in the new active db
        const result = await _conn.query('SHOW TABLES');
        const tables = result.toArray().map(r => r.name ?? r[Object.keys(r)[0]]);
        return { ok: true, tables };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── List tables for a specific database ──────────────────────────────────────

export async function listTablesForDb(dbName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const result = await _conn.query(
            `SELECT table_name FROM information_schema.tables WHERE table_catalog = '${dbName.replace(/'/g,"''")}' ORDER BY table_name`
        );
        const tables = result.toArray().map(r => r.table_name ?? r[Object.keys(r)[0]]);
        return { ok: true, tables };
    } catch (e) {
        // fallback: just show tables
        try {
            const r2 = await _conn.query('SHOW TABLES');
            return { ok: true, tables: r2.toArray().map(r => r.name ?? r[Object.keys(r)[0]]) };
        } catch { return { ok: true, tables: [] }; }
    }
}

// ── Describe table ────────────────────────────────────────────────────────────

export async function describeTable(tableName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const result = await _conn.query(`DESCRIBE "${tableName}"`);
        const cols = result.toArray().map(r => {
            const keys = Object.keys(r);
            return {
                name: r.column_name ?? r[keys[0]] ?? '',
                type: r.column_type ?? r[keys[1]] ?? '',
                null: r['null']     ?? r[keys[2]] ?? '',
            };
        });
        return { ok: true, columns: cols };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Export database → download .duckdb file ───────────────────────────────────
//
// Strategy: EXPORT DATABASE writes SQL + CSV files into DuckDB's virtual FS.
// We then read the schema SQL + each table's CSV, bundle them as a JSON snapshot,
// and offer it as a download. This avoids needing the parquet extension.

export async function exportDatabase() {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        // Get all tables
        const tblResult = await _conn.query('SHOW TABLES');
        const tables = tblResult.toArray().map(r => r.name ?? r[Object.keys(r)[0]]);

        const snapshot = { version: 1, tables: [] };

        for (const tbl of tables) {
            // Get schema via DESCRIBE
            const descResult = await _conn.query(`DESCRIBE "${tbl}"`);
            const cols = descResult.toArray().map(r => {
                const keys = Object.keys(r);
                return {
                    name:    r.column_name ?? r[keys[0]] ?? '',
                    type:    r.column_type ?? r[keys[1]] ?? '',
                    notNull: (r['null'] ?? r[keys[2]] ?? '') === 'NO',
                };
            });

            // Export data as JSON via DuckDB query
            const dataResult = await _conn.query(`SELECT * FROM "${tbl}"`);
            const schema = dataResult.schema.fields.map(f => ({
                name: f.name,
                type: f.type.toString(),
            }));
            const rows = dataResult.toArray().map(row => {
                const obj = {};
                schema.forEach(col => {
                    obj[col.name] = arrowValueToJs(row[col.name], col.type);
                });
                return obj;
            });

            snapshot.tables.push({ name: tbl, columns: cols, rows });
        }

        // Serialize and trigger download
        const json    = JSON.stringify(snapshot);
        const blob    = new Blob([json], { type: 'application/json' });
        const url     = URL.createObjectURL(blob);
        const a       = document.createElement('a');
        a.href        = url;
        a.download    = 'superui-db-export.json';
        a.click();
        URL.revokeObjectURL(url);

        return { ok: true, tableCount: tables.length };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Import database from .json snapshot ───────────────────────────────────────

export async function importDatabase(jsonText) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const snapshot = JSON.parse(jsonText);
        if (!snapshot.tables) return { ok: false, error: 'Invalid snapshot format' };

        let imported = 0;
        for (const tbl of snapshot.tables) {
            // Drop existing
            await _conn.query(`DROP TABLE IF EXISTS "${tbl.name}"`);

            // Rebuild CREATE TABLE
            const colDefs = tbl.columns.map(c =>
                `"${c.name}" ${c.type}${c.notNull ? ' NOT NULL' : ''}`
            ).join(', ');
            await _conn.query(`CREATE TABLE "${tbl.name}" (${colDefs})`);

            // Insert rows in batches
            if (tbl.rows && tbl.rows.length > 0) {
                const colNames = tbl.columns.map(c => `"${c.name}"`).join(', ');
                const BATCH = 200;
                for (let i = 0; i < tbl.rows.length; i += BATCH) {
                    const batch = tbl.rows.slice(i, i + BATCH);
                    const values = batch.map(row => {
                        const vals = tbl.columns.map(c => {
                            const v = row[c.name];
                            if (v === null || v === undefined) return 'NULL';
                            if (typeof v === 'boolean') return v ? 'true' : 'false';
                            if (typeof v === 'number') return String(v);
                            // Escape single quotes in strings
                            return `'${String(v).replace(/'/g, "''")}'`;
                        });
                        return `(${vals.join(', ')})`;
                    }).join(', ');
                    await _conn.query(`INSERT INTO "${tbl.name}" (${colNames}) VALUES ${values}`);
                }
            }
            imported++;
        }
        return { ok: true, tableCount: imported };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── List indexes ─────────────────────────────────────────────────────────────

export async function listIndexes(tableName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const result = await _conn.query(
            `SELECT index_name, is_unique, sql
             FROM duckdb_indexes()
             WHERE table_name = '${tableName.replace(/'/g, "''")}'`
        );
        const indexes = result.toArray().map(r => ({
            name:     r.index_name ?? '',
            unique:   r.is_unique  ?? false,
            sql:      r.sql        ?? '',
        }));
        return { ok: true, indexes };
    } catch (e) {
        // duckdb_indexes() may not exist in all builds — return empty
        return { ok: true, indexes: [] };
    }
}

// ── Generate CREATE INDEX script ──────────────────────────────────────────────

export function generateIndexScript(tableName, indexName, columns, unique) {
    const u    = unique ? 'UNIQUE ' : '';
    const cols = columns.map(c => `"${c}"`).join(', ');
    const sql  = `CREATE ${u}INDEX "${indexName}"\n    ON "${tableName}" (${cols});`;
    return { ok: true, sql };
}

// ── Create index ──────────────────────────────────────────────────────────────

export async function createIndex(tableName, indexName, columns, unique) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    const { sql } = generateIndexScript(tableName, indexName, columns, unique);
    try {
        await _conn.query(sql);
        return { ok: true, sql };
    } catch (e) {
        return { ok: false, error: String(e), sql };
    }
}

// ── Drop index ────────────────────────────────────────────────────────────────

export async function dropIndex(indexName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        await _conn.query(`DROP INDEX IF EXISTS "${indexName.replace(/"/g, '""')}"`);
        return { ok: true };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Load JSON array file into a table ─────────────────────────────────────────
// jsonText: string content of a JSON file containing an array of objects
// tableName: target table name (will be created / replaced)
// mode: 'columns' (default) — infer columns from object keys
//       'json'              — single JSON column, one row per element

export async function loadJsonFile(jsonText, tableName, mode = 'columns') {
    if (mode === 'json') return _loadJsonAsColumn(jsonText, tableName);
    return _loadJsonAsColumns(jsonText, tableName);
}

// ── internal: flat columns mode ───────────────────────────────────────────────

async function _loadJsonAsColumns(jsonText, tableName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const data = JSON.parse(jsonText);
        if (!Array.isArray(data)) return { ok: false, error: 'JSON должен быть массивом объектов' };
        if (data.length === 0)    return { ok: false, error: 'Массив пуст' };

        // Infer columns from first object
        const sample = data[0];
        const colNames = Object.keys(sample);

        // Infer SQL types from values
        function inferType(val) {
            if (val === null || val === undefined) return 'VARCHAR';
            if (typeof val === 'boolean') return 'BOOLEAN';
            if (typeof val === 'number') {
                return Number.isInteger(val) ? 'BIGINT' : 'DOUBLE';
            }
            if (typeof val === 'string') {
                // Try date
                if (/^\d{4}-\d{2}-\d{2}$/.test(val)) return 'DATE';
                if (/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}/.test(val)) return 'TIMESTAMP';
            }
            return 'VARCHAR';
        }

        // Use first non-null value per column for type inference
        const colTypes = {};
        for (const col of colNames) {
            for (const row of data) {
                if (row[col] !== null && row[col] !== undefined) {
                    colTypes[col] = inferType(row[col]);
                    break;
                }
            }
            if (!colTypes[col]) colTypes[col] = 'VARCHAR';
        }

        // Create table
        const colDefs = colNames.map(c => `"${c}" ${colTypes[c]}`).join(', ');
        await _conn.query(`DROP TABLE IF EXISTS "${tableName}"`);
        await _conn.query(`CREATE TABLE "${tableName}" (${colDefs})`);

        // Insert in batches
        const colList = colNames.map(c => `"${c}"`).join(', ');
        const BATCH = 200;
        for (let i = 0; i < data.length; i += BATCH) {
            const batch = data.slice(i, i + BATCH);
            const values = batch.map(row => {
                const vals = colNames.map(c => {
                    const v = row[c];
                    if (v === null || v === undefined) return 'NULL';
                    if (typeof v === 'boolean') return v ? 'true' : 'false';
                    if (typeof v === 'number')  return String(v);
                    return `'${String(v).replace(/'/g, "''")}'`;
                });
                return `(${vals.join(', ')})`;
            }).join(', ');
            await _conn.query(`INSERT INTO "${tableName}" (${colList}) VALUES ${values}`);
        }

        return { ok: true, rowCount: data.length, columns: colNames.length };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── internal: JSON column mode ────────────────────────────────────────────────
// Stores each element of a JSON array as a separate row in a single JSON column.
// If the root value is not an array, the whole document is stored as one row.

async function _loadJsonAsColumn(jsonText, tableName) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    try {
        const parsed = JSON.parse(jsonText);
        const items  = Array.isArray(parsed) ? parsed : [parsed];
        if (items.length === 0) return { ok: false, error: 'Массив пуст' };

        await _conn.query(`DROP TABLE IF EXISTS "${tableName}"`);
        await _conn.query(`CREATE TABLE "${tableName}" (id INTEGER, data JSON)`);

        // Serialize each element back to a JSON string and insert
        const BATCH = 200;
        for (let i = 0; i < items.length; i += BATCH) {
            const slice = items.slice(i, i + BATCH);
            const values = slice.map((item, j) => {
                const id      = i + j + 1;
                const jsonStr = JSON.stringify(item).replace(/'/g, "''");
                return `(${id}, '${jsonStr}'::JSON)`;
            }).join(', ');
            await _conn.query(`INSERT INTO "${tableName}" (id, data) VALUES ${values}`);
        }

        return { ok: true, rowCount: items.length, columns: 2, mode: 'json' };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Load Parquet file ─────────────────────────────────────────────────────────
// fileBytes: Uint8Array of the .parquet file content
// tableName: target table name

export async function loadParquetFile(fileBytes, tableName) {
    if (!_conn || !_db) return { ok: false, error: 'Not initialized' };
    try {
        const fname = `${tableName}_upload.parquet`;

        // Register the file in DuckDB's virtual filesystem
        await _db.registerFileBuffer(fname, fileBytes);

        // Create table from parquet
        await _conn.query(`DROP TABLE IF EXISTS "${tableName}"`);
        await _conn.query(`CREATE TABLE "${tableName}" AS SELECT * FROM read_parquet('${fname}')`);

        // Get row count
        const cnt = await _conn.query(`SELECT COUNT(*) AS n FROM "${tableName}"`);
        const rowCount = Number(cnt.toArray()[0]?.n ?? 0);

        // Cleanup virtual file
        try { await _db.dropFile(fname); } catch {}

        return { ok: true, rowCount };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Load Parquet from URL (S3 / HTTP) ─────────────────────────────────────────
// url: public HTTP/HTTPS URL to a .parquet file (S3 presigned, CDN, etc.)
// tableName: target table name

export async function loadParquetFromUrl(url, tableName) {
    if (!_conn || !_db) return { ok: false, error: 'Not initialized' };
    try {
        // Fetch the file via browser (handles CORS, S3 presigned URLs, etc.)
        const resp = await fetch(url);
        if (!resp.ok) return { ok: false, error: `HTTP ${resp.status}: ${resp.statusText}` };

        const buf   = await resp.arrayBuffer();
        const bytes = new Uint8Array(buf);
        const fname = `${tableName}_url.parquet`;

        await _db.registerFileBuffer(fname, bytes);

        await _conn.query(`DROP TABLE IF EXISTS "${tableName}"`);
        await _conn.query(`CREATE TABLE "${tableName}" AS SELECT * FROM read_parquet('${fname}')`);

        const cnt = await _conn.query(`SELECT COUNT(*) AS n FROM "${tableName}"`);
        const rowCount = Number(cnt.toArray()[0]?.n ?? 0);

        try { await _db.dropFile(fname); } catch {}

        return { ok: true, rowCount, bytes: buf.byteLength };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

export function opfsSupported() {
    return !!(typeof navigator !== 'undefined' &&
              navigator.storage &&
              navigator.storage.getDirectory);
}

// ── Save in-memory DB to .duckdb file (download) ──────────────────────────────
// Uses OPFS as intermediate: ATTACH → COPY FROM DATABASE → read bytes → download

export async function saveDatabaseToDisk(filename = 'my_database.db') {
    if (!_conn || !_db) return { ok: false, error: 'Not initialized' };
    try {
        const dbName = 'save_tmp_' + Date.now();
        const opfsPath = `opfs://${dbName}.db`;

        // Attach a new OPFS-backed database, copy everything, detach
        await _conn.query(`ATTACH '${opfsPath}' AS "${dbName}"`);
        await _conn.query(`COPY FROM DATABASE memory TO "${dbName}"`);
        await _conn.query(`DETACH "${dbName}"`);

        // Read the file bytes from OPFS
        const root = await navigator.storage.getDirectory();
        const fh   = await root.getFileHandle(`${dbName}.db`);
        const file = await fh.getFile();
        const buf  = await file.arrayBuffer();

        // Trigger download
        const blob = new Blob([buf], { type: 'application/octet-stream' });
        const url  = URL.createObjectURL(blob);
        const a    = document.createElement('a');
        a.href     = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);

        // Cleanup OPFS temp file
        await root.removeEntry(`${dbName}.db`, { recursive: true }).catch(() => {});
        await root.removeEntry(`${dbName}.db.wal`, { recursive: true }).catch(() => {});

        return { ok: true, bytes: buf.byteLength };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Load .duckdb file from disk → copy tables to memory ───────────────────────

export async function loadDatabaseFromDisk(fileBytes, filename) {
    if (!_conn || !_db) return { ok: false, error: 'Not initialized' };
    try {
        const fname  = filename || 'loaded.db';
        const dbName = 'loaded_' + Date.now();

        // Register file in virtual FS
        await _db.registerFileBuffer(fname, fileBytes);

        // Attach, copy to memory, detach
        await _conn.query(`ATTACH '${fname}' AS "${dbName}" (READ_ONLY)`);
        await _conn.query(`COPY FROM DATABASE "${dbName}" TO memory`);
        await _conn.query(`DETACH "${dbName}"`);

        try { await _db.dropFile(fname); } catch {}

        // Return list of tables now in memory
        const tblRes = await _conn.query('SHOW TABLES');
        const tables = tblRes.toArray().map(r => r.name ?? r[Object.keys(r)[0]]);
        return { ok: true, tables };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Export table to Parquet (download) ────────────────────────────────────────

export async function exportTableToParquet(tableName) {
    if (!_conn || !_db) return { ok: false, error: 'Not initialized' };
    try {
        const fname = `${tableName}_export.parquet`;

        // Write parquet to DuckDB virtual FS
        await _conn.query(`COPY "${tableName}" TO '${fname}' (FORMAT PARQUET)`);

        // Read bytes from virtual FS
        const buf = await _db.copyFileToBuffer(fname);

        // Trigger download
        const blob = new Blob([buf], { type: 'application/octet-stream' });
        const url  = URL.createObjectURL(blob);
        const a    = document.createElement('a');
        a.href     = url;
        a.download = fname;
        a.click();
        URL.revokeObjectURL(url);

        // Cleanup
        try { await _db.dropFile(fname); } catch {}

        return { ok: true, bytes: buf.byteLength };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── OPFS: delete persisted file ───────────────────────────────────────────────

export async function clearOpfs() {
    try {
        const root = await navigator.storage.getDirectory();
        await root.removeEntry('superui-demo.duckdb', { recursive: true }).catch(() => {});
        await root.removeEntry('superui-demo.duckdb.wal', { recursive: true }).catch(() => {});
        return { ok: true };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

async function insertBatches(table, rows, batchSize = 200) {
    for (let i = 0; i < rows.length; i += batchSize) {
        await _conn.query(`INSERT INTO "${table}" VALUES ${rows.slice(i, i + batchSize).join(',')}`);
    }
}

function rnd(i, mod)  { return ((i * 2654435761) >>> 0) % mod; }
function rnd2(i, mod) { return ((i * 1234567891 + 987654321) >>> 0) % mod; }

// ── Seed: HR data ─────────────────────────────────────────────────────────────

export async function seedDemoData(count = 1000) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    count = Math.max(1, Math.min(count, 1_000_000));
    try {
        await _conn.query('DROP TABLE IF EXISTS employees');
        await _conn.query(`
            CREATE TABLE employees (
                id        INTEGER PRIMARY KEY,
                name      VARCHAR NOT NULL,
                dept      VARCHAR,
                city      VARCHAR,
                salary    DECIMAL(10,2),
                hired_on  DATE,
                active    BOOLEAN
            )
        `);
        const depts  = ['Engineering','Sales','Marketing','HR','Finance','Support','Legal','Operations'];
        const cities = ['Moscow','Saint Petersburg','Novosibirsk','Kazan','Yekaterinburg','Samara','Omsk','Chelyabinsk'];
        const first  = ['Alexei','Maria','Ivan','Olga','Dmitry','Elena','Sergei','Anna','Pavel','Natalia',
                        'Andrei','Tatiana','Mikhail','Irina','Nikolai','Svetlana','Artem','Yulia','Evgeny','Oksana'];
        const last   = ['Ivanov','Petrov','Sidorov','Kozlov','Novikov','Morozov','Volkov','Sokolov',
                        'Popov','Lebedev','Smirnov','Fedorov','Orlov','Nikitin','Zaitsev','Kuznetsov',
                        'Soloviev','Vinogradov','Bogdanov','Voronov'];
        const rows = [];
        for (let i = 1; i <= count; i++) {
            const fn     = first[rnd(i, first.length)];
            const ln     = last[rnd2(i, last.length)];
            const dept   = depts[rnd(i + 7, depts.length)];
            const city   = cities[rnd2(i + 3, cities.length)];
            const salary = (30000 + rnd(i * 3, 170000)).toFixed(2);
            const year   = 2010 + rnd(i, 14);
            const month  = String(1 + rnd2(i, 12)).padStart(2, '0');
            const day    = String(1 + rnd(i * 2, 28)).padStart(2, '0');
            const active = rnd(i, 10) > 1 ? 'true' : 'false';
            rows.push(`(${i},'${fn} ${ln}','${dept}','${city}',${salary},'${year}-${month}-${day}',${active})`);
        }
        await insertBatches('employees', rows);
        return { ok: true };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Seed: Shop data ───────────────────────────────────────────────────────────

export async function seedShopData(count = 1000) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    count = Math.max(1, Math.min(count, 1_000_000));
    const prodCount = Math.max(10, Math.floor(count / 10));
    try {
        await _conn.query('DROP TABLE IF EXISTS orders');
        await _conn.query('DROP TABLE IF EXISTS products');
        await _conn.query(`
            CREATE TABLE products (
                id       INTEGER PRIMARY KEY,
                name     VARCHAR NOT NULL,
                category VARCHAR,
                price    DECIMAL(10,2),
                stock    INTEGER
            )
        `);
        const categories = ['Electronics','Clothing','Food','Books','Sports','Home','Toys','Beauty','Garden','Auto'];
        const adjectives = ['Premium','Basic','Pro','Ultra','Mini','Max','Smart','Classic','Eco','Lite'];
        const nouns      = ['Widget','Gadget','Device','Tool','Kit','Pack','Set','Box','Unit','Module'];
        const prodRows   = [];
        for (let i = 1; i <= prodCount; i++) {
            const cat   = categories[rnd(i, categories.length)];
            const adj   = adjectives[rnd2(i, adjectives.length)];
            const noun  = nouns[rnd(i + 5, nouns.length)];
            const price = (1 + rnd(i * 7, 99900) / 100).toFixed(2);
            const stock = rnd2(i, 1000);
            prodRows.push(`(${i},'${adj} ${noun} ${i}','${cat}',${price},${stock})`);
        }
        await insertBatches('products', prodRows);

        await _conn.query(`
            CREATE TABLE orders (
                id         INTEGER PRIMARY KEY,
                product_id INTEGER,
                customer   VARCHAR,
                quantity   INTEGER,
                total      DECIMAL(10,2),
                order_date DATE,
                status     VARCHAR
            )
        `);
        const statuses  = ['Pending','Processing','Shipped','Delivered','Cancelled','Returned'];
        const customers = ['Alice','Bob','Carol','Dave','Eve','Frank','Grace','Hank',
                           'Iris','Jack','Kate','Leo','Mia','Nick','Olivia','Pete',
                           'Quinn','Rose','Sam','Tina','Uma','Victor','Wendy','Xander'];
        const orderRows = [];
        for (let i = 1; i <= count; i++) {
            const prodId   = 1 + rnd(i * 3, prodCount);
            const customer = customers[rnd2(i, customers.length)];
            const qty      = 1 + rnd(i, 20);
            const total    = (qty * (1 + rnd2(i * 5, 50000) / 100)).toFixed(2);
            const year     = 2020 + rnd(i, 5);
            const month    = String(1 + rnd2(i, 12)).padStart(2, '0');
            const day      = String(1 + rnd(i * 2, 28)).padStart(2, '0');
            const status   = statuses[rnd2(i, statuses.length)];
            orderRows.push(`(${i},${prodId},'${customer}',${qty},${total},'${year}-${month}-${day}','${status}')`);
        }
        await insertBatches('orders', orderRows);
        return { ok: true };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Seed: All ─────────────────────────────────────────────────────────────────

export async function seedAllData(count = 1000) {
    const r1 = await seedDemoData(count);
    if (!r1.ok) return r1;
    return await seedShopData(count);
}

// ── Seed: Custom table by column specs ────────────────────────────────────────
// colSpecsJson: JSON string of [{name, type}, ...]

export async function seedCustomTable(tableName, colSpecsJson, count = 1000) {
    if (!_conn) return { ok: false, error: 'Not initialized' };
    count = Math.max(1, Math.min(count, 1_000_000));
    try {
        const cols = JSON.parse(colSpecsJson);
        const rows = [];

        for (let i = 1; i <= count; i++) {
            const vals = cols.map(col => generateValue(col.type, i, col.name));
            rows.push(`(${vals.join(', ')})`);
        }

        const colNames = cols.map(c => `"${c.name}"`).join(', ');
        const BATCH = 200;
        for (let i = 0; i < rows.length; i += BATCH) {
            await _conn.query(
                `INSERT INTO "${tableName}" (${colNames}) VALUES ${rows.slice(i, i + BATCH).join(',')}`
            );
        }
        return { ok: true };
    } catch (e) {
        return { ok: false, error: String(e) };
    }
}

// ── Value generator by SQL type ───────────────────────────────────────────────

const FIRST_NAMES = ['Alice','Bob','Carol','Dave','Eve','Frank','Grace','Hank',
                     'Iris','Jack','Kate','Leo','Mia','Nick','Olivia','Pete',
                     'Quinn','Rose','Sam','Tina','Uma','Victor','Wendy','Xander',
                     'Alexei','Maria','Ivan','Olga','Dmitry','Elena','Sergei','Anna'];
const LAST_NAMES  = ['Smith','Jones','Brown','Wilson','Taylor','Davies','Evans',
                     'Ivanov','Petrov','Sidorov','Kozlov','Novikov','Morozov'];
const CITIES      = ['Moscow','London','New York','Paris','Berlin','Tokyo',
                     'Sydney','Toronto','Dubai','Singapore','Amsterdam','Seoul'];
const STATUSES    = ['Active','Inactive','Pending','Archived','Draft','Published'];
const CATEGORIES  = ['Electronics','Clothing','Food','Books','Sports','Home','Toys'];
const DEPTS       = ['Engineering','Sales','Marketing','HR','Finance','Support','Legal'];
const LOREM_WORDS = ['lorem','ipsum','dolor','sit','amet','consectetur','adipiscing',
                     'elit','sed','do','eiusmod','tempor','incididunt','ut','labore'];

function generateValue(sqlType, i, colName) {
    const t = sqlType.toUpperCase();
    const n = colName.toLowerCase();

    // ── Integer types ──────────────────────────────────────────────────────
    if (t === 'INTEGER' || t === 'INT' || t === 'SMALLINT' || t === 'TINYINT') {
        // Detect semantic from column name
        if (n === 'id' || n.endsWith('_id')) return i;
        if (n.includes('age'))    return 18 + rnd(i, 62);
        if (n.includes('year'))   return 2000 + rnd(i, 24);
        if (n.includes('count') || n.includes('qty') || n.includes('quantity'))
            return 1 + rnd(i, 100);
        if (n.includes('stock'))  return rnd2(i, 1000);
        if (n.includes('score') || n.includes('rating')) return 1 + rnd(i, 10);
        return rnd(i * 3, 100000);
    }

    if (t === 'BIGINT' || t === 'HUGEINT') {
        if (n === 'id' || n.endsWith('_id')) return i;
        return rnd(i * 7, 1000000);
    }

    // ── Decimal / Float ────────────────────────────────────────────────────
    if (t.startsWith('DECIMAL') || t.startsWith('NUMERIC')) {
        if (n.includes('salary') || n.includes('wage'))
            return (30000 + rnd(i * 3, 170000)).toFixed(2);
        if (n.includes('price') || n.includes('cost') || n.includes('amount'))
            return (1 + rnd(i * 7, 99900) / 100).toFixed(2);
        if (n.includes('rate') || n.includes('percent') || n.includes('ratio'))
            return (rnd(i, 10000) / 100).toFixed(2);
        if (n.includes('lat'))  return (-90  + rnd(i, 18000) / 100).toFixed(6);
        if (n.includes('lon') || n.includes('lng'))
            return (-180 + rnd(i, 36000) / 100).toFixed(6);
        return (rnd(i * 5, 1000000) / 100).toFixed(2);
    }

    if (t === 'FLOAT' || t === 'DOUBLE' || t === 'REAL') {
        return (rnd(i * 5, 100000) / 100).toFixed(4);
    }

    // ── Boolean ────────────────────────────────────────────────────────────
    if (t === 'BOOLEAN' || t === 'BOOL') {
        if (n.includes('active') || n.includes('enabled') || n.includes('is_'))
            return rnd(i, 10) > 1 ? 'true' : 'false';
        return rnd(i, 2) === 0 ? 'true' : 'false';
    }

    // ── Date / Time ────────────────────────────────────────────────────────
    if (t === 'DATE') {
        const year  = 2010 + rnd(i, 14);
        const month = String(1 + rnd2(i, 12)).padStart(2, '0');
        const day   = String(1 + rnd(i * 2, 28)).padStart(2, '0');
        return `'${year}-${month}-${day}'`;
    }

    if (t === 'TIMESTAMP') {
        const year  = 2015 + rnd(i, 9);
        const month = String(1 + rnd2(i, 12)).padStart(2, '0');
        const day   = String(1 + rnd(i * 2, 28)).padStart(2, '0');
        const hour  = String(rnd(i * 3, 24)).padStart(2, '0');
        const min   = String(rnd2(i * 5, 60)).padStart(2, '0');
        const sec   = String(rnd(i * 7, 60)).padStart(2, '0');
        return `'${year}-${month}-${day} ${hour}:${min}:${sec}'`;
    }

    if (t === 'TIME') {
        const h = String(rnd(i, 24)).padStart(2, '0');
        const m = String(rnd2(i, 60)).padStart(2, '0');
        return `'${h}:${m}:00'`;
    }

    // ── UUID ───────────────────────────────────────────────────────────────
    if (t === 'UUID') {
        // Generate a deterministic UUID-like string
        const hex = (n, len) => (n >>> 0).toString(16).padStart(len, '0');
        return `'${hex(rnd(i,0xFFFFFFFF),8)}-${hex(rnd2(i,0xFFFF),4)}-4${hex(rnd(i*3,0xFFF),3)}-${hex(rnd2(i*7,0xFFFF),4)}-${hex(rnd(i*11,0xFFFFFFFF),8)}${hex(rnd2(i*13,0xFFFF),4)}'`;
    }

    // ── VARCHAR / TEXT / CHAR ──────────────────────────────────────────────
    // Semantic detection by column name
    if (n === 'name' || n.endsWith('_name') || n === 'full_name') {
        return `'${FIRST_NAMES[rnd(i, FIRST_NAMES.length)]} ${LAST_NAMES[rnd2(i, LAST_NAMES.length)]}'`;
    }
    if (n.includes('first') && n.includes('name')) {
        return `'${FIRST_NAMES[rnd(i, FIRST_NAMES.length)]}'`;
    }
    if (n.includes('last') && n.includes('name')) {
        return `'${LAST_NAMES[rnd2(i, LAST_NAMES.length)]}'`;
    }
    if (n.includes('email')) {
        const fn = FIRST_NAMES[rnd(i, FIRST_NAMES.length)].toLowerCase();
        const ln = LAST_NAMES[rnd2(i, LAST_NAMES.length)].toLowerCase();
        return `'${fn}.${ln}${i}@example.com'`;
    }
    if (n.includes('phone') || n.includes('tel')) {
        return `'+7${String(9000000000 + rnd(i * 7, 999999999)).slice(0,10)}'`;
    }
    if (n.includes('city') || n.includes('location')) {
        return `'${CITIES[rnd(i, CITIES.length)]}'`;
    }
    if (n.includes('country')) {
        const countries = ['Russia','USA','Germany','France','UK','Japan','China','Brazil'];
        return `'${countries[rnd(i, countries.length)]}'`;
    }
    if (n.includes('status') || n.includes('state')) {
        return `'${STATUSES[rnd(i, STATUSES.length)]}'`;
    }
    if (n.includes('category') || n.includes('type') || n.includes('kind')) {
        return `'${CATEGORIES[rnd(i, CATEGORIES.length)]}'`;
    }
    if (n.includes('dept') || n.includes('department') || n.includes('division')) {
        return `'${DEPTS[rnd(i, DEPTS.length)]}'`;
    }
    if (n.includes('title') || n.includes('subject') || n.includes('topic')) {
        const w1 = LOREM_WORDS[rnd(i, LOREM_WORDS.length)];
        const w2 = LOREM_WORDS[rnd2(i, LOREM_WORDS.length)];
        return `'${w1.charAt(0).toUpperCase() + w1.slice(1)} ${w2}'`;
    }
    if (n.includes('description') || n.includes('comment') || n.includes('note') || n.includes('text')) {
        const words = Array.from({length: 5 + rnd(i, 8)}, (_, k) =>
            LOREM_WORDS[rnd(i + k, LOREM_WORDS.length)]);
        return `'${words.join(' ')}'`;
    }
    if (n.includes('url') || n.includes('link') || n.includes('website')) {
        return `'https://example.com/${LOREM_WORDS[rnd(i, LOREM_WORDS.length)]}/${i}'`;
    }
    if (n.includes('code') || n.includes('sku') || n.includes('ref')) {
        return `'${LOREM_WORDS[rnd(i, LOREM_WORDS.length)].toUpperCase().slice(0,3)}-${String(1000 + i).padStart(6,'0')}'`;
    }
    if (n.includes('color') || n.includes('colour')) {
        const colors = ['Red','Blue','Green','Yellow','Black','White','Purple','Orange'];
        return `'${colors[rnd(i, colors.length)]}'`;
    }

    // Generic fallback: "value_N"
    return `'${n}_${i}'`;
}
