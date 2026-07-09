import { CommonModule, Location } from '@angular/common';
import Big from 'big.js';
import {
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  inject,
  signal,
  computed,
  effect,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { debounceTime, catchError } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { VcbOrderCreate, VcbOrderItem } from '../../../shared/models/vcb-orders.models';
import { ProductListItem, ProductVariantDetail } from '../../../shared/models/product.models';
import { VcbOrdersService } from '../../../shared/services/vcb-orders.service';
import { ProductService } from '../../../shared/services/product.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-vcb-orders-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ImageHoverPreview],
  templateUrl: './vcb-orders-create.html',
})
export class VcbOrdersCreateComponent implements OnInit, HasUnsavedChanges {
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly vcbOrdersService = inject(VcbOrdersService);
  private readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly swal = inject(SweetAlertService);
  public readonly location = inject(Location);

  apiOrigin = environment.apiUrl.replace('/api', '');

  editOrderId = signal<number | null>(null);
  isEditMode = signal(false);
  orderResource = rxResource({
    params: () => this.editOrderId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.vcbOrdersService.getVcbOrderById(params).pipe(
        catchError(() => {
          this.swal.error('โหลดข้อมูลออเดอร์ไม่สำเร็จ');
          this.router.navigate(['/procurement/vcb-orders']);
          return of(null);
        }),
      );
    },
  });

  searchTerm = signal('');

  productsResource = rxResource({
    params: () => this.searchTerm(),
    stream: ({ params }) => {
      if (!params || params.length <= 2) return of({ items: [] });
      return this.productService
        .getProducts({
          pageNumber: 1,
          pageSize: 20,
          searchTerm: params,
        })
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

  isSubmitting = signal(false);
  orderForm!: FormGroup;
  searchSubject = new Subject<string>();
  selectedProduct = signal<ProductListItem | null>(null);
  selectedSlipFile: File | null = null;
  slipFilePreviewUrl = signal<string | null>(null);

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  get items(): FormArray {
    return this.orderForm.get('items') as FormArray;
  }

  constructor() {
    effect(() => {
      const res = this.orderResource.value();
      if (res) {
        const order = res.value || res.data;
        if (order) {
          if (order.status !== 'Pending') {
            this.swal.warning(
              'ออเดอร์นี้ไม่ได้อยู่ในสถานะ "รอดำเนินการ" (Pending) ไม่สามารถแก้ไขได้',
            );
            this.router.navigate(['/procurement/vcb-orders']);
            return;
          }
          this.orderForm.patchValue({
            orderNo: order.orderNo,
            orderDate: this.formatDate(new Date(order.orderDate)),
            totalAmount: Number(order.totalAmount ?? 0).toFixed(2),
            branchId: order.branchId,
            notes: order.notes,
          });

          if (order.transferSlipUrl) {
            this.slipFilePreviewUrl.set(this.apiOrigin + order.transferSlipUrl);
          } else {
            this.slipFilePreviewUrl.set(null);
          }

          this.items.clear();
          order.items.forEach((item: VcbOrderItem) => {
            const itemForm = this.fb.group({
              variantId: [item.variantId, Validators.required],
              sku: [item.sku],
              imageUrl: [item.imageUrl || null],
              productName: [item.productName],
              variantName: [item.variantName || ''],
              quantity: [item.quantity, [Validators.required, Validators.min(1)]],
              totalPrice: [
                Number(item.totalPrice ?? 0).toFixed(2),
                [Validators.required, Validators.min(0)],
              ],
            });
            this.items.push(itemForm);
          });
          this.calculateTotal();
          this.orderForm.markAsPristine();
        }
      }
    });
  }

  ngOnInit(): void {
    this.initForm();
    this.setupProductSearch();
    this.checkEditMode();
  }

  hasUnsavedChanges(): boolean {
    return this.orderForm.dirty && !this.isSubmitting();
  }

  private initForm(): void {
    this.orderForm = this.fb.group({
      orderNo: ['', Validators.required],
      orderDate: [this.formatDate(new Date()), Validators.required],
      totalAmount: ['0.00', [Validators.required, Validators.min(0)]],
      branchId: [1, Validators.required],
      notes: [''],
      items: this.fb.array([], Validators.required),
    });
  }

  private checkEditMode(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');
      const mode = this.route.snapshot.queryParamMap.get('mode');
      if (id && mode === 'edit') {
        this.isEditMode.set(true);
        this.editOrderId.set(Number(id));
      }
    });
  }

  private formatDate(date: Date): string {
    const tzoffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzoffset).toISOString().slice(0, 16);
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
    // clear the search input visually by grabbing element
    const input = document.querySelector(
      'input[placeholder*="พิมพ์ชื่อสินค้า"]',
    ) as HTMLInputElement;
    if (input) input.value = '';
  }

  addVariantToOrder(variant: ProductVariantDetail, productName: string): void {
    const existingIndex = this.items.controls.findIndex(
      (c) => c.get('variantId')?.value === variant.variantId,
    );
    if (existingIndex > -1) {
      this.swal.warning('สินค้านี้ถูกเพิ่มในรายการแล้ว');
      return;
    }

    const itemForm = this.fb.group({
      variantId: [variant.variantId, Validators.required],
      sku: [variant.sku],
      imageUrl: [variant.imageUrl || null],
      productName: [productName],
      variantName: [variant.variantNameTh || ''],
      quantity: [1, [Validators.required, Validators.min(1)]],
      totalPrice: [
        Number(variant.basePrice ?? 0).toFixed(2),
        [Validators.required, Validators.min(0)],
      ],
    });

    this.items.push(itemForm);
    this.calculateTotal();
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.calculateTotal();
  }

  isVariantSelected(variantId: number): boolean {
    return this.items.controls.some((c) => c.get('variantId')?.value === variantId);
  }

  onFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.selectedSlipFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.slipFilePreviewUrl.set(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  }

  clearFile(): void {
    this.selectedSlipFile = null;
    this.slipFilePreviewUrl.set(null);
    const fileInput = document.getElementById('slipFile') as HTMLInputElement;
    if (fileInput) fileInput.value = '';
  }

  calculateTotal(): void {
    let total = new Big(0);
    this.items.controls.forEach((control) => {
      total = total.plus(new Big(control.get('totalPrice')?.value || 0));
    });
    this.orderForm.patchValue({ totalAmount: total.toFixed(2) }, { emitEvent: false });
  }

  formatFinancial(controlName: string, index?: number): void {
    if (index !== undefined) {
      const itemControl = this.items.at(index);
      const control = itemControl.get(controlName);
      if (control) {
        const val = control.value;
        const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
        control.setValue(num.toFixed(2), { emitEvent: false });
        if (controlName === 'totalPrice') {
          this.calculateTotal();
        }
      }
    } else {
      const control = this.orderForm.get(controlName);
      if (control) {
        const val = control.value;
        const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
        control.setValue(num.toFixed(2), { emitEvent: false });
      }
    }
  }

  onSubmit(): void {
    if (this.orderForm.invalid) {
      this.orderForm.markAllAsTouched();
      this.swal.warning('กรุณากรอกข้อมูลให้ครบถ้วน');
      return;
    }

    if (this.items.length === 0) {
      this.swal.warning('ต้องมีสินค้าอย่างน้อย 1 รายการ');
      return;
    }

    this.isSubmitting.set(true);
    const formValue = this.orderForm.value;
    const dto: VcbOrderCreate = {
      orderNo: formValue.orderNo,
      orderDate: new Date(formValue.orderDate).toISOString(),
      totalAmount: Number(formValue.totalAmount || 0).toFixed(2),
      branchId: formValue.branchId,
      notes: formValue.notes,
      items: formValue.items.map(
        (i: { variantId: number; quantity: number; totalPrice: string | number }) => ({
          variantId: i.variantId,
          quantity: i.quantity,
          totalPrice: Number(i.totalPrice || 0).toFixed(2),
        }),
      ),
    };

    const formData = new FormData();
    formData.append('data', JSON.stringify(dto));
    if (this.selectedSlipFile) {
      formData.append('slipFile', this.selectedSlipFile);
    }

    if (this.isEditMode() && this.editOrderId()) {
      this.vcbOrdersService.updateVcbOrder(this.editOrderId()!, formData).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.orderForm.markAsPristine();
            this.swal.success('แก้ไขออเดอร์ VCANBUY สำเร็จ').then(() => {
              this.router.navigate(['/procurement/vcb-orders']);
            });
          } else {
            this.isSubmitting.set(false);
            this.swal.error(res.error || res.errorMessage || 'เกิดข้อผิดพลาด');
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.swal.error(
            err.error?.errorMessage || err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ',
          );
        },
      });
    } else {
      this.vcbOrdersService.createVcbOrder(formData).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.orderForm.markAsPristine();
            this.swal.success('สร้างออเดอร์ VCANBUY สำเร็จ').then(() => {
              this.router.navigate(['/procurement/vcb-orders']);
            });
          } else {
            this.isSubmitting.set(false);
            this.swal.error(res.error || res.errorMessage || 'เกิดข้อผิดพลาด');
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.swal.error(
            err.error?.errorMessage || err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ',
          );
        },
      });
    }
  }
}
