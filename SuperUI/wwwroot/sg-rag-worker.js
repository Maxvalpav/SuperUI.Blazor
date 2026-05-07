// sg-rag-worker.js - Web Worker for parsing/embedding tasks
let transformers = null;
let embeddingPipeline = null;
let rerankerPipeline = null;

// Load transformers.js
async function loadTransformers(scriptUrl) {
  if (!transformers) {
    importScripts(scriptUrl);
    transformers = window.transformers;
    transformers.env.allowLocalModels = false;
  }
}

// Message handler
self.onmessage = async (e) => {
  const { type, payload } = e.data;
  
  try {
    switch (type) {
      case 'LOAD_TRANSFORMERS':
        await loadTransformers(payload.scriptUrl);
        self.postMessage({ type: 'TRANSFORMERS_LOADED' });
        break;
        
      case 'LOAD_EMBEDDING_MODEL':
        await loadTransformers(payload.scriptUrl);
        const progressCallback = (progress) => {
          self.postMessage({
            type: 'EMBEDDING_PROGRESS',
            payload: {
              stage: progress.status || '',
              loaded: progress.loaded || 0,
              total: progress.total || 0,
              percent: progress.total ? (progress.loaded / progress.total) * 100 : 0,
              file: progress.file || null,
              isComplete: progress.status === 'done',
            }
          });
        };
        
        embeddingPipeline = await transformers.pipeline('feature-extraction', payload.modelId, {
          dtype: payload.dtype || 'q8',
          progress_callback: progressCallback,
        });
        
        self.postMessage({ type: 'EMBEDDING_MODEL_LOADED', payload: { modelId: payload.modelId } });
        break;
        
      case 'EMBED':
        if (!embeddingPipeline) {
          throw new Error('Embedding model not loaded');
        }
        const output = await embeddingPipeline(payload.texts, { pooling: 'mean', normalize: true });
        const dim = output.dims[1];
        const result = [];
        for (let i = 0; i < payload.texts.length; i++) {
          result.push(Array.from(output.data.slice(i * dim, (i + 1) * dim)));
        }
        self.postMessage({ type: 'EMBED_RESULT', payload: { embeddings: result, taskId: payload.taskId } });
        break;
        
      case 'LOAD_RERANKER':
        await loadTransformers(payload.scriptUrl);
        const rerankProgressCallback = (progress) => {
          self.postMessage({
            type: 'RERANKER_PROGRESS',
            payload: {
              stage: progress.status || '',
              loaded: progress.loaded || 0,
              total: progress.total || 0,
              percent: progress.total ? (progress.loaded / progress.total) * 100 : 0,
              file: progress.file || null,
              isComplete: progress.status === 'done',
            }
          });
        };
        
        rerankerPipeline = await transformers.pipeline('text-classification', payload.modelId || 'Xenova/ms-marco-MiniLM-L-6-v2', {
          progress_callback: rerankProgressCallback,
        });
        
        self.postMessage({ type: 'RERANKER_LOADED', payload: { modelId: payload.modelId } });
        break;
        
      case 'RERANK':
        if (!rerankerPipeline) {
          // Return hits as-is if no reranker
          self.postMessage({ type: 'RERANK_RESULT', payload: { hits: payload.hits, taskId: payload.taskId } });
          return;
        }
        const pairs = payload.hits.map(hit => [payload.query, hit.chunk.text]);
        const results = await rerankerPipeline(pairs);
        const scoredHits = payload.hits.map((hit, i) => ({ hit, score: results[i].score }));
        scoredHits.sort((a, b) => b.score - a.score);
        const topN = payload.topN || 5;
        const rerankedHits = scoredHits.slice(0, topN).map(item => ({ ...item.hit, score: item.score }));
        self.postMessage({ type: 'RERANK_RESULT', payload: { hits: rerankedHits, taskId: payload.taskId } });
        break;
        
      default:
        self.postMessage({ type: 'ERROR', payload: { message: `Unknown message type: ${type}` } });
    }
  } catch (error) {
    self.postMessage({ type: 'ERROR', payload: { message: String(error), type } });
  }
};
