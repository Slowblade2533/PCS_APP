import { CommonModule } from '@angular/common';
import Big from 'big.js';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { debounceTime } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { VcbOrder, VcbOrderSearch } from '../../../shared/models/procurement.models';
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
  selector: 'app-vcb-orders-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, PaginationComponent, ImageHoverPreview],
  templateUrl: './vcb-orders-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VcbOrdersListComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly procurementService = inject(ProcurementService);
  private readonly swal = inject(SweetAlertService);

  orders: VcbOrder[] = [];
  apiOrigin = environment.apiUrl.replace('/api', '');
  searchParams: VcbOrderSearch = {
    page: 1,
    pageSize: 10,
    searchTerm: '',
    status: '',
  };
  totalCount = 0;
  totalPages = 0;
  isLoading = false;

  filterForm = new FormGroup({
    searchTerm: new FormControl(''),
    status: new FormControl(''),
  });

  ngOnInit(): void {
    this.setupFilters();
    this.loadOrders();
  }

  private setupFilters(): void {
    // Handle Search Term with Debounce
    this.filterForm
      .get('searchTerm')
      ?.valueChanges.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchParams.searchTerm = term || '';
        this.searchParams.page = 1;
        this.loadOrders();
      });

    // Handle Status Change immediately
    this.filterForm
      .get('status')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => {
        this.searchParams.status = status || '';
        this.searchParams.page = 1;
        this.loadOrders();
      });
  }

  loadOrders(): void {
    this.isLoading = true;
    this.procurementService.getVcbOrders(this.searchParams).subscribe({
      next: (res) => {
        this.orders = res.items || [];
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
    this.loadOrders();
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'badge-warning';
      case 'Shipping':
        return 'badge-info';
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
      case 'Pending':
        return 'รอดำเนินการ';
      case 'Shipping':
        return 'กำลังจัดส่ง';
      case 'Completed':
        return 'เสร็จสิ้น';
      case 'Cancelled':
        return 'ยกเลิก';
      default:
        return status;
    }
  }

  viewDetails(orderId: number, orderNo: string): void {
    const apiOrigin = environment.apiUrl.replace('/api', '');
    this.procurementService.getVcbOrderById(orderId).subscribe((res) => {
      const order = res.value || res.data;
      if (order && order.items && order.items.length > 0) {
        let slipSectionHtml = '';
        if (order.transferSlipUrl) {
          const slipUrl = `${apiOrigin}${order.transferSlipUrl}`;
          slipSectionHtml = `
            <div class="mb-4 flex flex-col md:flex-row gap-4 items-start justify-between bg-base-50 p-4 rounded-lg border border-base-200">
              <div class="text-left flex-1">
                <div class="mb-1"><strong>วันที่สั่งซื้อ:</strong> ${new Date(order.orderDate).toLocaleString('th-TH')}</div>
                <div class="mb-1"><strong>สาขาที่รับเข้า:</strong> ${order.branchId === 1 ? 'สาขาหลัก (Main Branch)' : order.branchId}</div>
                <div><strong>หมายเหตุ:</strong> ${escapeHtml(order.notes || '') || '-'}</div>
              </div>
              <div class="flex flex-col items-center gap-1 shrink-0 bg-base-100 p-2 rounded-lg border border-base-200">
                <span class="text-[11px] font-bold opacity-70">สลิปโอนเงิน (ชี้เพื่อขยายรูป)</span>
                <div class="w-24 h-24 rounded bg-base-200 cursor-pointer shadow-sm overflow-hidden flex items-center justify-center">
                  <img src="${slipUrl}" data-img-url="${slipUrl}" class="swal-hover-img max-h-full max-w-full object-contain" alt="Slip" onerror="this.src='assets/no-image.png'" />
                </div>
              </div>
            </div>
          `;
        } else {
          slipSectionHtml = `
            <div class="mb-4 text-left bg-base-50 p-4 rounded-lg border border-base-200">
              <div class="mb-1"><strong>วันที่สั่งซื้อ:</strong> ${new Date(order.orderDate).toLocaleString('th-TH')}</div>
              <div class="mb-1"><strong>สาขาที่รับเข้า:</strong> ${order.branchId === 1 ? 'สาขาหลัก (Main Branch)' : order.branchId}</div>
              <div><strong>หมายเหตุ:</strong> ${escapeHtml(order.notes || '') || '-'}</div>
            </div>
          `;
        }

        let itemsHtml = slipSectionHtml + `<div class="overflow-x-auto text-sm text-left"><table class="table table-zebra w-full">
          <thead><tr class="bg-base-200">
            <th class="p-2 w-12 text-center">รูป</th><th class="p-2">SKU</th><th class="p-2">ชื่อสินค้า</th><th class="p-2 text-right">จำนวน</th><th class="p-2 text-right">ราคา/ชิ้น</th><th class="p-2 text-right">รวม</th>
          </tr></thead><tbody>`;
        order.items.forEach((item) => {
          const pricePerUnit =
            item.quantity > 0 ? formatNumberWithCommas(new Big(item.totalPrice).div(item.quantity).toNumber()) : '0.00';
          const imgUrl = item.imageUrl ? `${apiOrigin}${item.imageUrl}` : 'assets/no-image.png';
          itemsHtml += `<tr>
            <td class="p-2 text-center">
              <div class="avatar inline-block">
                <div class="w-12 h-12 rounded bg-base-200 cursor-pointer shadow-sm">
                  <img src="${imgUrl}" data-img-url="${imgUrl}" class="swal-hover-img" alt="Image" onerror="this.src='assets/no-image.png'" />
                </div>
              </div>
            </td>
            <td class="p-2 whitespace-nowrap">${escapeHtml(item.sku) || '-'}</td>
            <td class="p-2">
              <div class="font-medium text-primary">${escapeHtml(item.productName) || '-'}</div>
              <div class="text-xs text-base-content/60">${escapeHtml(item.variantName) || '-'}</div>
            </td>
            <td class="p-2 text-right font-medium">${item.quantity}</td>
            <td class="p-2 text-right">${pricePerUnit}</td>
            <td class="p-2 text-right font-semibold text-info">${formatNumberWithCommas(item.totalPrice)}</td>
          </tr>`;
        });
        itemsHtml += `</tbody></table></div>`;

        Swal.fire({
          icon: 'info',
          title: `รายละเอียดออเดอร์ ${escapeHtml(orderNo)}`,
          html: itemsHtml,
          showConfirmButton: true,
          confirmButtonText: 'ปิด',
          confirmButtonColor: '#3085d6',
          width: '900px',
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
          },
        });
      } else {
        this.swal.warning('ไม่พบข้อมูลรายการสินค้าในออเดอร์นี้');
      }
    });
  }
}
