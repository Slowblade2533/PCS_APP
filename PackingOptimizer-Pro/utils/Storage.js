/**
 * Storage Utility for Packing Optimizer Pro
 * Provides persistent storage using localStorage.
 */
class StorageManager {
    static get(key, defaultValue = null) {
        try {
            const data = localStorage.getItem(`po_pro_${key}`);
            return data ? JSON.parse(data) : defaultValue;
        } catch (e) {
            console.error("Error reading from localStorage:", e);
            return defaultValue;
        }
    }

    static set(key, value) {
        try {
            localStorage.setItem(`po_pro_${key}`, JSON.stringify(value));
            return true;
        } catch (e) {
            console.error("Error writing to localStorage:", e);
            return false;
        }
    }

    static clear(key) {
        try {
            localStorage.removeItem(`po_pro_${key}`);
            return true;
        } catch (e) {
            console.error("Error clearing localStorage:", e);
            return false;
        }
    }
}
window.StorageManager = StorageManager;
