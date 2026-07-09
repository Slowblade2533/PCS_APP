/**
 * ProductPanel Component for Packing Optimizer Pro
 * Manages UI interactions for Product List CRUD in Thai.
 */
class ProductPanel {
    constructor(db, containerId, modalId, formId, onDataChanged) {
        this.db = db;
        this.container = document.getElementById(containerId);
        this.modal = document.getElementById(modalId);
        this.form = document.getElementById(formId);
        this.onDataChanged = onDataChanged;
        
        this.editingId = null;
        this.init();
    }

    init() {
        // Form Submit
        this.form.addEventListener('submit', (e) => this.handleSubmit(e));
        
        // Open Modal Trigger
        const addBtn = document.getElementById('btn-add-product');
        if (addBtn) {
            addBtn.addEventListener('click', () => this.openAddModal());
        }

        // Clear All Trigger
        const clearBtn = document.getElementById('btn-clear-products');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => this.handleClearAll());
        }

        // Quick Default Buttons
        const quick1 = document.getElementById('btn-quick-std-1');
        const quick2 = document.getElementById('btn-quick-std-2');
        if (quick1) quick1.addEventListener('click', () => this.applyQuickDefault(1));
        if (quick2) quick2.addEventListener('click', () => this.applyQuickDefault(2));

        this.render();
    }

    render() {
        const products = this.db.getAll();
        
        if (products.length === 0) {
            this.container.innerHTML = `
                <div style="text-align: center; padding: 32px 16px; color: var(--text-muted);">
                    <div style="font-size: 32px; margin-bottom: 8px;">📦</div>
                    <p style="font-size: 13px;">ยังไม่มีสินค้าในระบบ</p>
                </div>
            `;
            return;
        }

        this.container.innerHTML = `
            <div class="card-list">
                ${products.map(p => `
                    <div class="item-card" data-id="${p.id}">
                        <div class="item-card-header">
                            <div>
                                <div class="item-card-title">${escapeHtml(p.name)}</div>
                                <div class="item-card-subtitle">${escapeHtml(p.sku)}</div>
                            </div>
                            <span class="item-card-badge ${p.fragile ? 'fragile' : ''}">
                                ${p.fragile ? 'แตกง่าย ⚠️' : 'ปกติ'}
                            </span>
                        </div>
                        <div class="item-card-details">
                            <div>ขนาด: <span>${p.width}x${p.length}x${p.height}</span> ซม.</div>
                            <div>นน้ำหนัก: <span>${p.weight}</span> กก.</div>
                            <div>จำนวน: <span>${p.quantity}</span> ชิ้น</div>
                        </div>
                        <div class="item-card-actions">
                            <button class="btn btn-secondary btn-sm btn-edit-prod" data-id="${p.id}">แก้ไข</button>
                            <button class="btn btn-danger btn-sm btn-delete-prod" data-id="${p.id}">ลบ</button>
                        </div>
                    </div>
                `).join('')}
            </div>
        `;

        // Attach action events
        this.container.querySelectorAll('.btn-edit-prod').forEach(btn => {
            btn.addEventListener('click', () => this.openEditModal(btn.dataset.id));
        });

        this.container.querySelectorAll('.btn-delete-prod').forEach(btn => {
            btn.addEventListener('click', () => this.handleDelete(btn.dataset.id));
        });
    }

    openAddModal() {
        this.editingId = null;
        document.getElementById('product-modal-title').textContent = 'เพิ่มสินค้าใหม่';
        this.form.reset();
        
        // Apply default standard product size 1
        this.applyQuickDefault(1);

        // Reset checkboxes
        document.getElementById('prod-rot-roll').checked = true;
        document.getElementById('prod-rot-pitch').checked = true;
        document.getElementById('prod-rot-yaw').checked = true;
        document.getElementById('prod-fragile').checked = false;

        this.modal.classList.add('active');
    }

    applyQuickDefault(type) {
        if (type === 1) {
            document.getElementById('prod-sku').value = 'SKU-STD-01';
            document.getElementById('prod-name').value = 'สินค้าต่อกล่อง (ขนาดกลาง)';
            document.getElementById('prod-width').value = 25.5;
            document.getElementById('prod-length').value = 14.5;
            document.getElementById('prod-height').value = 14.0;
            document.getElementById('prod-weight').value = 2.0;
            document.getElementById('prod-qty').value = 1;
        } else if (type === 2) {
            document.getElementById('prod-sku').value = 'SKU-STD-02';
            document.getElementById('prod-name').value = 'สินค้าต่อกล่อง (ขนาดเล็ก)';
            document.getElementById('prod-width').value = 34.0;
            document.getElementById('prod-length').value = 16.0;
            document.getElementById('prod-height').value = 11.0;
            document.getElementById('prod-weight').value = 1.7;
            document.getElementById('prod-qty').value = 1;
        }
    }

    openEditModal(id) {
        const p = this.db.getById(id);
        if (!p) return;

        this.editingId = id;
        document.getElementById('product-modal-title').textContent = 'แก้ไขข้อมูลสินค้า';
        
        document.getElementById('prod-sku').value = p.sku;
        document.getElementById('prod-name').value = p.name;
        document.getElementById('prod-width').value = p.width;
        document.getElementById('prod-length').value = p.length;
        document.getElementById('prod-height').value = p.height;
        document.getElementById('prod-weight').value = p.weight;
        document.getElementById('prod-qty').value = p.quantity;
        
        document.getElementById('prod-rot-roll').checked = p.rotationAllowed.roll;
        document.getElementById('prod-rot-pitch').checked = p.rotationAllowed.pitch;
        document.getElementById('prod-rot-yaw').checked = p.rotationAllowed.yaw;
        document.getElementById('prod-fragile').checked = p.fragile;

        this.modal.classList.add('active');
    }

    handleSubmit(e) {
        e.preventDefault();
        
        const productData = {
            sku: document.getElementById('prod-sku').value,
            name: document.getElementById('prod-name').value,
            width: parseFloat(document.getElementById('prod-width').value),
            length: parseFloat(document.getElementById('prod-length').value),
            height: parseFloat(document.getElementById('prod-height').value),
            weight: parseFloat(document.getElementById('prod-weight').value),
            quantity: parseInt(document.getElementById('prod-qty').value),
            rotationAllowed: {
                roll: document.getElementById('prod-rot-roll').checked,
                pitch: document.getElementById('prod-rot-pitch').checked,
                yaw: document.getElementById('prod-rot-yaw').checked
            },
            fragile: document.getElementById('prod-fragile').checked
        };

        if (this.editingId) {
            this.db.update(this.editingId, productData);
            showToast('อัปเดตข้อมูลสินค้าเรียบร้อยแล้ว', 'success');
        } else {
            this.db.add(productData);
            showToast('เพิ่มสินค้าเรียบร้อยแล้ว', 'success');
        }

        this.modal.classList.remove('active');
        this.render();
        this.onDataChanged();
    }

    handleDelete(id) {
        if (confirm('คุณต้องการลบสินค้าชิ้นนี้ใช่หรือไม่?')) {
            this.db.delete(id);
            showToast('ลบสินค้าออกแล้ว', 'info');
            this.render();
            this.onDataChanged();
        }
    }

    handleClearAll() {
        if (confirm('คุณแน่ใจว่าต้องการล้างข้อมูลสินค้าทั้งหมดใช่หรือไม่?')) {
            this.db.clearAll();
            showToast('ล้างข้อมูลสินค้าทั้งหมดแล้ว', 'info');
            this.render();
            this.onDataChanged();
        }
    }
}

// Utility function to escape HTML
function escapeHtml(str) {
    if (!str) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

window.ProductPanel = ProductPanel;
