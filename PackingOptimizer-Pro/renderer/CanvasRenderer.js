/**
 * CanvasRenderer for Packing Optimizer Pro
 * Implements a high-performance, 100% offline 3D Isometric container renderer using standard 2D HTML5 Canvas.
 * Supports zoom, pan, layer filtering, SKU coloring, and placement order numbering.
 */
class CanvasRenderer {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas.getContext('2d');
        
        // Render view state
        this.scale = 10;
        this.panX = 0;
        this.panY = 0;
        this.currentParcel = null;
        this.currentLayerFilter = 'all'; // 'all' or 'base'
        
        // Mouse drag state
        this.isDragging = false;
        this.lastMouseX = 0;
        this.lastMouseY = 0;

        this.cos30 = Math.cos(Math.PI / 6); // 0.866
        this.sin30 = Math.sin(Math.PI / 6); // 0.5

        this.init();
    }

    init() {
        this.resize();
        
        // Window Resize
        window.addEventListener('resize', () => {
            this.resize();
            this.draw();
        });

        // Mouse Drag / Pan listeners
        this.canvas.addEventListener('mousedown', (e) => this.handleMouseDown(e));
        this.canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e));
        this.canvas.addEventListener('mouseup', () => this.handleMouseUp());
        this.canvas.addEventListener('mouseleave', () => this.handleMouseUp());

        // Zoom button bindings
        const zoomIn = document.getElementById('btn-zoom-in');
        if (zoomIn) zoomIn.addEventListener('click', () => this.adjustZoom(1.2));
        
        const zoomOut = document.getElementById('btn-zoom-out');
        if (zoomOut) zoomOut.addEventListener('click', () => this.adjustZoom(0.8));

        const resetView = document.getElementById('btn-reset-view');
        if (resetView) resetView.addEventListener('click', () => this.resetView());

        // Layer selector binding
        const layerSelect = document.getElementById('layer-select');
        if (layerSelect) {
            layerSelect.addEventListener('change', (e) => {
                this.currentLayerFilter = e.target.value;
                this.draw();
            });
        }
    }

    resize() {
        const container = this.canvas.parentElement;
        this.canvas.width = container.clientWidth;
        this.canvas.height = container.clientHeight;
    }

    resetView() {
        if (!this.currentParcel) return;
        this.autoFitScale(this.currentParcel.box);
        this.draw();
    }

    adjustZoom(factor) {
        this.scale *= factor;
        this.draw();
    }

    handleMouseDown(e) {
        this.isDragging = true;
        this.lastMouseX = e.clientX;
        this.lastMouseY = e.clientY;
        this.canvas.style.cursor = 'grabbing';
    }

    handleMouseMove(e) {
        if (!this.isDragging) return;
        const dx = e.clientX - this.lastMouseX;
        const dy = e.clientY - this.lastMouseY;
        this.panX += dx;
        this.panY += dy;
        this.lastMouseX = e.clientX;
        this.lastMouseY = e.clientY;
        this.draw();
    }

    handleMouseUp() {
        this.isDragging = false;
        this.canvas.style.cursor = 'grab';
    }

    // Projects 3D Box coordinates to 2D Canvas space
    proj(x, y, z, box) {
        const centerX = this.canvas.width / 2 + this.panX;
        const centerY = this.canvas.height / 2 + this.panY;
        
        // Center the coordinate origin inside the box view for balanced rotation perspective
        const ox = x - box.width / 2;
        const oy = y - box.length / 2;
        const oz = z - box.height / 2;

        return {
            x: centerX + (ox - oy) * this.cos30 * this.scale,
            y: centerY + (ox + oy) * this.sin30 * this.scale - oz * this.scale
        };
    }

    autoFitScale(box) {
        const bw = box.width;
        const bl = box.length;
        const bh = box.height;

        // Project the 8 bounding corners at scale = 1, centered at 0,0
        const corners = [
            [0, 0, 0], [bw, 0, 0], [bw, bl, 0], [0, bl, 0],
            [0, 0, bh], [bw, 0, bh], [bw, bl, bh], [0, bl, bh]
        ];

        let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
        corners.forEach(([x, y, z]) => {
            const ox = x - bw / 2;
            const oy = y - bl / 2;
            const oz = z - bh / 2;
            const px = (ox - oy) * this.cos30;
            const py = (ox + oy) * this.sin30 - oz;
            if (px < minX) minX = px;
            if (px > maxX) maxX = px;
            if (py < minY) minY = py;
            if (py > maxY) maxY = py;
        });

        const projW = maxX - minX;
        const projH = maxY - minY;

        const scaleX = (this.canvas.width * 0.7) / projW;
        const scaleY = (this.canvas.height * 0.7) / projH;

        this.scale = Math.min(scaleX, scaleY);
        this.panX = 0;
        this.panY = 0;
    }

    // Generates distinctive HSL colors for each SKU
    getSkuColors(sku) {
        let hash = 0;
        for (let i = 0; i < sku.length; i++) {
            hash = sku.charCodeAt(i) + ((hash << 5) - hash);
        }
        const hue = Math.abs(hash) % 360;
        return {
            top: `hsl(${hue}, 80%, 65%)`,
            left: `hsl(${hue}, 80%, 55%)`,
            right: `hsl(${hue}, 80%, 45%)`,
            border: `hsl(${hue}, 80%, 30%)`
        };
    }

    clear() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    }

    drawPlaceholder() {
        this.clear();
        this.ctx.fillStyle = '#64748b';
        this.ctx.font = '14px sans-serif';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText('โปรดกดเริ่มคำนวณการจัดกล่องเพื่อแสดงผล 3D', this.canvas.width / 2, this.canvas.height / 2);
    }

    draw(parcel = null) {
        if (parcel) {
            this.currentParcel = parcel;
            if (this.scale === 10 && this.panX === 0) {
                this.autoFitScale(parcel.box);
            }
        }

        if (!this.currentParcel) {
            this.drawPlaceholder();
            return;
        }

        this.clear();
        
        const box = this.currentParcel.box;
        const placements = this.currentParcel.placements;

        // 1. Draw outer box background / back walls (dashed lines)
        this.drawBoxWireframe(box);

        // 2. Filter & Sort placements (Painter's Algorithm: Back-to-Front)
        // Sort by Z (bottom-to-top), then X (back-to-front), then Y (left-to-right)
        let filteredPlacements = [...placements];
        if (this.currentLayerFilter === 'base') {
            filteredPlacements = filteredPlacements.filter(p => p.z === 0);
        }

        filteredPlacements.sort((a, b) => {
            if (a.z !== b.z) return a.z - b.z;
            if (a.x !== b.x) return a.x - b.x;
            return a.y - b.y;
        });

        // 3. Draw each item (cuboid)
        filteredPlacements.forEach((p, index) => {
            this.drawItemCuboid(p, index + 1, box);
        });

        // 4. Draw front wireframe edges of the box for realistic overlays
        this.drawBoxWireframeFront(box);
    }

    drawBoxWireframe(box) {
        const bw = box.width;
        const bl = box.length;
        const bh = box.height;

        this.ctx.save();
        this.ctx.strokeStyle = 'rgba(255, 255, 255, 0.12)';
        this.ctx.lineWidth = 1.5;
        this.ctx.setLineDash([4, 4]);

        const drawLine = (x1, y1, z1, x2, y2, z2) => {
            const p1 = this.proj(x1, y1, z1, box);
            const p2 = this.proj(x2, y2, z2, box);
            this.ctx.beginPath();
            this.ctx.moveTo(p1.x, p1.y);
            this.ctx.lineTo(p2.x, p2.y);
            this.ctx.stroke();
        };

        // Draw back edges (connecting corner 0,0,0)
        drawLine(0, 0, 0, bw, 0, 0);
        drawLine(0, 0, 0, 0, bl, 0);
        drawLine(0, 0, 0, 0, 0, bh);

        this.ctx.restore();
    }

    drawBoxWireframeFront(box) {
        const bw = box.width;
        const bl = box.length;
        const bh = box.height;

        this.ctx.save();
        
        // Draw outline of box in clean overlay style
        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
        this.ctx.strokeStyle = isDark ? 'rgba(255, 255, 255, 0.25)' : 'rgba(15, 23, 42, 0.25)';
        this.ctx.lineWidth = 2;

        const drawLine = (x1, y1, z1, x2, y2, z2) => {
            const p1 = this.proj(x1, y1, z1, box);
            const p2 = this.proj(x2, y2, z2, box);
            this.ctx.beginPath();
            this.ctx.moveTo(p1.x, p1.y);
            this.ctx.lineTo(p2.x, p2.y);
            this.ctx.stroke();
        };

        // Top edges
        drawLine(0, 0, bh, bw, 0, bh);
        drawLine(bw, 0, bh, bw, bl, bh);
        drawLine(bw, bl, bh, 0, bl, bh);
        drawLine(0, bl, bh, 0, 0, bh);

        // Front edges
        drawLine(bw, 0, 0, bw, bl, 0);
        drawLine(bw, bl, 0, 0, bl, 0);
        
        // Columns
        drawLine(bw, 0, 0, bw, 0, bh);
        drawLine(0, bl, 0, 0, bl, bh);
        drawLine(bw, bl, 0, bw, bl, bh);

        this.ctx.restore();
    }

    drawItemCuboid(item, seqNum, box) {
        const x = item.x;
        const y = item.y;
        const z = item.z;
        const w = item.width;
        const l = item.length;
        const h = item.height;

        const colors = this.getSkuColors(item.sku);

        const drawFace = (p1, p2, p3, p4, fillStyle) => {
            this.ctx.beginPath();
            this.ctx.moveTo(p1.x, p1.y);
            this.ctx.lineTo(p2.x, p2.y);
            this.ctx.lineTo(p3.x, p3.y);
            this.ctx.lineTo(p4.x, p4.y);
            this.ctx.closePath();
            this.ctx.fillStyle = fillStyle;
            this.ctx.fill();
            this.ctx.strokeStyle = colors.border;
            this.ctx.lineWidth = 0.5;
            this.ctx.stroke();
        };

        // 1. Top Face
        const t1 = this.proj(x, y, z + h, box);
        const t2 = this.proj(x + w, y, z + h, box);
        const t3 = this.proj(x + w, y + l, z + h, box);
        const t4 = this.proj(x, y + l, z + h, box);
        drawFace(t1, t2, t3, t4, colors.top);

        // 2. Front-Left Face
        const fl1 = this.proj(x, y + l, z, box);
        const fl2 = this.proj(x + w, y + l, z, box);
        const fl3 = this.proj(x + w, y + l, z + h, box);
        const fl4 = this.proj(x, y + l, z + h, box);
        drawFace(fl1, fl2, fl3, fl4, colors.left);

        // 3. Front-Right Face
        const fr1 = this.proj(x + w, y, z, box);
        const fr2 = this.proj(x + w, y + l, z, box);
        const fr3 = this.proj(x + w, y + l, z + h, box);
        const fr4 = this.proj(x + w, y, z + h, box);
        drawFace(fr1, fr2, fr3, fr4, colors.right);

        // 4. Draw placement sequence text on the Top Face
        // Center of the Top Face
        const topCenter = this.proj(x + w / 2, y + l / 2, z + h, box);
        
        this.ctx.save();
        this.ctx.fillStyle = '#0f172a'; // Dark slate for clean readability
        this.ctx.font = 'bold 11px sans-serif';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        
        // Draw number text with a small white circular background
        this.ctx.beginPath();
        this.ctx.arc(topCenter.x, topCenter.y, 8, 0, Math.PI * 2);
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fill();
        this.ctx.strokeStyle = '#0f172a';
        this.ctx.lineWidth = 1;
        this.ctx.stroke();

        this.ctx.fillStyle = '#0f172a';
        this.ctx.fillText(seqNum, topCenter.x, topCenter.y + 0.5);
        this.ctx.restore();
    }
}

window.CanvasRenderer = CanvasRenderer;
