/**
 * Product Database for Packing Optimizer Pro
 * Manages the CRUD lifecycle of products with LocalStorage persistence.
 */
class ProductDatabase {
    constructor() {
        this.key = 'products';
        this.products = this.load();
    }

    load() {
        const defaults = [
            {
                id: 'p1',
                sku: 'SKU-BOOK-01',
                name: 'Standard Textbook',
                width: 15.0,
                length: 22.0,
                height: 3.0,
                weight: 0.8,
                quantity: 5,
                rotationAllowed: { roll: true, pitch: true, yaw: true },
                fragile: false
            },
            {
                id: 'p2',
                sku: 'SKU-MUG-02',
                name: 'Ceramic Coffee Mug',
                width: 12.0,
                length: 12.0,
                height: 10.0,
                weight: 0.4,
                quantity: 2,
                rotationAllowed: { roll: false, pitch: false, yaw: true }, // Keep upright
                fragile: true
            },
            {
                id: 'p3',
                sku: 'SKU-LAPTOP-03',
                name: 'Slim Ultrabook Laptop',
                width: 24.0,
                length: 35.0,
                height: 2.0,
                weight: 1.6,
                quantity: 1,
                rotationAllowed: { roll: true, pitch: true, yaw: true },
                fragile: true
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
