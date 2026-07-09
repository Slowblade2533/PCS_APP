import { CommonModule } from '@angular/common';
import Big from 'big.js';
import { Component, DestroyRef, OnInit, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { debounceTime } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environment';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { VcbDelivery, VcbDeliveryItem, VcbDeliveryOrder, VcbDeliverySearch } from '../../../shared/models/vcb-deliveries.models';
import { PagedResult } from '../../../shared/models/pagination.models';
import { VcbDeliveriesService } from '../../../shared/services/vcb-deliveries.service';
import { SweetAlertService, escapeHtml } from '../../../shared/services/sweet-alert.service';

import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';

function formatNumberWithCommas(value: number | string | null | undefined): string {
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
  selector: 'app-vcb-deliveries-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    PaginationComponent,
    ImageHoverPreview,
  ],
  templateUrl: './vcb-deliveries-list.html',
})
export class VcbDeliveriesList implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly vcbDeliveriesService = inject(VcbDeliveriesService);
  private readonly swal = inject(SweetAlertService);

  apiOrigin = environment.apiUrl.replace('/api', '');

  page = signal(1);
  pageSize = signal(10);
  searchTerm = signal('');
  status = signal('');

  deliveriesResource = rxResource<PagedResult<VcbDelivery>, VcbDeliverySearch>({
    params: () => ({
      page: this.page(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      status: this.status(),
    }),
    stream: ({ params }) => this.vcbDeliveriesService.getVcbDeliveries(params),
  });

  deliveries = computed(() => this.deliveriesResource.value()?.items || []);
  totalCount = computed(() => this.deliveriesResource.value()?.totalCount || 0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  isLoading = computed(() => this.deliveriesResource.isLoading());

  filterForm = new FormGroup({
    searchTerm: new FormControl(''),
    status: new FormControl(''),
  });

  ngOnInit(): void {
    this.setupFilters();
  }

  private setupFilters(): void {
    this.filterForm
      .get('searchTerm')
      ?.valueChanges.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchTerm.set(term || '');
        this.page.set(1);
      });

    this.filterForm
      .get('status')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => {
        this.status.set(status || '');
        this.page.set(1);
      });
  }

  onPageChange(page: number): void {
    this.page.set(page);
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

  getSplitOrderNumbers(orderNumbers: string | undefined): string[] {
    if (!orderNumbers) return [];
    return orderNumbers
      .split(',')
      .map((s) => s.trim())
      .filter((s) => !!s);
  }

  getShippingLogo(company: string | undefined): string | null {
    if (!company) return null;
    const cmp = company.trim().toLowerCase();
    if (cmp.includes('sixth party')) {
      return '/logistics/sixth_party.jpg';
    } else if (cmp.includes('nim express') || cmp.includes('นิ่มซี่เส็ง')) {
      return '/logistics/nim_express.jpg';
    } else if (cmp.includes('thaipost') || cmp.includes('ไปรษณีย์ไทย')) {
      return '/logistics/thaipost_ems.jpg';
    } else if (
      cmp.includes('blue & white') ||
      cmp.includes('blue &amp; white') ||
      cmp.includes('blue and white') ||
      cmp.includes('blue & white logistic')
    ) {
      return '/logistics/blue_n_white.jpg';
    } else if (cmp.includes('dhl')) {
      return '/logistics/dhl.jpg';
    }
    return null;
  }

  viewDetails(deliveryId: number, deliveryNo: string): void {
    this.vcbDeliveriesService.getVcbDeliveryById(deliveryId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      const delivery = res.value ?? res.data;
      if (delivery && delivery.items && delivery.items.length > 0) {
        const orderNos = (delivery.orders || [])
          .map((o: VcbDeliveryOrder) => o.orderNo)
          .filter((no): no is string => !!no);
        const shippingLogo = this.getShippingLogo(delivery.domesticShippingCompany);
        const slipUrl = delivery.transferSlipUrl
          ? `${this.apiOrigin}${delivery.transferSlipUrl}`
          : null;

        let headerHtml = `
          <div class="mb-4 p-3 bg-base-200/50 dark:bg-base-800/30 rounded-lg text-sm grid grid-cols-1 md:grid-cols-3 gap-3 text-left border border-base-200">
            <div>
              <span class="font-bold text-base-content/60 block text-xs mb-1">เลขใบสั่งซื้อ (VCB Orders)</span>
              <div class="flex flex-wrap gap-1">
                ${
                  orderNos.length > 0
                    ? orderNos
                        .map(
                          (no: string) =>
                            `<span class="inline-flex items-center rounded bg-orange-400 px-1.5 py-0.5 text-[11px] font-bold text-neutral-950 border border-orange-500/20 shadow-sm">${escapeHtml(no)}</span>`,
                        )
                        .join('')
                    : '<span class="text-xs text-base-content/50">-</span>'
                }
              </div>
            </div>
            <div>
              <span class="font-bold text-base-content/60 block text-xs mb-1">บริษัทขนส่ง</span>
              <div class="flex items-center gap-2 mt-1">
                ${
                  shippingLogo
                    ? `<img src="${shippingLogo}" alt="Logo" class="w-8 h-8 rounded-full object-cover border border-base-200" />`
                    : ''
                }
                <span class="font-medium text-base-content">${escapeHtml(delivery.domesticShippingCompany) || '-'}</span>
              </div>
            </div>
            <div>
              <span class="font-bold text-base-content/60 block text-xs mb-1">หลักฐานการโอนเงิน (สลิป)</span>
              ${
                slipUrl
                  ? `<div class="w-12 h-12 rounded bg-base-200 cursor-pointer shadow-sm mt-1 overflow-hidden border border-base-200 inline-block">
                     <img src="${slipUrl}" data-img-url="${slipUrl}" class="swal-hover-img w-full h-full object-cover" alt="Slip" onerror="this.src='assets/no-image.png'" />
                   </div>`
                  : '<span class="text-xs text-base-content/50 mt-1 block">ยังไม่ได้อัปโหลดสลิป</span>'
              }
            </div>
            <div>
              <span class="font-bold text-base-content/60 block text-xs mb-1">ผู้บันทึก</span>
              <span class="font-medium text-base-content text-xs">${escapeHtml(delivery.createdByUsername || '') || '-'} ${delivery.createdAt ? `<span class="opacity-50">(${new Date(delivery.createdAt).toLocaleString('th-TH')})</span>` : ''}</span>
            </div>
            <div>
              <span class="font-bold text-base-content/60 block text-xs mb-1">ผู้แก้ไขล่าสุด</span>
              <span class="font-medium text-base-content text-xs">${escapeHtml(delivery.updatedByUsername || '') || '-'} ${delivery.updatedByUsername && delivery.updatedAt ? `<span class="opacity-50">(${new Date(delivery.updatedAt).toLocaleString('th-TH')})</span>` : ''}</span>
            </div>
            <div></div>
          </div>
        `;

        let itemsHtml =
          headerHtml +
          `<div class="overflow-x-auto text-sm text-left"><table class="table table-zebra w-full">
          <thead><tr class="bg-base-200">
            <th class="p-2">เลขที่กล่อง</th><th class="p-2">Tracking (ในไทย)</th><th class="p-2 text-right">น้ำหนักรวม (kg)</th><th class="p-2">ขนาด</th><th class="p-2 text-right">ค่าจัดส่ง</th>
          </tr></thead><tbody>`;
        delivery.items.forEach((item: VcbDeliveryItem) => {
          itemsHtml += `<tr class="bg-base-100 font-medium">
            <td class="p-2 whitespace-nowrap">${escapeHtml(item.packageBoxNo)}</td>
            <td class="p-2">${escapeHtml(item.domesticTrackingNo || '') || '-'}</td>
            <td class="p-2 text-right">${formatNumberWithCommas(item.totalWeight)}</td>
            <td class="p-2 text-sm">${escapeHtml(item.boxDimensions || '') || '-'}</td>
            <td class="p-2 text-right text-info">${formatNumberWithCommas(item.shippingCost)}</td>
          </tr>`;

          if (item.containedBoxNumbers) {
            try {
              const subBoxes = JSON.parse(item.containedBoxNumbers);
              if (Array.isArray(subBoxes) && subBoxes.length > 0) {
                itemsHtml += `<tr><td colspan="5" class="p-0 border-b border-base-200"><div class="bg-base-50 p-3 pl-8">
                  <h5 class="text-xs font-bold text-base-content/60 mb-2">กล่องย่อยภายใน (${escapeHtml(item.packageBoxNo)})</h5>
                  <table class="table table-xs table-zebra w-full max-w-xl border border-base-200 bg-base-100">
                    <thead><tr class="bg-base-200/50">
                      <th>รหัสกล่องย่อย</th><th>ขนาด (กยส)</th><th class="text-right">น้ำหนัก (kg)</th>
                    </tr></thead><tbody>`;
                subBoxes.forEach(
                  (sub: { boxNo: string; dimensions?: string; weight?: number | string }) => {
                    itemsHtml += `<tr>
                    <td>${escapeHtml(sub.boxNo)}</td>
                    <td>${escapeHtml(sub.dimensions || '') || '-'}</td>
                    <td class="text-right">${sub.weight ? formatNumberWithCommas(sub.weight) : '-'}</td>
                  </tr>`;
                  },
                );
                itemsHtml += `</tbody></table></div></td></tr>`;
              }
            } catch (e) {
              // Not valid JSON or string
            }
          }
        });
        itemsHtml += `</tbody></table></div>`;

        Swal.fire({
          icon: 'info',
          title: `รายละเอียดใบส่งสินค้า ${escapeHtml(deliveryNo)}`,
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
              const el = img as HTMLImageElement;
              const url = el.getAttribute('data-img-url');
              if (!url) return;

              el.addEventListener('mouseenter', (e) => {
                if (!previewDiv) return;
                previewDiv.innerHTML = `<img src="${url}" class="max-w-[480px] max-h-[480px] object-contain rounded" alt="Preview" />`;
                previewDiv.classList.remove('hidden');
                updatePos(e);
              });

              el.addEventListener('mousemove', (e) => {
                updatePos(e);
              });

              el.addEventListener('mouseleave', () => {
                if (!previewDiv) return;
                previewDiv.classList.add('hidden');
                previewDiv.innerHTML = '';
              });
            });
          },
          willClose: () => {
            const previewDiv = document.getElementById('swal-img-preview-overlay');
            if (previewDiv) {
              previewDiv.classList.add('hidden');
              previewDiv.innerHTML = '';
            }
          },
        });
      } else {
        this.swal.warning('ไม่พบข้อมูลรายการกล่องในใบส่งสินค้านี้');
      }
    });
  }
}
