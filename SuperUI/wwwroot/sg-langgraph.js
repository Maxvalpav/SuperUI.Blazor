/**
 * sg-langgraph.js - SuperUI LangGraph.js Bridge
 * Wraps LangGraph.js for use in Blazor.
 */

const _instances = new Map();

export async function initGraph(instanceId, dotNetRef, config) {
    // In a real scenario, we would import { StateGraph } from "@langchain/langgraph"
    // For this implementation, we assume LangGraph is loaded or we use a CDN-based import
    // If it's not available, we might need to load it dynamically.
    
    const instance = new LangGraphInstance(instanceId, dotNetRef, config);
    _instances.set(instanceId, instance);
    return instanceId;
}

export async function sendMessage(instanceId, input) {
    const instance = _instances.get(instanceId);
    if (instance) {
        return await instance.sendMessage(input);
    }
}

export async function respondToInterrupt(instanceId, data) {
    const instance = _instances.get(instanceId);
    if (instance) {
        return await instance.respondToInterrupt(data);
    }
}

export function dispose(instanceId) {
    const instance = _instances.get(instanceId);
    if (instance) {
        instance.dispose();
        _instances.delete(instanceId);
    }
}

class LangGraphInstance {
    constructor(instanceId, dotNetRef, config) {
        this.instanceId = instanceId;
        this.dotNetRef = dotNetRef;
        this.config = config;
        this.graph = null;
        this.app = null;
        this.currentState = {};
        this.interruptResolve = null;
        
        this._initialize();
    }

    async _initialize() {
        try {
            // Placeholder for real LangGraph initialization
            // In a real app, 'config.graphDefinition' would be a JS string or object
            // describing the nodes and edges.
            
            // Example:
            // const workflow = new StateGraph({ ... });
            // workflow.addNode("agent", ...);
            // ...
            // this.app = workflow.compile({ checkpointer: new MemorySaver() });
            
            console.log(`[LangGraph] Instance ${this.instanceId} initialized with config:`, this.config);
            
            await this.dotNetRef.invokeMethodAsync("OnInitializedInternal", {
                nodes: this.config.nodes || [],
                edges: this.config.edges || []
            });
        } catch (error) {
            console.error("[LangGraph] Initialization failed:", error);
            await this.dotNetRef.invokeMethodAsync("OnErrorInternal", error.message);
        }
    }

    async sendMessage(input) {
        try {
            // Initialize state with user input
            this.currentState = { 
                messages: [{ role: "user", content: input }],
                threadId: this.config.threadId || "default"
            };
            
            let currentNodeId = "start";
            const visitedNodes = new Set();

            while (currentNodeId && currentNodeId !== "__end__" && currentNodeId !== "end") {
                if (visitedNodes.size > 100) {
                    throw new Error("Potential infinite loop detected in graph");
                }
                visitedNodes.add(currentNodeId);

                // 1. Notify UI about current node
                await this.dotNetRef.invokeMethodAsync("OnStepInternal", {
                    node: currentNodeId,
                    state: this.currentState,
                    content: null
                });

                // 2. Execute node logic in C#
                // C# will return updated state and optional content
                const result = await this.dotNetRef.invokeMethodAsync("OnNodeExecuteInternal", currentNodeId, this.currentState);
                
                if (result) {
                    if (result.state) this.currentState = result.state;
                    
                    // 3. Notify UI about step completion with content
                    await this.dotNetRef.invokeMethodAsync("OnStepInternal", {
                        node: currentNodeId,
                        state: this.currentState,
                        content: result.content
                    });

                    // 4. Handle Interruption if requested by C#
                    if (result.interrupt) {
                        const interruptResponse = await new Promise(resolve => {
                            this.interruptResolve = resolve;
                            this.dotNetRef.invokeMethodAsync("OnInterruptInternal", {
                                node: currentNodeId,
                                message: result.interrupt.message,
                                data: result.interrupt.data
                            });
                        });
                        
                        // Merge interrupt response back into state
                        this.currentState.lastInterruptResponse = interruptResponse;
                    }
                }

                // 5. Determine next node
                currentNodeId = this._getNextNode(currentNodeId);
            }

            // Final step
            await this.dotNetRef.invokeMethodAsync("OnStepInternal", {
                node: "__end__",
                state: this.currentState,
                content: "Workflow completed."
            });

        } catch (error) {
            console.error("[LangGraph] Execution error:", error);
            await this.dotNetRef.invokeMethodAsync("OnErrorInternal", error.message);
        }
    }

    _getNextNode(currentId) {
        // If state specifies the next node, use it (Conditional Routing)
        if (this.currentState && this.currentState.next_node) {
            const next = this.currentState.next_node;
            // Clear it to prevent reuse
            delete this.currentState.next_node;
            return next;
        }

        // Fallback to simple edge-based transition
        const edge = (this.config.edges || []).find(e => e.sourceId === currentId);
        return edge ? edge.targetId : null;
    }

    async respondToInterrupt(data) {
        if (this.interruptResolve) {
            this.interruptResolve(data);
            this.interruptResolve = null;
        }
    }

    dispose() {
        this.dotNetRef = null;
        this.app = null;
        this.graph = null;
    }
}
