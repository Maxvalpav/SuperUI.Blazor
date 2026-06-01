const _instances = new Map();

async function _loadScript(url) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = url;
        script.onload = () => resolve();
        script.onerror = (e) => reject(new Error(`Failed to load ${url}`));
        document.head.appendChild(script);
    });
}

async function _loadStylesheet(url) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`link[href="${url}"]`)) {
            resolve();
            return;
        }
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = url;
        link.onload = () => resolve();
        link.onerror = (e) => reject(new Error(`Failed to load ${url}`));
        document.head.appendChild(link);
    });
}

function _createElementInfo(element) {
    if (!element) return null;
    const bo = element.businessObject;
    return {
        id: element.id,
        type: bo.$type,
        name: bo.name || null,
        sourceId: element.source?.id || null,
        targetId: element.target?.id || null
    };
}

export async function initBpmn(dotNetRef, containerRef, containerId, mode, xml, sources) {
    if (!containerRef) {
        console.error(`Container ref is null for ${containerId}`);
        return;
    }

    try {
        await _loadStylesheet(sources.diagramCss);
        await _loadStylesheet(sources.bpmnFontCss);
        await _loadStylesheet(sources.bpmnEmbeddedCss);

        let scriptUrl;
        switch (mode) {
            case 0: scriptUrl = sources.modelerScript; break;
            case 1: scriptUrl = sources.viewerScript; break;
            case 2: scriptUrl = sources.navigatedViewerScript; break;
            default: scriptUrl = sources.modelerScript;
        }

        await _loadScript(scriptUrl);

        let BpmnClass = window.BpmnJS;
        
        const bpmn = new BpmnClass({
            container: containerRef
        });

        _instances.set(containerId, {
            bpmn,
            dotNetRef,
            mode,
            lastXml: xml
        });

        const eventBus = bpmn.get('eventBus');

        if (mode === 0) {
            eventBus.on('commandStack.changed', async () => {
                try {
                    const { xml: newXml } = await bpmn.saveXML({ format: true });
                    const instance = _instances.get(containerId);
                    if (instance) {
                        instance.lastXml = newXml;
                    }
                    try { dotNetRef?.invokeMethodAsync('OnXmlChangedAsync', newXml)?.catch(() => {}); } catch {}
                } catch (e) {
                    console.error('Failed to save XML:', e);
                }
            });
        }

        eventBus.on('element.click', (event) => {
            const info = _createElementInfo(event.element);
            if (info) {
                try { dotNetRef?.invokeMethodAsync('OnElementClickedAsync', info)?.catch(() => {}); } catch {}
            }
        });

        eventBus.on('element.dblclick', (event) => {
            const info = _createElementInfo(event.element);
            if (info) {
                try { dotNetRef?.invokeMethodAsync('OnElementDblClickedAsync', info)?.catch(() => {}); } catch {}
            }
        });

        eventBus.on('selection.changed', (event) => {
            const info = event.newSelection?.[0] ? _createElementInfo(event.newSelection[0]) : null;
            try { dotNetRef?.invokeMethodAsync('OnSelectionChangedAsync', info)?.catch(() => {}); } catch {}
        });

        if (xml && xml.trim().length > 0) {
            await bpmn.importXML(xml);
        } else {
            const defaultXml = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_1" isExecutable="false">
    <bpmn:startEvent id="StartEvent_1" name="Start" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="BPMNDiagram_1">
    <bpmndi:BPMNPlane id="BPMNPlane_1" bpmnElement="Process_1">
      <bpmndi:BPMNShape id="_BPMNShape_StartEvent_2" bpmnElement="StartEvent_1">
        <dc:Bounds x="179" y="159" width="36" height="36" />
        <bpmndi:BPMNLabel>
          <dc:Bounds x="184" y="202" width="26" height="14" />
        </bpmndi:BPMNLabel>
      </bpmndi:BPMNShape>
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;
            await bpmn.importXML(defaultXml);
        }

        const canvas = bpmn.get('canvas');
        canvas.zoom('fit-viewport');

    } catch (error) {
        console.error('Failed to initialize BPMN:', error);
        throw error;
    }
}

export async function updateBpmn(containerId, mode, xml, sources) {
    const instance = _instances.get(containerId);
    if (!instance) return;

    try {
        if (xml && xml !== instance.lastXml) {
            await instance.bpmn.importXML(xml);
            instance.lastXml = xml;
            const canvas = instance.bpmn.get('canvas');
            canvas.zoom('fit-viewport');
        }
    } catch (error) {
        console.error('Failed to update BPMN:', error);
    }
}

export async function getXml(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return null;
    try {
        const { xml } = await instance.bpmn.saveXML({ format: true });
        return xml;
    } catch (e) {
        console.error('Failed to get XML:', e);
        return null;
    }
}

export async function getSvg(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return null;
    try {
        const { svg } = await instance.bpmn.saveSVG();
        return svg;
    } catch (e) {
        console.error('Failed to get SVG:', e);
        return null;
    }
}

export async function undo(containerId) {
    const instance = _instances.get(containerId);
    if (!instance || instance.mode !== 0) return;
    try {
        const commandStack = instance.bpmn.get('commandStack');
        commandStack.undo();
    } catch (e) {
        console.error('Failed to undo:', e);
    }
}

export async function redo(containerId) {
    const instance = _instances.get(containerId);
    if (!instance || instance.mode !== 0) return;
    try {
        const commandStack = instance.bpmn.get('commandStack');
        commandStack.redo();
    } catch (e) {
        console.error('Failed to redo:', e);
    }
}

export async function zoomIn(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return;
    try {
        const canvas = instance.bpmn.get('canvas');
        const currentZoom = canvas.zoom();
        canvas.zoom(currentZoom * 1.2);
    } catch (e) {
        console.error('Failed to zoom in:', e);
    }
}

export async function zoomOut(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return;
    try {
        const canvas = instance.bpmn.get('canvas');
        const currentZoom = canvas.zoom();
        canvas.zoom(currentZoom / 1.2);
    } catch (e) {
        console.error('Failed to zoom out:', e);
    }
}

export async function zoomReset(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return;
    try {
        const canvas = instance.bpmn.get('canvas');
        canvas.zoom(1);
    } catch (e) {
        console.error('Failed to reset zoom:', e);
    }
}

export async function fitViewport(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return;
    try {
        const canvas = instance.bpmn.get('canvas');
        canvas.zoom('fit-viewport');
    } catch (e) {
        console.error('Failed to fit viewport:', e);
    }
}

export async function download(containerId, format) {
    const instance = _instances.get(containerId);
    if (!instance) return;

    try {
        let content, mimeType, extension;
        if (format === 'svg') {
            const { svg } = await instance.bpmn.saveSVG();
            content = svg;
            mimeType = 'image/svg+xml';
            extension = 'svg';
        } else {
            const { xml } = await instance.bpmn.saveXML({ format: true });
            content = xml;
            mimeType = 'application/xml';
            extension = 'bpmn';
        }

        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `diagram.${extension}`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    } catch (e) {
        console.error('Failed to download:', e);
    }
}

export async function dispose(containerId) {
    const instance = _instances.get(containerId);
    if (!instance) return;
    try {
        instance.bpmn.destroy();
        _instances.delete(containerId);
    } catch (e) {
        console.error('Failed to dispose:', e);
    }
}
