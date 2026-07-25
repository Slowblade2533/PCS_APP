import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PackingService, FileParseResult, ParsedParcelCard } from '../../../services/packing.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-packing-import',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './packing-import.component.html',
  styleUrl: './packing-import.component.css'
})
export class PackingImportComponent implements OnInit {
  isDragging = false;
  selectedFiles: File[] = [];
  parseResults: FileParseResult[] = [];
  parcelCards: ParsedParcelCard[] = [];
  todayDate = new Date();

  summary = {
    totalFiles: 0,
    totalOrders: 0,
    totalParcels: 0,
    totalItems: 0,
    matchedSkus: 0,
    unmatchedSkus: 0
  };

  activePlatformFilter: 'ALL' | 'Lazada' | 'Shopee' = 'ALL';
  printMode: 'compact' | 'full' = 'compact';
  isProcessing = false;
  showPrintModal = false;
  historyBatches: any[] = [];
  isSavingDraft = false;

  constructor(
    private packingService: PackingService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadHistoryBatches();
  }

  get lazadaSummary() {
    const cards = this.parcelCards.filter(c => c.platform === 'Lazada');
    const totalOrders = new Set(cards.map(c => c.orderNo)).size;
    const totalParcels = cards.length;
    let totalItems = 0;
    cards.forEach(c => c.items.forEach(i => totalItems += i.orderedQuantity));
    return { totalOrders, totalParcels, totalItems };
  }

  get shopeeSummary() {
    const cards = this.parcelCards.filter(c => c.platform === 'Shopee');
    const totalOrders = new Set(cards.map(c => c.orderNo)).size;
    const totalParcels = cards.length;
    let totalItems = 0;
    cards.forEach(c => c.items.forEach(i => totalItems += i.orderedQuantity));
    return { totalOrders, totalParcels, totalItems };
  }

  get filteredParcelCards(): ParsedParcelCard[] {
    if (this.activePlatformFilter === 'ALL') {
      return this.parcelCards;
    }
    return this.parcelCards.filter(c => c.platform === this.activePlatformFilter);
  }

  get filteredMasterPickList() {
    const cards = this.filteredParcelCards;
    const map = new Map<string, { platform: string; sku: string; productName: string; isSkuMatched: boolean; totalQty: number; qrCodeUrl?: string }>();

    for (const card of cards) {
      for (const item of card.items) {
        const key = `${item.platform}_${item.sku}`;
        if (!map.has(key)) {
          map.set(key, {
            platform: item.platform,
            sku: item.sku,
            productName: item.productName,
            isSkuMatched: item.isSkuMatched,
            totalQty: item.orderedQuantity,
            qrCodeUrl: item.qrCodeUrl
          });
        } else {
          map.get(key)!.totalQty += item.orderedQuantity;
        }
      }
    }

    return Array.from(map.values()).sort((a, b) => a.platform.localeCompare(b.platform) || a.sku.localeCompare(b.sku));
  }

  setPlatformFilter(platform: 'ALL' | 'Lazada' | 'Shopee'): void {
    this.activePlatformFilter = platform;
    this.cdr.detectChanges();
  }

  setPrintMode(mode: 'compact' | 'full'): void {
    this.printMode = mode;
    this.cdr.detectChanges();
  }

  onFileSelected(event: any): void {
    const input = event.target as HTMLInputElement;
    const files: FileList | null = input.files;
    if (files && files.length > 0) {
      this.handleFiles(Array.from(files));
    }
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer && event.dataTransfer.files.length > 0) {
      this.handleFiles(Array.from(event.dataTransfer.files));
    }
  }

  async handleFiles(files: File[]): Promise<void> {
    const excelFiles = files.filter(f => f.name.endsWith('.xlsx') || f.name.endsWith('.xls'));
    if (excelFiles.length === 0) {
      Swal.fire('ข้อผิดพลาด', 'กรุณาเลือกไฟล์ Excel (.xlsx หรือ .xls) เท่านั้น', 'warning');
      return;
    }

    this.selectedFiles = excelFiles;
    this.isProcessing = true;
    this.cdr.detectChanges();

    try {
      const res = await this.packingService.parseExcelFiles(excelFiles);
      this.parseResults = res.results;
      this.parcelCards = res.parcelCards;
      this.summary = res.summary;

      const invalidFiles = res.results.filter(r => !r.isValid);
      if (invalidFiles.length > 0) {
        const errorMsgs = invalidFiles.map(f => `• ${f.fileName}: ${f.errorMessage}`).join('\n');
        Swal.fire({
          icon: 'warning',
          title: 'พบไฟล์ที่ไม่ถูกต้องตามรูปแบบแพลตฟอร์ม',
          html: `<pre class="text-left text-sm text-red-600 bg-red-50 p-3 rounded">${errorMsgs}</pre>`,
          confirmButtonText: 'ตกลง'
        });
      }
    } catch (err: any) {
      Swal.fire('ข้อผิดพลาด', 'เกิดข้อผิดพลาดในการประมวลผลไฟล์ Excel: ' + (err?.message || ''), 'error');
    } finally {
      this.isProcessing = false;
      this.cdr.detectChanges();
    }
  }

  openPrintPreview(): void {
    if (this.parcelCards.length === 0) {
      Swal.fire('ไม่มีข้อมูล', 'กรุณานำเข้าไฟล์ Excel ที่มีข้อมูลรายการแพ๊คก่อนกดพิมพ์', 'info');
      return;
    }
    this.todayDate = new Date();
    this.showPrintModal = true;
    this.cdr.detectChanges();
  }

  closePrintPreview(): void {
    this.showPrintModal = false;
    this.cdr.detectChanges();
  }

  triggerPrint(): void {
    window.print();
  }

  async saveDraftBatch(): Promise<void> {
    if (this.parcelCards.length === 0) {
      Swal.fire('ไม่มีข้อมูล', 'ไม่มีรายการแพ๊คสินค้าสำหรับบันทึกร่าง', 'info');
      return;
    }

    this.isSavingDraft = true;
    this.cdr.detectChanges();

    const items: any[] = [];
    for (const card of this.parcelCards) {
      for (const item of card.items) {
        items.push({
          platform: item.platform,
          orderNo: item.orderNo,
          trackingNo: item.trackingNo,
          shippingProvider: item.shippingProvider,
          sku: item.sku,
          variantId: item.variantId || null,
          productName: item.productName,
          orderedQuantity: item.orderedQuantity,
          shippedQuantity: 0,
          orderDate: item.orderDate ? new Date(item.orderDate).toISOString() : null,
          isMultiParcel: item.isMultiParcel,
          parcelSeq: item.parcelSeq,
          totalParcelsInOrder: item.totalParcelsInOrder,
          isSkuMatched: item.isSkuMatched
        });
      }
    }

    const payload = {
      totalFiles: this.summary.totalFiles,
      totalOrders: this.summary.totalOrders,
      totalParcels: this.summary.totalParcels,
      totalItems: this.summary.totalItems,
      notes: `ร่างนำเข้าเอกสารแพ๊ค ${new Date().toLocaleString('th-TH')}`,
      items
    };

    this.packingService.saveDraftBatch(payload).subscribe({
      next: (res) => {
        this.isSavingDraft = false;
        this.cdr.detectChanges();
        Swal.fire({
          icon: 'success',
          title: 'สำเร็จ',
          text: `บันทึกร่างเอกสารแพ๊คเรียบร้อย รหัสงวด: ${res.batchNo}`,
          confirmButtonText: 'ตกลง'
        });
        this.loadHistoryBatches();
      },
      error: (err) => {
        this.isSavingDraft = false;
        this.cdr.detectChanges();
        Swal.fire('ข้อผิดพลาด', 'ไม่สามารถบันทึกร่างเอกสารแพ๊คได้: ' + (err?.error?.message || err?.message || ''), 'error');
      }
    });
  }

  loadHistoryBatches(): void {
    this.packingService.getBatches(1, 20).subscribe({
      next: (res) => {
        this.historyBatches = res.items || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load history batches', err);
      }
    });
  }

  viewBatchDetail(batchId: number): void {
    this.packingService.getBatchById(batchId).subscribe({
      next: async (batch) => {
        const rawItems: any[] = (batch.items || []).map((i: any) => ({
          platform: i.platform,
          orderNo: i.orderNo,
          trackingNo: i.trackingNo,
          shippingProvider: i.shippingProvider || '',
          sku: i.sku,
          productName: i.productName || '',
          orderedQuantity: i.orderedQuantity,
          shippedQuantity: i.shippedQuantity,
          orderDate: i.orderDate,
          isMultiParcel: i.isMultiParcel,
          parcelSeq: i.parcelSeq,
          totalParcelsInOrder: i.totalParcelsInOrder,
          isSkuMatched: i.isSkuMatched,
          variantId: i.variantId,
          fileName: batch.batchNo
        }));

        this.parcelCards = await this.packingService.buildParcelCards(rawItems);
        this.summary = {
          totalFiles: batch.totalFiles,
          totalOrders: batch.totalOrders,
          totalParcels: batch.totalParcels,
          totalItems: batch.totalItems,
          matchedSkus: rawItems.filter(r => r.isSkuMatched).length,
          unmatchedSkus: rawItems.filter(r => !r.isSkuMatched).length
        };
        this.todayDate = new Date();
        this.showPrintModal = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        Swal.fire('ข้อผิดพลาด', 'ไม่สามารถดึงข้อมูลรายละเอียดงวดแพ๊คได้', 'error');
      }
    });
  }

  clearLoadedData(): void {
    this.selectedFiles = [];
    this.parseResults = [];
    this.parcelCards = [];
    this.summary = {
      totalFiles: 0,
      totalOrders: 0,
      totalParcels: 0,
      totalItems: 0,
      matchedSkus: 0,
      unmatchedSkus: 0
    };
    this.activePlatformFilter = 'ALL';
    this.cdr.detectChanges();
  }
}
