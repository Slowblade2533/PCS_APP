/**
 * Dashboard Component for Packing Optimizer Pro
 * Manages stats representation, config values gathering, and triggering optimizer in Thai.
 */
class Dashboard {
    constructor(productDb, boxDb, onRunOptimization) {
        this.productDb = productDb;
        this.boxDb = boxDb;
        this.onRunOptimization = onRunOptimization;
        
        this.init();
    }

    init() {
        // Run button listener
        const runBtn = document.getElementById('btn-run-optimization');
        if (runBtn) {
            runBtn.addEventListener('click', () => this.handleRun());
        }

        this.updateStats();
    }

    updateStats() {
        const products = this.productDb.getAll();
        const boxes = this.boxDb.getAll();

        const totalSKUs = products.length;
        const totalItems = products.reduce((acc, p) => acc + p.quantity, 0);
        const totalBoxes = boxes.length;

        // Calculate total items volume in Liters
        // Vol = (W * L * H * Qty) / 1000 cm3
        const totalVolumeCm3 = products.reduce((acc, p) => acc + (p.width * p.length * p.height * p.quantity), 0);
        const totalVolumeLiters = totalVolumeCm3 / 1000;

        document.getElementById('stat-total-skus').textContent = totalSKUs;
        document.getElementById('stat-total-items').textContent = totalItems;
        document.getElementById('stat-total-boxes').textContent = totalBoxes;
        document.getElementById('stat-total-volume').textContent = totalVolumeLiters.toFixed(1) + ' L';
    }

    getSettings() {
        return {
            bubbleThickness: parseFloat(document.getElementById('cfg-bubble-thickness').value) || 0.0,
            bubbleLayers: parseInt(document.getElementById('cfg-bubble-layers').value) || 0,
            foamThickness: parseFloat(document.getElementById('cfg-foam-thickness').value) || 0.0,
            stretchFilm: document.getElementById('cfg-stretch-film').checked,
            cartonThickness: parseFloat(document.getElementById('cfg-carton-thickness').value) || 0.0,
            algorithm: document.getElementById('cfg-algorithm').value || 'laff',
            maxParcelWeight: parseFloat(document.getElementById('cfg-max-weight').value) || 20.0,
            allowSplit: document.getElementById('cfg-allow-split').checked
        };
    }

    handleRun() {
        const products = this.productDb.getAll();
        const boxes = this.boxDb.getAll();

        if (products.length === 0) {
            showToast('กรุณาเพิ่มสินค้าลงในรายการอย่างน้อย 1 รายการก่อนคำนวณ', 'error');
            return;
        }

        if (boxes.length === 0) {
            showToast('กรุณาเลือกหรือเพิ่มขนาดกล่องในระบบอย่างน้อย 1 ขนาด', 'error');
            return;
        }

        showToast('กำลังคำนวณการจัดกล่อง...', 'info');
        
        // Trigger external coordinator routine
        const settings = this.getSettings();
        this.onRunOptimization(products, boxes, settings);
    }
}

window.Dashboard = Dashboard;
