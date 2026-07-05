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

    // Local Helper: Dimensional check
    canProductFitInBox(p, box, settings) {
        const padding = (settings.bubbleThickness * settings.bubbleLayers * 2) + (settings.foamThickness * 2);
        const pw = p.width + padding;
        const pl = p.length + padding;
        const ph = p.height + padding;

        const bw = box.width - (settings.cartonThickness * 2);
        const bl = box.length - (settings.cartonThickness * 2);
        const bh = box.height - (settings.cartonThickness * 2);

        if (bw <= 0 || bl <= 0 || bh <= 0) return false;
        if (p.weight > box.maxWeight || p.weight > settings.maxParcelWeight) return false;

        const orientations = [
            [pw, pl, ph], [pw, ph, pl], [pl, pw, ph],
            [pl, ph, pw], [ph, pw, pl], [ph, pl, pw]
        ];

        for (const [w, l, h] of orientations) {
            let validOrientation = true;
            if (!p.rotationAllowed.roll || !p.rotationAllowed.pitch) {
                if (h !== ph) validOrientation = false;
            }
            if (!p.rotationAllowed.yaw) {
                if (w !== pw || l !== pl) validOrientation = false;
            }

            if (validOrientation && w <= bw && l <= bl && h <= bh) {
                return true;
            }
        }
        return false;
    }

    // Local Helper: Pack cargo into a single box
    packCargo(box, productsList, settings) {
        const bw = box.width - (settings.cartonThickness * 2);
        const bl = box.length - (settings.cartonThickness * 2);
        const bh = box.height - (settings.cartonThickness * 2);

        if (bw <= 0 || bl <= 0 || bh <= 0) return null;

        let spaces = [{ x: 0, y: 0, z: 0, w: bw, l: bl, h: bh }];
        const padding = (settings.bubbleThickness * settings.bubbleLayers * 2) + (settings.foamThickness * 2);
        
        const items = [];
        productsList.forEach(p => {
            for (let i = 0; i < p.quantity; i++) {
                items.push({
                    id: p.id,
                    sku: p.sku,
                    name: p.name,
                    w: p.width + padding,
                    l: p.length + padding,
                    h: p.height + padding,
                    weight: p.weight,
                    padding: padding,
                    rotationAllowed: p.rotationAllowed
                });
            }
        });

        items.sort((a, b) => (b.w * b.l * b.h) - (a.w * a.l * a.h));

        const placements = [];
        let currentWeight = 0;

        for (const item of items) {
            // Check box weight limit and general simulation weight limit (e.g. 20kg)
            if (currentWeight + item.weight > box.maxWeight || currentWeight + item.weight > settings.maxParcelWeight) {
                return null;
            }

            let bestPlacement = null;
            let bestScore = Infinity;

            for (const space of spaces) {
                const orientations = [
                    [item.w, item.l, item.h], [item.w, item.h, item.l], [item.l, item.w, item.h],
                    [item.l, item.h, item.w], [item.h, item.w, item.l], [item.h, item.l, item.w]
                ];

                for (const [iw, il, ih] of orientations) {
                    let validOrientation = true;
                    if (!item.rotationAllowed.roll || !item.rotationAllowed.pitch) {
                        if (ih !== item.h) validOrientation = false;
                    }
                    if (!item.rotationAllowed.yaw) {
                        if (iw !== item.w || il !== item.l) validOrientation = false;
                    }

                    if (validOrientation && iw <= space.w && il <= space.l && ih <= space.h) {
                        const score = space.z * 1000000 + space.y * 1000 + space.x;
                        if (score < bestScore) {
                            bestScore = score;
                            bestPlacement = {
                                space: space,
                                w: iw, l: il, h: ih,
                                x: space.x, y: space.y, z: space.z
                            };
                        }
                    }
                }
            }

            if (bestPlacement) {
                const px = bestPlacement.x + item.padding / 2;
                const py = bestPlacement.y + item.padding / 2;
                const pz = bestPlacement.z + item.padding / 2;
                const pw = bestPlacement.w - item.padding;
                const pl = bestPlacement.l - item.padding;
                const ph = bestPlacement.h - item.padding;

                placements.push({
                    productId: item.id,
                    sku: item.sku,
                    name: item.name,
                    x: px, y: py, z: pz,
                    width: pw, length: pl, height: ph,
                    orientation: 0
                });

                currentWeight += item.weight;

                const sx = bestPlacement.x;
                const sy = bestPlacement.y;
                const sz = bestPlacement.z;
                const sw = bestPlacement.w;
                const sl = bestPlacement.l;
                const sh = bestPlacement.h;

                let nextSpaces = [];
                for (const S of spaces) {
                    const overlapX = (S.x < sx + sw) && (S.x + S.w > sx);
                    const overlapY = (S.y < sy + sl) && (S.y + S.l > sy);
                    const overlapZ = (S.z < sz + sh) && (S.z + S.h > sz);

                    if (overlapX && overlapY && overlapZ) {
                        if (sx + sw < S.x + S.w) nextSpaces.push({ x: sx + sw, y: S.y, z: S.z, w: (S.x + S.w) - (sx + sw), l: S.l, h: S.h });
                        if (S.x < sx) nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: sx - S.x, l: S.l, h: S.h });
                        if (sy + sl < S.y + S.l) nextSpaces.push({ x: S.x, y: sy + sl, z: S.z, w: S.w, l: (S.y + S.l) - (sy + sl), h: S.h });
                        if (S.y < sy) nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: S.w, l: sy - S.y, h: S.h });
                        if (sz + sh < S.z + S.h) nextSpaces.push({ x: S.x, y: S.y, z: sz + sh, w: S.w, l: S.l, h: (S.z + S.h) - (sz + sh) });
                        if (S.z < sz) nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: S.w, l: S.l, h: sz - S.z });
                    } else {
                        nextSpaces.push(S);
                    }
                }

                spaces = [];
                for (let i = 0; i < nextSpaces.length; i++) {
                    let isSubset = false;
                    for (let j = 0; j < nextSpaces.length; j++) {
                        if (i === j) continue;
                        const a = nextSpaces[i];
                        const b = nextSpaces[j];
                        if (a.x >= b.x && a.y >= b.y && a.z >= b.z &&
                            a.x + a.w <= b.x + b.w &&
                            a.y + a.l <= b.y + b.l &&
                            a.z + a.h <= b.z + b.h) {
                            isSubset = true;
                            break;
                        }
                    }
                    if (!isSubset) spaces.push(nextSpaces[i]);
                }
            } else {
                return null;
            }
        }

        const totalItemVol = productsList.reduce((acc, p) => acc + (p.width * p.length * p.height * p.quantity), 0);
        const boxVol = box.width * box.length * box.height;

        let maxPlacedX = 0;
        let maxPlacedY = 0;
        let maxPlacedZ = 0;
        placements.forEach(pl => {
            if (pl.x + pl.width > maxPlacedX) maxPlacedX = pl.x + pl.width;
            if (pl.y + pl.length > maxPlacedY) maxPlacedY = pl.y + pl.length;
            if (pl.z + pl.height > maxPlacedZ) maxPlacedZ = pl.z + pl.height;
        });

        const gapW = Math.max(0, bw - maxPlacedX);
        const gapL = Math.max(0, bl - maxPlacedY);
        const gapH = Math.max(0, bh - maxPlacedZ);
        const hasSnugLock = (gapW <= 1.0) || (gapL <= 1.0) || (gapH <= 1.0);

        return {
            box: box,
            weight: currentWeight,
            volumeUtilization: Math.min(99.0, (totalItemVol / boxVol) * 100),
            isFullFit: true,
            placements: placements,
            gaps: { width: gapW, length: gapL, height: gapH },
            hasSnugLock: hasSnugLock
        };
    }

    runSingleSimulationPacking(tempProducts, sortedBoxes, tempSettings) {
        // 1. Try single box packing first
        for (const box of sortedBoxes) {
            const solution = this.packCargo(box, tempProducts, tempSettings);
            if (solution !== null) {
                solution.packagingDetails = MathUtil.calculatePackagingDetails(solution, tempSettings, this.productDb);
                return {
                    parcels: [solution],
                    totalBoxCost: solution.box.cost,
                    totalTapeCost: solution.packagingDetails.tapeCost,
                    totalBubbleCost: solution.packagingDetails.bubbleCost,
                    totalLabelCost: solution.packagingDetails.labelCost,
                    totalCost: solution.packagingDetails.totalParcelMaterialCost,
                    totalVolumeUtilization: solution.volumeUtilization,
                    totalWeight: solution.weight
                };
            }
        }

        // 2. Fallback to greedy multi-box split
        const itemsToPack = [];
        tempProducts.forEach(p => {
            for (let i = 0; i < p.quantity; i++) {
                itemsToPack.push(p);
            }
        });

        const parcels = [];
        while (itemsToPack.length > 0) {
            const firstItem = itemsToPack[0];
            const targetBox = sortedBoxes.find(b => this.canProductFitInBox(firstItem, b, tempSettings));
            if (!targetBox) return null; // Unpackable

            let currentBoxPlacements = [];
            let currentWeight = 0;
            const bw = targetBox.width - (tempSettings.cartonThickness * 2);
            const bl = targetBox.length - (tempSettings.cartonThickness * 2);
            const bh = targetBox.height - (tempSettings.cartonThickness * 2);

            let spaces = [{ x: 0, y: 0, z: 0, w: bw, l: bl, h: bh }];
            const padding = (tempSettings.bubbleThickness * tempSettings.bubbleLayers * 2) + (tempSettings.foamThickness * 2);
            const packedIndices = [];

            for (let i = 0; i < itemsToPack.length; i++) {
                const p = itemsToPack[i];
                const iw = p.width + padding;
                const il = p.length + padding;
                const ih = p.height + padding;

                if (currentWeight + p.weight > targetBox.maxWeight || currentWeight + p.weight > tempSettings.maxParcelWeight) {
                    continue;
                }

                let bestPlacement = null;
                let bestScore = Infinity;

                for (const space of spaces) {
                    const orientations = [
                        [iw, il, ih], [iw, ih, il], [il, iw, ih],
                        [il, ih, iw], [ih, iw, il], [ih, il, iw]
                    ];

                    for (const [rw, rl, rh] of orientations) {
                        let validOrientation = true;
                        if (!p.rotationAllowed.roll || !p.rotationAllowed.pitch) {
                            if (rh !== ih) validOrientation = false;
                        }
                        if (!p.rotationAllowed.yaw) {
                            if (rw !== iw || rl !== il) validOrientation = false;
                        }

                        if (validOrientation && rw <= space.w && rl <= space.l && rh <= space.h) {
                            const score = space.z * 1000000 + space.y * 1000 + space.x;
                            if (score < bestScore) {
                                bestScore = score;
                                bestPlacement = {
                                    space: space,
                                    w: rw, l: rl, h: rh,
                                    x: space.x, y: space.y, z: space.z
                                };
                            }
                        }
                    }
                }

                if (bestPlacement) {
                    const px = bestPlacement.x + padding / 2;
                    const py = bestPlacement.y + padding / 2;
                    const pz = bestPlacement.z + padding / 2;
                    const pw = bestPlacement.w - padding;
                    const pl = bestPlacement.l - padding;
                    const ph = bestPlacement.h - padding;

                    currentBoxPlacements.push({
                        productId: p.id,
                        sku: p.sku,
                        name: p.name,
                        x: px, y: py, z: pz,
                        width: pw, length: pl, height: ph,
                        orientation: 0
                    });

                    currentWeight += p.weight;
                    packedIndices.push(i);

                    const sx = bestPlacement.x;
                    const sy = bestPlacement.y;
                    const sz = bestPlacement.z;
                    const sw = bestPlacement.w;
                    const sl = bestPlacement.l;
                    const sh = bestPlacement.h;

                    let nextSpaces = [];
                    for (const S of spaces) {
                        const overlapX = (S.x < sx + sw) && (S.x + S.w > sx);
                        const overlapY = (S.y < sy + sl) && (S.y + S.l > sy);
                        const overlapZ = (S.z < sz + sh) && (S.z + S.h > sz);

                        if (overlapX && overlapY && overlapZ) {
                            if (sx + sw < S.x + S.w) nextSpaces.push({ x: sx + sw, y: S.y, z: S.z, w: (S.x + S.w) - (sx + sw), l: S.l, h: S.h });
                            if (S.x < sx) nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: sx - S.x, l: S.l, h: S.h });
                            if (sy + sl < S.y + S.l) nextSpaces.push({ x: S.x, y: sy + sl, z: S.z, w: S.w, l: (S.y + S.l) - (sy + sl), h: S.h });
                            if (S.y < sy) nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: S.w, l: sy - S.y, h: S.h });
                            if (sz + sh < S.z + S.h) nextSpaces.push({ x: S.x, y: S.y, z: sz + sh, w: S.w, l: S.l, h: (S.z + S.h) - (sz + sh) });
                            if (S.z < sz) nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: S.w, l: S.l, h: sz - S.z });
                        } else {
                            nextSpaces.push(S);
                        }
                    }

                    spaces = [];
                    for (let k = 0; k < nextSpaces.length; k++) {
                        let isSubset = false;
                        for (let m = 0; m < nextSpaces.length; m++) {
                            if (k === m) continue;
                            const a = nextSpaces[k];
                            const b = nextSpaces[m];
                            if (a.x >= b.x && a.y >= b.y && a.z >= b.z &&
                                a.x + a.w <= b.x + b.w &&
                                a.y + a.l <= b.y + b.l &&
                                a.z + a.h <= b.z + b.h) {
                                isSubset = true;
                                break;
                            }
                        }
                        if (!isSubset) spaces.push(nextSpaces[k]);
                    }
                }
            }

            const parcelVolume = targetBox.width * targetBox.length * targetBox.height;
            const cargoVol = currentBoxPlacements.reduce((acc, p) => acc + (p.width * p.length * p.height), 0);

            let maxPlacedX = 0;
            let maxPlacedY = 0;
            let maxPlacedZ = 0;
            currentBoxPlacements.forEach(pl => {
                if (pl.x + pl.width > maxPlacedX) maxPlacedX = pl.x + pl.width;
                if (pl.y + pl.length > maxPlacedY) maxPlacedY = pl.y + pl.length;
                if (pl.z + pl.height > maxPlacedZ) maxPlacedZ = pl.z + pl.height;
            });

            const gapW = Math.max(0, bw - maxPlacedX);
            const gapL = Math.max(0, bl - maxPlacedY);
            const gapH = Math.max(0, bh - maxPlacedZ);
            const hasSnugLock = (gapW <= 1.0) || (gapL <= 1.0) || (gapH <= 1.0);

            parcels.push({
                box: targetBox,
                weight: currentWeight,
                volumeUtilization: (cargoVol / parcelVolume) * 100,
                isFullFit: true,
                placements: currentBoxPlacements,
                gaps: { width: gapW, length: gapL, height: gapH },
                hasSnugLock: hasSnugLock
            });

            packedIndices.sort((a, b) => b - a);
            for (const idx of packedIndices) {
                itemsToPack.splice(idx, 1);
            }
        }

        parcels.forEach(p => {
            p.packagingDetails = MathUtil.calculatePackagingDetails(p, tempSettings, this.productDb);
        });

        const totalBoxCost = parcels.reduce((acc, p) => acc + p.box.cost, 0);
        const totalTapeCost = parcels.reduce((acc, p) => acc + p.packagingDetails.tapeCost, 0);
        const totalBubbleCost = parcels.reduce((acc, p) => acc + p.packagingDetails.bubbleCost, 0);
        const totalLabelCost = parcels.reduce((acc, p) => acc + p.packagingDetails.labelCost, 0);
        const totalCost = totalBoxCost + totalTapeCost + totalBubbleCost + totalLabelCost;

        const avgVolume = parcels.reduce((acc, p) => acc + p.volumeUtilization, 0) / parcels.length;
        const totalWeight = parcels.reduce((acc, p) => acc + p.weight, 0);

        return {
            parcels: parcels,
            totalBoxCost: totalBoxCost,
            totalTapeCost: totalTapeCost,
            totalBubbleCost: totalBubbleCost,
            totalLabelCost: totalLabelCost,
            totalCost: totalCost,
            totalVolumeUtilization: avgVolume,
            totalWeight: totalWeight
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
                        weight: result.totalWeight,
                        boxText: boxText,
                        cost: result.totalCost,
                        utilization: result.totalVolumeUtilization,
                        resultData: result
                    });
                } else {
                    rows.push({
                        qty: q,
                        weight: product.weight * q,
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
                            <th style="padding: 10px 8px; font-weight: 700; width: 70px;">นน. (กก.)</th>
                            <th style="padding: 10px 8px; font-weight: 700;">กล่องแนะนำ</th>
                            <th style="padding: 10px 8px; font-weight: 700; width: 65px; text-align: right;">ค่ากล่อง</th>
                            <th style="padding: 10px 8px; font-weight: 700; width: 50px; text-align: center;">ดูรูป</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rows.map(r => `
                            <tr style="border-bottom: 1px solid var(--border-color); transition: var(--transition-smooth);" class="sim-row-hover">
                                <td style="padding: 10px 8px; font-weight: 600;">${r.qty}</td>
                                <td style="padding: 10px 8px; color: var(--text-secondary);">${r.weight.toFixed(2)}</td>
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
