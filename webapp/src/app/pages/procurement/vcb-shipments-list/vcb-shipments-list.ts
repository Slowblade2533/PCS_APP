import { CommonModule } from '@angular/common';
import Big from 'big.js';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { debounceTime } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environment';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { VcbShipment, VcbShipmentSearch } from '../../../shared/models/procurement.models';
import { ProcurementService } from '../../../shared/services/procurement.service';
import { SweetAlertService, escapeHtml } from '../../../shared/services/sweet-alert.service';

function formatNumberWithCommas(value: any): string {
  if (value === null || value === undefined || value === '') return '0.00';
  const num = typeof value === 'number' ? value : parseFloat(value);
  return isNaN(num)
    ? '0.00'
    : new Intl.NumberFormat('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(num);
}

@Component({
  selector: 'app-vcb-shipments-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, PaginationComponent],
  templateUrl: './vcb-shipments-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VcbShipmentsListComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly procurementService = inject(ProcurementService);
  private readonly swal = inject(SweetAlertService);

  isLoading = false;
  searchParams: VcbShipmentSearch = {
    page: 1,
    pageSize: 10,
    searchTerm: '',
    status: '',
  };
  shipments: VcbShipment[] = [];
  totalCount = 0;
  totalPages = 0;

  filterForm = new FormGroup({
    searchTerm: new FormControl(''),
    status: new FormControl(''),
  });

  ngOnInit(): void {
    this.setupFilters();
    this.loadShipments();
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Draft':
        return 'badge-warning';
      case 'Completed':
        return 'badge-success';
      case 'Cancelled':
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'Draft':
        return 'ร่าง/รอตรวจสอบ';
      case 'Completed':
        return 'รับสินค้าแล้ว';
      case 'Cancelled':
        return 'ยกเลิก';
      default:
        return status;
    }
  }

  loadShipments(): void {
    this.isLoading = true;
    this.procurementService.getVcbShipments(this.searchParams).subscribe({
      next: (res) => {
        this.shipments = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.totalPages = Math.ceil(this.totalCount / this.searchParams.pageSize);
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  onPageChange(page: number): void {
    this.searchParams.page = page;
    this.loadShipments();
  }

  viewDetails(shipmentId: number, billNo: string): void {
    const apiOrigin = environment.apiUrl.replace('/api', '');
    this.procurementService.getVcbShipmentById(shipmentId).subscribe((res: any) => {
      const shipment = res.value || res.data || res;
      if (shipment && shipment.items && shipment.items.length > 0) {
        let itemsHtml = `<div class="overflow-x-auto text-sm text-left"><table class="table table-zebra w-full">
          <thead><tr class="bg-base-200">
            <th class="p-2 w-16 text-center">ภาพ</th>
            <th class="p-2">SKU</th>
            <th class="p-2">ชื่อสินค้า/ตัวเลือก</th>
            <th class="p-2">หมายเลขกล่อง</th>
            <th class="p-2 text-center">สถานะ</th>
            <th class="p-2 text-right">จำนวน(ดี/เสีย)</th>
            <th class="p-2 text-right">คืนเงิน</th>
          </tr></thead><tbody>`;
        shipment.items.forEach((item: any) => {
          let statusText =
            item.receiptStatus === 'Complete'
              ? '<span class="text-success">รับครบ</span>'
              : item.receiptStatus === 'Incomplete'
                ? '<span class="text-warning">รับไม่ครบ</span>'
                : item.receiptStatus === 'Over'
                  ? '<span class="text-info">รับเกิน</span>'
                  : item.receiptStatus === 'WrongItem'
                    ? '<span class="text-error">ผิดรุ่น/ผิดสี</span>'
                    : item.receiptStatus;
          
          let imgSrc = item.imageUrl ? `${apiOrigin}${item.imageUrl}` : 'assets/images/no-image.png';

          itemsHtml += `<tr>
            <td class="p-2 text-center">
              <div class="avatar inline-block">
                <div class="w-12 h-12 rounded bg-base-200 cursor-pointer shadow-sm">
                  <img src="${imgSrc}" data-img-url="${imgSrc}" class="swal-hover-img" alt="Product Image" onerror="this.src='assets/images/no-image.png'" />
                </div>
              </div>
            </td>
            <td class="p-2 whitespace-nowrap">${escapeHtml(item.sku) || '-'}</td>
            <td class="p-2">
              <div class="font-medium text-primary">${escapeHtml(item.productName) || '-'}</div>
              <div class="text-xs text-base-content/60">${escapeHtml(item.variantName) || '-'}</div>
            </td>
            <td class="p-2">${escapeHtml(item.boxNumbers) || '-'}</td>
            <td class="p-2 text-center">${statusText}</td>
            <td class="p-2 text-right">
              <span class="text-success font-medium">${item.goodQuantity}</span> / 
              <span class="text-error font-medium">${item.defectiveQuantity}</span>
            </td>
            <td class="p-2 text-right font-semibold text-warning">${item.refundAmount > 0 ? formatNumberWithCommas(item.refundAmount) : '-'}</td>
          </tr>`;
        });
        itemsHtml += `</tbody></table></div>`;

        Swal.fire({
          icon: 'info',
          title: `รายละเอียดใบรับสินค้า ${escapeHtml(billNo)}<br><span class="text-base font-normal text-base-content/70">อ้างอิงบิลขนส่ง: ${escapeHtml(shipment.deliveryNo || String(shipment.deliveryId))}</span>`,
          html: itemsHtml,
          showConfirmButton: true,
          confirmButtonText: 'ปิด',
          confirmButtonColor: '#3085d6',
          width: '800px',
          didOpen: () => {
            const imgs = document.querySelectorAll('.swal-hover-img');
            let previewDiv = document.getElementById('swal-img-preview-overlay');
            if (!previewDiv) {
              previewDiv = document.createElement('div');
              previewDiv.id = 'swal-img-preview-overlay';
              previewDiv.className =
                'fixed z-[9999] pointer-events-none bg-base-100 p-2 rounded-lg shadow-2xl border border-base-200 transition-none hidden';
              document.body.appendChild(previewDiv);
            }

            const updatePos = (e: MouseEvent) => {
              if (!previewDiv) return;
              const x = e.clientX;
              const y = e.clientY;
              const w = window.innerWidth;
              const h = window.innerHeight;
              let left = x + 20;
              let top = y - 250;
              if (top < 10) top = 10;
              else if (top + 500 > h - 10) top = h - 510;
              if (left + 500 > w - 10) left = x - 520;
              previewDiv.style.left = `${left}px`;
              previewDiv.style.top = `${top}px`;
            };

            imgs.forEach((img) => {
              img.addEventListener('mouseenter', (e: any) => {
                const src = e.target.getAttribute('data-img-url');
                previewDiv!.innerHTML = `<img src="${src}" class="max-w-[500px] max-h-[500px] object-contain rounded-md" />`;
                previewDiv!.style.display = 'block';
                updatePos(e);
              });
              img.addEventListener('mousemove', updatePos as EventListener);
              img.addEventListener('mouseleave', () => {
                previewDiv!.style.display = 'none';
              });
            });
          },
          willClose: () => {
            const previewDiv = document.getElementById('swal-img-preview-overlay');
            if (previewDiv) {
              previewDiv.remove();
            }
          }
        });
      } else {
        this.swal.warning('ไม่พบข้อมูลรายการพัสดุในบิลนี้');
      }
    });
  }

  private setupFilters(): void {
    // Handle Search Term with Debounce
    this.filterForm
      .get('searchTerm')
      ?.valueChanges.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchParams.searchTerm = term || '';
        this.searchParams.page = 1;
        this.loadShipments();
      });

    // Handle Status Change immediately
    this.filterForm
      .get('status')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => {
        this.searchParams.status = status || '';
        this.searchParams.page = 1;
        this.loadShipments();
      });
  }
}
