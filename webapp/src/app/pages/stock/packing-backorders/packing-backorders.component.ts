import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PackingService, ParsedItem } from '../../../services/packing.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-packing-backorders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './packing-backorders.component.html',
  styleUrl: './packing-backorders.component.css'
})
export class PackingBackordersComponent implements OnInit {
  backorders: any[] = [];
  filteredBackorders: any[] = [];
  searchTerm = '';
  isLoading = false;

  showResolveModal = false;
  selectedItem: any = null;

  resolutionType: 'EXACT_FULFILLMENT' | 'SUBSTITUTION' | 'REFUND_CANCEL' = 'EXACT_FULFILLMENT';
  substituteSku = '';
  substituteVariantId: number | null = null;
  shippedQuantity = 1;
  customerNote = '';
  followUpTrackingNo = '';
  isResolving = false;

  constructor(private packingService: PackingService) {}

  ngOnInit(): void {
    this.loadBackorders();
  }

  loadBackorders(): void {
    this.isLoading = true;
    this.packingService.getPendingBackorders().subscribe({
      next: (res) => {
        this.backorders = res || [];
        this.applyFilter();
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Error loading backorders', err);
      }
    });
  }

  applyFilter(): void {
    const term = this.searchTerm.toLowerCase().trim();
    if (!term) {
      this.filteredBackorders = [...this.backorders];
      return;
    }

    this.filteredBackorders = this.backorders.filter(item => 
      (item.orderNo && item.orderNo.toLowerCase().includes(term)) ||
      (item.trackingNo && item.trackingNo.toLowerCase().includes(term)) ||
      (item.sku && item.sku.toLowerCase().includes(term)) ||
      (item.productName && item.productName.toLowerCase().includes(term))
    );
  }

  openResolveModal(item: any): void {
    this.selectedItem = item;
    this.resolutionType = 'EXACT_FULFILLMENT';
    this.substituteSku = '';
    this.substituteVariantId = null;
    this.shippedQuantity = item.pendingQuantity || (item.orderedQuantity - item.shippedQuantity);
    this.customerNote = '';
    this.followUpTrackingNo = '';
    this.showResolveModal = true;
  }

  closeResolveModal(): void {
    this.showResolveModal = false;
    this.selectedItem = null;
  }

  submitResolution(): void {
    if (!this.selectedItem) return;

    if (this.resolutionType === 'SUBSTITUTION' && !this.substituteSku) {
      Swal.fire('ข้อผิดพลาด', 'กรุณาระบุ SKU สินค้าที่ส่งทดแทน', 'warning');
      return;
    }

    this.isResolving = true;

    const payload = {
      batchItemId: this.selectedItem.id,
      actualShippedVariantId: this.substituteVariantId || this.selectedItem.variantId || 0,
      shippedQuantity: this.shippedQuantity,
      resolutionType: this.resolutionType,
      customerAgreementNote: this.customerNote,
      followUpTrackingNo: this.followUpTrackingNo
    };

    this.packingService.resolveBackorder(payload).subscribe({
      next: () => {
        this.isResolving = false;
        Swal.fire('สำเร็จ', 'บันทึกการจัดส่งสินค้าทดแทน/ตามหลังและตัดสต๊อกเรียบร้อย', 'success');
        this.closeResolveModal();
        this.loadBackorders();
      },
      error: (err) => {
        this.isResolving = false;
        Swal.fire('ข้อผิดพลาด', 'ไม่สามารถบันทึกรายการได้: ' + (err?.error?.message || err?.message || ''), 'error');
      }
    });
  }
}
