/**
 * ShipmentEngine for Packing Optimizer Pro
 *
 * KEY CONCEPT — Box Tare Weight Integration:
 * The carrier's 20 kg limit applies to the TOTAL parcel weight (cargo + box itself).
 * Therefore: effective_max_payload = min(box.maxWeight, 20 - box.tare)
 *
 * Algorithm phases:
 * 1. Filter standard boxes against carrier size constraints (100cm/180cm/20kg).
 * 2. Check if each product physically fits (6-orientation rotation test).
 * 3. Pack items using 3D Maximal Empty Spaces (MES) bin packing.
 * 4. If no standard box works → calculate a custom (cut-to-order) box.
 * 5. If no single box works → greedy multi-box split partition.
 */
class ShipmentEngine {

    /**
     * Returns the effective maximum payload (cargo weight) for a box,
     * accounting for box tare weight and carrier 20 kg total limit.
     * @param {Object} box 
     * @returns {number} max cargo weight in kg
     */
    static getMaxPayload(box) {
        const tare = box.tare || 0;
        // Carrier limit is 20 kg for total parcel (cargo + box)
        const carrierPayload = 20.0 - tare;
        return Math.min(box.maxWeight, carrierPayload);
    }

    /**
     * 1. Filter standard boxes based on carrier shipping constraints.
     *    - Any side <= 100 cm
     *    - Sum of 3 sides (W+L+H) <= 180 cm
     *    - Box tare must leave at least 0.01 kg payload headroom under 20 kg
     */
    static filterValidBoxes(boxes) {
        return boxes.filter(box => {
            // Dimensional constraints
            if (box.width > 100.0 || box.length > 100.0 || box.height > 100.0) return false;
            if (box.width + box.length + box.height > 180.0) return false;
            // Box must have usable payload remaining after tare
            if (ShipmentEngine.getMaxPayload(box) < 0.01) return false;
            return true;
        });
    }

    /**
     * 2. Checks if a single product can physically fit inside a box
     *    considering rotation rules, padding, and the tare-adjusted weight limit.
     */
    static canProductFitInBox(p, box, settings) {
        const padding = (settings.bubbleThickness * settings.bubbleLayers * 2) + (settings.foamThickness * 2);
        const pw = p.width + padding;
        const pl = p.length + padding;
        const ph = p.height + padding;

        const bw = box.width - (settings.cartonThickness * 2);
        const bl = box.length - (settings.cartonThickness * 2);
        const bh = box.height - (settings.cartonThickness * 2);

        if (bw <= 0 || bl <= 0 || bh <= 0) return false;

        // Weight check against effective payload (carrier limit minus box tare)
        const maxPayload = ShipmentEngine.getMaxPayload(box);
        if (p.weight > maxPayload) return false;

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
            if (validOrientation && w <= bw && l <= bl && h <= bh) return true;
        }
        return false;
    }

    /**
     * 3. Packs a list of products into a specific box using 3D MES (Maximal Empty Spaces).
     *    Weight limit is the tare-adjusted effective payload.
     * @returns {Object|null} packing result or null if items don't fit
     */
    static packCargo(box, productsList, settings) {
        const bw = box.width - (settings.cartonThickness * 2);
        const bl = box.length - (settings.cartonThickness * 2);
        const bh = box.height - (settings.cartonThickness * 2);

        if (bw <= 0 || bl <= 0 || bh <= 0) return null;

        let spaces = [{ x: 0, y: 0, z: 0, w: bw, l: bl, h: bh }];
        const padding = (settings.bubbleThickness * settings.bubbleLayers * 2) + (settings.foamThickness * 2);

        // Build flat item list with padding applied
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

        // LAFF heuristic: sort by volume descending
        items.sort((a, b) => (b.w * b.l * b.h) - (a.w * a.l * a.h));

        const placements = [];
        let currentWeight = 0;
        // Effective payload = carrier 20 kg minus box's own tare weight
        const maxPayload = ShipmentEngine.getMaxPayload(box);

        for (const item of items) {
            if (currentWeight + item.weight > maxPayload) {
                return null;
            }

            let bestPlacement = null;
            let bestScore = Infinity;

            for (const space of spaces) {
                const orientations = [
                    [item.w, item.l, item.h], [item.w, item.h, item.l],
                    [item.l, item.w, item.h], [item.l, item.h, item.w],
                    [item.h, item.w, item.l], [item.h, item.l, item.w]
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
                        // BLB score: lowest Z, then lowest Y, then lowest X
                        const score = space.z * 1000000 + space.y * 1000 + space.x;
                        if (score < bestScore) {
                            bestScore = score;
                            bestPlacement = { space, w: iw, l: il, h: ih, x: space.x, y: space.y, z: space.z };
                        }
                    }
                }
            }

            if (!bestPlacement) return null; // Item could not be placed

            const px = bestPlacement.x + item.padding / 2;
            const py = bestPlacement.y + item.padding / 2;
            const pz = bestPlacement.z + item.padding / 2;

            placements.push({
                productId: item.id,
                sku: item.sku,
                name: item.name,
                x: px,
                y: py,
                z: pz,
                width:  bestPlacement.w - item.padding,
                length: bestPlacement.l - item.padding,
                height: bestPlacement.h - item.padding,
                orientation: 0
            });

            currentWeight += item.weight;

            // Split overlapping MES spaces
            const { x: sx, y: sy, z: sz, w: sw, l: sl, h: sh } = bestPlacement;
            let nextSpaces = [];
            for (const S of spaces) {
                const overlapX = (S.x < sx + sw) && (S.x + S.w > sx);
                const overlapY = (S.y < sy + sl) && (S.y + S.l > sy);
                const overlapZ = (S.z < sz + sh) && (S.z + S.h > sz);

                if (overlapX && overlapY && overlapZ) {
                    if (sx + sw < S.x + S.w) nextSpaces.push({ x: sx+sw, y: S.y, z: S.z, w: (S.x+S.w)-(sx+sw), l: S.l, h: S.h });
                    if (S.x < sx)            nextSpaces.push({ x: S.x,   y: S.y, z: S.z, w: sx-S.x,             l: S.l, h: S.h });
                    if (sy + sl < S.y + S.l) nextSpaces.push({ x: S.x, y: sy+sl, z: S.z, w: S.w, l: (S.y+S.l)-(sy+sl), h: S.h });
                    if (S.y < sy)            nextSpaces.push({ x: S.x, y: S.y,   z: S.z, w: S.w, l: sy-S.y,               h: S.h });
                    if (sz + sh < S.z + S.h) nextSpaces.push({ x: S.x, y: S.y, z: sz+sh, w: S.w, l: S.l, h: (S.z+S.h)-(sz+sh) });
                    if (S.z < sz)            nextSpaces.push({ x: S.x, y: S.y, z: S.z,   w: S.w, l: S.l, h: sz-S.z });
                } else {
                    nextSpaces.push(S);
                }
            }

            // Remove subset spaces
            spaces = [];
            for (let i = 0; i < nextSpaces.length; i++) {
                let isSubset = false;
                for (let j = 0; j < nextSpaces.length; j++) {
                    if (i === j) continue;
                    const a = nextSpaces[i], b = nextSpaces[j];
                    if (a.x >= b.x && a.y >= b.y && a.z >= b.z &&
                        a.x+a.w <= b.x+b.w && a.y+a.l <= b.y+b.l && a.z+a.h <= b.z+b.h) {
                        isSubset = true; break;
                    }
                }
                if (!isSubset) spaces.push(nextSpaces[i]);
            }
        }

        const totalItemVol = productsList.reduce((acc, p) => acc + (p.width * p.length * p.height * p.quantity), 0);
        const boxVol = box.width * box.length * box.height;

        let maxX = 0, maxY = 0, maxZ = 0;
        placements.forEach(pl => {
            if (pl.x + pl.width  > maxX) maxX = pl.x + pl.width;
            if (pl.y + pl.length > maxY) maxY = pl.y + pl.length;
            if (pl.z + pl.height > maxZ) maxZ = pl.z + pl.height;
        });

        const gapW = Math.max(0, bw - maxX);
        const gapL = Math.max(0, bl - maxY);
        const gapH = Math.max(0, bh - maxZ);

        return {
            box: box,
            cargoWeight: currentWeight,
            totalWeight: currentWeight + (box.tare || 0), // total parcel weight including box
            volumeUtilization: Math.min(99.0, (totalItemVol / boxVol) * 100),
            isFullFit: true,
            placements: placements,
            gaps: { width: gapW, length: gapL, height: gapH },
            hasSnugLock: (gapW <= 1.0) || (gapL <= 1.0) || (gapH <= 1.0)
        };
    }

    /**
     * 4. Calculates a custom (cut-to-order) box to fit all products when standard boxes fail.
     *    Derives minimum bounding box from a virtual 100×100×100 packing simulation.
     * @returns {Object|null} packing result or null if carrier constraints cannot be met
     */
    static calculateCustomBox(productsList, settings) {
        const totalCargoWeight = productsList.reduce((acc, p) => acc + (p.weight * p.quantity), 0);
        if (totalCargoWeight > 20.0) return null;

        const cartonThickness = settings.cartonThickness || 0.3;
        const maxInnerSide = 100.0 - 2 * cartonThickness;

        // Virtual unlimited container for bounding box discovery
        const virtualBox = {
            width:     maxInnerSide + 2 * cartonThickness,
            length:    maxInnerSide + 2 * cartonThickness,
            height:    maxInnerSide + 2 * cartonThickness,
            maxWeight: 20.0,
            tare:      0      // temporary — real tare computed below
        };

        const solution = ShipmentEngine.packCargo(virtualBox, productsList, settings);
        if (!solution) return null;

        let maxX = 0, maxY = 0, maxZ = 0;
        solution.placements.forEach(pl => {
            if (pl.x + pl.width  > maxX) maxX = pl.x + pl.width;
            if (pl.y + pl.length > maxY) maxY = pl.y + pl.length;
            if (pl.z + pl.height > maxZ) maxZ = pl.z + pl.height;
        });

        // Round up to 1 decimal and add carton walls
        const bw = Math.ceil((maxX + 2 * cartonThickness) * 10) / 10;
        const bl = Math.ceil((maxY + 2 * cartonThickness) * 10) / 10;
        const bh = Math.ceil((maxZ + 2 * cartonThickness) * 10) / 10;

        // Carrier size constraints
        if (bw > 100.0 || bl > 100.0 || bh > 100.0) return null;
        if (bw + bl + bh > 180.0) return null;

        // Estimate tare for the custom box (5-layer since it carries >10 kg or is a larger box)
        const tare = MathUtil.estimateBoxTare(bw, bl, bh, 20.0);

        // Verify cargo still fits within carrier payload after tare
        if (totalCargoWeight + tare > 20.0) return null;

        const cost = MathUtil.calculateCustomBoxCost(bw, bl, bh);
        const customBox = {
            id:        'b_custom_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
            name:      `กล่องสั่งตัด (${bw.toFixed(1)}×${bl.toFixed(1)}×${bh.toFixed(1)} ซม.)`,
            width:     bw,
            length:    bl,
            height:    bh,
            maxWeight: 20.0,
            tare:      tare,
            cost:      cost,
            carrier:   'สั่งตัดพิเศษ',
            isCustom:  true
        };

        return ShipmentEngine.packCargo(customBox, productsList, settings);
    }

    /**
     * 5. Main packing coordinator.
     *    Priority: standard box → custom box → greedy multi-box split.
     * @param {Array}  products  list of products
     * @param {Array}  boxes     list of available boxes
     * @param {Object} settings  packaging configuration
     * @returns {{ success: boolean, parcels?: Array, message?: string }}
     */
    static optimizePacking(products, boxes, settings) {
        const validStandardBoxes = ShipmentEngine.filterValidBoxes(boxes);
        const sortedBoxes = [...validStandardBoxes].sort((a, b) => a.cost - b.cost);

        // Verify each product can be individually packaged (single unit check)
        for (const p of products) {
            const fitsInStandard = sortedBoxes.some(box => ShipmentEngine.canProductFitInBox(p, box, settings));
            if (!fitsInStandard) {
                const singleSol = ShipmentEngine.calculateCustomBox([{ ...p, quantity: 1 }], settings);
                if (!singleSol) {
                    return {
                        success: false,
                        message: `สินค้า SKU ${p.sku} มีขนาดหรือน้ำหนักเกินขีดจำกัดขนส่ง (ด้านใด ≤ 100 ซม., กว้าง+ยาว+สูง ≤ 180 ซม., น้ำหนักรวมพัสดุ ≤ 20 กก.)`
                    };
                }
            }
        }

        // --- Phase A: Try single standard box ---
        for (const box of sortedBoxes) {
            const solution = ShipmentEngine.packCargo(box, products, settings);
            if (solution !== null) {
                return { success: true, parcels: [solution] };
            }
        }

        // --- Phase B: Try single custom box ---
        const customSol = ShipmentEngine.calculateCustomBox(products, settings);
        if (customSol !== null) {
            return { success: true, parcels: [customSol] };
        }

        // --- Phase C: Multi-box split (requires allowSplit) ---
        if (!settings.allowSplit) {
            return {
                success: false,
                message: 'ขนาดพัสดุรวมเกินความจุของกล่องเดียว และระบบไม่อนุญาตให้แยกบรรจุหลายกล่อง'
            };
        }

        // Flatten items to individual units, sorted largest-first
        const itemsToPack = [];
        products.forEach(p => {
            for (let i = 0; i < p.quantity; i++) {
                itemsToPack.push({ ...p, quantity: 1 });
            }
        });
        itemsToPack.sort((a, b) => (b.width * b.length * b.height) - (a.width * a.length * a.height));

        const parcels = [];

        while (itemsToPack.length > 0) {
            const currentGroup = [itemsToPack.shift()];

            // Greedily add remaining items to this group as long as they fit
            let i = 0;
            while (i < itemsToPack.length) {
                const candidate = itemsToPack[i];
                const testGroup = [...currentGroup, candidate];
                const groupWeight = testGroup.reduce((acc, p) => acc + p.weight, 0);

                // Pre-filter: total cargo alone already exceeds 20 kg → skip
                if (groupWeight > 20.0) { i++; continue; }

                let fits = sortedBoxes.some(box => {
                    const sol = ShipmentEngine.packCargo(box, testGroup, settings);
                    return sol !== null;
                });

                if (!fits) {
                    const cSol = ShipmentEngine.calculateCustomBox(testGroup, settings);
                    fits = cSol !== null;
                }

                if (fits) {
                    currentGroup.push(candidate);
                    itemsToPack.splice(i, 1);
                } else {
                    i++;
                }
            }

            // Pack the current group into the cheapest fitting container
            let parcelSolution = null;
            for (const box of sortedBoxes) {
                const sol = ShipmentEngine.packCargo(box, currentGroup, settings);
                if (sol !== null) { parcelSolution = sol; break; }
            }
            if (!parcelSolution) {
                parcelSolution = ShipmentEngine.calculateCustomBox(currentGroup, settings);
            }
            if (!parcelSolution) {
                return { success: false, message: 'เกิดข้อผิดพลาดในการแบ่งพัสดุหลายกล่อง' };
            }
            parcels.push(parcelSolution);
        }

        return { success: true, parcels };
    }
}

window.ShipmentEngine = ShipmentEngine;
