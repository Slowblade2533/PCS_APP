import { Component, DestroyRef, inject, OnInit, signal, computed, effect } from '@angular/core';
import Big from 'big.js';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ProductSearchFilter } from '../../../shared/components/product-search-filter/product-search-filter';
import { ProductListItem, ProductSearchParams } from '../../../shared/models/product.models';
import { PagedResult } from '../../../shared/models/pagination.models';
import { ProductService } from '../../../shared/services/product.service';
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
  selector: 'app-products-list',
  imports: [FormsModule, RouterLink, PaginationComponent, ImageHoverPreview, ProductSearchFilter],
  templateUrl: './products-list.html',
})
export class ProductsList implements OnInit {
  private destroyRef = inject(DestroyRef);
  private productService = inject(ProductService);
  private swal = inject(SweetAlertService);

  pageNumber = signal<number>(1);
  pageSize = signal<number>(10);
  searchTerm = signal<string>('');
  selectedCategory = signal<string>('');
  selectedInventoryGroup = signal<string>('');
  selectedStatus = signal<string>('');
  selectedType = signal<string>('');

  productsResource = rxResource<PagedResult<ProductListItem>, ProductSearchParams>({
    params: () => ({
      searchTerm: this.searchTerm(),
      categoryId: this.selectedCategory() ? Number(this.selectedCategory()) : null,
      productStatus: this.selectedStatus(),
      productType: this.selectedType(),
      inventoryGroup: this.selectedInventoryGroup(),
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
    }),
    stream: ({ params }) => {
      const s = params.searchTerm?.trim() || '';
      if (s.length > 0 && s.length < 3) {
        return of({ items: [], totalCount: 0, totalPages: 1, pageNumber: 1, pageSize: 10 });
      }
      return this.productService.getProducts(params);
    },
  });

  products = computed(() => this.productsResource.value()?.items || []);
  totalCount = computed(() => this.productsResource.value()?.totalCount || 0);
  totalPages = computed(() => this.productsResource.value()?.totalPages || 1);
  isLoading = computed(() => this.productsResource.isLoading());

  apiOrigin = environment.apiUrl;

  private readonly searchTrigger$ = new Subject<string>();

  constructor() {
    effect(() => {
      const err = this.productsResource.error();
      if (err) {
        console.error('Error fetching products:', err);
      }
    });
  }

  ngOnInit() {
    this.searchTrigger$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.pageNumber.set(1);
      });
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageNumber.set(page);
    }
  }

  onSearch() {
    this.pageNumber.set(1);
  }

  onSearchChange(value: string) {
    this.searchTrigger$.next(value);
  }

  viewDetails(productId: number, productName: string): void {
    this.productService.getProductById(productId).subscribe({
      next: (product) => {
        if (product && product.variants && product.variants.length > 0) {
          let itemsHtml = `<div class="overflow-x-auto text-sm text-left"><table class="table table-zebra w-full" id="swal-skus-table">
            <thead><tr class="bg-base-200">
              <th class="p-2 w-12 text-center">รูป</th><th class="p-2">SKU</th><th class="p-2">ชื่อตัวเลือก</th><th class="p-2 text-right">สต็อก</th><th class="p-2 text-right">ราคา</th>
            </tr></thead><tbody>`;
          product.variants.forEach((v: any) => {
            const imgUrl = v.imageUrl ? `${this.apiOrigin}${v.imageUrl}` : 'assets/no-image.png';
            const price = v.basePrice !== undefined ? formatNumberWithCommas(v.basePrice) : '0.00';
            const stock = v.currentQuantity !== undefined ? v.currentQuantity : 0;
            const name = v.variantNameTh || '-';
            itemsHtml += `<tr>
              <td class="p-2 text-center">
                <div class="avatar inline-block">
                  <div class="w-10 h-10 rounded bg-base-200 cursor-pointer shadow-sm">
                    <img src="${imgUrl}" data-img-url="${imgUrl}" class="swal-hover-img" alt="Image" onerror="this.src='assets/no-image.png'" />
                  </div>
                </div>
              </td>
              <td class="p-2 whitespace-nowrap">${escapeHtml(v.sku) || '-'}</td>
              <td class="p-2">
                <div class="font-medium text-primary">${escapeHtml(name)}</div>
                <div class="text-xs text-base-content/60">Barcode: ${escapeHtml(v.barcode) || '-'}</div>
              </td>
              <td class="p-2 text-right font-medium">${stock}</td>
              <td class="p-2 text-right font-semibold text-info">${price}</td>
            </tr>`;
          });
          itemsHtml += `</tbody></table></div>`;

          Swal.fire({
            icon: 'info',
            title: `รายการ SKU ของ ${escapeHtml(productName)}`,
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
            },
          });
        } else {
          this.swal.warning('ไม่พบข้อมูลรายการ SKU สำหรับสินค้านี้');
        }
      },
      error: () => {
        this.swal.error('ไม่สามารถดึงข้อมูลรายละเอียดสินค้าได้');
      },
    });
  }
}
