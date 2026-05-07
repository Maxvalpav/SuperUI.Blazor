// sg-rag.js — SuperUI RAG JS Bridge (ESM)
// Manages multiple RAG instances keyed by instanceId.

// ── Instance registry ─────────────────────────────────────────────────────────
const _instances = new Map();

// ── Script loader ─────────────────────────────────────────────────────────────
const _loaded = new Set();
function _loadScript(url) {
  if (_loaded.has(url)) return Promise.resolve();
  return new Promise((resolve, reject) => {
    const el = document.createElement('script');
    el.src = url;
    el.onload = () => { _loaded.add(url); resolve(); };
    el.onerror = () => reject(new Error(`Failed to load script: ${url}`));
    document.head.appendChild(el);
  });
}

// ── Vendor module cache ───────────────────────────────────────────────────────
const _moduleCache = new Map();
async function _importModule(url) {
  if (_moduleCache.has(url)) return _moduleCache.get(url);
  const mod = await import(/* @vite-ignore */ url);
  _moduleCache.set(url, mod);
  return mod;
}

// ── Utility helpers ───────────────────────────────────────────────────────────
function _uuid() {
  return crypto.randomUUID ? crypto.randomUUID()
    : 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
        const r = Math.random() * 16 | 0;
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
      });
}

function _now() { return new Date().toISOString(); }

function _cosine(a, b) {
  let dot = 0, na = 0, nb = 0;
  for (let i = 0; i < a.length; i++) {
    dot += a[i] * b[i];
    na  += a[i] * a[i];
    nb  += b[i] * b[i];
  }
  const denom = Math.sqrt(na) * Math.sqrt(nb);
  return denom === 0 ? 0 : dot / denom;
}

// ── BM25 implementation ──────────────────────────────────────────────────────
class BM25 {
  constructor(k1 = 1.5, b = 0.75) {
    this.k1 = k1;
    this.b = b;
    this.documents = [];
    this.avgDocLen = 0;
    this.docFreq = new Map(); // term -> number of docs containing term
    this.termFreq = []; // doc index -> map of term -> freq
  }

  fit(documents) {
    this.documents = documents.map(doc => doc.toLowerCase());
    const tokenized = this.documents.map(doc => this._tokenize(doc));
    this.termFreq = tokenized.map(tokens => {
      const freq = new Map();
      for (const token of tokens) {
        freq.set(token, (freq.get(token) || 0) + 1);
      }
      return freq;
    });

    // Calculate docFreq
    this.docFreq = new Map();
    for (const freqMap of this.termFreq) {
      for (const term of freqMap.keys()) {
        this.docFreq.set(term, (this.docFreq.get(term) || 0) + 1);
      }
    }

    // Calculate avgDocLen
    const totalLen = tokenized.reduce((sum, tokens) => sum + tokens.length, 0);
    this.avgDocLen = totalLen / this.documents.length;
  }

  _tokenize(text) {
    return text.toLowerCase().match(/\w+/g) || [];
  }

  score(query) {
    const queryTokens = this._tokenize(query);
    const scores = [];
    const numDocs = this.documents.length;

    for (let i = 0; i < numDocs; i++) {
      let score = 0;
      const docLen = this.termFreq[i].size; // approximate
      const freqMap = this.termFreq[i];

      for (const term of queryTokens) {
        const df = this.docFreq.get(term) || 0;
        if (df === 0) continue;

        const tf = freqMap.get(term) || 0;
        const idf = Math.log((numDocs - df + 0.5) / (df + 0.5) + 1);
        const numerator = tf * (this.k1 + 1);
        const denominator = tf + this.k1 * (1 - this.b + this.b * (docLen / this.avgDocLen));
        score += idf * (numerator / denominator);
      }
      scores.push(score);
    }
    return scores;
  }
}

// ── Reciprocal Rank Fusion ────────────────────────────────────────────────────
function _reciprocalRankFusion(rankings, k = 60) {
  const scores = new Map();
  for (const ranking of rankings) {
    ranking.forEach((item, rank) => {
      const id = item.id;
      const current = scores.get(id) || 0;
      scores.set(id, current + 1 / (k + rank + 1));
    });
  }
  // Sort by score descending
  return Array.from(scores.entries())
    .sort((a, b) => b[1] - a[1])
    .map(([id, score]) => ({ id, score }));
}

function _base64ToArrayBuffer(b64) {
  const bin = atob(b64);
  const buf = new ArrayBuffer(bin.length);
  const view = new Uint8Array(buf);
  for (let i = 0; i < bin.length; i++) view[i] = bin.charCodeAt(i);
  return buf;
}

function _arrayBufferToBase64(buf) {
  const bytes = new Uint8Array(buf);
  let bin = '';
  for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
  return btoa(bin);
}

function _float32ToBase64(arr) {
  return _arrayBufferToBase64(arr.buffer);
}

function _base64ToFloat32(b64) {
  return new Float32Array(_base64ToArrayBuffer(b64));
}

// ── IndexedDB setup ───────────────────────────────────────────────────────────
async function _openDb(dbName) {
  await _loadScript('https://cdn.jsdelivr.net/npm/idb@8.0.0/build/umd.js');
  const idb = window.idb;
  return idb.openDB(dbName, 1, {
    upgrade(db) {
      if (!db.objectStoreNames.contains('collections')) {
        db.createObjectStore('collections', { keyPath: 'name' });
      }
      if (!db.objectStoreNames.contains('documents')) {
        const docs = db.createObjectStore('documents', { keyPath: 'id' });
        docs.createIndex('byCollection', 'collection');
        docs.createIndex('byCreatedAt',  'createdAt');
      }
      if (!db.objectStoreNames.contains('chunks')) {
        const chunks = db.createObjectStore('chunks', { keyPath: 'id' });
        chunks.createIndex('byDocument',   'documentId');
        chunks.createIndex('byCollection', 'collection');
      }
      if (!db.objectStoreNames.contains('vectors')) {
        const vecs = db.createObjectStore('vectors', { keyPath: 'chunkId' });
        vecs.createIndex('byCollection', 'collection');
      }
      if (!db.objectStoreNames.contains('settings')) {
        db.createObjectStore('settings', { keyPath: 'key' });
      }
      if (!db.objectStoreNames.contains('snapshots')) {
        const snaps = db.createObjectStore('snapshots', { keyPath: 'id' });
        snaps.createIndex('byCreatedAt', 'createdAt');
        snaps.createIndex('byKind',      'kind');
      }
      if (!db.objectStoreNames.contains('chats')) {
        const chats = db.createObjectStore('chats', { keyPath: 'id' });
        chats.createIndex('byCollection', 'collection');
        chats.createIndex('byCreatedAt',  'createdAt');
      }
    }
  });
}

// ── In-memory vector index ────────────────────────────────────────────────────
async function _buildIndex(inst, collection, useHnsw = false) {
  const db = inst.idb;
  if (!db) return;
  
  // Get all chunks and vectors
  const allChunks = await db.getAllFromIndex('chunks', 'byCollection', collection);
  const allVecs = await db.getAllFromIndex('vectors', 'byCollection', collection);
  
  if (!allVecs.length) {
    inst.index[collection] = { matrix: new Float32Array(0), ids: [], dim: 0, bm25: null, chunkTexts: [], hnsw: null };
    return;
  }
  
  const dim = allVecs[0].vector.length;
  const matrix = new Float32Array(allVecs.length * dim);
  const ids = [];
  const chunkTexts = [];
  
  // Create a map for quick lookup
  const vecMap = new Map(allVecs.map(v => [v.chunkId, v]));
  
  for (const chunk of allChunks) {
    const vec = vecMap.get(chunk.id);
    if (vec) {
      const i = ids.length;
      matrix.set(vec.vector, i * dim);
      ids.push(chunk.id);
      chunkTexts.push(chunk.text);
    }
  }
  
  // Build BM25 index
  const bm25 = new BM25();
  bm25.fit(chunkTexts);
  
  let hnsw = null;
  if (useHnsw && ids.length > 100000) {
    try {
      // Load hnswlib-wasm if available
      const HNSWLib = await _importModule('https://cdn.jsdelivr.net/npm/hnswlib-wasm@0.0.7/dist/hnswlib-wasm.js');
      hnsw = new HNSWLib.HierarchicalNSW('l2', dim);
      hnsw.initIndex(ids.length, 16, 200, 100);
      for (let i = 0; i < ids.length; i++) {
        hnsw.addPoint(matrix.subarray(i * dim, (i + 1) * dim), i);
      }
    } catch (err) {
      console.warn('[sg-rag] Failed to load HNSW index, falling back to linear scan:', err);
      hnsw = null;
    }
  }
  
  inst.index[collection] = { matrix, ids, dim, bm25, chunkTexts, hnsw };
}

// ── HNSW search ─────────────────────────────────────────────────────────────
function _searchHnsw(inst, collection, queryVec, topK) {
  const idx = inst.index[collection];
  if (!idx || !idx.hnsw) return null;
  
  const result = idx.hnsw.searchKnn(queryVec, topK);
  return result.neighbors.map((neighbor, i) => ({
    id: idx.ids[neighbor],
    score: 1 - result.distances[i] // convert distance to similarity score
  }));
}

function _searchIndex(inst, collection, queryVec, topK, minScore) {
  const idx = inst.index[collection];
  if (!idx || idx.ids.length === 0) return [];
  
  // Try HNSW first if available
  const hnswHits = _searchHnsw(inst, collection, queryVec, topK);
  if (hnswHits) {
    return hnswHits.filter(hit => hit.score >= minScore);
  }
  
  // Fall back to linear scan
  const { matrix, ids, dim } = idx;
  const scores = [];
  for (let i = 0; i < ids.length; i++) {
    const vec = matrix.subarray(i * dim, (i + 1) * dim);
    const score = _cosine(queryVec, vec);
    if (score >= minScore) scores.push({ id: ids[i], score });
  }
  scores.sort((a, b) => b.score - a.score);
  return scores.slice(0, topK);
}

// ── Embedding ─────────────────────────────────────────────────────────────────
async function _embed(inst, texts) {
  if (!inst.embeddingPipeline) throw new Error('Embedding model not loaded');
  const output = await inst.embeddingPipeline(texts, { pooling: 'mean', normalize: true });
  // output.data is a flat Float32Array; output.dims = [batchSize, dim]
  const dim = output.dims[1];
  const result = [];
  for (let i = 0; i < texts.length; i++) {
    result.push(Array.from(output.data.slice(i * dim, (i + 1) * dim)));
  }
  return result;
}

// ── Chunking strategies ───────────────────────────────────────────────────────
function _chunkCharacters(text, chunkSize, overlap) {
  const chunks = [];
  const step = chunkSize - overlap;
  for (let start = 0; start < text.length; start += step) {
    chunks.push(text.slice(start, start + chunkSize));
    if (start + chunkSize >= text.length) break;
  }
  return chunks;
}

function _chunkSentences(text, chunkSize, overlap) {
  const sentences = text.split(/(?<=[.!?])\s+/).filter(s => s.trim());
  const chunks = [];
  let current = '';
  for (const sent of sentences) {
    if (current.length + sent.length + 1 > chunkSize && current.length > 0) {
      chunks.push(current.trim());
      // carry overlap tail
      const words = current.split(' ');
      const tail = words.slice(-Math.ceil(overlap / 6)).join(' ');
      current = tail + ' ' + sent;
    } else {
      current = current ? current + ' ' + sent : sent;
    }
  }
  if (current.trim()) chunks.push(current.trim());
  return chunks;
}

function _chunkRecursive(text, chunkSize, overlap, separators) {
  if (text.length <= chunkSize) return [text];
  const sep = separators[0];
  const rest = separators.slice(1);
  if (!sep && sep !== '') return _chunkCharacters(text, chunkSize, overlap);
  const parts = sep === '' ? text.split('') : text.split(sep);
  const chunks = [];
  let current = '';
  for (const part of parts) {
    const candidate = current ? current + sep + part : part;
    if (candidate.length <= chunkSize) {
      current = candidate;
    } else {
      if (current) {
        if (current.length > chunkSize && rest.length > 0) {
          chunks.push(..._chunkRecursive(current, chunkSize, overlap, rest));
        } else {
          chunks.push(current);
        }
        // overlap: keep tail of current
        const tail = current.slice(-overlap);
        current = tail ? tail + sep + part : part;
      } else {
        if (part.length > chunkSize && rest.length > 0) {
          chunks.push(..._chunkRecursive(part, chunkSize, overlap, rest));
        } else {
          current = part;
        }
      }
    }
  }
  if (current.trim()) {
    if (current.length > chunkSize && rest.length > 0) {
      chunks.push(..._chunkRecursive(current, chunkSize, overlap, rest));
    } else {
      chunks.push(current);
    }
  }
  // merge small adjacent pieces
  const merged = [];
  let acc = '';
  for (const c of chunks) {
    if (acc.length + c.length + 1 <= chunkSize) {
      acc = acc ? acc + sep + c : c;
    } else {
      if (acc) merged.push(acc);
      acc = c;
    }
  }
  if (acc) merged.push(acc);
  return merged.filter(c => c.trim());
}

// ── Code-aware chunker ────────────────────────────────────────────────────────
// Splits source code into logical units: top-level functions, classes, methods.
// Falls back to recursive chunking for languages without specific patterns.
// Each chunk gets metadata: {language, symbol, startLine, endLine}.

const _CODE_LANG_MAP = {
  // C-family
  '.c':    'c',      '.h':    'c',
  '.cpp':  'cpp',    '.cc':   'cpp',    '.cxx': 'cpp',   '.hpp': 'cpp',
  '.cs':   'csharp', '.java': 'java',
  // Web
  '.js':   'javascript', '.mjs': 'javascript', '.cjs': 'javascript',
  '.ts':   'typescript', '.tsx': 'typescript', '.jsx': 'javascript',
  '.vue':  'vue',    '.svelte': 'svelte',
  // Python / Ruby / Go / Rust / Swift / Kotlin
  '.py':   'python', '.pyw': 'python',
  '.rb':   'ruby',
  '.go':   'go',
  '.rs':   'rust',
  '.swift':'swift',
  '.kt':   'kotlin', '.kts': 'kotlin',
  // Shell / config
  '.sh':   'bash',   '.bash': 'bash',   '.zsh': 'bash',
  '.ps1':  'powershell',
  '.sql':  'sql',
  '.r':    'r',      '.R':   'r',
  '.php':  'php',
  '.lua':  'lua',
  '.dart': 'dart',
  '.scala':'scala',
  '.ex':   'elixir', '.exs': 'elixir',
  '.hs':   'haskell',
  '.ml':   'ocaml',  '.mli': 'ocaml',
  '.fs':   'fsharp', '.fsx': 'fsharp',
  '.razor':'razor',  '.cshtml': 'razor',
  '.yaml': 'yaml',   '.yml': 'yaml',
  '.toml': 'toml',   '.ini': 'ini',
  '.xml':  'xml',    '.xaml': 'xml',
};

function _detectLanguage(fileName) {
  const ext = ('.' + fileName.split('.').pop()).toLowerCase();
  return _CODE_LANG_MAP[ext] || 'text';
}

// Language-specific top-level block patterns
const _CODE_PATTERNS = {
  python: /^(async\s+def\s+\w+|def\s+\w+|class\s+\w+)/m,
  javascript: /^(export\s+(default\s+)?(async\s+)?function\s+\w+|export\s+(default\s+)?class\s+\w+|(async\s+)?function\s+\w+|class\s+\w+|const\s+\w+\s*=\s*(async\s+)?\(|module\.exports)/m,
  typescript: /^(export\s+(default\s+)?(async\s+)?function\s+\w+|export\s+(default\s+)?class\s+\w+|(async\s+)?function\s+\w+|class\s+\w+|interface\s+\w+|type\s+\w+\s*=|const\s+\w+\s*=\s*(async\s+)?\()/m,
  csharp: /^(\s*(public|private|protected|internal|static|async|override|virtual|abstract)\s+.*\s+(class|interface|enum|struct|record|void|Task|string|int|bool|var|List|Dictionary)\s+\w+)/m,
  java: /^(\s*(public|private|protected|static|final|abstract)\s+.*\s+(class|interface|enum|void|String|int|boolean)\s+\w+)/m,
  go: /^(func\s+(\(\w+\s+\*?\w+\)\s+)?\w+\s*\(|type\s+\w+\s+(struct|interface))/m,
  rust: /^(pub\s+)?(fn\s+\w+|struct\s+\w+|enum\s+\w+|impl\s+\w+|trait\s+\w+|mod\s+\w+)/m,
  cpp: /^(\s*(template\s*<[^>]*>\s*)?(class|struct|namespace|void|int|bool|auto|inline)\s+\w+)/m,
  php: /^(\s*(public|private|protected|static|abstract|final)?\s*(function\s+\w+|class\s+\w+|interface\s+\w+|trait\s+\w+))/m,
  ruby: /^(\s*(def\s+\w+|class\s+\w+|module\s+\w+))/m,
  swift: /^(\s*(public|private|internal|open|fileprivate)?\s*(func\s+\w+|class\s+\w+|struct\s+\w+|enum\s+\w+|protocol\s+\w+|extension\s+\w+))/m,
  kotlin: /^(\s*(fun\s+\w+|class\s+\w+|object\s+\w+|interface\s+\w+|data\s+class\s+\w+))/m,
};

function _chunkCode(text, lang, chunkSize, overlap) {
  const lines = text.split('\n');
  const pattern = _CODE_PATTERNS[lang];

  if (!pattern) {
    // No language-specific pattern — use recursive with code separators
    return _chunkRecursive(text, chunkSize, overlap, ['\n\n', '\n', ' ', '']);
  }

  // Find top-level block boundaries by scanning for pattern matches at low indent
  const blockStarts = [];
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const indent = line.match(/^(\s*)/)[1].length;
    // Only consider top-level or near-top-level declarations (indent <= 2 spaces / 1 tab)
    if (indent <= 2 && pattern.test(line)) {
      blockStarts.push(i);
    }
  }

  if (blockStarts.length === 0) {
    // No top-level blocks found — fall back to recursive
    return _chunkRecursive(text, chunkSize, overlap, ['\n\n', '\n', ' ', '']);
  }

  // Build chunks from block boundaries
  const chunks = [];
  for (let b = 0; b < blockStarts.length; b++) {
    const start = blockStarts[b];
    const end   = b + 1 < blockStarts.length ? blockStarts[b + 1] : lines.length;
    const blockText = lines.slice(start, end).join('\n').trim();

    if (!blockText) continue;

    if (blockText.length <= chunkSize) {
      chunks.push({ text: blockText, startLine: start + 1, endLine: end });
    } else {
      // Block too large — sub-chunk it recursively
      const subChunks = _chunkRecursive(blockText, chunkSize, overlap, ['\n\n', '\n', ' ', '']);
      let lineOffset = start;
      for (const sub of subChunks) {
        const subLines = sub.split('\n').length;
        chunks.push({ text: sub, startLine: lineOffset + 1, endLine: lineOffset + subLines });
        lineOffset += subLines;
      }
    }
  }

  // If there's content before the first block (imports, comments, etc.)
  if (blockStarts[0] > 0) {
    const preamble = lines.slice(0, blockStarts[0]).join('\n').trim();
    if (preamble) {
      chunks.unshift({ text: preamble, startLine: 1, endLine: blockStarts[0] });
    }
  }

  return chunks.filter(c => c.text.trim());
}

async function _chunkSemantic(inst, text, chunkSize, threshold) {
  const sentences = text.split(/(?<=[.!?])\s+/).filter(s => s.trim());
  if (sentences.length <= 1) return [text];
  const embeddings = await _embed(inst, sentences);
  const chunks = [];
  let current = sentences[0];
  for (let i = 1; i < sentences.length; i++) {
    const sim = _cosine(embeddings[i - 1], embeddings[i]);
    if (sim < threshold || current.length + sentences[i].length + 1 > chunkSize) {
      chunks.push(current.trim());
      current = sentences[i];
    } else {
      current += ' ' + sentences[i];
    }
  }
  if (current.trim()) chunks.push(current.trim());
  return chunks;
}

async function _applyChunking(inst, text, opts) {
  const strategy  = (opts.strategy  || 'Recursive').toLowerCase();
  const chunkSize = opts.chunkSize  || 512;
  const overlap   = opts.overlap    || 64;
  const seps      = opts.separators || ['\n\n', '\n', '. ', ' ', ''];
  const threshold = opts.semanticSimilarityThreshold || 0.5;
  const lang      = opts.codeLanguage || 'text';

  switch (strategy) {
    case 'characters': return _chunkCharacters(text, chunkSize, overlap);
    case 'sentences':  return _chunkSentences(text, chunkSize, overlap);
    case 'semantic':   return _chunkSemantic(inst, text, chunkSize, threshold);
    case 'code': {
      // Returns array of {text, startLine, endLine} objects
      const codeChunks = _chunkCode(text, lang, chunkSize, overlap);
      // Normalize to plain strings for the embedding pipeline;
      // metadata is stored separately in _ingestTextInternal via opts.codeChunkMeta
      opts._codeChunkMeta = codeChunks.map(c => ({
        startLine: c.startLine || 0,
        endLine:   c.endLine   || 0,
        language:  lang,
      }));
      return codeChunks.map(c => typeof c === 'string' ? c : c.text);
    }
    case 'recursive':
    default:           return _chunkRecursive(text, chunkSize, overlap, seps);
  }
}

// ── Document parsers ──────────────────────────────────────────────────────────
async function _parsePdf(arrayBuffer, sources) {
  const pdfSrc = sources.pdfJsScript || 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.4.168/build/pdf.min.mjs';
  const workerSrc = sources.pdfJsWorker || 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.4.168/build/pdf.worker.min.mjs';
  const pdfjsLib = await _importModule(pdfSrc);
  pdfjsLib.GlobalWorkerOptions.workerSrc = workerSrc;
  const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
  let text = '';
  for (let i = 1; i <= pdf.numPages; i++) {
    const page = await pdf.getPage(i);
    const content = await page.getTextContent();
    text += content.items.map(item => item.str).join(' ') + '\n';
  }
  return text;
}

async function _parseDocx(arrayBuffer, sources) {
  const mammothSrc = sources.mammothScript || 'https://cdn.jsdelivr.net/npm/mammoth@1.8.0/mammoth.browser.min.js';
  await _loadScript(mammothSrc);
  const result = await window.mammoth.extractRawText({ arrayBuffer });
  return result.value;
}

async function _parseMd(text, sources) {
  const markedSrc = sources.markedScript || 'https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js';
  await _loadScript(markedSrc);
  const html = window.marked.parse(text);
  const div = document.createElement('div');
  div.innerHTML = html;
  return div.textContent || div.innerText || '';
}

function _parseHtml(text) {
  const parser = new DOMParser();
  const doc = parser.parseFromString(text, 'text/html');
  return doc.body.textContent || '';
}

async function _parseDocument(arrayBuffer, fileName, mimeType, sources) {
  const ext = (fileName || '').split('.').pop().toLowerCase();
  const mime = (mimeType || '').toLowerCase();

  if (mime.includes('pdf') || ext === 'pdf') {
    return { text: await _parsePdf(arrayBuffer, sources), format: 'Pdf', language: null };
  }
  if (mime.includes('wordprocessingml') || ext === 'docx') {
    return { text: await _parseDocx(arrayBuffer, sources), format: 'Docx', language: null };
  }
  const decoder = new TextDecoder('utf-8');
  const text = decoder.decode(arrayBuffer);

  if (mime.includes('markdown') || ext === 'md') {
    return { text: await _parseMd(text, sources), format: 'Md', language: null };
  }
  if (mime.includes('html') || ext === 'html' || ext === 'htm') {
    return { text: _parseHtml(text), format: 'Html', language: null };
  }
  if (ext === 'json') {
    return { text, format: 'Json', language: null };
  }

  // Check if it's a known code extension
  const lang = _detectLanguage(fileName || '');
  if (lang !== 'text' || _CODE_LANG_MAP['.' + ext]) {
    return { text, format: 'Code', language: lang };
  }

  return { text, format: 'Txt', language: null };
}

// ── RAG prompt builders ───────────────────────────────────────────────────────
function _buildRagPrompt(question, contextBlocks, systemPrompt, mode) {
  const modeStr = (mode || 'Strict').toLowerCase();
  let sysMsg = systemPrompt || '';

  if (modeStr === 'strict') {
    sysMsg = sysMsg || 'You are a helpful assistant. Answer ONLY using the provided context. If the context does not contain enough information, respond with "No information found."';
  } else if (modeStr === 'hybrid') {
    sysMsg = sysMsg || 'You are a helpful assistant. Prefer the provided context when answering. You may supplement with general knowledge. Cite context sources using [#n] notation.';
  } else {
    sysMsg = sysMsg || 'You are a helpful assistant. Use the provided context as supplementary information. You may answer freely.';
  }

  const contextText = contextBlocks.map((b, i) => `[#${i + 1}] ${b.title}: ${b.text}`).join('\n\n');
  const userMsg = contextBlocks.length > 0
    ? `Context:\n${contextText}\n\nQuestion: ${question}`
    : question;

  return { sysMsg, userMsg };
}

// ── Citation extraction ───────────────────────────────────────────────────────
// Returns array of contextBlocks that were cited (legacy, used by ask())
function _extractCitations(answer, contextBlocks) {
  const cited = new Set();
  const re = /\[#(\d+)\]/g;
  let m;
  while ((m = re.exec(answer)) !== null) {
    const idx = parseInt(m[1], 10) - 1;
    if (idx >= 0 && idx < contextBlocks.length) cited.add(idx);
  }
  return Array.from(cited).map(i => contextBlocks[i]);
}

// Returns a Set of chunkIds cited in the answer, resolved via citationMap (1-based → chunkId)
function _extractCitationIds(answer, citationMap) {
  const cited = new Set();
  const re = /\[#(\d+)\]/g;
  let m;
  while ((m = re.exec(answer)) !== null) {
    const num = parseInt(m[1], 10);
    const chunkId = citationMap.get(num);
    if (chunkId) cited.add(chunkId);
  }
  return cited;
}


// ── Safe WebLLM unload helper ─────────────────────────────────────────────────
// WebLLM 0.2.83 throws AbortError ("Buffer was unmapped before mapping was resolved")
// during dispose when pending WebGPU mapAsync operations are cancelled. This is
// expected behaviour — the GPU buffers are destroyed before async mapping completes.
// We swallow AbortError and DOMException silently; re-throw anything else.
async function _safeUnloadLlm(engine) {
  if (!engine || typeof engine.unload !== 'function') return;
  try {
    await engine.unload();
  } catch (err) {
    const name = err?.name || '';
    // AbortError and DOMException are expected from WebGPU buffer teardown
    if (name === 'AbortError' || name === 'DOMException' || err instanceof DOMException) return;
    // Also swallow the specific message pattern from WebGPU
    const msg = String(err?.message || '');
    if (msg.includes('mapAsync') || msg.includes('unmapped') || msg.includes('GPUBuffer')) return;
    // Anything else is unexpected — log but don't rethrow to avoid breaking dispose chain
    console.warn('[sg-rag] LLM unload warning:', err);
  }
}
export async function init(dotnetRef, instanceId, options) {
  if (_instances.has(instanceId)) return;

  // Suppress WebGPU buffer teardown AbortErrors that WebLLM emits as unhandled
  // rejections during model unload. These are benign and expected behaviour when
  // GPU buffers are destroyed while pending mapAsync operations are in flight.
  if (!window.__sgRagGpuErrorHandlerInstalled) {
    window.__sgRagGpuErrorHandlerInstalled = true;
    window.addEventListener('unhandledrejection', (event) => {
      const err = event.reason;
      if (!err) return;
      const msg = String(err?.message || err || '');
      if (
        msg.includes('mapAsync') ||
        msg.includes('unmapped') ||
        msg.includes('GPUBuffer') ||
        err?.name === 'AbortError'
      ) {
        event.preventDefault();
      }
    });
  }

  const opts = options || {};
  const dbName = opts.indexedDbName || 'sg-rag';
  const sources = opts.sources || {};

  // Pre-load idb script using the configured URL
  const idbSrc = sources.idbScript || 'https://cdn.jsdelivr.net/npm/idb@8.0.0/build/umd.js';
  await _loadScript(idbSrc);

  let idb = null;
  if (opts.persistToIndexedDb !== false) {
    idb = await _openDb(dbName);
    // Ensure default collection exists
    const defCol = opts.defaultCollection || 'default';
    const existing = await idb.get('collections', defCol);
    if (!existing) {
      await idb.put('collections', {
        name: defCol,
        vectorDim: 0,
        embeddingModel: null,
        createdAt: _now(),
        docCount: 0,
        chunkCount: 0,
      });
    }
  }

  _instances.set(instanceId, {
    dotnetRef,
    options: opts,
    embeddingPipeline: null,
    rerankerPipeline: null,
    llmEngine: null,
    idb,
    index: {},       // collection -> { matrix, ids, dim }
    sources,
  });
}

// ── Exported: dispose ─────────────────────────────────────────────────────────
export async function dispose(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst) return;
  if (inst.llmEngine) {
    await _safeUnloadLlm(inst.llmEngine);
  }
  if (inst.idb) {
    try { inst.idb.close(); } catch (_) {}
  }
  _instances.delete(instanceId);
}

// ── Exported: checkWebGpu ─────────────────────────────────────────────────────
export function checkWebGpu() {
  const available = !!(navigator.gpu);
  return { available, adapter: available ? 'gpu' : null };
}

// ── Exported: loadEmbeddingModel ──────────────────────────────────────────────
export async function loadEmbeddingModel(instanceId, kind, modelId, dtype) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);

  const src = inst.sources.transformersScript ||
    'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.17.2/dist/transformers.min.js';

  const { pipeline, env } = await _importModule(src);
  env.allowLocalModels = false;

  const progressCallback = (progress) => {
    try {
      inst.dotnetRef.invokeMethodAsync('OnEmbeddingProgressCallback', {
        stage:      progress.status || '',
        loaded:     progress.loaded || 0,
        total:      progress.total  || 0,
        percent:    progress.total  ? (progress.loaded / progress.total) * 100 : 0,
        file:       progress.file   || null,
        isComplete: progress.status === 'done',
      });
    } catch (_) {}
  };

  inst.embeddingPipeline = await pipeline('feature-extraction', modelId, {
    dtype: dtype || 'q8',
    progress_callback: progressCallback,
  });

  try {
    inst.dotnetRef.invokeMethodAsync('OnEmbeddingReadyCallback', modelId);
  } catch (_) {}
}

// ── Exported: loadLlm ─────────────────────────────────────────────────────────
export async function loadLlm(instanceId, provider, modelId, opts) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);

  const providerLower = (provider || '').toLowerCase();

  if (providerLower === 'webllm') {
    const src = inst.sources.webLlmScript ||
      'https://cdn.jsdelivr.net/npm/@mlc-ai/web-llm@0.2.83/lib/index.js';
    const webllm = await _importModule(src);

    // Validate model_id against the built-in list to give a clear error early
    const knownIds = (webllm.prebuiltAppConfig?.model_list || []).map(m => m.model_id);
    if (knownIds.length > 0 && !knownIds.includes(modelId)) {
      const suggestion = knownIds.find(id => id.toLowerCase().includes('llama-3.2-1b')) ||
                         knownIds.find(id => id.toLowerCase().includes('smollm')) ||
                         knownIds[0];
      throw new Error(
        `Unknown WebLLM model_id: "${modelId}". ` +
        `Valid examples: "Llama-3.2-1B-Instruct-q4f16_1-MLC", "SmolLM2-1.7B-Instruct-q4f16_1-MLC". ` +
        (suggestion ? `Nearest match: "${suggestion}".` : '')
      );
    }

    const progressCallback = (report) => {
      try {
        inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
          stage:      report.text || '',
          loaded:     report.progress || 0,
          total:      1,
          percent:    (report.progress || 0) * 100,
          file:       null,
          isComplete: report.progress >= 1,
        });
      } catch (_) {}
    };

    inst.llmEngine = await webllm.CreateMLCEngine(modelId, {
      initProgressCallback: progressCallback,
    });
  } else if (providerLower === 'openaicompatible') {
    // Store config; actual calls use fetch
    // opts.apiKey / opts.baseUrl override the values set in init()
    inst.llmEngine = {
      kind:         'openai',
      baseUrl:      opts?.baseUrl  || inst.options.openAiBaseUrl || 'https://api.openai.com/v1',
      apiKey:       opts?.apiKey   || inst.options.openAiApiKey  || '',
      model:        inst.options.openAiModel || modelId || 'gpt-4o-mini',
      extraHeaders: {},
    };
    try {
      inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
        stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true,
      });
    } catch (_) {}
  } else if (providerLower === 'openrouter') {
    // OpenRouter is OpenAI-compatible with base URL https://openrouter.ai/api/v1
    // HTTP-Referer and X-Title are optional attribution headers.
    // HTTP headers only allow ISO-8859-1 (Latin-1) — we sanitize both values.
    const extraHeaders = {};
    const referer = inst.options.openRouterReferer || window.location.origin;
    // Only include X-Title if explicitly set by the consumer (avoid document.title
    // which may contain non-Latin-1 characters like Cyrillic, CJK, em-dash, etc.)
    const rawTitle = inst.options.openRouterTitle || '';
    const title = rawTitle.replace(/[^\x20-\x7E]/g, '').trim(); // ASCII printable only

    if (referer) extraHeaders['HTTP-Referer'] = referer;
    if (title)   extraHeaders['X-Title']      = title;

    inst.llmEngine = {
      kind:         'openai',
      baseUrl:      'https://openrouter.ai/api/v1',
      apiKey:       opts?.apiKey || inst.options.openAiApiKey || '',
      model:        modelId || 'openrouter/free',
      extraHeaders,
    };
    try {
      inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
        stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true,
      });
    } catch (_) {}
  }
  // 'none' provider: no-op
}

// ── Exported: unloadEmbedding ─────────────────────────────────────────────────
export async function unloadEmbedding(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst) return;
  inst.embeddingPipeline = null;
}

// ── Exported: unloadLlm ───────────────────────────────────────────────────────
export async function unloadLlm(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst) return;
  await _safeUnloadLlm(inst.llmEngine);
  inst.llmEngine = null;
}

// ── Exported: loadReranker ──────────────────────────────────────────────────────
export async function loadReranker(instanceId, modelId = 'Xenova/ms-marco-MiniLM-L-6-v2') {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);

  const src = inst.sources.transformersScript ||
    'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.17.2/dist/transformers.min.js';

  const { pipeline, env } = await _importModule(src);
  env.allowLocalModels = false;

  const progressCallback = (progress) => {
    try {
      inst.dotnetRef.invokeMethodAsync('OnEmbeddingProgressCallback', {
        stage:      progress.status || '',
        loaded:     progress.loaded || 0,
        total:      progress.total  || 0,
        percent:    progress.total  ? (progress.loaded / progress.total) * 100 : 0,
        file:       progress.file   || null,
        isComplete: progress.status === 'done',
      });
    } catch (_) {}
  };

  inst.rerankerPipeline = await pipeline('text-classification', modelId, {
    progress_callback: progressCallback,
  });

  try {
    inst.dotnetRef.invokeMethodAsync('OnEmbeddingReadyCallback', modelId);
  } catch (_) {}
}

// ── Exported: unloadReranker ─────────────────────────────────────────────────
export async function unloadReranker(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst) return;
  inst.rerankerPipeline = null;
}

// ── Exported: rerank ─────────────────────────────────────────────────────────
export async function rerank(instanceId, query, hits, topN = 5) {
  const inst = _instances.get(instanceId);
  if (!inst) return hits;
  if (!inst.rerankerPipeline) return hits.slice(0, topN);

  // Prepare query-chunk pairs
  const pairs = hits.map(hit => [query, hit.chunk.text]);
  const results = await inst.rerankerPipeline(pairs);

  // Sort hits by reranker score (higher is better for ms-marco models)
  const scoredHits = hits.map((hit, i) => ({ hit, score: results[i].score }));
  scoredHits.sort((a, b) => b.score - a.score);

  // Return top N
  return scoredHits.slice(0, topN).map(item => ({
    ...item.hit, score: item.score
  }));
}

// ── Exported: ingestFile ──────────────────────────────────────────────────────
export async function ingestFile(instanceId, base64Data, fileName, mimeType, collection, chunkOpts, docId) {
  const inst = _instances.get(instanceId);
  if (!inst) return { documentId: '', title: fileName, chunkCount: 0, success: false, error: 'Instance not found' };

  try {
    const arrayBuffer = _base64ToArrayBuffer(base64Data);
    const parsed = await _parseDocument(arrayBuffer, fileName, mimeType, inst.sources);
    const text   = typeof parsed === 'string' ? parsed : parsed.text;
    const lang   = parsed.language || null;
    const fmt    = parsed.format   || 'Auto';
    const title  = fileName || 'Untitled';

    // Auto-select Code strategy for code files
    const opts = { ...chunkOpts };
    if (lang && (!opts.strategy || opts.strategy === 'Recursive')) {
      opts.strategy     = 'Code';
      opts.codeLanguage = lang;
    } else if (lang) {
      opts.codeLanguage = lang;
    }

    return _ingestTextInternal(inst, instanceId, title, text, collection, opts, docId, fileName, arrayBuffer.byteLength, lang, fmt);
  } catch (err) {
    return { documentId: docId || '', title: fileName, chunkCount: 0, success: false, error: String(err) };
  }
}

// ── Exported: ingestText ──────────────────────────────────────────────────────
export async function ingestText(instanceId, title, text, format, collection, chunkOpts, docId) {
  const inst = _instances.get(instanceId);
  if (!inst) return { documentId: '', title, chunkCount: 0, success: false, error: 'Instance not found' };

  try {
    let parsedText = text;
    const fmt = (format || 'Txt').toLowerCase();
    if (fmt === 'md') {
      parsedText = await _parseMd(text, inst.sources);
    } else if (fmt === 'html') {
      parsedText = _parseHtml(text);
    }
    return _ingestTextInternal(inst, instanceId, title, parsedText, collection, chunkOpts, docId, title, new TextEncoder().encode(text).length, null, format);
  } catch (err) {
    return { documentId: docId || '', title, chunkCount: 0, success: false, error: String(err) };
  }
}

// ── Internal ingest helper ────────────────────────────────────────────────────
async function _ingestTextInternal(inst, instanceId, title, text, collection, chunkOpts, docId, source, sizeBytes, codeLanguage, docFormat) {
  const col = collection || inst.options.defaultCollection || 'default';
  const id  = docId || _uuid();

  // Check embedding model is loaded
  if (!inst.embeddingPipeline) {
    return {
      documentId: id, title, chunkCount: 0,
      success: false,
      error: 'Embedding model not loaded! Please load the embedding model first in the Setup tab.'
    };
  }

  try {
    inst.dotnetRef.invokeMethodAsync('OnIndexProgressCallback', {
      documentId: id, chunksDone: 0, total: 0, phase: 'chunking',
    });
  } catch (_) {}

  const rawChunks = await _applyChunking(inst, text, chunkOpts || {});
  // Grab code metadata if code strategy was used
  const codeChunkMeta = chunkOpts?._codeChunkMeta || null;

  const chunkTexts = rawChunks.map(c => c.trim()).filter(c => c.length > 0);
  const totalChunks = chunkTexts.length;

  try {
    inst.dotnetRef.invokeMethodAsync('OnIndexProgressCallback', {
      documentId: id, chunksDone: 0, total: totalChunks, phase: 'embedding',
    });
  } catch (_) {}

  // Embed in small batches to report progress
  const embeddings = [];
  const batchSize = 1; // Process one chunk at a time for accurate progress
  for (let i = 0; i < chunkTexts.length; i += batchSize) {
    const batch = chunkTexts.slice(i, i + batchSize);
    const batchEmbeddings = await _embed(inst, batch);
    embeddings.push(...batchEmbeddings);
    
    try {
      inst.dotnetRef.invokeMethodAsync('OnIndexProgressCallback', {
        documentId: id, chunksDone: Math.min(i + batchSize, totalChunks), total: totalChunks, phase: 'embedding',
      });
    } catch (_) {}
  }

  const chunkRecords = chunkTexts.map((chunkText, i) => {
    const meta = {};
    if (codeLanguage) meta.language = codeLanguage;
    if (codeChunkMeta && codeChunkMeta[i]) {
      meta.startLine = String(codeChunkMeta[i].startLine);
      meta.endLine   = String(codeChunkMeta[i].endLine);
    }
    return {
      id:         `${id}_c${i}`,
      documentId: id,
      collection: col,
      index:      i,
      text:       chunkText,
      tokenCount: Math.ceil(chunkText.length / 4),
      metadata:   meta,
    };
  });

  const vectorRecords = embeddings.map((vec, i) => ({
    chunkId:    chunkRecords[i].id,
    collection: col,
    vector:     vec,
    model:      inst.options.embeddingModel || '',
    dim:        vec.length,
  }));

  const docMeta = {};
  if (codeLanguage) docMeta.language = codeLanguage;

  const docRecord = {
    id:         id,
    collection: col,
    title,
    source:     source || title,
    format:     docFormat || 'Auto',
    sizeBytes:  sizeBytes || 0,
    createdAt:  _now(),
    metadata:   docMeta,
    chunkCount: chunkRecords.length,
  };

  if (inst.idb) {
    try {
      inst.dotnetRef.invokeMethodAsync('OnIndexProgressCallback', {
        documentId: id, chunksDone: 0, total: chunkRecords.length, phase: 'persisting',
      });
    } catch (_) {}

    const tx = inst.idb.transaction(['documents', 'chunks', 'vectors', 'collections'], 'readwrite');
    await tx.objectStore('documents').put(docRecord);
    
    for (let i = 0; i < chunkRecords.length; i++) {
      await tx.objectStore('chunks').put(chunkRecords[i]);
      await tx.objectStore('vectors').put(vectorRecords[i]);
      
      try {
        inst.dotnetRef.invokeMethodAsync('OnIndexProgressCallback', {
          documentId: id, chunksDone: i + 1, total: chunkRecords.length, phase: 'persisting',
        });
      } catch (_) {}
    }

    // Update collection metadata
    const colStore = tx.objectStore('collections');
    let colRec = await colStore.get(col);
    if (!colRec) {
      colRec = { name: col, vectorDim: vectorRecords[0]?.dim || 0, embeddingModel: null, createdAt: _now(), docCount: 0, chunkCount: 0 };
    }
    colRec.docCount   = (colRec.docCount   || 0) + 1;
    colRec.chunkCount = (colRec.chunkCount || 0) + chunkRecords.length;
    colRec.vectorDim  = vectorRecords[0]?.dim || colRec.vectorDim;
    await colStore.put(colRec);
    await tx.done;
  }

  // Rebuild in-memory index for this collection
  await _buildIndex(inst, col);

  try {
    inst.dotnetRef.invokeMethodAsync('OnIndexProgressCallback', {
      documentId: id, chunksDone: chunkRecords.length, total: chunkRecords.length, phase: 'done',
    });
  } catch (_) {}

  return { documentId: id, title, chunkCount: chunkRecords.length, success: true, error: null };
}

// ── Exported: previewChunks ───────────────────────────────────────────────────
export async function previewChunks(instanceId, text, chunkOpts) {
  const inst = _instances.get(instanceId);
  if (!inst) return [];
  const rawChunks = await _applyChunking(inst, text, chunkOpts || {});
  return rawChunks.map((t, i) => ({
    id:         `preview_c${i}`,
    documentId: '',
    index:      i,
    text:       t.trim(),
    tokenCount: Math.ceil(t.length / 4),
    metadata:   {},
  }));
}


// ── Exported: listCollections ─────────────────────────────────────────────────
export async function listCollections(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return [];
  const cols = await inst.idb.getAll('collections');
  return cols.map(c => ({
    name:           c.name,
    vectorDim:      c.vectorDim      || 0,
    embeddingModel: c.embeddingModel || null,
    createdAt:      c.createdAt      || _now(),
    docCount:       c.docCount       || 0,
    chunkCount:     c.chunkCount     || 0,
  }));
}

// ── Exported: listDocuments ───────────────────────────────────────────────────
export async function listDocuments(instanceId, collection) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return [];
  const col = collection || inst.options.defaultCollection || 'default';
  const docs = await inst.idb.getAllFromIndex('documents', 'byCollection', col);
  return docs.map(d => ({
    id:         d.id,
    collection: d.collection,
    title:      d.title,
    source:     d.source,
    format:     d.format,
    sizeBytes:  d.sizeBytes  || 0,
    createdAt:  d.createdAt  || '',
    metadata:   d.metadata   || {},
    chunkCount: d.chunkCount || 0,
  }));
}

// ── Exported: getDocument ─────────────────────────────────────────────────────
export async function getDocument(instanceId, docId) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return null;
  const doc = await inst.idb.get('documents', docId);
  if (!doc) return null;
  return {
    id:         doc.id,
    collection: doc.collection,
    title:      doc.title,
    source:     doc.source,
    format:     doc.format,
    sizeBytes:  doc.sizeBytes  || 0,
    createdAt:  doc.createdAt  || '',
    metadata:   doc.metadata   || {},
    chunkCount: doc.chunkCount || 0,
  };
}

// ── Exported: removeDocument ──────────────────────────────────────────────────
export async function removeDocument(instanceId, docId) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return;

  const doc = await inst.idb.get('documents', docId);
  if (!doc) return;

  const chunks = await inst.idb.getAllFromIndex('chunks', 'byDocument', docId);
  const tx = inst.idb.transaction(['documents', 'chunks', 'vectors', 'collections'], 'readwrite');

  await tx.objectStore('documents').delete(docId);
  for (const c of chunks) {
    await tx.objectStore('chunks').delete(c.id);
    await tx.objectStore('vectors').delete(c.id);
  }

  // Update collection counts
  const colStore = tx.objectStore('collections');
  const colRec = await colStore.get(doc.collection);
  if (colRec) {
    colRec.docCount   = Math.max(0, (colRec.docCount   || 0) - 1);
    colRec.chunkCount = Math.max(0, (colRec.chunkCount || 0) - chunks.length);
    await colStore.put(colRec);
  }
  await tx.done;

  // Rebuild index
  await _buildIndex(inst, doc.collection);
}

// ── Exported: reindexDocument ─────────────────────────────────────────────────
export async function reindexDocument(instanceId, docId, chunkOpts) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return;

  const doc = await inst.idb.get('documents', docId);
  if (!doc) throw new Error(`Document ${docId} not found`);

  // Get original text from first chunk (we stored it)
  const oldChunks = await inst.idb.getAllFromIndex('chunks', 'byDocument', docId);
  const fullText = oldChunks.sort((a, b) => a.index - b.index).map(c => c.text).join(' ');

  // Remove old chunks and vectors
  const tx1 = inst.idb.transaction(['chunks', 'vectors'], 'readwrite');
  for (const c of oldChunks) {
    await tx1.objectStore('chunks').delete(c.id);
    await tx1.objectStore('vectors').delete(c.id);
  }
  await tx1.done;

  // Re-ingest with new chunk options
  const newChunks = await _applyChunking(inst, fullText, chunkOpts || {});
  const chunkTexts = newChunks.map(c => c.trim()).filter(c => c.length > 0);
  const embeddings = await _embed(inst, chunkTexts);

  const chunkRecords = chunkTexts.map((text, i) => ({
    id:         `${docId}_c${i}_r${Date.now()}`,
    documentId: docId,
    collection: doc.collection,
    index:      i,
    text,
    tokenCount: Math.ceil(text.length / 4),
    metadata:   {},
  }));

  const vectorRecords = embeddings.map((vec, i) => ({
    chunkId:    chunkRecords[i].id,
    collection: doc.collection,
    vector:     vec,
    dim:        vec.length,
  }));

  const tx2 = inst.idb.transaction(['documents', 'chunks', 'vectors', 'collections'], 'readwrite');
  doc.chunkCount = chunkRecords.length;
  await tx2.objectStore('documents').put(doc);
  for (const c of chunkRecords) await tx2.objectStore('chunks').put(c);
  for (const v of vectorRecords) await tx2.objectStore('vectors').put(v);

  const colStore = tx2.objectStore('collections');
  const colRec = await colStore.get(doc.collection);
  if (colRec) {
    colRec.chunkCount = Math.max(0, (colRec.chunkCount || 0) - oldChunks.length) + chunkRecords.length;
    await colStore.put(colRec);
  }
  await tx2.done;

  await _buildIndex(inst, doc.collection);
}

// ── Exported: clearCollection ─────────────────────────────────────────────────
export async function clearCollection(instanceId, collection) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return;
  const col = collection || inst.options.defaultCollection || 'default';

  const docs = await inst.idb.getAllFromIndex('documents', 'byCollection', col);
  const tx = inst.idb.transaction(['documents', 'chunks', 'vectors', 'collections'], 'readwrite');

  for (const doc of docs) {
    await tx.objectStore('documents').delete(doc.id);
  }
  const chunks = await tx.objectStore('chunks').index('byCollection').getAll(col);
  for (const c of chunks) {
    await tx.objectStore('chunks').delete(c.id);
    await tx.objectStore('vectors').delete(c.id);
  }

  const colStore = tx.objectStore('collections');
  const colRec = await colStore.get(col);
  if (colRec) {
    colRec.docCount   = 0;
    colRec.chunkCount = 0;
    await colStore.put(colRec);
  }
  await tx.done;

  inst.index[col] = { matrix: new Float32Array(0), ids: [], dim: 0 };
}

// ── Exported: deleteCollection ────────────────────────────────────────────────
export async function deleteCollection(instanceId, collection) {
  await clearCollection(instanceId, collection);
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return;
  await inst.idb.delete('collections', collection);
  delete inst.index[collection];
}

// ── Exported: createCollection ────────────────────────────────────────────────
export async function createCollection(instanceId, name) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return;
  const existing = await inst.idb.get('collections', name);
  if (existing) return;
  await inst.idb.put('collections', {
    name,
    vectorDim:      0,
    embeddingModel: null,
    createdAt:      _now(),
    docCount:       0,
    chunkCount:     0,
  });
}

// ── BM25 search ───────────────────────────────────────────────────────────────
function _searchBM25(inst, collection, query, topK) {
  const idx = inst.index[collection];
  if (!idx || !idx.bm25 || idx.ids.length === 0) return [];
  const scores = idx.bm25.score(query);
  const hits = [];
  for (let i = 0; i < scores.length; i++) {
    if (scores[i] > 0) {
      hits.push({ id: idx.ids[i], score: scores[i] });
    }
  }
  hits.sort((a, b) => b.score - a.score);
  return hits.slice(0, topK);
}

// ── Hybrid search ──────────────────────────────────────────────────────────────
async function _searchHybrid(inst, collection, query, topK, minScore) {
  const idx = inst.index[collection];
  if (!idx || idx.ids.length === 0) return [];

  const [queryVec] = await _embed(inst, [query]);
  
  // Get cosine hits
  const cosineHits = _searchIndex(inst, collection, queryVec, topK, minScore);
  
  // Get BM25 hits
  const bm25Hits = _searchBM25(inst, collection, query, topK);
  
  // Use reciprocal rank fusion
  const fusedHits = _reciprocalRankFusion([cosineHits, bm25Hits]);
  
  return fusedHits.slice(0, topK);
}

// ── Cross-collection search ───────────────────────────────────────────────────
async function _searchCrossCollection(inst, collections, query, topK, minScore, useHybrid) {
  const allHits = [];
  for (const col of collections) {
    if (!inst.index[col]) await _buildIndex(inst, col);
    
    let colHits;
    if (useHybrid) {
      colHits = await _searchHybrid(inst, col, query, topK, minScore);
    } else {
      const [queryVec] = await _embed(inst, [query]);
      colHits = _searchIndex(inst, col, queryVec, topK, minScore);
    }
    allHits.push(...colHits);
  }
  // Sort all hits by score descending and take topK
  allHits.sort((a, b) => b.score - a.score);
  return allHits.slice(0, topK);
}

// ── Exported: search ──────────────────────────────────────────────────────────
export async function search(instanceId, query, collection, topK, minScore, useHybrid = true) {
  const inst = _instances.get(instanceId);
  if (!inst) return [];

  let collections;
  if (Array.isArray(collection)) {
    collections = collection;
  } else {
    collections = [collection || inst.options.defaultCollection || 'default'];
  }
  
  const k = topK || 10;
  const minS = minScore != null ? minScore : (inst.options.similarityThreshold || 0);

  let hits;
  if (collections.length === 1) {
    const col = collections[0];
    if (!inst.index[col]) await _buildIndex(inst, col);
    
    if (useHybrid) {
      hits = await _searchHybrid(inst, col, query, k, minS);
    } else {
      const [queryVec] = await _embed(inst, [query]);
      hits = _searchIndex(inst, col, queryVec, k, minS);
    }
  } else {
    hits = await _searchCrossCollection(inst, collections, query, k, minS, useHybrid);
  }

  if (!inst.idb || hits.length === 0) return [];

  const results = [];
  for (const hit of hits) {
    const chunk = await inst.idb.get('chunks', hit.id);
    if (!chunk) continue;
    const doc = await inst.idb.get('documents', chunk.documentId);
    results.push({
      chunk: {
        id:         chunk.id,
        documentId: chunk.documentId,
        index:      chunk.index,
        text:       chunk.text,
        tokenCount: chunk.tokenCount || 0,
        metadata:   chunk.metadata   || {},
      },
      document: doc ? {
        id:         doc.id,
        collection: doc.collection,
        title:      doc.title,
        source:     doc.source,
        format:     doc.format,
        sizeBytes:  doc.sizeBytes  || 0,
        createdAt:  doc.createdAt  || '',
        metadata:   doc.metadata   || {},
        chunkCount: doc.chunkCount || 0,
      } : { id: chunk.documentId, collection: collections[0], title: '', source: '', format: 'Auto', sizeBytes: 0, createdAt: '', metadata: {}, chunkCount: 0 },
      score:          hit.score,
      highlightSpans: [],
    });
  }
  return results;
}

// ── Exported: ask ─────────────────────────────────────────────────────────────
export async function ask(instanceId, question, collection, topK, systemPrompt, mode) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);

  // 1. Validate
  if (!inst.embeddingPipeline) throw new Error('Embedding model not loaded. Please load an embedding model first.');
  if (!inst.llmEngine)         throw new Error('LLM not loaded. Please connect an LLM provider first.');

  const t0 = Date.now();

  // 2+3. Embed query & search
  const hits = await search(instanceId, question, collection, topK || 5, 0);

  // 4. Context assembly with citation map: citationNumber (1-based) → chunkId
  const maxChars = (inst.options.maxContextTokens || 3000) * 4;
  let usedChars = 0;
  const contextBlocks = [];
  const citationMap = new Map(); // number → chunkId
  for (const hit of hits) {
    const block = { title: hit.document.title || 'Document', text: hit.chunk.text, chunkId: hit.chunk.id };
    if (usedChars + block.text.length > maxChars) break;
    contextBlocks.push(block);
    citationMap.set(contextBlocks.length, hit.chunk.id); // 1-based
    usedChars += block.text.length;
  }

  // 5. Prompt by mode
  const { sysMsg, userMsg } = _buildRagPrompt(question, contextBlocks, systemPrompt, mode);
  const messages = [
    { role: 'system', content: sysMsg  },
    { role: 'user',   content: userMsg },
  ];

  let answerText = '';
  let promptTokens = Math.ceil((sysMsg.length + userMsg.length) / 4);
  let completionTokens = 0;

  // 6. LLM call
  if (inst.llmEngine.kind === 'openai') {
    const abortCtrl = new AbortController();
    const response = await fetch(`${inst.llmEngine.baseUrl}/chat/completions`, {
      method:  'POST',
      signal:  abortCtrl.signal,
      headers: {
        'Content-Type':  'application/json',
        'Authorization': `Bearer ${inst.llmEngine.apiKey}`,
        ...(inst.llmEngine.extraHeaders || {}),
      },
      body: JSON.stringify({ model: inst.llmEngine.model, messages, stream: false }),
    });
    if (!response.ok) {
      const errText = await response.text().catch(() => response.statusText);
      throw new Error(`LLM API error ${response.status}: ${errText}`);
    }
    const data = await response.json();
    answerText       = data.choices?.[0]?.message?.content || '';
    promptTokens     = data.usage?.prompt_tokens     || promptTokens;
    completionTokens = data.usage?.completion_tokens || 0;
  } else {
    // WebLLM
    const completion = await inst.llmEngine.chat.completions.create({ messages, stream: false });
    answerText       = completion.choices?.[0]?.message?.content || '';
    completionTokens = completion.usage?.completion_tokens || 0;
  }

  // 7. Citations: resolve [#n] → chunkId via citationMap, dedupe
  const citedChunkIds = _extractCitationIds(answerText, citationMap);
  const sources = hits.filter(h => citedChunkIds.has(h.chunk.id));

  return {
    question,
    answer:           answerText,
    sources:          sources.length > 0 ? sources : hits.slice(0, contextBlocks.length),
    promptTokens,
    completionTokens,
    durationMs:       Date.now() - t0,
  };
}

// ── Exported: askStream ───────────────────────────────────────────────────────
export async function askStream(instanceId, question, collection, topK, systemPrompt, mode, streamId) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);

  // 1. Validate
  if (!inst.embeddingPipeline) throw new Error('Embedding model not loaded. Please load an embedding model first.');
  if (!inst.llmEngine)         throw new Error('LLM not loaded. Please connect an LLM provider first.');

  const t0 = Date.now();

  // 2+3. Embed query & search
  const hits = await search(instanceId, question, collection, topK || 5, 0);

  // 4. Context assembly with citation map
  const maxChars = (inst.options.maxContextTokens || 3000) * 4;
  let usedChars = 0;
  const contextBlocks = [];
  const citationMap = new Map(); // 1-based number → chunkId
  for (const hit of hits) {
    const block = { title: hit.document.title || 'Document', text: hit.chunk.text, chunkId: hit.chunk.id };
    if (usedChars + block.text.length > maxChars) break;
    contextBlocks.push(block);
    citationMap.set(contextBlocks.length, hit.chunk.id);
    usedChars += block.text.length;
  }

  // 5. Prompt by mode
  const { sysMsg, userMsg } = _buildRagPrompt(question, contextBlocks, systemPrompt, mode);
  const messages = [
    { role: 'system', content: sysMsg  },
    { role: 'user',   content: userMsg },
  ];

  let fullAnswer = '';

  // Store active AbortController / WebLLM engine on instance for cancellation
  inst._activeStreamId = streamId;

  const _sendToken = (token) => {
    fullAnswer += token;
    try { inst.dotnetRef.invokeMethodAsync('OnStreamTokenCallback', token); } catch (_) {}
  };

  const _sendComplete = async () => {
    // 7. Citations via citationMap
    const citedChunkIds = _extractCitationIds(fullAnswer, citationMap);
    const sources = hits.filter(h => citedChunkIds.has(h.chunk.id));
    const answer = {
      question,
      answer:           fullAnswer,
      sources:          sources.length > 0 ? sources : hits.slice(0, contextBlocks.length),
      promptTokens:     Math.ceil((sysMsg.length + userMsg.length) / 4),
      completionTokens: Math.ceil(fullAnswer.length / 4),
      durationMs:       Date.now() - t0,
    };
    // 8. OnStreamComplete
    try { inst.dotnetRef.invokeMethodAsync('OnStreamCompleteCallback', answer); } catch (_) {}

    // 9. Persist chat message to IndexedDB (optional)
    if (inst.idb && inst.options.persistToIndexedDb !== false) {
      try {
        const col = collection || inst.options.defaultCollection || 'default';
        const chatMsg = {
          id:         _uuid(),
          collection: col,
          question,
          answer:     fullAnswer,
          createdAt:  _now(),
          durationMs: answer.durationMs,
        };
        await inst.idb.put('chats', chatMsg);
      } catch (_) {} // non-critical
    }
  };

  // 6. LLM streaming
  if (inst.llmEngine.kind === 'openai') {
    // OpenAI SSE with AbortController for cancellation
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;

    let response;
    try {
      response = await fetch(`${inst.llmEngine.baseUrl}/chat/completions`, {
        method:  'POST',
        signal:  abortCtrl.signal,
        headers: {
          'Content-Type':  'application/json',
          'Authorization': `Bearer ${inst.llmEngine.apiKey}`,
          ...(inst.llmEngine.extraHeaders || {}),
        },
        body: JSON.stringify({ model: inst.llmEngine.model, messages, stream: true }),
      });
    } catch (err) {
      if (err?.name === 'AbortError') { await _sendComplete(); return; }
      throw err;
    }

    if (!response.ok) {
      const errText = await response.text().catch(() => response.statusText);
      throw new Error(`LLM API error ${response.status}: ${errText}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder('utf-8');
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        // Check if cancelled between reads
        if (abortCtrl.signal.aborted) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop();
        for (const line of lines) {
          const trimmed = line.trim();
          if (!trimmed.startsWith('data:')) continue;
          const payload = trimmed.slice(5).trim();
          if (payload === '[DONE]') { buffer = ''; break; }
          try {
            const parsed = JSON.parse(payload);
            const token = parsed.choices?.[0]?.delta?.content;
            if (token) _sendToken(token);
          } catch (_) {}
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') throw err;
    } finally {
      try { reader.cancel(); } catch (_) {}
      inst._activeAbortCtrl = null;
    }

    await _sendComplete();

  } else {
    // WebLLM async iterator with interruptGenerate() for cancellation
    inst._activeWebLlmEngine = inst.llmEngine;
    let stream;
    try {
      stream = await inst.llmEngine.chat.completions.create({ messages, stream: true });
      for await (const chunk of stream) {
        const token = chunk.choices?.[0]?.delta?.content;
        if (token) _sendToken(token);
      }
    } catch (err) {
      // InterruptError or similar from WebLLM on cancellation — not a real error
      const msg = String(err?.message || '');
      if (!msg.includes('interrupt') && !msg.includes('cancel') && err?.name !== 'AbortError') {
        throw err;
      }
    } finally {
      inst._activeWebLlmEngine = null;
    }

    await _sendComplete();
  }
}

// ── Exported: cancelStream ────────────────────────────────────────────────────
export async function cancelStream(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst) return;

  // Cancel OpenAI fetch stream
  if (inst._activeAbortCtrl) {
    try { inst._activeAbortCtrl.abort(); } catch (_) {}
    inst._activeAbortCtrl = null;
  }

  // Interrupt WebLLM generation
  if (inst._activeWebLlmEngine) {
    try { await inst._activeWebLlmEngine.interruptGenerate(); } catch (_) {}
    inst._activeWebLlmEngine = null;
  }
}

// ── Multimodal content builder ────────────────────────────────────────────────
// Builds OpenAI-compatible content array for vision/file requests.
// Supports: images (base64 image_url), PDFs (file content type), plain text.
// If no attachments, returns plain string for backward compatibility.
function _buildMultimodalContent(text, attachments) {
  if (!attachments || attachments.length === 0) return text || '';

  const parts = [];

  // Add text part first (if any)
  if (text && text.trim()) {
    parts.push({ type: 'text', text: text.trim() });
  }

  for (const att of attachments) {
    if (att.isImage) {
      // Image: use image_url with base64 data URL
      parts.push({
        type: 'image_url',
        image_url: {
          url: `data:${att.mimeType};base64,${att.base64}`,
        },
      });
    } else if (att.isPdf) {
      // PDF: OpenRouter file content type
      // See: https://openrouter.ai/docs/guides/overview/multimodal/pdfs
      parts.push({
        type: 'file',
        file: {
          filename: att.name,
          file_data: `data:${att.mimeType};base64,${att.base64}`,
        },
      });
    } else {
      // Plain text files (txt, md, csv, json, docx) — decode and embed as text
      try {
        const decoded = atob(att.base64);
        const bytes = new Uint8Array(decoded.length);
        for (let i = 0; i < decoded.length; i++) bytes[i] = decoded.charCodeAt(i);
        const fileText = new TextDecoder('utf-8').decode(bytes);
        const truncated = fileText.length > 8000 ? fileText.slice(0, 8000) + '\n...[truncated]' : fileText;
        parts.push({
          type: 'text',
          text: `\n\n--- File: ${att.name} ---\n${truncated}\n--- End of file ---`,
        });
      } catch (_) {
        parts.push({ type: 'text', text: `[Could not read file: ${att.name}]` });
      }
    }
  }

  // If only one text part, return as string (wider model compatibility)
  if (parts.length === 1 && parts[0].type === 'text') return parts[0].text;

  return parts;
}

// ── Exported: chatDirectStream ────────────────────────────────────────────────
// Pure LLM chat without any document retrieval. Maintains conversation history
// per instanceId so multi-turn context works correctly.
// attachments: array of {name, mimeType, base64, isImage, isPdf} or null
export async function chatDirectStream(instanceId, message, systemPrompt, attachments, streamId) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);
  if (!inst.llmEngine) throw new Error('LLM not loaded. Please connect an LLM provider first.');

  // Initialise per-instance conversation history
  if (!inst._directHistory) inst._directHistory = [];

  const sysMsg = systemPrompt || 'You are a helpful assistant.';

  // Build multimodal user content (text + optional images/PDFs)
  const userContent = _buildMultimodalContent(message, attachments);

  // Build messages array: system + full history + new user message
  const messages = [
    { role: 'system', content: sysMsg },
    ...inst._directHistory,
    { role: 'user', content: userContent },
  ];

  // Store text-only version in history (base64 would bloat it)
  const historyText = message || (attachments?.length ? `[${attachments.length} file(s)]` : '');
  inst._directHistory.push({ role: 'user', content: historyText });

  let fullAnswer = '';
  inst._activeStreamId = streamId;

  const _sendToken = (token) => {
    fullAnswer += token;
    try { inst.dotnetRef.invokeMethodAsync('OnStreamTokenCallback', token); } catch (_) {}
  };

  const _sendComplete = () => {
    // Add assistant turn to history for next round
    inst._directHistory.push({ role: 'assistant', content: fullAnswer });

    // Keep history bounded to last 20 turns (10 exchanges) to avoid token overflow
    if (inst._directHistory.length > 20) {
      inst._directHistory = inst._directHistory.slice(-20);
    }

    const answer = {
      question:         message,
      answer:           fullAnswer,
      sources:          [],
      promptTokens:     Math.ceil(messages.reduce((s, m) => s + m.content.length, 0) / 4),
      completionTokens: Math.ceil(fullAnswer.length / 4),
      durationMs:       0,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnStreamCompleteCallback', answer); } catch (_) {}
  };

  if (inst.llmEngine.kind === 'openai') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;

    let response;
    try {
      response = await fetch(`${inst.llmEngine.baseUrl}/chat/completions`, {
        method:  'POST',
        signal:  abortCtrl.signal,
        headers: {
          'Content-Type':  'application/json',
          'Authorization': `Bearer ${inst.llmEngine.apiKey}`,
          ...(inst.llmEngine.extraHeaders || {}),
        },
        body: JSON.stringify({ model: inst.llmEngine.model, messages, stream: true }),
      });
    } catch (err) {
      if (err?.name === 'AbortError') { _sendComplete(); return; }
      throw err;
    }

    if (!response.ok) {
      const errText = await response.text().catch(() => response.statusText);
      throw new Error(`LLM API error ${response.status}: ${errText}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder('utf-8');
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        if (abortCtrl.signal.aborted) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop();
        for (const line of lines) {
          const trimmed = line.trim();
          if (!trimmed.startsWith('data:')) continue;
          const payload = trimmed.slice(5).trim();
          if (payload === '[DONE]') { buffer = ''; break; }
          try {
            const parsed = JSON.parse(payload);
            const token = parsed.choices?.[0]?.delta?.content;
            if (token) _sendToken(token);
          } catch (_) {}
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') throw err;
    } finally {
      try { reader.cancel(); } catch (_) {}
      inst._activeAbortCtrl = null;
    }

    _sendComplete();

  } else {
    // WebLLM
    inst._activeWebLlmEngine = inst.llmEngine;
    try {
      const stream = await inst.llmEngine.chat.completions.create({ messages, stream: true });
      for await (const chunk of stream) {
        const token = chunk.choices?.[0]?.delta?.content;
        if (token) _sendToken(token);
      }
    } catch (err) {
      const msg = String(err?.message || '');
      if (!msg.includes('interrupt') && !msg.includes('cancel') && err?.name !== 'AbortError') throw err;
    } finally {
      inst._activeWebLlmEngine = null;
    }

    _sendComplete();
  }
}

// ── Exported: clearDirectHistory ─────────────────────────────────────────────
export function clearDirectHistory(instanceId) {
  const inst = _instances.get(instanceId);
  if (inst) inst._directHistory = [];
}


// ── Exported: renderMarkdown ──────────────────────────────────────────────────
// Parses markdown text to sanitized HTML using marked.js.
// markedSrc: CDN URL for marked.js (from SgRagSources.MarkedScript).
// Returns HTML string safe for innerHTML injection.
export async function renderMarkdown(text, markedSrc) {
  const src = markedSrc || 'https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js';
  await _loadScript(src);

  const marked = window.marked;
  if (!marked) return _escapeHtml(text);

  // Configure marked: safe defaults, no mangle, no headerIds
  marked.setOptions({
    gfm:       true,   // GitHub Flavored Markdown
    breaks:    true,   // \n → <br>
    pedantic:  false,
  });

  const html = marked.parse(text || '');

  // Basic sanitization: strip <script> and on* handlers
  return _sanitizeHtml(html);
}

// Minimal HTML sanitizer — removes script tags and inline event handlers.
// For production use consider DOMPurify; this covers the common LLM output cases.
function _sanitizeHtml(html) {
  return html
    .replace(/<script[\s\S]*?<\/script>/gi, '')
    .replace(/\son\w+\s*=\s*["'][^"']*["']/gi, '')
    .replace(/\son\w+\s*=\s*[^\s>]*/gi, '')
    .replace(/javascript\s*:/gi, 'nojs:');
}

function _escapeHtml(text) {
  return String(text)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

// ── Exported: exportDb ────────────────────────────────────────────────────────
export async function exportDb(instanceId, collection) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return JSON.stringify({ collections: [], documents: [], chunks: [], vectors: [] });

  let collections, documents, chunks, vectors;

  if (collection) {
    collections = [await inst.idb.get('collections', collection)].filter(Boolean);
    documents   = await inst.idb.getAllFromIndex('documents', 'byCollection', collection);
    chunks      = await inst.idb.getAllFromIndex('chunks',    'byCollection', collection);
    vectors     = await inst.idb.getAllFromIndex('vectors',   'byCollection', collection);
  } else {
    collections = await inst.idb.getAll('collections');
    documents   = await inst.idb.getAll('documents');
    chunks      = await inst.idb.getAll('chunks');
    vectors     = await inst.idb.getAll('vectors');
  }

  // Encode Float32Array vectors as base64 for JSON transport
  const encodedVectors = vectors.map(v => ({
    ...v,
    vector: _float32ToBase64(new Float32Array(v.vector)),
  }));

  return JSON.stringify({ collections, documents, chunks, vectors: encodedVectors });
}

// ── Exported: importDb ────────────────────────────────────────────────────────
export async function importDb(instanceId, data, merge) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return;

  const parsed = JSON.parse(data);
  const { collections = [], documents = [], chunks = [], vectors = [] } = parsed;

  if (!merge) {
    // Clear existing data
    const tx0 = inst.idb.transaction(['collections', 'documents', 'chunks', 'vectors'], 'readwrite');
    await tx0.objectStore('collections').clear();
    await tx0.objectStore('documents').clear();
    await tx0.objectStore('chunks').clear();
    await tx0.objectStore('vectors').clear();
    await tx0.done;
    inst.index = {};
  }

  const tx = inst.idb.transaction(['collections', 'documents', 'chunks', 'vectors'], 'readwrite');

  for (const c of collections) await tx.objectStore('collections').put(c);
  for (const d of documents)   await tx.objectStore('documents').put(d);
  for (const c of chunks)      await tx.objectStore('chunks').put(c);

  for (const v of vectors) {
    const decoded = {
      ...v,
      vector: Array.from(_base64ToFloat32(v.vector)),
    };
    await tx.objectStore('vectors').put(decoded);
  }

  await tx.done;

  // Rebuild all in-memory indexes
  const allCols = await inst.idb.getAll('collections');
  for (const col of allCols) {
    await _buildIndex(inst, col.name);
  }
}

// ── Exported: createSnapshot ──────────────────────────────────────────────────
export async function createSnapshot(instanceId, kind, note) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) throw new Error('IDB not available');

  const data = await exportDb(instanceId, null);
  const sizeBytes = new TextEncoder().encode(data).length;
  const snap = {
    id:        _uuid(),
    kind:      kind || 'Manual',
    note:      note || null,
    createdAt: _now(),
    sizeBytes,
    data,
  };

  await inst.idb.put('snapshots', snap);

  return {
    id:        snap.id,
    kind:      snap.kind,
    note:      snap.note,
    createdAt: snap.createdAt,
    sizeBytes: snap.sizeBytes,
  };
}

// ── Exported: listSnapshots ───────────────────────────────────────────────────
export async function listSnapshots(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) return [];
  const snaps = await inst.idb.getAll('snapshots');
  return snaps
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .map(s => ({
      id:        s.id,
      kind:      s.kind,
      note:      s.note      || null,
      createdAt: s.createdAt || '',
      sizeBytes: s.sizeBytes || 0,
    }));
}

// ── Exported: restoreSnapshot ─────────────────────────────────────────────────
export async function restoreSnapshot(instanceId, snapId) {
  const inst = _instances.get(instanceId);
  if (!inst || !inst.idb) throw new Error('IDB not available');

  const snap = await inst.idb.get('snapshots', snapId);
  if (!snap) throw new Error(`Snapshot ${snapId} not found`);

  await importDb(instanceId, snap.data, false);
}

// ── Chat export functions ──────────────────────────────────────────────────────

function _exportChatToMarkdown(messages) {
  let md = '# Chat History\n\n';
  for (const msg of messages) {
    md += `## ${msg.isUser ? 'User' : 'Assistant'}\n\n${msg.content}\n\n`;
    if (msg.sources && msg.sources.length > 0) {
      md += '### Sources:\n';
      for (const src of msg.sources) {
        md += `- [${src.document?.title || 'Document'}]: ${src.chunk?.text?.slice(0, 100) || ''}...\n`;
      }
      md += '\n';
    }
  }
  return md;
}

function _exportChatToHtml(messages) {
  let html = '<!DOCTYPE html><html><head><meta charset="UTF-8"><title>Chat History</title></head><body>';
  html += '<h1>Chat History</h1>';
  for (const msg of messages) {
    html += `<h2>${msg.isUser ? 'User' : 'Assistant'}</h2>`;
    html += `<p>${(msg.content || '').replace(/\n/g, '<br>')}</p>`;
    if (msg.sources && msg.sources.length > 0) {
      html += '<h3>Sources:</h3><ul>';
      for (const src of msg.sources) {
        html += `<li><strong>${src.document?.title || 'Document'}</strong>: ${(src.chunk?.text || '').slice(0, 100)}...</li>`;
      }
      html += '</ul>';
    }
  }
  html += '</body></html>';
  return html;
}

async function _exportChatToPdf(messages, jsPdfScript) {
  await _loadScript(jsPdfScript || 'https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js');
  const { jsPDF } = window.jspdf;
  const doc = new jsPDF();
  let y = 20;
  doc.setFontSize(18);
  doc.text('Chat History', 20, y);
  y += 20;
  
  for (const msg of messages) {
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text(msg.isUser ? 'User' : 'Assistant', 20, y);
    y += 7;
    doc.setFont('helvetica', 'normal');
    const lines = doc.splitTextToSize(msg.content || '', 170);
    doc.text(lines, 20, y);
    y += (lines.length * 7) + 10;
    
    if (y > 280) {
      doc.addPage();
      y = 20;
    }
  }
  return doc.output('datauristring');
}

// ── Exported: exportChat ───────────────────────────────────────────────────
export async function exportChat(instanceId, format, messages) {
  switch (format.toLowerCase()) {
    case 'markdown':
    case 'md':
      return { content: _exportChatToMarkdown(messages), type: 'text/markdown', extension: 'md' };
    case 'html':
      return { content: _exportChatToHtml(messages), type: 'text/html', extension: 'html' };
    case 'pdf':
      const pdfData = await _exportChatToPdf(messages);
      return { content: pdfData, type: 'application/pdf', extension: 'pdf' };
    default:
      throw new Error(`Unsupported export format: ${format}`);
  }
}
