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
