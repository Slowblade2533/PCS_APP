/**
 * Main Application Coordinator for Packing Optimizer Pro
 * Bootstraps databases, panels, theme switching, sidebar tabs, and run loop in Thai.
 */

document.addEventListener('DOMContentLoaded', () => {
    // 1. Toast Notification System
    window.showToast = function(message, type = 'success') {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        
        let icon = 'ℹ️';
        if (type === 'success') icon = '✅';
        if (type === 'error') icon = '❌';
        if (type === 'warning') icon = '⚠️';

        toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;
        container.appendChild(toast);

        // Animation entry
        setTimeout(() => toast.classList.add('show'), 10);

        // Remove after 3.5s
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 350);
        }, 3500);
    };

    // 2. Theme Switching Logic
    const themeToggle = document.getElementById('theme-toggle');
    const themeIcon = document.getElementById('theme-icon');
    const themeText = document.getElementById('theme-text');
    const htmlTag = document.documentElement;

    function applyTheme(theme) {
        htmlTag.setAttribute('data-theme', theme);
        localStorage.setItem('po_theme', theme);
        if (theme === 'dark') {
            themeIcon.textContent = '☀️';
            themeText.textContent = 'โหมดสว่าง';
        } else {
            themeIcon.textContent = '🌙';
            themeText.textContent = 'โหมดมืด';
        }
    }

    // Load initial theme
    const savedTheme = localStorage.getItem('po_theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    applyTheme(savedTheme);

    themeToggle.addEventListener('click', () => {
        const currentTheme = htmlTag.getAttribute('data-theme');
        const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
        applyTheme(nextTheme);
    });

    // 3. Sidebar Tab Toggling
    const tabProducts = document.getElementById('tab-products');
    const tabBoxes = document.getElementById('tab-boxes');
    const paneProducts = document.getElementById('pane-products');
    const paneBoxes = document.getElementById('pane-boxes');

    tabProducts.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
        document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
        tabProducts.classList.add('active');
        paneProducts.classList.add('active');
    });

    tabBoxes.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
        document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
        tabBoxes.classList.add('active');
        paneBoxes.classList.add('active');
    });

    // 4. Initialize Database and UI Panels
    const productDb = new window.ProductDatabase();
    const boxDb = new window.BoxDatabase();

    const resultPanel = new window.ResultPanel('results-panel-container');
    const canvasRenderer = new window.CanvasRenderer('packing-canvas');

    const dashboard = new window.Dashboard(productDb, boxDb, (products, boxes, settings) => {
        runMockOptimization(products, boxes, settings);
    });

    const simulatorPanel = new window.SimulatorPanel(
        productDb,
        boxDb,
        (simulationResult) => {
            resultPanel.render(simulationResult);
            if (simulationResult.parcels.length > 0) {
                canvasRenderer.draw(simulationResult.parcels[0]);
            }
        }
    );

    const productPanel = new window.ProductPanel(
        productDb, 
        'product-list-container', 
        'product-modal', 
        'product-form',
        () => {
            dashboard.updateStats();
            simulatorPanel.updateSkuDropdown();
        }
    );

    const boxPanel = new window.BoxPanel(
        boxDb, 
        'box-list-container', 
        'box-modal', 
        'box-form',
        () => {
            dashboard.updateStats();
        }
    );

    // Initial Stats update
    dashboard.updateStats();

    // Helper to check if a product physically fits inside a box considering rotation and padding
    function canProductFitInBox(p, box, settings) {
        // 1. Calculate adjusted product dimensions including bubble wrap and foam padding
        const padding = (settings.bubbleThickness * settings.bubbleLayers * 2) + (settings.foamThickness * 2);
        const pw = p.width + padding;
        const pl = p.length + padding;
        const ph = p.height + padding;

        // 2. Calculate usable inner dimensions of the box (subtracted by carton wall thickness)
        const bw = box.width - (settings.cartonThickness * 2);
        const bl = box.length - (settings.cartonThickness * 2);
        const bh = box.height - (settings.cartonThickness * 2);

        // If inner box dimensions are zero or negative, it can't fit
        if (bw <= 0 || bl <= 0 || bh <= 0) return false;

        // 3. Weight check: individual item weight cannot exceed box max weight limit
        if (p.weight > box.maxWeight) return false;

        // 4. Dimensional rotation checks (6 orientations)
        const orientations = [
            [pw, pl, ph],
            [pw, ph, pl],
            [pl, pw, ph],
            [pl, ph, pw],
            [ph, pw, pl],
            [ph, pl, pw]
        ];

        for (const [w, l, h] of orientations) {
            let validOrientation = true;

            // Rotation restrictions check
            if (!p.rotationAllowed.roll || !p.rotationAllowed.pitch) {
                // Height must stay vertical (match ph)
                if (h !== ph) validOrientation = false;
            }
            if (!p.rotationAllowed.yaw) {
                // Cannot rotate around Z-axis (width and length must match pw and pl)
                if (w !== pw || l !== pl) validOrientation = false;
            }

            if (validOrientation) {
                if (w <= bw && l <= bl && h <= bh) {
                    return true; // Fits!
                }
            }
        }

        return false;
    }

    // 5. High-performance 3D Bin Packing Engine using Maximal Empty Spaces (MES) and LAFF
    function packCargo(box, productsList, settings) {
        const bw = box.width - (settings.cartonThickness * 2);
        const bl = box.length - (settings.cartonThickness * 2);
        const bh = box.height - (settings.cartonThickness * 2);

        if (bw <= 0 || bl <= 0 || bh <= 0) return null;

        // Initialize Maximal Empty Spaces (MES) list
        let spaces = [{ x: 0, y: 0, z: 0, w: bw, l: bl, h: bh }];

        // Build flat list of items, with padding added
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
                    origW: p.width,
                    origL: p.length,
                    origH: p.height,
                    padding: padding,
                    rotationAllowed: p.rotationAllowed
                });
            }
        });

        // Sort items by volume descending (LAFF heuristic)
        items.sort((a, b) => (b.w * b.l * b.h) - (a.w * a.l * a.h));

        const placements = [];
        let currentWeight = 0;

        for (const item of items) {
            // Weight check
            if (currentWeight + item.weight > box.maxWeight) {
                return null;
            }

            // Find best empty space and orientation (Bottom-Left-Back heuristic: minimize Z, then Y, then X)
            let bestPlacement = null;
            let bestScore = Infinity;

            for (const space of spaces) {
                // Try all 6 rotations
                const orientations = [
                    [item.w, item.l, item.h],
                    [item.w, item.h, item.l],
                    [item.l, item.w, item.h],
                    [item.l, item.h, item.w],
                    [item.h, item.w, item.l],
                    [item.h, item.l, item.w]
                ];

                for (const [iw, il, ih] of orientations) {
                    // Rotation rules validation
                    let validOrientation = true;
                    if (!item.rotationAllowed.roll || !item.rotationAllowed.pitch) {
                        if (ih !== item.h) validOrientation = false;
                    }
                    if (!item.rotationAllowed.yaw) {
                        if (iw !== item.w || il !== item.l) validOrientation = false;
                    }

                    if (validOrientation) {
                        if (iw <= space.w && il <= space.l && ih <= space.h) {
                            // Score: lowest Z, then lowest Y, then lowest X
                            const score = space.z * 1000000 + space.y * 1000 + space.x;
                            if (score < bestScore) {
                                bestScore = score;
                                bestPlacement = {
                                    space: space,
                                    w: iw,
                                    l: il,
                                    h: ih,
                                    x: space.x,
                                    y: space.y,
                                    z: space.z
                                };
                            }
                        }
                    }
                }
            }

            if (bestPlacement) {
                // Calculate product boundary centered in the padded space
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
                    x: px,
                    y: py,
                    z: pz,
                    width: pw,
                    length: pl,
                    height: ph,
                    orientation: 0
                });

                currentWeight += item.weight;

                // Split overlapping spaces
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
                        // Split S into up to 6 new spaces
                        if (sx + sw < S.x + S.w) {
                            nextSpaces.push({ x: sx + sw, y: S.y, z: S.z, w: (S.x + S.w) - (sx + sw), l: S.l, h: S.h });
                        }
                        if (S.x < sx) {
                            nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: sx - S.x, l: S.l, h: S.h });
                        }
                        if (sy + sl < S.y + S.l) {
                            nextSpaces.push({ x: S.x, y: sy + sl, z: S.z, w: S.w, l: (S.y + S.l) - (sy + sl), h: S.h });
                        }
                        if (S.y < sy) {
                            nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: S.w, l: sy - S.y, h: S.h });
                        }
                        if (sz + sh < S.z + S.h) {
                            nextSpaces.push({ x: S.x, y: S.y, z: sz + sh, w: S.w, l: S.l, h: (S.z + S.h) - (sz + sh) });
                        }
                        if (S.z < sz) {
                            nextSpaces.push({ x: S.x, y: S.y, z: S.z, w: S.w, l: S.l, h: sz - S.z });
                        }
                    } else {
                        nextSpaces.push(S);
                    }
                }

                // Remove subset spaces (redundant)
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
                    if (!isSubset) {
                        spaces.push(nextSpaces[i]);
                    }
                }
            } else {
                // Item could not fit
                return null;
            }
        }

        const totalItemVol = productsList.reduce((acc, p) => acc + (p.width * p.length * p.height * p.quantity), 0);
        const boxVol = box.width * box.length * box.height;

        // Calculate cargo bounding box
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

    function runMockOptimization(products, boxes, settings) {
        const sortedBoxes = [...boxes].sort((a, b) => a.cost - b.cost);
        
        // Check if any product is too large for all boxes individually
        for (const p of products) {
            const canFitInAnyBox = sortedBoxes.some(box => canProductFitInBox(p, box, settings));
            if (!canFitInAnyBox) {
                showToast(`สินค้า SKU ${p.sku} มีขนาดใหญ่เกินไป ไม่สามารถบรรจุลงกล่องใด ๆ ในระบบได้`, 'error');
                resultPanel.render(null); // Displays failed status
                return;
            }
        }

        const totalCargoVolume = products.reduce((acc, p) => acc + (p.width * p.length * p.height * p.quantity), 0);
        const totalCargoWeight = products.reduce((acc, p) => acc + (p.weight * p.quantity), 0);

        // 1. Try to find a single box that fits ALL items using our 3D Bin Packing algorithm
        let singleBoxSolution = null;
        for (const box of sortedBoxes) {
            const solution = packCargo(box, products, settings);
            if (solution !== null) {
                singleBoxSolution = solution;
                break;
            }
        }

        let parcels = [];

        if (singleBoxSolution) {
            parcels.push(singleBoxSolution);
        } else {
            // Cannot fit in a single box, try splitting if allowed
            if (!settings.allowSplit) {
                showToast("ขนาดพัสดุเกินความจุของกล่องใบเดียว และระบบไม่อนุญาตให้แยกบรรจุหลายกล่อง", "error");
                resultPanel.render(null);
                return;
            }

            // Greedy partition packing
            // Split products into individual units
            const itemsToPack = [];
            products.forEach(p => {
                for (let i = 0; i < p.quantity; i++) {
                    itemsToPack.push(p);
                }
            });

            // Pack items one by one into parcels
            while (itemsToPack.length > 0) {
                // Find a box size that can fit the first remaining item
                const firstItem = itemsToPack[0];
                const targetBox = sortedBoxes.find(b => canProductFitInBox(firstItem, b, settings));
                if (!targetBox) {
                    itemsToPack.shift();
                    continue;
                }

                // Try to pack as many remaining items as possible into this box size
                let currentBoxPlacements = [];
                let currentWeight = 0;
                
                // Set up inner dimensions
                const bw = targetBox.width - (settings.cartonThickness * 2);
                const bl = targetBox.length - (settings.cartonThickness * 2);
                const bh = targetBox.height - (settings.cartonThickness * 2);

                let spaces = [{ x: 0, y: 0, z: 0, w: bw, l: bl, h: bh }];
                const padding = (settings.bubbleThickness * settings.bubbleLayers * 2) + (settings.foamThickness * 2);

                // Pack items greedy style
                const packedIndices = [];
                for (let i = 0; i < itemsToPack.length; i++) {
                    const p = itemsToPack[i];
                    const iw = p.width + padding;
                    const il = p.length + padding;
                    const ih = p.height + padding;

                    // Check weight limit
                    if (currentWeight + p.weight > targetBox.maxWeight) {
                        continue;
                    }

                    // Find best empty space
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
                        // Place item
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

                        // Split spaces
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

                        // Remove subsets
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

                // Add parcel
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

                // Remove packed items from itemsToPack list
                packedIndices.sort((a, b) => b - a);
                for (const idx of packedIndices) {
                    itemsToPack.splice(idx, 1);
                }
            }
        }

        // Calculate detailed packaging costs
        parcels.forEach(p => {
            p.packagingDetails = MathUtil.calculatePackagingDetails(p, settings, productDb);
        });

        const totalBoxCost = parcels.reduce((acc, p) => acc + p.box.cost, 0);
        const totalTapeCost = parcels.reduce((acc, p) => acc + p.packagingDetails.tapeCost, 0);
        const totalBubbleCost = parcels.reduce((acc, p) => acc + p.packagingDetails.bubbleCost, 0);
        const totalLabelCost = parcels.reduce((acc, p) => acc + p.packagingDetails.labelCost, 0);
        const totalCost = totalBoxCost + totalTapeCost + totalBubbleCost + totalLabelCost;

        const avgVolumeUtilization = parcels.reduce((acc, p) => acc + p.volumeUtilization, 0) / parcels.length;
        const overallScore = Math.max(10, Math.min(100, 100 - (parcels.length - 1) * 10 + (avgVolumeUtilization - 70)));

        const results = {
            parcels: parcels,
            overallScore: overallScore,
            totalBoxCost: totalBoxCost,
            totalTapeCost: totalTapeCost,
            totalBubbleCost: totalBubbleCost,
            totalLabelCost: totalLabelCost,
            totalCost: totalCost,
            totalVolumeUtilization: avgVolumeUtilization,
            totalWeight: totalCargoWeight
        };

        // Render results
        resultPanel.render(results);
        if (parcels.length > 0) {
            canvasRenderer.draw(parcels[0]);
        }
    }
});
