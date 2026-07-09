/**
 * ResultPanel Component for Packing Optimizer Pro
 * Manages the visualization of recommendation details in Thai.
 */
class ResultPanel {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.currentResults = null;
        this.init();
    }

    init() {
        this.clear();
    }

    clear() {
        this.container.innerHTML = `
            <div style="text-align: center; padding: 60px 20px; color: var(--text-muted);">
                <div style="font-size: 48px; margin-bottom: 12px; filter: grayscale(100%);">📊</div>
                <h3>ยังไม่มีผลลัพธ์การจัดกล่อง</h3>
                <p style="font-size: 13px; margin-top: 6px;">ตั้งค่าตัวแปรบรรจุภัณฑ์และกด "เริ่มคำนวณการจัดกล่อง" เพื่อเริ่มต้นคำนวณ</p>
            </div>
        `;
    }

    render(results) {
        this.currentResults = results;
        
        if (!results || results.length === 0 || (results.parcels && results.parcels.length === 0)) {
            this.container.innerHTML = `
                <div class="recommendation-alert" style="background-color: var(--color-danger-light); border-color: rgba(239, 68, 68, 0.2); color: var(--color-danger);">
                    <div class="recommendation-title" style="color: var(--color-danger);">
                        ⚠️ การจัดกล่องล้มเหลว
                    </div>
                    <div>ไม่สามารถบรรจุสินค้าลงกล่องพัสดุได้สำเร็จ กรุณาตรวจสอบขนาดและน้ำหนักสินค้าเปรียบเทียบกับข้อจำกัดของขนาดกล่องที่มีอยู่ในระบบ</div>
                </div>
            `;
            return;
        }

        const { parcels, overallScore, totalBoxCost = results.totalCost || 0, totalTapeCost = 0, totalBubbleCost = 0, totalLabelCost = 0, totalCost, totalVolumeUtilization, totalWeight, totalTareWeight = 0, totalParcelWeight } = results;

        const scorePercent = Math.min(100, Math.max(0, overallScore));
        
        this.container.innerHTML = `
            <div class="results-panel-card">
                <div class="section-title">
                    <span>📊 สรุปผลการจัดกล่อง</span>
                </div>

                <div class="score-display">
                    <div class="score-circle" style="--score-percent: ${scorePercent}%">
                        <div class="score-text">${Math.round(overallScore)} คะแนน</div>
                    </div>
                    <div class="stat-label">คะแนนประสิทธิภาพโดยรวม</div>
                </div>

                <div class="recommendation-alert">
                    <div class="recommendation-title">
                        🎉 ผลลัพธ์การจัดส่งแนะนำ
                    </div>
                    <div>จำนวนที่ใช้จัดส่ง: <strong>${parcels.length}</strong> กล่อง</div>
                    
                    <div style="margin: 8px 0; border-top: 1px dashed rgba(255,255,255,0.15); border-bottom: 1px dashed rgba(255,255,255,0.15); padding: 8px 0; font-size: 11px; color: var(--text-secondary);">
                        <div style="font-weight: 700; color: var(--text-primary); margin-bottom: 4px;">รายละเอียดต้นทุนวัสดุบรรจุภัณฑ์:</div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 2px;">
                            <span>📦 ค่ากล่องพัสดุรวม:</span>
                            <strong>${totalBoxCost.toFixed(2)} บาท</strong>
                        </div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 2px;">
                            <span>🎗️ ค่าเทปปิดกล่องรวม:</span>
                            <strong>${totalTapeCost.toFixed(2)} บาท</strong>
                        </div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 2px;">
                            <span>🫧 ค่าบับเบิ้ลกันกระแทกรวม:</span>
                            <strong>${totalBubbleCost.toFixed(2)} บาท</strong>
                        </div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 2px;">
                            <span>🏷️ ค่าใบปะหน้าสินค้า (แผ่นละ 5฿):</span>
                            <strong>${totalLabelCost.toFixed(2)} บาท</strong>
                        </div>
                        <div style="display: flex; justify-content: space-between; font-size: 12.5px; font-weight: 700; color: var(--color-primary); margin-top: 6px; border-top: 1px solid var(--border-color); padding-top: 6px;">
                            <span>💰 ต้นทุนวัสดุรวมทั้งหมด:</span>
                            <span>${totalCost.toFixed(2)} บาท</span>
                        </div>
                    </div>

                    <div>การใช้พื้นที่เฉลี่ย: <strong>${totalVolumeUtilization.toFixed(1)}%</strong></div>
                    <div style="margin-top:6px; border-top: 1px dashed rgba(255,255,255,0.1); padding-top:6px;">
                        <div style="display:flex; justify-content:space-between; font-size:11px; color:var(--text-secondary);">
                            <span>📦 น้ำหนักสินค้ารวม:</span><strong>${totalWeight.toFixed(2)} กก.</strong>
                        </div>
                        <div style="display:flex; justify-content:space-between; font-size:11px; color:var(--text-secondary);">
                            <span>🗃️ น้ำหนักกล่องรวม (tare):</span><strong>${(totalTareWeight||0).toFixed(2)} กก.</strong>
                        </div>
                        <div style="display:flex; justify-content:space-between; font-size:12px; font-weight:700; color:var(--color-warning); margin-top:4px;">
                            <span>⚖️ น้ำหนักพัสดุรวม (ส่งขนส่ง):</span><strong>${(totalParcelWeight||totalWeight).toFixed(2)} / 20 กก.</strong>
                        </div>
                    </div>
                </div>

                <div>
                    <h4 style="font-size: 13px; margin-bottom: 8px; color: var(--text-secondary);">รายละเอียดรายพัสดุ:</h4>
                    <div class="result-list">
                        ${parcels.map((p, idx) => `
                            <div class="result-item" style="border-left: 3px solid ${p.isFullFit ? 'var(--color-success)' : 'var(--color-warning)'}; flex-direction: column; align-items: stretch; gap: 6px;">
                                <div style="display: flex; justify-content: space-between; width: 100%;">
                                    <div>
                                        <strong style="display: block;">พัสดุใบที่ #${idx + 1}: ${escapeHtml(p.box.name)}</strong>
                                        <span style="font-size: 11px; color: var(--text-muted);">
                                            สินค้า: ${p.placements.length} ชิ้น | นน.สินค้า: ${(p.cargoWeight||p.weight||0).toFixed(2)} กก. | กล่อง: ${(p.box.tare||0).toFixed(2)} กก. | รวม: ${((p.cargoWeight||p.weight||0)+(p.box.tare||0)).toFixed(2)}/${p.box.maxWeight} กก.
                                        </span>
                                    </div>
                                    <div style="text-align: right;">
                                        <div style="font-weight: 700; color: var(--color-primary);">${p.volumeUtilization.toFixed(1)}% ความจุ</div>
                                        <span style="font-size: 11px; color: var(--text-muted);">${p.box.carrier}</span>
                                    </div>
                                </div>
                                ${p.gaps ? `
                                <div style="width: 100%; border-top: 1px dashed var(--border-color); padding-top: 6px; font-size: 10.5px; color: var(--text-secondary);">
                                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 2px;">
                                        <span>🔒 สถานะการล็อคแน่น: ${p.hasSnugLock ? '<span style="color: var(--color-success); font-weight: 700;">พอดีแน่นหนา (Gap < 1 ซม.)</span>' : '<span style="color: var(--color-warning);">มีช่องว่างเลื่อนขยับได้</span>'}</span>
                                    </div>
                                    <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 4px; font-family: monospace;">
                                        <span>กว้างเหลือ: ${p.gaps.width.toFixed(1)}ซม.</span>
                                        <span>ยาวเหลือ: ${p.gaps.length.toFixed(1)}ซม.</span>
                                        <span>สูงเหลือ: ${p.gaps.height.toFixed(1)}ซม.</span>
                                    </div>
                                    ${!p.hasSnugLock ? `<div style="color: var(--text-muted); font-size: 9px; margin-top: 2px;">💡 แนะนำหนุนวัสดุกันกระแทกในช่องว่างเพื่อให้สินค้าล็อคไม่สั่นไหว</div>` : ''}
                                </div>
                                ` : ''}
                            </div>
                        `).join('')}
                    </div>
                </div>

                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 8px;">
                    <button class="btn btn-secondary btn-sm" id="btn-export-json">ส่งออก JSON</button>
                    <button class="btn btn-secondary btn-sm" id="btn-export-csv">ส่งออก CSV</button>
                    <button class="btn btn-primary btn-sm" id="btn-print-report" style="grid-column: span 2;">พิมพ์ใบส่งพัสดุ / Slip</button>
                </div>
            </div>
        `;

        // Attach Export Listeners
        document.getElementById('btn-export-json').addEventListener('click', () => this.exportJson());
        document.getElementById('btn-export-csv').addEventListener('click', () => this.exportCsv());
        document.getElementById('btn-print-report').addEventListener('click', () => window.print());
    }

    exportJson() {
        if (!this.currentResults) return;
        window.ExportUtil.exportToJson(this.currentResults, 'packing-results.json');
        showToast('ส่งออกไฟล์ JSON เรียบร้อยแล้ว', 'success');
    }

    exportCsv() {
        if (!this.currentResults) return;
        window.ExportUtil.exportToCsv(this.currentResults, 'packing-results.csv');
        showToast('ส่งออกไฟล์ CSV เรียบร้อยแล้ว', 'success');
    }
}

window.ResultPanel = ResultPanel;
