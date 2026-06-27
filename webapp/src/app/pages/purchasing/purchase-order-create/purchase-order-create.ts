import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed, effect } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of, Subject } from 'rxjs';
import { catchError, debounceTime } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { PurchaseOrderCreatePayload, PurchaseOrderDetail, PurchaseOrderItemCreatePayload, PurchaseOrderStatus } from '../../../shared/models/purchase-orders.models';
import { PaymentMethod } from '../../../shared/models/shared.models';
import { PurchaseOrdersService } from '../../../shared/services/purchase-orders.service';
import { ProductService } from '../../../shared/services/product.service';
import { ProductListItem, ProductVariantDetail } from '../../../shared/models/product.models';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { AuthService } from '../../../shared/services/auth.service';

@Component({
  selector: 'app-purchase-order-create',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe, ImageHoverPreview],
  templateUrl: './purchase-order-create.html',
})
export class PurchaseOrderCreate implements OnInit {
  private poService = inject(PurchaseOrdersService);
  private productService = inject(ProductService);
  private swal = inject(SweetAlertService);
  public auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  apiOrigin = environment.apiUrl.replace('/api', '');

  poId = signal<number | null>(null);
  isViewMode = signal<boolean>(false);

  poResource = rxResource({
    params: () => this.poId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.poService.getById(params).pipe(catchError(() => of(null)));
    },
  });

  detail = computed(() => this.poResource.value() || null);
  loading = computed(() => this.poResource.isLoading());

  // Search Products State
  searchTerm = signal('');
  searchSubject = new Subject<string>();
  selectedProduct = signal<ProductListItem | null>(null);

  productsResource = rxResource({
    params: () => this.searchTerm(),
    stream: ({ params }) => {
      if (!params || params.length <= 2) return of({ items: [] });
      return this.productService
        .getProducts({ pageNumber: 1, pageSize: 20, searchTerm: params })
        .pipe(catchError(() => of({ items: [] })));
    },
  });

  productDetailResource = rxResource({
    params: () => this.selectedProduct()?.productId,
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.productService.getProductById(params).pipe(
        catchError(() => {
          this.swal.error('ไม่สามารถดึงข้อมูลสินค้านี้ได้');
          return of(null);
        }),
      );
    },
  });

  isFetchingVariants = computed(() => this.productDetailResource.isLoading());
  isSearching = computed(() => this.productsResource.isLoading());
  searchResults = computed(() => this.productsResource.value()?.items || []);
  selectedProductVariants = computed(() => this.productDetailResource.value()?.variants || []);

  // Form State
  poNo = signal<string>('');
  poDate = signal<string>(new Date().toLocaleDateString('en-CA'));
  supplierName = signal<string>('');
  supplierPhone = signal<string>('');
  supplierTaxId = signal<string>('');
  supplierAddress = signal<string>('');
  paymentMethod = signal<PaymentMethod>('TRANSFER');
  paymentRefNo = signal<string>('');
  sourceAccountInfo = signal<string>('');
  vatRate = signal<number>(7);
  discountTotal = signal<number>(0);
  shippingCost = signal<number>(0);
  expectedDeliveryDate = signal<string>('');
  slipAttachmentUrl = signal<string>('');
  notes = signal<string>('');

  items = signal<(PurchaseOrderItemCreatePayload & { sku?: string; productName?: string; variantName?: string; imageUrl?: string })[]>([]);

  submitting = signal<boolean>(false);
  uploadingSlip = signal<boolean>(false);
  error = signal<string | null>(null);

  statusOptions: { value: PurchaseOrderStatus; label: string }[] = [
    { value: 'DRAFT', label: 'ฉบับร่าง' },
    { value: 'ORDERED', label: 'สั่งซื้อแล้ว' },
    { value: 'PARTIALLY_RECEIVED', label: 'รับสินค้าบางส่วน' },
    { value: 'RECEIVED', label: 'รับสินค้าครบแล้ว' },
    { value: 'CANCELLED', label: 'ยกเลิก' },
  ];

  ngOnInit() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.poId.set(Number(id));
        this.isViewMode.set(true);
      }
    });
    this.setupProductSearch();
  }

  private setupProductSearch(): void {
    this.searchSubject
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchTerm.set(term);
        if (!term || term.length <= 2) {
          this.selectedProduct.set(null);
        }
      });
  }

  onSearchChange(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this.searchSubject.next(term);
  }

  selectProduct(prod: ProductListItem): void {
    this.selectedProduct.set(prod);
    this.searchTerm.set('');
    // clear the search input visually
    const input = document.querySelector('input[placeholder*="พิมพ์ชื่อสินค้า"]') as HTMLInputElement;
    if (input) input.value = '';
  }

  addVariantToOrder(variant: ProductVariantDetail, productName: string): void {
    if (this.isVariantSelected(variant.variantId)) {
      this.swal.warning('สินค้านี้ถูกเพิ่มในรายการแล้ว');
      return;
    }

    // Remove the initial empty row if it's the only one
    if (this.items().length === 1 && this.items()[0].variantId === 0) {
      this.items.set([]);
    }

    this.items.update((curr) => [
      ...curr,
      {
        variantId: variant.variantId,
        quantity: 1,
        unitPrice: variant.basePrice || 0,
        sku: variant.sku,
        productName: productName,
        variantName: variant.variantNameTh || '',
        imageUrl: variant.imageUrl || undefined,
      },
    ]);
  }

  isVariantSelected(variantId: number): boolean {
    return this.items().some((item) => item.variantId === variantId);
  }

  addItem() {
    this.items.update((curr) => [...curr, { variantId: 0, quantity: 1, unitPrice: 0 }]);
  }

  removeItem(index: number) {
    this.items.update((curr) => curr.filter((_, i) => i !== index));
  }

  updateItem(index: number, field: keyof PurchaseOrderItemCreatePayload, value: any) {
    this.items.update((curr) => {
      const newItems = [...curr];
      (newItems[index] as any)[field] = value;
      return newItems;
    });
  }

  get subTotal(): number {
    return this.items().reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
  }

  get amountAfterDiscount(): number {
    return this.subTotal - this.discountTotal() + this.shippingCost();
  }

  get vatAmount(): number {
    return (this.amountAfterDiscount * this.vatRate()) / 100;
  }

  get grandTotal(): number {
    return this.amountAfterDiscount + this.vatAmount;
  }

  onSlipSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.uploadingSlip.set(true);
      this.poService.uploadSlip(file).subscribe({
        next: (res) => {
          this.slipAttachmentUrl.set(res.imageUrl);
          this.uploadingSlip.set(false);
        },
        error: () => {
          this.swal.error('ไม่สามารถอัปโหลดไฟล์ได้');
          this.uploadingSlip.set(false);
        },
      });
    }
  }

  enableEditMode() {
    const d = this.detail();
    if (!d) return;

    this.poNo.set(d.poNo);
    this.poDate.set(d.poDate.split('T')[0]);
    this.supplierName.set(d.supplierName);
    this.supplierPhone.set(d.supplierPhone || '');
    this.supplierTaxId.set(d.supplierTaxId || '');
    this.supplierAddress.set(d.supplierAddress || '');
    this.paymentMethod.set(d.paymentMethod || 'TRANSFER');
    this.paymentRefNo.set(d.paymentRefNo || '');
    this.sourceAccountInfo.set(d.sourceAccountInfo || '');
    this.vatRate.set(d.vatRate);
    this.discountTotal.set(d.discountTotal);
    this.shippingCost.set(d.shippingCost);
    this.expectedDeliveryDate.set(d.expectedDeliveryDate ? d.expectedDeliveryDate.split('T')[0] : '');
    this.slipAttachmentUrl.set(d.slipAttachmentUrl || '');
    this.notes.set(d.notes || '');

    this.items.set(
      d.items.map((i) => ({
        variantId: i.variantId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        sku: i.sku,
        productName: i.productName,
        variantName: i.variantName,
        imageUrl: i.imageUrl,
      })),
    );

    this.isViewMode.set(false);
  }

  deletePo() {
    if (!this.poId()) return;
    this.swal.confirm('ยืนยันการลบ', 'คุณต้องการลบใบสั่งซื้อนี้ใช่หรือไม่? ข้อมูลจะไม่สามารถกู้คืนได้').then((res) => {
      if (res.isConfirmed) {
        this.poService.delete(this.poId()!).subscribe({
          next: () => {
            this.swal.success('ลบใบสั่งซื้อสำเร็จ');
            this.router.navigate(['/purchasing/purchase-orders']);
          },
          error: (err) => {
            this.swal.error(err.error?.message || 'เกิดข้อผิดพลาดในการลบใบสั่งซื้อ');
          }
        });
      }
    });
  }

  onSubmit() {
    if (this.items().length === 0) {
      this.error.set('กรุณาเพิ่มรายการสินค้าอย่างน้อย 1 รายการ');
      return;
    }

    if (this.items().some((i) => !i.variantId || i.variantId <= 0)) {
      this.error.set('กรุณาระบุรหัสสินค้า (Variant ID) ให้ครบถ้วนในทุกรายการ');
      return;
    }

    const payload: PurchaseOrderCreatePayload = {
      poNo: this.poNo(),
      poDate: this.poDate(),
      supplierName: this.supplierName(),
      supplierPhone: this.supplierPhone(),
      supplierTaxId: this.supplierTaxId(),
      supplierAddress: this.supplierAddress(),
      discountTotal: this.discountTotal() || 0,
      shippingCost: this.shippingCost() || 0,
      vatRate: this.vatRate() || 0,
      expectedDeliveryDate: this.expectedDeliveryDate() ? this.expectedDeliveryDate() : undefined,
      paymentMethod: this.paymentMethod(),
      paymentRefNo: this.paymentRefNo(),
      sourceAccountInfo: this.sourceAccountInfo(),
      slipAttachmentUrl: this.slipAttachmentUrl(),
      notes: this.notes(),
      items: this.items().map((i) => ({
        variantId: Number(i.variantId),
        quantity: Number(i.quantity) || 1,
        unitPrice: Number(i.unitPrice) || 0,
      })),
    };

    this.submitting.set(true);
    this.error.set(null);

    const request$ = this.poId()
      ? this.poService.update(this.poId()!, payload)
      : this.poService.create(payload);

    request$.subscribe({
      next: () => {
        this.swal.success('บันทึกข้อมูลเรียบร้อย');
        this.router.navigate(['/purchasing/purchase-orders']);
      },
      error: (err) => {
        if (err.error?.errors) {
          const errorMessages = Object.values(err.error.errors).flat().join('\n');
          this.error.set(errorMessages);
        } else {
          this.error.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกรายการ');
        }
        this.submitting.set(false);
      },
    });
  }

  updateStatus(status: PurchaseOrderStatus) {
    if (!this.poId()) return;
    if (!confirm(`ยืนยันการเปลี่ยนสถานะเป็น ${status}?`)) return;

    this.submitting.set(true);
    this.poService.updateStatus(this.poId()!, status).subscribe({
      next: () => {
        window.location.reload();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาด');
        this.submitting.set(false);
      },
    });
  }

  getStatusBadgeClass(status: string | undefined): string {
    if (!status) return '';
    switch (status) {
      case 'DRAFT':
        return 'badge-neutral';
      case 'ORDERED':
        return 'badge-info';
      case 'PARTIALLY_RECEIVED':
        return 'badge-warning';
      case 'RECEIVED':
        return 'badge-success';
      case 'CANCELLED':
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }

  getStatusLabel(status: string | undefined): string {
    if (!status) return '';
    const found = this.statusOptions.find((o) => o.value === status);
    return found ? found.label : status;
  }

  getPaymentLabel(method: string | undefined): string {
    if (!method) return '-';
    switch (method) {
      case 'CASH':
        return 'เงินสด';
      case 'TRANSFER':
        return 'โอนเงิน';
      case 'CREDIT':
        return 'เครดิต';
      default:
        return method;
    }
  }
}
