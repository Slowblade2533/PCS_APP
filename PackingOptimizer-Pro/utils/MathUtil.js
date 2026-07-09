/**
 * Math Utilities for Packing Optimizer Pro
 */
class MathUtil {
    /**
     * Custom rounding rule for packaging costs (Satang rounding)
     * .00 -> .00
     * .01-.24 -> .25
     * .25 -> .25
     * .26-.49 -> .50
     * .50 -> .50
     * .51-.74 -> .75
     * .75 -> .75
     * .76-.99 -> 1.00
     * @param {number} val 
     * @returns {number}
     */
    static roundToSatang(val) {
        const integerPart = Math.floor(val);
        const cents = Math.round((val - integerPart) * 100);
        
        if (cents === 0) {
            return integerPart;
        }
        if (cents <= 25) {
            return integerPart + 0.25;
        }
        if (cents <= 50) {
            return integerPart + 0.50;
        }
        if (cents <= 75) {
            return integerPart + 0.75;
        }
        return integerPart + 1.00;
    }

    /**
     * Estimates the empty box weight (tare) based on box dimensions and load category.
     * Rule: boxes with maxWeight <= 10 kg = 3-layer corrugated (~0.07 g/cm²)
     *       boxes with maxWeight >  10 kg = 5-layer corrugated (~0.10 g/cm²)
     * Formula validated against known references:
     *   AA (13×17×7, 3-layer): SA=862 cm² → 0.060 kg ≈ 0.05 kg ✓
     *   B  (17×25×9, 3-layer): SA=1606 cm² → 0.112 kg ≈ 0.10 kg ✓
     *   50×50×35 (5-layer):    SA=12000 cm² → 1.20 kg ≈ 1.25 kg ✓
     * @param {number} w  box width (cm)
     * @param {number} l  box length (cm)
     * @param {number} h  box height (cm)
     * @param {number} maxWeight  box maximum cargo weight (kg)
     * @returns {number} tare weight in kg (rounded to 2 decimals)
     */
    static estimateBoxTare(w, l, h, maxWeight) {
        const surfaceAreaCm2 = 2 * (w * l + l * h + h * w);
        // 3-layer for light boxes, 5-layer for heavy-duty boxes
        const densityGPerCm2 = (maxWeight <= 10) ? 0.070 : 0.100;
        const tare = surfaceAreaCm2 * densityGPerCm2 / 1000;
        return Math.round(tare * 100) / 100;
    }

    /**
     * Calculates custom box ordering cost based on surface area.
     * @param {number} w width (cm)
     * @param {number} l length (cm)
     * @param {number} h height (cm)
     * @returns {number} cost in Baht (rounded to Satang)
     */
    static calculateCustomBoxCost(w, l, h) {
        const areaM2 = 2 * (w * l + l * h + h * w) / 10000;
        const rawCost = areaM2 * 20.0 + 2.0; // 20 Baht per m2 + 2 Baht base
        return Math.max(5.0, MathUtil.roundToSatang(rawCost));
    }

    /**
     * Calculates the detailed packaging materials used (Tape length/cost, Bubble area/cost)
     * @param {Object} parcel 
     * @param {Object} settings 
     * @param {Object} productDb 
     */
    static calculatePackagingDetails(parcel, settings, productDb) {
        // Constants
        const TAPE_ROLL_LENGTH_M = 22.86; // 25 yards (25 * 0.9144)
        const TAPE_ROLL_COST = 50.0;
        const BUBBLE_ROLL_WIDTH_M = 0.65;
        const BUBBLE_ROLL_LENGTH_M = 100.0;
        const BUBBLE_ROLL_COST = 180.0;

        // Tape calculation: 2 * length of box + 20 cm overlap, converted to meters
        const tapeLengthM = (2 * parcel.box.length + 20) / 100;
        const rawTapeCost = tapeLengthM * (TAPE_ROLL_COST / TAPE_ROLL_LENGTH_M);
        const tapeCost = MathUtil.roundToSatang(rawTapeCost);

        // Bubble wrap calculation
        let rawBubbleCostTotal = 0;
        let totalBubbleAreaM2 = 0;
        
        parcel.placements.forEach(pl => {
            const p = productDb.getById(pl.productId);
            if (!p) return;

            // Fragile items require at least 2 layers of bubble wrap
            const layers = p.fragile ? Math.max(2, settings.bubbleLayers) : settings.bubbleLayers;
            if (layers > 0) {
                // Sort dimensions: d1, d2 (perimeter) and d3 (width of cut sheet)
                const dims = [p.width, p.length, p.height].sort((a, b) => a - b);
                const d1 = dims[0];
                const d2 = dims[1];
                const d3 = dims[2];

                const sheetWidth = d3 + 10; // Allow 10cm overlap for side folds
                const sheetLength = (layers * 2 * (d1 + d2)) + 10; // layers * perimeter + 10cm overlap
                const areaM2 = (sheetWidth * sheetLength) / 10000;

                totalBubbleAreaM2 += areaM2;
                rawBubbleCostTotal += areaM2 * (BUBBLE_ROLL_COST / (BUBBLE_ROLL_WIDTH_M * BUBBLE_ROLL_LENGTH_M));
            }
        });

        const bubbleCost = MathUtil.roundToSatang(rawBubbleCostTotal);
        const boxCost = MathUtil.roundToSatang(parcel.box.cost);
        const labelCost = MathUtil.roundToSatang(5.0); // Flat fee of 5 Baht per parcel for shipping label

        return {
            tapeLength: tapeLengthM,
            tapeCost: tapeCost,
            bubbleArea: totalBubbleAreaM2,
            bubbleCost: bubbleCost,
            labelCost: labelCost,
            totalParcelMaterialCost: boxCost + tapeCost + bubbleCost + labelCost
        };
    }
}
window.MathUtil = MathUtil;
