/**
 * Export Utility for Packing Optimizer Pro
 * Provides JSON download, CSV formatting, and print options in Thai.
 */
class ExportUtil {
    static exportToJson(data, filename) {
        const jsonStr = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonStr], { type: 'application/json' });
        this.downloadBlob(blob, filename);
    }

    static exportToCsv(data, filename) {
        const headers = [
            'ลำดับพัสดุ',
            'ชื่อกล่อง',
            'ขนาดกล่อง (กxยxส)',
            'ผู้ให้บริการขนส่ง',
            'รหัสสินค้า (SKU)',
            'ชื่อสินค้า',
            'พิกัดแนวแกน X (ซม.)',
            'พิกัดแนวแกน Y (ซม.)',
            'พิกัดแนวแกน Z (ซม.)',
            'ขนาดสินค้าที่จัดวาง (กxยxส)'
        ];

        const rows = [];
        rows.push(headers.join(','));

        if (data && data.parcels) {
            data.parcels.forEach((parcel, parcelIdx) => {
                const boxDim = `${parcel.box.width}x${parcel.box.length}x${parcel.box.height}`;
                
                parcel.placements.forEach(placement => {
                    const placedDim = `${placement.width}x${placement.length}x${placement.height}`;
                    
                    const rowData = [
                        `กล่องใบที่ #${parcelIdx + 1}`,
                        this.escapeCsvField(parcel.box.name),
                        boxDim,
                        this.escapeCsvField(parcel.box.carrier),
                        this.escapeCsvField(placement.sku),
                        this.escapeCsvField(placement.name),
                        placement.x,
                        placement.y,
                        placement.z,
                        placedDim
                    ];
                    
                    rows.push(rowData.join(','));
                });
            });
        }

        const csvContent = "\uFEFF" + rows.join('\n'); // Add BOM for Excel UTF-8 support
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        this.downloadBlob(blob, filename);
    }

    static escapeCsvField(field) {
        if (field === null || field === undefined) return '';
        let value = String(field);
        if (value.includes(',') || value.includes('"') || value.includes('\n')) {
            value = value.replace(/"/g, '""');
            return `"${value}"`;
        }
        return value;
    }

    static downloadBlob(blob, filename) {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
}

window.ExportUtil = ExportUtil;
