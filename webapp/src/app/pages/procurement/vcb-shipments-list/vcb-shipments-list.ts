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
import { VcbShipment, VcbShipmentItem, VcbShipmentSearch } from '../../../shared/models/vcb-shipments.models';
import { VcbDelivery, VcbDeliveryItem, VcbDeliveryOrder } from '../../../shared/models/vcb-deliveries.models';
import { PagedResult } from '../../../shared/models/pagination.models';
import { VcbShipmentsService } from '../../../shared/services/vcb-shipments.service';
import { VcbDeliveriesService } from '../../../shared/services/vcb-deliveries.service';
import { SweetAlertService, escapeHtml } from '../../../shared/services/sweet-alert.service';
import { AuthService } from '../../../shared/services/auth.service';

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
  selector: 'app-vcb-shipments-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, PaginationComponent],
  templateUrl: './vcb-shipments-list.html',
})
export class VcbShipmentsListComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly vcbShipmentsService = inject(VcbShipmentsService);
  private readonly vcbDeliveriesService = inject(VcbDeliveriesService);
  private readonly swal = inject(SweetAlertService);
  private readonly authService = inject(AuthService);

  page = signal(1);
  pageSize = signal(10);
  searchTerm = signal('');
  status = signal('');

  shipmentsResource = rxResource<PagedResult<VcbShipment>, VcbShipmentSearch>({
    params: () => ({
      page: this.page(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      status: this.status(),
    }),
    stream: ({ params }) => this.vcbShipmentsService.getVcbShipments(params),
  });

  shipments = computed(() => this.shipmentsResource.value()?.items || []);
  totalCount = computed(() => this.shipmentsResource.value()?.totalCount || 0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  isLoading = computed(() => this.shipmentsResource.isLoading());

  isSuperuser(): boolean {
    return this.authService.isSuperuser();
  }

  filterForm = new FormGroup({
    searchTerm: new FormControl(''),
    status: new FormControl(''),
  });

  ngOnInit(): void {
    this.setupFilters();
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

  onPageChange(page: number): void {
    this.page.set(page);
  }

  viewDetails(shipmentId: number, billNo: string): void {
    const apiOrigin = environment.apiUrl.replace('/api', '');
    this.vcbShipmentsService.getVcbShipmentById(shipmentId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      const shipment = res.value ?? res.data;
      if (shipment && shipment.items && shipment.items.length > 0) {
        this.vcbDeliveriesService.getVcbDeliveryById(shipment.deliveryId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((deliveryRes) => {
          const delivery = deliveryRes.value ?? deliveryRes.data;

          // VCB Orders Nos
          const orderNos = (delivery?.orders || []).map((o: VcbDeliveryOrder) => o.orderNo).filter((no): no is string => !!no);
          const orderNosHtml =
            orderNos.length > 0
              ? orderNos
                  .map(
                    (no: string) =>
                      `<span class="inline-flex items-center rounded bg-orange-400 px-1.5 py-0.5 text-[11px] font-bold text-neutral-950 border border-orange-500/20 shadow-sm">${escapeHtml(no as string)}</span>`,
                  )
                  .join(' ')
              : '<span class="text-xs text-base-content/50">-</span>';

          // Shipping Logo
          const shippingLogo = this.getShippingLogo(delivery?.domesticShippingCompany);
          const shippingLogoHtml = shippingLogo
            ? `<img src="${shippingLogo}" alt="Logo" class="w-6 h-6 rounded-full object-cover border border-base-200" />`
            : '';

          // Format Date Helpers
          const formatDate = (dateStr: string | Date | undefined | null) => {
            if (!dateStr) return '-';
            const d = new Date(dateStr);
            if (isNaN(d.getTime())) return '-';
            const day = String(d.getDate()).padStart(2, '0');
            const month = String(d.getMonth() + 1).padStart(2, '0');
            const year = d.getFullYear();
            const hours = String(d.getHours()).padStart(2, '0');
            const minutes = String(d.getMinutes()).padStart(2, '0');
            return `${day}/${month}/${year} ${hours}:${minutes}`;
          };

          // Build general info header card
          let headerHtml = `
            <div class="mb-4 p-3 bg-base-200/50 dark:bg-base-800/30 rounded-lg text-sm grid grid-cols-1 md:grid-cols-2 gap-3 text-left border border-base-200">
              <div>
                <span class="font-bold text-base-content/60 block text-xs mb-1">เลขใบสั่งซื้อ (VCB Orders)</span>
                <div class="flex flex-wrap gap-1">
                  ${orderNosHtml}
                </div>
              </div>
              <div>
                <span class="font-bold text-base-content/60 block text-xs mb-1">บริษัทขนส่ง</span>
                <div class="flex items-center gap-1.5 mt-1">
                  ${shippingLogoHtml}
                  <span class="font-medium text-base-content">${escapeHtml(delivery?.domesticShippingCompany || '') || '-'}</span>
                </div>
              </div>
              <div>
                <span class="font-bold text-base-content/60 block text-xs mb-1">วันที่ส่งสินค้า</span>
                <span class="font-medium text-base-content">${formatDate(delivery?.orderDate)}</span>
              </div>
              <div>
                <span class="font-bold text-base-content/60 block text-xs mb-1">วันที่ได้รับสินค้า</span>
                <span class="font-medium text-base-content">${formatDate(shipment.receiptDate)}</span>
              </div>
              <div>
                <span class="font-bold text-base-content/60 block text-xs mb-1">ผู้บันทึก</span>
                <span class="font-medium text-base-content">${escapeHtml(shipment.createdByUsername || '') || '-'} ${shipment.createdAt ? `<span class="text-xs text-base-content/50">(${formatDate(shipment.createdAt)})</span>` : ''}</span>
              </div>
              <div>
                <span class="font-bold text-base-content/60 block text-xs mb-1">ผู้แก้ไขล่าสุด</span>
                <span class="font-medium text-base-content">${escapeHtml(shipment.updatedByUsername || '') || '-'} ${shipment.updatedByUsername && shipment.updatedAt ? `<span class="text-xs text-base-content/50">(${formatDate(shipment.updatedAt)})</span>` : ''}</span>
              </div>
            </div>
          `;

          // Build boxes details html
          let boxesHtml = '';
          if (delivery?.items && delivery.items.length > 0) {
            delivery.items.forEach((boxItem: VcbDeliveryItem) => {
              boxesHtml += `
                <tr class="font-medium">
                  <td class="p-2 text-primary font-bold">${escapeHtml(boxItem.packageBoxNo)}</td>
                  <td class="p-2">${escapeHtml(boxItem.domesticTrackingNo || '') || '-'}</td>
                  <td class="p-2 text-right">${escapeHtml(boxItem.boxDimensions || '') || '-'}</td>
                  <td class="p-2 text-right">${formatNumberWithCommas(boxItem.totalWeight)} kg</td>
                </tr>
              `;
              if (boxItem.containedBoxNumbers) {
                try {
                  const subBoxes = JSON.parse(boxItem.containedBoxNumbers);
                  if (Array.isArray(subBoxes) && subBoxes.length > 0) {
                    boxesHtml += `
                      <tr>
                        <td colspan="4" class="p-0 border-b border-base-200">
                          <div class="bg-base-100/70 p-2 pl-8 border-l-2 border-primary/30">
                            <h5 class="text-xs font-bold text-base-content/50 mb-1">กล่องย่อยภายใน (CX) ของชุด ${escapeHtml(boxItem.packageBoxNo)}</h5>
                            <table class="table table-xs table-zebra w-full max-w-lg border border-base-200 bg-base-100">
                              <thead>
                                <tr class="bg-base-200/30 text-xs">
                                  <th>รหัสกล่องย่อย</th>
                                  <th>ขนาด (กยส)</th>
                                  <th class="text-right">น้ำหนัก (kg)</th>
                                </tr>
                              </thead>
                              <tbody>
                    `;
                    subBoxes.forEach(
                      (sub: { boxNo: string; dimensions?: string; weight?: number | string }) => {
                        boxesHtml += `
                                <tr>
                                  <td class="font-semibold text-secondary">${escapeHtml(sub.boxNo)}</td>
                                  <td>${escapeHtml(sub.dimensions || '') || '-'}</td>
                                  <td class="text-right font-medium">${formatNumberWithCommas(sub.weight)} kg</td>
                                </tr>
                      `;
                      },
                    );
                    boxesHtml += `
                              </tbody>
                            </table>
                          </div>
                        </td>
                      </tr>
                    `;
                  }
                } catch (e) {
                  console.error('Failed to parse sub boxes', e);
                }
              }
            });
          } else {
            boxesHtml = `<tr><td colspan="4" class="text-center p-4 text-base-content/50">ไม่มีข้อมูลกล่องพัสดุ</td></tr>`;
          }

          let boxesSectionHtml = `
            <div class="mb-4">
              <h4 class="font-bold text-sm text-base-content/80 mb-2">📦 รายการกล่องพัสดุในไทย (Package Boxes)</h4>
              <div class="overflow-x-auto border border-base-200 rounded-lg bg-base-50/50">
                <table class="table table-xs table-zebra w-full text-left">
                  <thead>
                    <tr class="bg-base-200/50">
                      <th class="p-2">เลขที่กล่องชุด (TX)</th>
                      <th class="p-2">Tracking (ในไทย)</th>
                      <th class="p-2 text-right">ขนาดกล่องชุด (กxยxส cm)</th>
                      <th class="p-2 text-right">น้ำหนักชุด (kg)</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${boxesHtml}
                  </tbody>
                </table>
              </div>
            </div>
          `;

          let itemsHtml =
            headerHtml +
            boxesSectionHtml +
            `<h4 class="font-bold text-sm text-base-content/80 mb-2">🛍️ รายการสินค้าที่ได้รับ (Received Products)</h4>` +
            `<div class="overflow-x-auto text-sm text-left"><table class="table table-zebra w-full text-left">
            <thead><tr class="bg-base-200">
              <th class="p-2 w-16 text-center">ภาพ</th>
              <th class="p-2">SKU</th>
              <th class="p-2">ชื่อสินค้า/ตัวเลือก</th>
              <th class="p-2">หมายเลขกล่อง</th>
              <th class="p-2 text-center">สถานะ</th>
              <th class="p-2 text-right">จำนวน(ดี/เสีย)</th>
              <th class="p-2 text-right">คืนเงิน</th>
            </tr></thead><tbody>`;

          shipment.items.forEach((item: VcbShipmentItem) => {
            let statusText =
              item.receiptStatus === 'Complete'
                ? '<span class="text-success font-medium">รับครบ</span>'
                : item.receiptStatus === 'Incomplete'
                  ? '<span class="text-warning font-medium">รับไม่ครบ</span>'
                  : item.receiptStatus === 'Over'
                    ? '<span class="text-info font-medium">รับเกิน</span>'
                    : item.receiptStatus === 'WrongItem'
                      ? '<span class="text-error font-medium">ผิดรุ่น/ผิดสี</span>'
                      : item.receiptStatus;

            let imgSrc = item.imageUrl
              ? `${apiOrigin}${item.imageUrl}`
              : 'assets/images/no-image.png';

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
              <td class="p-2">${escapeHtml(item.boxNumbers || '') || '-'}</td>
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
                img.addEventListener('mouseenter', (e: Event) => {
                  const src = (e.target as HTMLElement).getAttribute('data-img-url');
                  previewDiv!.innerHTML = `<img src="${src}" class="max-w-[500px] max-h-[500px] object-contain rounded-md" />`;
                  previewDiv!.style.display = 'block';
                  updatePos(e as MouseEvent);
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
        });
      } else {
        this.swal.warning('ไม่พบข้อมูลรายการพัสดุในบิลนี้');
      }
    });
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
}
