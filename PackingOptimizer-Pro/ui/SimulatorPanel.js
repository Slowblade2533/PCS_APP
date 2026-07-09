/**
 * SimulatorPanel Component for Packing Optimizer Pro
 * Manages the quantity scaling simulation (Qty 1 to n) and limits packing weight to 20kg.
 */
class SimulatorPanel {
    constructor(productDb, boxDb, onSelectSimulationRun) {
        this.productDb = productDb;
        this.boxDb = boxDb;
        this.onSelectSimulationRun = onSelectSimulationRun;

        this.tabBtn = document.getElementById('tab-simulator');
        this.pane = document.getElementById('pane-simulator');
        this.selectSku = document.getElementById('sim-product-select');
        this.inputMaxQty = document.getElementById('sim-max-qty');
        this.inputMaxWeight = document.getElementById('sim-max-weight');
        this.runBtn = document.getElementById('btn-run-simulation');
        this.resultsContainer = document.getElementById('simulation-results-container');

        this.init();
    }

    init() {
        if (this.tabBtn) {
            this.tabBtn.addEventListener('click', () => this.activateTab());
        }

        if (this.runBtn) {
            this.runBtn.addEventListener('click', () => this.runSimulation());
        }

        this.updateSkuDropdown();
    }

    activateTab() {
        document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
        document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));

        this.tabBtn.classList.add('active');
        this.pane.classList.add('active');
        this.updateSkuDropdown();
    }

    updateSkuDropdown() {
        if (!this.selectSku) return;
        const products = this.productDb.getAll();
        
        if (products.length === 0) {
            this.selectSku.innerHTML = `<option value="">-- กรุณาเพิ่มสินค้าก่อน --</option>`;
            return;
        }

        this.selectSku.innerHTML = products.map(p => `
            <option value="${p.id}">${escapeHtml(p.sku)} - ${escapeHtml(p.name)}</option>
        `).join('');
    }

    // Delegate simulation run to ShipmentEngine.optimizePacking
    runSingleSimulationPacking(tempProducts, sortedBoxes, tempSettings) {
        const optimization = ShipmentEngine.optimizePacking(tempProducts, sortedBoxes, tempSettings);
        if (!optimization.success) return null;

        const parcels = optimization.parcels;

        parcels.forEach(p => {
            p.packagingDetails = MathUtil.calculatePackagingDetails(p, tempSettings, this.productDb);
        });

        const totalBoxCost = parcels.reduce((acc, p) => acc + p.box.cost, 0);
        const totalTapeCost = parcels.reduce((acc, p) => acc + p.packagingDetails.tapeCost, 0);
        const totalBubbleCost = parcels.reduce((acc, p) => acc + p.packagingDetails.bubbleCost, 0);
        const totalLabelCost = parcels.reduce((acc, p) => acc + p.packagingDetails.labelCost, 0);
        const totalCost = totalBoxCost + totalTapeCost + totalBubbleCost + totalLabelCost;

        const avgVolume = parcels.reduce((acc, p) => acc + p.volumeUtilization, 0) / parcels.length;
        const totalCargoWeight = parcels.reduce((acc, p) => acc + (p.cargoWeight || 0), 0);
        const totalTareWeight  = parcels.reduce((acc, p) => acc + (p.box.tare || 0), 0);
        const totalParcelWeight = totalCargoWeight + totalTareWeight;

        return {
            parcels: parcels,
            totalBoxCost: totalBoxCost,
            totalTapeCost: totalTapeCost,
            totalBubbleCost: totalBubbleCost,
            totalLabelCost: totalLabelCost,
            totalCost: totalCost,
            totalVolumeUtilization: avgVolume,
            totalWeight: totalCargoWeight,
            totalTareWeight: totalTareWeight,
            totalParcelWeight: totalParcelWeight
        };
    }

    runSimulation() {
        const productId = this.selectSku.value;
        if (!productId) {
            showToast('กรุณาเลือกสินค้าที่ต้องการจำลอง', 'error');
            return;
        }

        const product = this.productDb.getById(productId);
        if (!product) return;

        const maxQty = parseInt(this.inputMaxQty.value) || 10;
        const maxWeight = parseFloat(this.inputMaxWeight.value) || 20.0;

        const boxes = this.boxDb.getAll();
        if (boxes.length === 0) {
            showToast('กรุณาเพิ่มกล่องในระบบก่อนคำนวณ', 'error');
            return;
        }

        this.resultsContainer.innerHTML = `
            <div style="text-align: center; padding: 24px; color: var(--text-secondary);">
                <div style="font-size: 24px; margin-bottom: 8px; animation: spin 1.5s linear infinite; display: inline-block;">🔄</div>
                <p style="font-size: 13px;">กำลังวิเคราะห์พัสดุจำนวน 1 ถึง ${maxQty} ชิ้น...</p>
            </div>
        `;

        setTimeout(() => {
            const rows = [];
            const sortedBoxes = [...boxes].sort((a, b) => a.cost - b.cost);

            for (let q = 1; q <= maxQty; q++) {
                const tempProducts = [{
                    ...product,
                    quantity: q
                }];

                const tempSettings = {
                    bubbleThickness: parseFloat(document.getElementById('cfg-bubble-thickness').value) || 0.0,
                    bubbleLayers: parseInt(document.getElementById('cfg-bubble-layers').value) || 0,
                    foamThickness: parseFloat(document.getElementById('cfg-foam-thickness').value) || 0.0,
                    cartonThickness: parseFloat(document.getElementById('cfg-carton-thickness').value) || 0.0,
                    stretchFilm: document.getElementById('cfg-stretch-film').checked,
                    allowSplit: true,
                    maxParcelWeight: maxWeight
                };

                const result = this.runSingleSimulationPacking(tempProducts, sortedBoxes, tempSettings);

                if (result) {
                    const boxSummary = {};
                    result.parcels.forEach(p => {
                        boxSummary[p.box.name] = (boxSummary[p.box.name] || 0) + 1;
                    });
                    const boxText = Object.entries(boxSummary).map(([name, count]) => `${name} (${count} ใบ)`).join(', ');

                    rows.push({
                        qty: q,
                        cargoWeight: result.totalWeight,
                        tareWeight: result.totalTareWeight,
                        parcelWeight: result.totalParcelWeight,
                        boxText: boxText,
                        cost: result.totalCost,
                        utilization: result.totalVolumeUtilization,
                        resultData: result
                    });
                } else {
                    rows.push({
                        qty: q,
                        cargoWeight: product.weight * q,
                        tareWeight: 0,
                        parcelWeight: product.weight * q,
                        boxText: '<span style="color: var(--color-danger);">ขนาดเกินขีดจำกัดกล่อง</span>',
                        cost: 0,
                        utilization: 0,
                        failed: true
                    });
                }
            }

            this.renderResultsTable(rows);
        }, 80);
    }

    renderResultsTable(rows) {
        if (rows.length === 0) {
            this.resultsContainer.innerHTML = '';
            return;
        }

        this.resultsContainer.innerHTML = `
            <div style="overflow-x: auto; margin-top: 10px; border: 1px solid var(--border-color); border-radius: var(--radius-md);">
                <table style="width: 100%; border-collapse: collapse; text-align: left; font-size: 12px; background-color: var(--bg-secondary);">
                    <thead>
                        <tr style="background-color: var(--bg-tertiary); border-bottom: 1px solid var(--border-color);">
                            <th style="padding: 10px 8px; font-weight: 700; width: 45px;">ชิ้น</th>
                            <th style="padding: 10px 8px; font-weight: 700; width: 90px;" title="นน.สินค้า + นน.กล่อง = พัสดุรวม"> ⚖️ พัสดุ (กก.)</th>
                            <th style="padding: 10px 8px; font-weight: 700;">กล่องแนะนำ</th>
                            <th style="padding: 10px 8px; font-weight: 700; width: 65px; text-align: right;">ค่ากล่อง</th>
                            <th style="padding: 10px 8px; font-weight: 700; width: 50px; text-align: center;">ดูรูป</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rows.map(r => `
                            <tr style="border-bottom: 1px solid var(--border-color); transition: var(--transition-smooth);" class="sim-row-hover">
                                <td style="padding: 10px 8px; font-weight: 600;">${r.qty}</td>
                                <td style="padding: 10px 8px; color: var(--text-secondary);" title="สินค้า: ${(r.cargoWeight||0).toFixed(2)} กก. + กล่อง: ${(r.tareWeight||0).toFixed(2)} กก.">
                                    <span style="font-weight:700;">${(r.parcelWeight||0).toFixed(2)}</span>
                                    <div style="font-size:9px; color:var(--text-muted); line-height:1.2;">
                                        <span>${(r.cargoWeight||0).toFixed(2)}+${(r.tareWeight||0).toFixed(2)}</span>
                                    </div>
                                </td>
                                <td style="padding: 10px 8px; font-weight: 500; font-size: 11px;">${r.boxText}</td>
                                <td style="padding: 10px 8px; text-align: right; font-weight: 700; color: var(--color-primary);">
                                    ${r.failed ? '-' : r.cost.toFixed(1) + ' ฿'}
                                </td>
                                <td style="padding: 10px 8px; text-align: center;">
                                    ${r.failed ? '-' : `<button class="btn btn-secondary btn-sm btn-view-sim-layout" data-idx="${r.qty - 1}" style="padding: 3px 6px; font-size: 10px; border-radius: 4px;">👁️</button>`}
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
            
            <style>
                .sim-row-hover:hover {
                    background-color: var(--bg-primary);
                }
            </style>
        `;

        // Bind View buttons
        this.resultsContainer.querySelectorAll('.btn-view-sim-layout').forEach(btn => {
            btn.addEventListener('click', () => {
                const idx = parseInt(btn.dataset.idx);
                const row = rows[idx];
                if (row && row.resultData) {
                    // Trigger callback to render this specific layout in the main Visualizer
                    this.onSelectSimulationRun(row.resultData);
                    showToast(`แสดงแบบจำลองจำนวนสินค้า ${row.qty} ชิ้น`, 'success');
                }
            });
        });
    }
}

window.SimulatorPanel = SimulatorPanel;
