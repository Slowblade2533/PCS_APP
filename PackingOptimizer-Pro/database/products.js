/**
 * Product Database for Packing Optimizer Pro
 * Manages the CRUD lifecycle of products with LocalStorage persistence.
 */
class ProductDatabase {
    constructor() {
        this.key = 'products_v3';
        this.products = this.load();
    }

    load() {
        const defaults = [
            {
                id: 'p1',
                sku: 'SKU-STD-01',
                name: 'สินค้าต่อกล่อง (ขนาดกลาง)',
                width: 25.5,
                length: 14.5,
                height: 14.0,
                weight: 2.0,
                quantity: 1,
                rotationAllowed: { roll: true, pitch: true, yaw: true },
                fragile: false
            },
            {
                id: 'p2',
                sku: 'SKU-STD-02',
                name: 'สินค้าต่อกล่อง (ขนาดเล็ก)',
                width: 34.0,
                length: 16.0,
                height: 11.0,
                weight: 1.7,
                quantity: 1,
                rotationAllowed: { roll: true, pitch: true, yaw: true },
                fragile: false
            }
        ];
        return window.StorageManager.get(this.key, defaults);
    }

    save() {
        window.StorageManager.set(this.key, this.products);
    }

    getAll() {
        return this.products;
    }

    getById(id) {
        return this.products.find(p => p.id === id) || null;
    }

    add(product) {
        const newProduct = {
            id: 'p_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
            sku: product.sku || 'SKU-NEW',
            name: product.name || 'New Product',
            width: parseFloat(product.width) || 1.0,
            length: parseFloat(product.length) || 1.0,
            height: parseFloat(product.height) || 1.0,
            weight: parseFloat(product.weight) || 0.1,
            quantity: parseInt(product.quantity) || 1,
            rotationAllowed: product.rotationAllowed || { roll: true, pitch: true, yaw: true },
            fragile: !!product.fragile
        };
        this.products.push(newProduct);
        this.save();
        return newProduct;
    }

    update(id, updatedProduct) {
        const index = this.products.findIndex(p => p.id === id);
        if (index !== -1) {
            this.products[index] = {
                ...this.products[index],
                sku: updatedProduct.sku,
                name: updatedProduct.name,
                width: parseFloat(updatedProduct.width),
                length: parseFloat(updatedProduct.length),
                height: parseFloat(updatedProduct.height),
                weight: parseFloat(updatedProduct.weight),
                quantity: parseInt(updatedProduct.quantity),
                rotationAllowed: updatedProduct.rotationAllowed,
                fragile: !!updatedProduct.fragile
            };
            this.save();
            return this.products[index];
        }
        return null;
    }

    delete(id) {
        const initialLength = this.products.length;
        this.products = this.products.filter(p => p.id !== id);
        if (this.products.length !== initialLength) {
            this.save();
            return true;
        }
        return false;
    }

    clearAll() {
        this.products = [];
        this.save();
    }
}
window.ProductDatabase = ProductDatabase;
