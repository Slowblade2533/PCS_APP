/**
 * BoxPanel Component for Packing Optimizer Pro
 * Manages UI interactions for Packaging Box List CRUD in Thai.
 */
class BoxPanel {
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
        const addBtn = document.getElementById('btn-add-box');
        if (addBtn) {
            addBtn.addEventListener('click', () => this.openAddModal());
        }

        // Clear All Trigger
        const clearBtn = document.getElementById('btn-clear-boxes');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => this.handleClearAll());
        }

        this.render();
    }

    render() {
        const boxes = this.db.getAll();
        
        if (boxes.length === 0) {
            this.container.innerHTML = `
                <div style="text-align: center; padding: 32px 16px; color: var(--text-muted);">
                    <div style="font-size: 32px; margin-bottom: 8px;">📦</div>
                    <p style="font-size: 13px;">ยังไม่มีขนาดกล่องในระบบ</p>
                </div>
            `;
            return;
        }

        this.container.innerHTML = `
            <div class="card-list">
                ${boxes.map(b => `
                    <div class="item-card" data-id="${b.id}">
                        <div class="item-card-header">
                            <div>
                                <div class="item-card-title">${escapeHtml(b.name)}</div>
                                <div class="item-card-subtitle">${escapeHtml(b.carrier)}</div>
                            </div>
                            <span class="item-card-badge" style="background-color: var(--color-primary-light); color: var(--color-primary);">
                                ${b.cost.toFixed(2)} บาท
                            </span>
                        </div>
                        <div class="item-card-details">
                            <div>ขนาด: <span>${b.width}×${b.length}×${b.height}</span> ซม.</div>
                            <div>น้ำหนักกล่องเปล่า: <span>${(b.tare !== undefined ? b.tare : '?').toString()}</span> กก.</div>
                            <div>Payload สุทธิ: <span>${Math.min(b.maxWeight, 20 - (b.tare||0)).toFixed(2)}</span> กก.</div>
                        </div>
                        <div class="item-card-actions">
                            <button class="btn btn-secondary btn-sm btn-edit-box" data-id="${b.id}">แก้ไข</button>
                            <button class="btn btn-danger btn-sm btn-delete-box" data-id="${b.id}">ลบ</button>
                        </div>
                    </div>
                `).join('')}
            </div>
        `;

        // Attach action events
        this.container.querySelectorAll('.btn-edit-box').forEach(btn => {
            btn.addEventListener('click', () => this.openEditModal(btn.dataset.id));
        });

        this.container.querySelectorAll('.btn-delete-box').forEach(btn => {
            btn.addEventListener('click', () => this.handleDelete(btn.dataset.id));
        });
    }

    openAddModal() {
        this.editingId = null;
        document.getElementById('box-modal-title').textContent = 'เพิ่มขนาดกล่องใหม่';
        this.form.reset();
        this.modal.classList.add('active');
    }

    openEditModal(id) {
        const b = this.db.getById(id);
        if (!b) return;

        this.editingId = id;
        document.getElementById('box-modal-title').textContent = 'แก้ไขขนาดกล่อง';
        
        document.getElementById('box-name').value = b.name;
        document.getElementById('box-width').value = b.width;
        document.getElementById('box-length').value = b.length;
        document.getElementById('box-height').value = b.height;
        document.getElementById('box-max-weight').value = b.maxWeight;
        document.getElementById('box-tare').value = b.tare !== undefined ? b.tare : '';
        document.getElementById('box-cost').value = b.cost;
        document.getElementById('box-carrier').value = b.carrier;

        this.modal.classList.add('active');
    }

    handleSubmit(e) {
        e.preventDefault();
        
        const boxData = {
            name: document.getElementById('box-name').value,
            width: parseFloat(document.getElementById('box-width').value),
            length: parseFloat(document.getElementById('box-length').value),
            height: parseFloat(document.getElementById('box-height').value),
            maxWeight: parseFloat(document.getElementById('box-max-weight').value),
            tare: document.getElementById('box-tare').value !== '' ? parseFloat(document.getElementById('box-tare').value) : undefined,
            cost: parseFloat(document.getElementById('box-cost').value),
            carrier: document.getElementById('box-carrier').value
        };

        if (this.editingId) {
            this.db.update(this.editingId, boxData);
            showToast('อัปเดตข้อมูลขนาดกล่องเรียบร้อยแล้ว', 'success');
        } else {
            this.db.add(boxData);
            showToast('เพิ่มกล่องพัสดุเรียบร้อยแล้ว', 'success');
        }

        this.modal.classList.remove('active');
        this.render();
        this.onDataChanged();
    }

    handleDelete(id) {
        if (confirm('คุณต้องการลบกล่องขนาดนี้ใช่หรือไม่?')) {
            this.db.delete(id);
            showToast('ลบประเภทกล่องออกแล้ว', 'info');
            this.render();
            this.onDataChanged();
        }
    }

    handleClearAll() {
        if (confirm('คุณแน่ใจว่าต้องการล้างข้อมูลขนาดกล่องทั้งหมดใช่หรือไม่?')) {
            this.db.clearAll();
            showToast('ล้างข้อมูลกล่องทั้งหมดแล้ว', 'info');
            this.render();
            this.onDataChanged();
        }
    }
}

window.BoxPanel = BoxPanel;
