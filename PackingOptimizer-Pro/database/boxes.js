/**
 * Box Database for Packing Optimizer Pro
 * Manages standard and custom boxes with LocalStorage persistence.
 */
class BoxDatabase {
    constructor() {
        this.key = 'boxes';
        this.boxes = this.load();
    }

    load() {
        const defaults = [
            { id: 'b_00_np', name: '00 (ไม่พิมพ์)', width: 9.75, length: 14.0, height: 6.0, maxWeight: 2.0, cost: 1.10, carrier: 'Thailand Post' },
            { id: 'b_00', name: '00', width: 9.75, length: 14.0, height: 6.0, maxWeight: 2.0, cost: 1.20, carrier: 'Thailand Post' },
            { id: 'b_0', name: '0', width: 11.0, length: 17.0, height: 6.0, maxWeight: 3.0, cost: 1.60, carrier: 'Thailand Post' },
            { id: 'b_0_4', name: '0+4', width: 11.0, length: 17.0, height: 10.0, maxWeight: 3.0, cost: 2.00, carrier: 'Thailand Post' },
            { id: 'b_AA', name: 'AA', width: 13.0, length: 17.0, height: 7.0, maxWeight: 4.0, cost: 2.10, carrier: 'Thailand Post' },
            { id: 'b_A', name: 'A', width: 14.0, length: 20.0, height: 6.0, maxWeight: 5.0, cost: 2.20, carrier: 'Thailand Post' },
            { id: 'b_Q', name: 'Q', width: 14.5, length: 22.5, height: 7.0, maxWeight: 5.0, cost: 2.50, carrier: 'Thailand Post' },
            { id: 'b_AB', name: 'AB', width: 14.0, length: 19.0, height: 9.0, maxWeight: 5.0, cost: 2.60, carrier: 'Thailand Post' },
            { id: 'b_2A', name: '2A', width: 14.0, length: 20.0, height: 12.0, maxWeight: 6.0, cost: 3.00, carrier: 'Thailand Post' },
            { id: 'b_B', name: 'B', width: 17.0, length: 25.0, height: 9.0, maxWeight: 8.0, cost: 3.40, carrier: 'Thailand Post' },
            { id: 'b_B_7', name: 'B+7', width: 17.0, length: 25.0, height: 16.0, maxWeight: 8.0, cost: 4.60, carrier: 'Thailand Post' },
            { id: 'b_2B', name: '2B', width: 17.0, length: 25.0, height: 18.0, maxWeight: 8.0, cost: 4.70, carrier: 'Thailand Post' },
            { id: 'b_CD', name: 'CD', width: 15.0, length: 15.0, height: 15.0, maxWeight: 8.0, cost: 3.30, carrier: 'Thailand Post' },
            { id: 'b_C', name: 'C', width: 20.0, length: 30.0, height: 11.0, maxWeight: 10.0, cost: 4.80, carrier: 'Thailand Post' },
            { id: 'b_C_8', name: 'C+8', width: 20.0, length: 30.0, height: 19.0, maxWeight: 10.0, cost: 6.50, carrier: 'Thailand Post' },
            { id: 'b_2C', name: '2C', width: 20.0, length: 30.0, height: 22.0, maxWeight: 12.0, cost: 6.90, carrier: 'Thailand Post' },
            { id: 'b_D', name: 'D', width: 22.0, length: 35.0, height: 14.0, maxWeight: 15.0, cost: 6.30, carrier: 'Thailand Post' },
            { id: 'b_D_7', name: 'D-7', width: 22.0, length: 35.0, height: 7.0, maxWeight: 10.0, cost: 5.50, carrier: 'Thailand Post' },
            { id: 'b_D_11', name: 'D+11', width: 22.0, length: 35.0, height: 25.0, maxWeight: 15.0, cost: 8.70, carrier: 'Thailand Post' },
            { id: 'b_2D', name: '2D', width: 22.0, length: 35.0, height: 28.0, maxWeight: 15.0, cost: 9.00, carrier: 'Thailand Post' },
            { id: 'b_S_plus', name: 'S+', width: 24.0, length: 37.0, height: 14.0, maxWeight: 15.0, cost: 7.50, carrier: 'Thailand Post' },
            { id: 'b_E', name: 'E', width: 24.0, length: 40.0, height: 17.0, maxWeight: 20.0, cost: 8.00, carrier: 'Thailand Post' },
            { id: 'b_F_sm', name: 'F (เล็ก)', width: 31.0, length: 36.0, height: 13.0, maxWeight: 20.0, cost: 9.50, carrier: 'Thailand Post' },
            { id: 'b_F_lg', name: 'F (ใหญ่)', width: 32.0, length: 48.0, height: 30.0, maxWeight: 25.0, cost: 16.00, carrier: 'Thailand Post' },
            { id: 'b_M', name: 'M', width: 27.0, length: 43.0, height: 20.0, maxWeight: 20.0, cost: 10.00, carrier: 'Thailand Post' },
            { id: 'b_M_plus', name: 'M+', width: 35.0, length: 45.0, height: 25.0, maxWeight: 25.0, cost: 15.00, carrier: 'Thailand Post' },
            { id: 'b_G', name: 'G', width: 31.0, length: 36.0, height: 26.0, maxWeight: 20.0, cost: 12.00, carrier: 'Thailand Post' },
            { id: 'b_6_cha', name: '6(ฉ)', width: 30.0, length: 45.0, height: 22.0, maxWeight: 20.0, cost: 12.20, carrier: 'Thailand Post' },
            { id: 'b_7', name: '7', width: 35.0, length: 50.0, height: 32.0, maxWeight: 25.0, cost: 18.00, carrier: 'Thailand Post' },
            { id: 'b_H', name: 'H', width: 41.0, length: 45.0, height: 35.0, maxWeight: 30.0, cost: 20.00, carrier: 'Thailand Post' },
            { id: 'b_L', name: 'L', width: 40.0, length: 50.0, height: 30.0, maxWeight: 30.0, cost: 19.00, carrier: 'Thailand Post' },
            { id: 'b_I_3', name: 'I 3 ชั้น', width: 45.0, length: 55.0, height: 40.0, maxWeight: 30.0, cost: 27.00, carrier: 'Thailand Post' },
            { id: 'b_I_5', name: 'I 5 ชั้น', width: 45.0, length: 55.0, height: 40.0, maxWeight: 35.0, cost: 48.00, carrier: 'Thailand Post' },
            { id: 'b_BIGBOX', name: 'BIG BOX (5 ชั้น)', width: 52.0, length: 92.0, height: 48.0, maxWeight: 50.0, cost: 125.00, carrier: 'Thailand Post' },
            { id: 'b_AH', name: 'AH', width: 14.0, length: 20.0, height: 35.0, maxWeight: 10.0, cost: 5.50, carrier: 'Thailand Post' },
            { id: 'b_BH', name: 'BH', width: 17.0, length: 25.0, height: 35.0, maxWeight: 12.0, cost: 7.00, carrier: 'Thailand Post' },
            { id: 'b_P1', name: 'P1', width: 24.0, length: 58.0, height: 17.0, maxWeight: 20.0, cost: 13.00, carrier: 'Thailand Post' },
            { id: 'b_P2', name: 'P2', width: 33.0, length: 58.0, height: 24.0, maxWeight: 25.0, cost: 19.00, carrier: 'Thailand Post' },
            { id: 'b_P3', name: 'P3', width: 20.0, length: 80.0, height: 20.0, maxWeight: 25.0, cost: 14.00, carrier: 'Thailand Post' },
            { id: 'b_P4', name: 'P4', width: 30.0, length: 100.0, height: 20.0, maxWeight: 30.0, cost: 23.00, carrier: 'Thailand Post' }
        ];

        const stored = window.StorageManager.get(this.key, null);
        // Automatically overwrite if the stored list is the old 6-item default list
        if (stored === null || stored.length < 10) {
            window.StorageManager.set(this.key, defaults);
            return defaults;
        }
        return stored;
    }

    save() {
        window.StorageManager.set(this.key, this.boxes);
    }

    getAll() {
        return this.boxes;
    }

    getById(id) {
        return this.boxes.find(b => b.id === id) || null;
    }

    add(box) {
        const newBox = {
            id: 'b_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
            name: box.name || 'New Box Type',
            width: parseFloat(box.width) || 10.0,
            length: parseFloat(box.length) || 10.0,
            height: parseFloat(box.height) || 10.0,
            maxWeight: parseFloat(box.maxWeight) || 5.0,
            cost: parseFloat(box.cost) || 0.0,
            carrier: box.carrier || 'Generic Carrier'
        };
        this.boxes.push(newBox);
        this.save();
        return newBox;
    }

    update(id, updatedBox) {
        const index = this.boxes.findIndex(b => b.id === id);
        if (index !== -1) {
            this.boxes[index] = {
                ...this.boxes[index],
                name: updatedBox.name,
                width: parseFloat(updatedBox.width),
                length: parseFloat(updatedBox.length),
                height: parseFloat(updatedBox.height),
                maxWeight: parseFloat(updatedBox.maxWeight),
                cost: parseFloat(updatedBox.cost),
                carrier: updatedBox.carrier
            };
            this.save();
            return this.boxes[index];
        }
        return null;
    }

    delete(id) {
        const initialLength = this.boxes.length;
        this.boxes = this.boxes.filter(b => b.id !== id);
        if (this.boxes.length !== initialLength) {
            this.save();
            return true;
        }
        return false;
    }

    clearAll() {
        this.boxes = [];
        this.save();
    }
}
window.BoxDatabase = BoxDatabase;
