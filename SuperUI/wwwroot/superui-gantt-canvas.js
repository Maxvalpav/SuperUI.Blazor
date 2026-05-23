/**
 * superui-gantt-canvas.js
 * JavaScript Interop for SgGantCanvas Blazor component.
 * Handles high-performance rendering of Gantt chart using HTML5 Canvas.
 */

export class SgGantCanvas {
    constructor(container, dotNetRef, options) {
        this.container = container;
        this.dotNetRef = dotNetRef;
        this.options = options;
        
        this.canvas = container.querySelector('.sg-gantt-canvas-element');
        this.ctx = this.canvas.getContext('2d', { alpha: false }); // Optimization: no alpha if possible
        
        this.scrollX = 0;
        this.scrollY = 0;
        this.zoom = options.initialZoom || 1.0;
        this.leftPanelBody = container.closest('.sg-gantt-main')?.querySelector('.sg-gantt-left-body');
        
        this.data = {
            tasks: [],
            dependencies: [],
            resources: [],
            milestones: []
        };

        this.state = {
            hoveredTaskId: null,
            selectedTaskIds: new Set(),
            dragMode: null, // 'move', 'resize-start', 'resize-end', 'progress', 'dependency', 'selection'
            dragTaskId: null,
            dragStartX: 0,
            dragStartY: 0,
            currentDragX: 0,
            currentDragY: 0,
            isDragging: false, // Added to distinguish between click and drag
            dragThreshold: 5,  // Threshold in pixels
            snappedX: null,
            selectionRect: null,
            tooltip: {
                visible: false,
                taskId: null,
                x: 0,
                y: 0,
                timeout: null
            }
        };

        this.initEvents();
        this.resize();
        
        // Use requestAnimationFrame for smooth rendering
        this.needsUpdate = true;
        this.animate();
    }

    initEvents() {
        this._handlers = {
            mousedown: this.handleMouseDown.bind(this),
            mousemove: this.handleMouseMove.bind(this),
            mouseup:   this.handleMouseUp.bind(this),
            keydown:   this.handleKeyDown.bind(this),
            wheel:     this.handleWheel.bind(this),
            dblclick:  this.handleDblClick.bind(this),
        };
        this.canvas.addEventListener('mousedown', this._handlers.mousedown);
        window.addEventListener('mousemove', this._handlers.mousemove);
        window.addEventListener('mouseup',   this._handlers.mouseup);
        window.addEventListener('keydown',   this._handlers.keydown);
        this.canvas.addEventListener('wheel', this._handlers.wheel, { passive: false });
        this.canvas.addEventListener('dblclick', this._handlers.dblclick);

        this._resizeObserver = new ResizeObserver(() => this.resize());
        this._resizeObserver.observe(this.container);
    }

    dispose() {
        this._disposed = true;
        if (this._rafId) {
            cancelAnimationFrame(this._rafId);
            this._rafId = 0;
        }
        if (this._resizeObserver) {
            try { this._resizeObserver.disconnect(); } catch {}
            this._resizeObserver = null;
        }
        const h = this._handlers;
        if (h) {
            try { this.canvas.removeEventListener('mousedown', h.mousedown); } catch {}
            try { window.removeEventListener('mousemove', h.mousemove); } catch {}
            try { window.removeEventListener('mouseup',   h.mouseup);   } catch {}
            try { window.removeEventListener('keydown',   h.keydown);   } catch {}
            try { this.canvas.removeEventListener('wheel', h.wheel);    } catch {}
            try { this.canvas.removeEventListener('dblclick', h.dblclick); } catch {}
            this._handlers = null;
        }
        if (this.state?.tooltip?.timeout) {
            clearTimeout(this.state.tooltip.timeout);
            this.state.tooltip.timeout = null;
        }
        this.dotNetRef = null;
        this.data = null;
        this.canvas = null;
        this.ctx = null;
        this.container = null;
    }

    resize() {
        const rect = this.container.getBoundingClientRect();
        const dpr = window.devicePixelRatio || 1;
        this.canvas.width = rect.width * dpr;
        this.canvas.height = rect.height * dpr;
        this.canvas.style.width = `${rect.width}px`;
        this.canvas.style.height = `${rect.height}px`;
        this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        this.needsUpdate = true;
    }

    setData(data) {
        this.data = data;
        if (data.viewOptions) {
            this.options.viewOptions = data.viewOptions;
        }
        if (data.columnWidth) {
            this.options.columnWidth = data.columnWidth;
        }
        if (data.projectStart) {
            this.projectStart = new Date(data.projectStart);
        }
        if (data.bottomUnit) {
            this.bottomUnit = data.bottomUnit;
        }
        this.needsUpdate = true;
    }

    setOptions(options) {
        this.options = { ...this.options, ...options };
        this.needsUpdate = true;
    }

    animate() {
        if (this._disposed) return;
        if (this.needsUpdate) {
            this.render();
            this.needsUpdate = false;
        }
        this._rafId = requestAnimationFrame(this.animate.bind(this));
    }

    handleMouseDown(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        const hit = this.hitTest(x, y);
        this.state.isDragging = false;
        this.state.dragStartX = x;
        this.state.dragStartY = y;
        this.state.currentDragX = x;
        this.state.currentDragY = y;

        if (hit) {
            this.state.dragTaskId = hit.taskId;
            this.state.dragMode = hit.mode;
            
            if (e.ctrlKey) {
                if (this.state.selectedTaskIds.has(hit.taskId)) {
                    this.state.selectedTaskIds.delete(hit.taskId);
                } else {
                    this.state.selectedTaskIds.add(hit.taskId);
                }
            } else {
                if (!this.state.selectedTaskIds.has(hit.taskId)) {
                    this.state.selectedTaskIds.clear();
                    this.state.selectedTaskIds.add(hit.taskId);
                }
            }
            
            this.dotNetRef.invokeMethodAsync('OnSelectionChangedInternal', Array.from(this.state.selectedTaskIds));
            this.needsUpdate = true;
        } else {
            if (!e.ctrlKey) {
                this.state.selectedTaskIds.clear();
                this.dotNetRef.invokeMethodAsync('OnClearSelectionInternal');
            }
            
            // Start rubber band selection
            this.state.dragMode = 'selection';
            this.needsUpdate = true;
        }
    }

    handleMouseMove(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        if (this.state.dragMode) {
            // Check if we started dragging (moved past threshold)
            if (!this.state.isDragging) {
                const dx = x - this.state.dragStartX;
                const dy = y - this.state.dragStartY;
                if (Math.sqrt(dx * dx + dy * dy) > this.state.dragThreshold) {
                    this.state.isDragging = true;
                    this.hideTooltip();
                }
            }

            if (this.state.isDragging) {
                this.state.currentDragX = x;
                this.state.currentDragY = y;

                if (this.state.dragMode === 'selection') {
                    this.updateSelectionFromRect();
                } else {
                    // Snap to grid logic for task movements
                    if (this.options.snapToGrid) {
                        const worldX = x + this.scrollX;
                        const snappedWorldX = Math.round(worldX / this.options.columnWidth) * this.options.columnWidth;
                        this.state.snappedX = snappedWorldX - this.scrollX;
                    } else {
                        this.state.snappedX = x;
                    }
                }
                this.needsUpdate = true;
                return;
            }
        }

        const hit = this.hitTest(x, y);
        const newHoveredId = hit ? hit.taskId : null;
        if (this.state.hoveredTaskId !== newHoveredId) {
            this.state.hoveredTaskId = newHoveredId;
            this.needsUpdate = true;
            
            if (newHoveredId) {
                this.canvas.style.cursor = hit.mode === 'move' ? 'move' : 
                                         hit.mode.startsWith('resize') ? 'ew-resize' : 'pointer';
                this.showTooltip(newHoveredId, x, y);
            } else {
                this.canvas.style.cursor = 'default';
                this.hideTooltip();
            }
        } else if (newHoveredId) {
            this.updateTooltipPosition(x, y);
        }
    }

    showTooltip(taskId, x, y) {
        if (this.state.tooltip.timeout) clearTimeout(this.state.tooltip.timeout);
        
        this.state.tooltip.timeout = setTimeout(() => {
            this.state.tooltip.visible = true;
            this.state.tooltip.taskId = taskId;
            this.state.tooltip.x = x + 15;
            this.state.tooltip.y = y + 15;
            this.needsUpdate = true;
        }, 500);
    }

    updateTooltipPosition(x, y) {
        if (this.state.tooltip.visible) {
            this.state.tooltip.x = x + 15;
            this.state.tooltip.y = y + 15;
            this.needsUpdate = true;
        }
    }

    hideTooltip() {
        if (this.state.tooltip.timeout) clearTimeout(this.state.tooltip.timeout);
        this.state.tooltip.visible = false;
        this.state.tooltip.taskId = null;
        this.needsUpdate = true;
    }

    updateSelectionFromRect() {
        const x1 = Math.min(this.state.dragStartX, this.state.currentDragX) + this.scrollX;
        const x2 = Math.max(this.state.dragStartX, this.state.currentDragX) + this.scrollX;
        const y1 = Math.min(this.state.dragStartY, this.state.currentDragY) + this.scrollY - this.options.headerHeight;
        const y2 = Math.max(this.state.dragStartY, this.state.currentDragY) + this.scrollY - this.options.headerHeight;

        const newSelection = new Set();
        this.data.tasks.forEach(task => {
            const taskY = task.rowIndex * this.options.rowHeight;
            const taskH = this.options.rowHeight;
            const taskX = task.x;
            const taskW = task.width;

            if (taskX < x2 && taskX + taskW > x1 && taskY < y2 && taskY + taskH > y1) {
                newSelection.add(task.id);
            }
        });

        this.state.selectedTaskIds = newSelection;
    }

    handleMouseUp(e) {
        if (!this.state.dragMode) return;

        if (this.state.dragMode === 'selection') {
            if (this.state.isDragging) {
                this.dotNetRef.invokeMethodAsync('OnSelectionChangedInternal', Array.from(this.state.selectedTaskIds));
            }
            this.state.dragMode = null;
            this.state.isDragging = false;
            this.needsUpdate = true;
            return;
        }

        if (this.state.dragMode === 'dependency') {
            if (this.state.isDragging) {
                const rect = this.canvas.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                const hit = this.hitTest(x, y);
                
                if (hit && hit.taskId !== this.state.dragTaskId) {
                    this.dotNetRef.invokeMethodAsync('OnDependencyCreatedInternal', this.state.dragTaskId, hit.taskId);
                }
            }
            
            this.state.dragMode = null;
            this.state.dragTaskId = null;
            this.state.isDragging = false;
            this.needsUpdate = true;
            return;
        }

        if (this.state.dragMode) {
            if (this.state.isDragging) {
                const finalX = this.options.snapToGrid ? this.state.snappedX : this.state.currentDragX;
                const dx = finalX - this.state.dragStartX;
                const dy = this.state.currentDragY - this.state.dragStartY;
                
                this.dotNetRef.invokeMethodAsync('OnTaskInteractionEndInternal', 
                    this.state.dragTaskId, 
                    this.state.dragMode, 
                    dx, dy);
            }
            
            this.state.dragMode = null;
            this.state.dragTaskId = null;
            this.state.isDragging = false;
            this.state.snappedX = null;
            this.needsUpdate = true;
        }
    }

    handleWheel(e) {
        if (e.ctrlKey) {
            e.preventDefault();
            const delta = e.deltaY > 0 ? -1 : 1;
            this.dotNetRef.invokeMethodAsync('OnZoomFromWheel', delta);
        } else {
            const oldX = this.scrollX;
            const oldY = this.scrollY;

            this.scrollX += e.deltaX;
            this.scrollY += e.deltaY;
            
            // Constrain scroll
            this.scrollY = Math.max(0, this.scrollY);
            this.scrollX = Math.max(0, this.scrollX);
            
            if (oldX !== this.scrollX || oldY !== this.scrollY) {
                this.needsUpdate = true;
                this.syncLeftScroll();
                this.dotNetRef.invokeMethodAsync('OnScrollInternal', this.scrollX, this.scrollY);
            }
        }
    }

    syncLeftScroll() {
        if (this.leftPanelBody) {
            // Use requestAnimationFrame to sync scroll with next frame
            requestAnimationFrame(() => {
                if (this.leftPanelBody.scrollTop !== this.scrollY) {
                    this.leftPanelBody.scrollTop = this.scrollY;
                }
            });
        }
    }

    handleDblClick(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        const hit = this.hitTest(x, y);
        if (hit) {
            this.dotNetRef.invokeMethodAsync('OnTaskDoubleClickInternal', hit.taskId);
        }
    }

    handleKeyDown(e) {
        if (e.target !== this.canvas && e.target !== document.body) return;

        const step = 20;
        const scrollStep = 50;

        switch (e.key) {
            case 'ArrowLeft':
                this.scrollX = Math.max(0, this.scrollX - scrollStep);
                this.needsUpdate = true;
                break;
            case 'ArrowRight':
                this.scrollX += scrollStep;
                this.needsUpdate = true;
                break;
            case 'ArrowUp':
                this.scrollY = Math.max(0, this.scrollY - scrollStep);
                this.needsUpdate = true;
                break;
            case 'ArrowDown':
                this.scrollY += scrollStep;
                this.needsUpdate = true;
                break;
            case 'Delete':
                if (this.state.selectedTaskIds.size > 0) {
                    this.dotNetRef.invokeMethodAsync('OnDeleteSelectedInternal', Array.from(this.state.selectedTaskIds));
                }
                break;
            case '+':
            case '=':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.setZoom(this.zoom + 0.1);
                }
                break;
            case '-':
            case '_':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.setZoom(this.zoom - 0.1);
                }
                break;
            case 'z':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.dotNetRef.invokeMethodAsync('OnUndoInternal');
                }
                break;
            case 'y':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.dotNetRef.invokeMethodAsync('OnRedoInternal');
                }
                break;
        }

        if (this.needsUpdate) {
            this.dotNetRef.invokeMethodAsync('OnScrollInternal', this.scrollX, this.scrollY);
        }
    }

    setZoom(zoom) {
        const oldZoom = this.zoom;
        this.zoom = Math.max(0.1, Math.min(5.0, zoom));
        if (oldZoom !== this.zoom) {
            this.dotNetRef.invokeMethodAsync('OnZoomChangedInternal', this.zoom);
            this.needsUpdate = true;
        }
    }

    hitTest(x, y) {
        // Adjust for scroll and zoom
        const worldX = x + this.scrollX;
        const worldY = y + this.scrollY - this.options.headerHeight;

        if (worldY < 0) return null;

        const rowIndex = Math.floor(worldY / this.options.rowHeight);
        const task = this.data.tasks.find(t => t.rowIndex === rowIndex);
        
        if (!task) return null;

        const taskX = task.x; // These should be calculated in C# or passed in data
        const taskW = task.width;
        const taskY = rowIndex * this.options.rowHeight + (this.options.rowHeight - this.options.barHeight) / 2;
        
        if (worldX >= taskX && worldX <= taskX + taskW && 
            worldY >= taskY && worldY <= taskY + this.options.barHeight) {
            
            // Check handles
            const handleSize = 8;
            if (worldX <= taskX + handleSize) return { taskId: task.id, mode: 'resize-start' };
            if (worldX >= taskX + taskW - handleSize) return { taskId: task.id, mode: 'resize-end' };
            
            // Progress handle
            const progressX = taskX + taskW * task.progress;
            if (Math.abs(worldX - progressX) < 10) return { taskId: task.id, mode: 'progress' };

            // Dependency connector (right side)
            if (Math.abs(worldX - (taskX + taskW + 15)) < 15) return { taskId: task.id, mode: 'dependency' };

            return { taskId: task.id, mode: 'move' };
        }

        return null;
    }

    render() {
        const { ctx, canvas, data, options, scrollX, scrollY, zoom } = this;
        const width = canvas.width / (window.devicePixelRatio || 1);
        const height = canvas.height / (window.devicePixelRatio || 1);

        ctx.fillStyle = options.theme.backgroundColor;
        ctx.fillRect(0, 0, width, height);

        ctx.save();
        
        // 1. Draw Grid Lines (Background)
        this.drawGrid(ctx, width, height);

        // 2. Draw Today Line (Background)
        if (options.viewOptions?.showTodayLine) {
            this.drawTodayLine(ctx, width, height);
        }

        // 3. Draw Dependencies
        ctx.save();
        ctx.translate(-scrollX, -scrollY + options.headerHeight);
        this.drawDependencies(ctx);
        
        // Draw temporary dependency arrow
        if (this.state.dragMode === 'dependency') {
            const fromTask = this.data.tasks.find(t => t.id === this.state.dragTaskId);
            if (fromTask) {
                const x1 = fromTask.x + fromTask.width + 15;
                const y1 = fromTask.rowIndex * options.rowHeight + options.rowHeight / 2;
                const x2 = this.state.currentDragX + this.scrollX;
                const y2 = this.state.currentDragY + this.scrollY - options.headerHeight;

                ctx.strokeStyle = options.theme.primaryColor;
                ctx.setLineDash([5, 5]);
                ctx.beginPath();
                ctx.moveTo(x1, y1);
                ctx.lineTo(x2, y2);
                ctx.stroke();
                ctx.setLineDash([]);
            }
        }
        ctx.restore();

        // 3. Draw Tasks
        ctx.save();
        ctx.translate(-scrollX, -scrollY + options.headerHeight);
        this.drawTasks(ctx, width, height);
        ctx.restore();

        // 4. Draw Header (Fixed on top)
        this.drawHeader(ctx, width);
        
        // 5. Draw Tooltip
        if (this.state.tooltip.visible) {
            this.drawTooltip(ctx, width, height);
        }

        // 6. Draw Selection Rectangle
        if (this.state.dragMode === 'selection') {
            ctx.strokeStyle = options.theme.primaryColor;
            ctx.setLineDash([5, 5]);
            ctx.strokeRect(this.state.dragStartX, this.state.dragStartY, 
                           this.state.currentDragX - this.state.dragStartX, 
                           this.state.currentDragY - this.state.dragStartY);
            ctx.setLineDash([]);
            ctx.fillStyle = options.theme.primaryColor;
            ctx.globalAlpha = 0.1;
            ctx.fillRect(this.state.dragStartX, this.state.dragStartY, 
                         this.state.currentDragX - this.state.dragStartX, 
                         this.state.currentDragY - this.state.dragStartY);
            ctx.globalAlpha = 1.0;
        }

        ctx.restore();
    }

    drawTooltip(ctx, canvasWidth, canvasHeight) {
        const task = this.data.tasks.find(t => t.id === this.state.tooltip.taskId);
        if (!task) return;

        const padding = 10;
        const lineHeight = 18;
        const lines = [
            `Name: ${task.name}`,
            `Progress: ${Math.round(task.progress * 100)}%`,
            `Critical: ${task.isCritical ? 'Yes' : 'No'}`
        ];

        ctx.font = '12px sans-serif';
        const metrics = lines.map(line => ctx.measureText(line));
        const width = Math.max(...metrics.map(m => m.width)) + padding * 2;
        const height = lines.length * lineHeight + padding * 2;

        let x = this.state.tooltip.x;
        let y = this.state.tooltip.y;

        // Smart positioning
        if (x + width > canvasWidth) x = x - width - 20;
        if (y + height > canvasHeight) y = y - height - 20;

        ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        this.drawRoundRect(ctx, x, y, width, height, 4);
        ctx.fill();

        ctx.fillStyle = '#ffffff';
        ctx.textAlign = 'left';
        ctx.textBaseline = 'top';
        lines.forEach((line, i) => {
            ctx.fillText(line, x + padding, y + padding + i * lineHeight);
        });
    }

    drawTodayLine(ctx, width, height) {
        if (!this.projectStart) return;
        
        const today = new Date();
        const diff = (today - this.projectStart) / (1000 * 60 * 60 * 24);
        
        let todayX = 0;
        if (this.bottomUnit === 'Day') todayX = diff * this.options.columnWidth;
        else if (this.bottomUnit === 'Hour') todayX = diff * 24 * this.options.columnWidth;
        else if (this.bottomUnit === 'Week') todayX = (diff / 7) * this.options.columnWidth;
        else if (this.bottomUnit === 'Month') todayX = (diff / 30.44) * this.options.columnWidth;
        else if (this.bottomUnit === 'Year') todayX = (diff / 365.25) * this.options.columnWidth;
        else if (this.bottomUnit === 'Minute15') todayX = diff * 24 * 4 * this.options.columnWidth;

        const x = todayX - this.scrollX;
        if (x > 0 && x < width) {
            ctx.strokeStyle = '#ff4d4f';
            ctx.lineWidth = 2;
            ctx.setLineDash([5, 5]);
            ctx.beginPath();
            ctx.moveTo(x, this.options.headerHeight);
            ctx.lineTo(x, height);
            ctx.stroke();
            ctx.setLineDash([]);
            
            // Today marker
            ctx.fillStyle = '#ff4d4f';
            ctx.beginPath();
            ctx.moveTo(x - 5, this.options.headerHeight);
            ctx.lineTo(x + 5, this.options.headerHeight);
            ctx.lineTo(x, this.options.headerHeight + 8);
            ctx.fill();
        }
    }

    drawGrid(ctx, width, height) {
        const { options, scrollX, scrollY } = this;
        ctx.strokeStyle = options.theme.gridColor;
        ctx.lineWidth = 1;

        // Vertical lines (Time)
        const startX = Math.floor(scrollX / options.columnWidth) * options.columnWidth;
        for (let x = startX - scrollX; x < width; x += options.columnWidth) {
            ctx.beginPath();
            ctx.moveTo(x, options.headerHeight);
            ctx.lineTo(x, height);
            ctx.stroke();
        }

        // Horizontal lines (Rows)
        const startY = Math.floor(scrollY / options.rowHeight) * options.rowHeight;
        for (let y = startY - scrollY + options.headerHeight; y < height; y += options.rowHeight) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(width, y);
            ctx.stroke();
        }
    }

    drawTasks(ctx, width, height) {
        const { data, options, scrollY, state } = this;
        const visibleStartY = scrollY;
        const visibleEndY = scrollY + height;

        data.tasks.forEach(task => {
            const taskY = task.rowIndex * options.rowHeight;
            if (taskY + options.rowHeight < visibleStartY || taskY > visibleEndY) return;

            const isHovered = state.hoveredTaskId === task.id;
            const isSelected = state.selectedTaskIds.has(task.id);
            const isDragging = state.dragTaskId === task.id;

            let x = task.x;
            let y = taskY + (options.rowHeight - options.barHeight) / 2;
            let w = Math.max(1, task.width); // Minimum width 1px

            if (isDragging) {
                // Draw original bar as semi-transparent
                ctx.globalAlpha = 0.3;
                ctx.fillStyle = task.color || options.theme.primaryColor;
                if (task.isMilestone) this.drawRhomb(ctx, x, y, options.barHeight, options.barHeight);
                else if (task.isSummary) this.drawSummaryBar(ctx, x, y, w, options.barHeight);
                else this.drawRoundRect(ctx, x, y, w, options.barHeight, 4);
                ctx.fill();
                ctx.globalAlpha = 1.0;

                // Ghost bar (preview)
                const dragX = options.snapToGrid ? state.snappedX : state.currentDragX;
                const dx = dragX - state.dragStartX;
                if (state.dragMode === 'move') x += dx;
                else if (state.dragMode === 'resize-start') { x += dx; w -= dx; }
                else if (state.dragMode === 'resize-end') { w += dx; }
                
                ctx.fillStyle = task.color || options.theme.primaryColor;
                ctx.globalAlpha = 0.6;
                if (task.isMilestone) this.drawRhomb(ctx, x, y, options.barHeight, options.barHeight);
                else if (task.isSummary) this.drawSummaryBar(ctx, x, y, w, options.barHeight);
                else this.drawRoundRect(ctx, x, y, w, options.barHeight, 4);
                ctx.fill();
                ctx.globalAlpha = 1.0;
            } else {
                // Draw Bar
                ctx.fillStyle = task.color || options.theme.primaryColor;
                if (task.isCritical && options.showCriticalPath) ctx.fillStyle = options.theme.criticalPathColor || '#ff4d4f';
                if (isHovered || isSelected) ctx.globalAlpha = 1.0;
                else ctx.globalAlpha = 0.9;
                
                ctx.shadowBlur = isHovered || isSelected ? 12 : 4;
                ctx.shadowColor = 'rgba(0,0,0,0.15)';
                ctx.shadowOffsetY = 2;

                if (task.isMilestone) {
                    this.drawRhomb(ctx, x, y, options.barHeight, options.barHeight);
                } else if (task.isSummary) {
                    this.drawSummaryBar(ctx, x, y, w, options.barHeight);
                } else {
                    // Advanced Gradient
                    const grad = ctx.createLinearGradient(x, y, x, y + options.barHeight);
                    grad.addColorStop(0, this.lightenColor(ctx.fillStyle, 25));
                    grad.addColorStop(0.5, ctx.fillStyle);
                    grad.addColorStop(1, this.lightenColor(ctx.fillStyle, -10));
                    ctx.fillStyle = grad;
                    this.drawRoundRect(ctx, x, y, w, options.barHeight, 8);
                }
                ctx.fill();
                ctx.shadowBlur = 0;
                ctx.shadowOffsetY = 0;

                // Draw Baselines
                if (options.showBaselines && task.baselineX !== undefined) {
                    ctx.save();
                    ctx.fillStyle = options.theme.baselineColor || 'rgba(0,0,0,0.2)';
                    this.drawRoundRect(ctx, task.baselineX, y + options.barHeight + 2, task.baselineWidth, 4, 2);
                    ctx.fill();
                    ctx.restore();
                }
            }

            // Progress
            if (task.progress > 0 && !task.isMilestone && !task.isSummary) {
                ctx.fillStyle = 'rgba(255,255,255,0.3)';
                let progressW = w * task.progress;
                if (isDragging && state.dragMode === 'progress') {
                    const dx = state.currentDragX - state.dragStartX;
                    progressW = Math.max(0, Math.min(w, progressW + dx));
                }
                this.drawRoundRect(ctx, x, y, progressW, options.barHeight, 4);
                ctx.fill();

                // Progress Handle
                if (isHovered || isDragging) {
                    ctx.fillStyle = '#ffffff';
                    ctx.beginPath();
                    ctx.arc(x + progressW, y + options.barHeight / 2, 4, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.strokeStyle = options.theme.primaryColor;
                    ctx.stroke();
                }
            }

            // Dependency Connector
            if (isHovered || (isDragging && state.dragMode === 'dependency')) {
                ctx.fillStyle = options.theme.primaryColor;
                ctx.beginPath();
                ctx.arc(x + w + 15, y + options.barHeight / 2, 5, 0, Math.PI * 2);
                ctx.fill();
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 2;
                ctx.stroke();
            }

            // Text
            if (w > 20) {
                ctx.globalAlpha = 1.0;
                ctx.fillStyle = task.textColor || '#ffffff';
                ctx.font = '11px sans-serif';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                const displayText = task.name;
                const textWidth = ctx.measureText(displayText).width;
                if (textWidth < w - 10) {
                    ctx.fillText(displayText, x + w / 2, y + options.barHeight / 2);
                }
            }

            // Selection Highlight
            if (isSelected) {
                ctx.strokeStyle = options.theme.primaryColor;
                ctx.lineWidth = 2;
                ctx.strokeRect(x - 2, y - 2, w + 4, options.barHeight + 4);
            }
        });
    }

    drawDependencies(ctx) {
        const { data, options } = this;
        ctx.strokeStyle = options.theme.gridColor;
        ctx.lineWidth = 1.5;

        data.dependencies.forEach(dep => {
            const fromTask = data.tasks.find(t => t.id === dep.fromTaskId);
            const toTask = data.tasks.find(t => t.id === dep.toTaskId);
            if (!fromTask || !toTask) return;

            const x1 = fromTask.x + fromTask.width;
            const y1 = fromTask.rowIndex * options.rowHeight + options.rowHeight / 2;
            const x2 = toTask.x;
            const y2 = toTask.rowIndex * options.rowHeight + options.rowHeight / 2;

            ctx.beginPath();
            ctx.moveTo(x1, y1);
            const cp1x = x1 + Math.max(30, (x2 - x1) / 2);
            const cp2x = x2 - Math.max(30, (x2 - x1) / 2);
            ctx.bezierCurveTo(cp1x, y1, cp2x, y2, x2, y2);
            ctx.stroke();

            // Arrow head
            ctx.save();
            ctx.translate(x2, y2);
            ctx.rotate(0); // Assuming horizontal arrival
            ctx.beginPath();
            ctx.moveTo(-8, -4);
            ctx.lineTo(0, 0);
            ctx.lineTo(-8, 4);
            ctx.fillStyle = ctx.strokeStyle;
            ctx.fill();
            ctx.restore();
        });
    }

    drawHeader(ctx, width) {
        const { options, scrollX, projectStart, bottomUnit } = this;
        ctx.fillStyle = options.theme.backgroundColor;
        ctx.fillRect(0, 0, width, options.headerHeight);
        
        ctx.strokeStyle = options.theme.gridColor;
        ctx.lineWidth = 1;
        
        const h2 = options.headerHeight / 2;
        
        ctx.beginPath(); ctx.moveTo(0, 0); ctx.lineTo(width, 0); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(0, h2); ctx.lineTo(width, h2); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(0, options.headerHeight); ctx.lineTo(width, options.headerHeight); ctx.stroke();

        ctx.fillStyle = options.theme.textColor;
        ctx.font = '10px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';

        const startX = Math.floor(scrollX / options.columnWidth) * options.columnWidth;
        
        let lastMonth = -1;
        for (let x = startX - scrollX; x < width; x += options.columnWidth) {
            const worldX = x + scrollX;
            const unitOffset = worldX / options.columnWidth;
            
            ctx.beginPath();
            ctx.moveTo(x, h2);
            ctx.lineTo(x, options.headerHeight);
            ctx.stroke();

            if (projectStart) {
                const date = new Date(projectStart);
                if (bottomUnit === 'Day') date.setDate(date.getDate() + unitOffset);
                else if (bottomUnit === 'Hour') date.setHours(date.getHours() + unitOffset);
                else if (bottomUnit === 'Week') date.setDate(date.getDate() + unitOffset * 7);
                else if (bottomUnit === 'Month') date.setMonth(date.getMonth() + unitOffset);
                else if (bottomUnit === 'Year') date.setFullYear(date.getFullYear() + unitOffset);
                
                // Draw Month/Year on top level
                if (date.getMonth() !== lastMonth) {
                    ctx.save();
                    ctx.font = 'bold 11px sans-serif';
                    const monthLabel = date.toLocaleString('default', { month: 'long', year: 'numeric' });
                    ctx.fillText(monthLabel, x + 50, h2 / 2); // Simplified positioning
                    ctx.restore();
                    lastMonth = date.getMonth();
                    
                    // Draw separator for top level
                    ctx.beginPath();
                    ctx.moveTo(x, 0);
                    ctx.lineTo(x, h2);
                    ctx.stroke();
                }

                let label = '';
                if (bottomUnit === 'Day') label = date.getDate();
                else if (bottomUnit === 'Hour') label = date.getHours() + ':00';
                else if (bottomUnit === 'Month') label = date.toLocaleString('default', { month: 'short' });
                
                if (options.columnWidth > 20) {
                    ctx.fillText(label, x + options.columnWidth / 2, h2 + h2 / 2);
                }
            }
        }
    }

    drawRoundRect(ctx, x, y, width, height, radius) {
        if (width < 0) return;
        ctx.beginPath();
        ctx.moveTo(x + radius, y);
        ctx.lineTo(x + width - radius, y);
        ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
        ctx.lineTo(x + width, y + height - radius);
        ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
        ctx.lineTo(x + radius, y + height);
        ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
        ctx.lineTo(x, y + radius);
        ctx.quadraticCurveTo(x, y, x + radius, y);
        ctx.closePath();
    }

    drawRhomb(ctx, x, y, width, height) {
        const cx = x + width / 2;
        const cy = y + height / 2;
        ctx.beginPath();
        ctx.moveTo(cx, y);
        ctx.lineTo(x + width, cy);
        ctx.lineTo(cx, y + height);
        ctx.lineTo(x, cy);
        ctx.closePath();
    }

    drawSummaryBar(ctx, x, y, width, height) {
        const h2 = height / 2;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(x + width, y);
        ctx.lineTo(x + width, y + height - 4);
        ctx.lineTo(x + width - 10, y + height + 2);
        ctx.lineTo(x + width - 10, y + height - 4);
        ctx.lineTo(x + 10, y + height - 4);
        ctx.lineTo(x + 10, y + height + 2);
        ctx.lineTo(x, y + height - 4);
        ctx.closePath();
    }

    lightenColor(color, percent) {
        const num = parseInt(color.replace("#", ""), 16),
            amt = Math.round(2.55 * percent),
            R = (num >> 16) + amt,
            G = (num >> 8 & 0x00FF) + amt,
            B = (num & 0x0000FF) + amt;
        return "#" + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 + (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 + (B < 255 ? B < 1 ? 0 : B : 255)).toString(16).slice(1);
    }
}

export function init(container, dotNetRef, options) {
    return new SgGantCanvas(container, dotNetRef, options);
}

export function setData(instance, data) {
    instance.setData(data);
}

export function setOptions(instance, options) {
    instance.setOptions(options);
}

export function setZoom(instance, zoom) {
    instance.setZoom(zoom);
}

export function scrollTo(instance, x, y) {
    if (x !== null && x !== undefined) instance.scrollX = x;
    if (y !== null && y !== undefined) instance.scrollY = y;
    instance.needsUpdate = true;
    instance.syncLeftScroll();
}

export function dispose(instance) {
    if (instance && typeof instance.dispose === 'function') {
        instance.dispose();
    }
}
