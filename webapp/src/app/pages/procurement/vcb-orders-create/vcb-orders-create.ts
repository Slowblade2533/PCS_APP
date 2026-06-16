import { CommonModule, Location } from '@angular/common';
import Big from 'big.js';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, finalize, switchMap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { VcbOrderCreate } from '../../../shared/models/procurement.models';
import { ProductListItem, ProductVariantDetail } from '../../../shared/models/product.models';
import { ProcurementService } from '../../../shared/services/procurement.service';
import { ProductService } from '../../../shared/services/product.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-vcb-orders-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ImageHoverPreview],
  templateUrl: './vcb-orders-create.html',
})
export class VcbOrdersCreateComponent implements OnInit, HasUnsavedChanges {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly procurementService = inject(ProcurementService);
  private readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly swal = inject(SweetAlertService);
  public readonly location = inject(Location);

  apiOrigin = environment.apiUrl.replace('/api', '');

  editOrderId: number | null = null;
  isEditMode = false;
  isFetchingVariants = false;
  isLoadingData = false;
  isSearching = false;
  isSubmitting = false;
  orderForm!: FormGroup;
  searchResults: ProductListItem[] = [];
  searchSubject = new Subject<string>();
  selectedProduct: ProductListItem | null = null;
  selectedProductVariants: ProductVariantDetail[] = [];
  selectedSlipFile: File | null = null;
  slipFilePreviewUrl: string | null = null;

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: any): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  get items(): FormArray {
    return this.orderForm.get('items') as FormArray;
  }

  ngOnInit(): void {
    this.initForm();
    this.setupProductSearch();
    this.checkEditMode();
  }

  hasUnsavedChanges(): boolean {
    return this.orderForm.dirty && !this.isSubmitting;
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
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      const mode = this.route.snapshot.queryParamMap.get('mode');
      if (id && mode === 'edit') {
        this.isEditMode = true;
        this.editOrderId = Number(id);
        this.loadOrderData();
      }
    });
  }
  private loadOrderData(): void {
    if (!this.editOrderId) return;
    this.isLoadingData = true;
    this.procurementService.getVcbOrderById(this.editOrderId).subscribe({
      next: (res) => {
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
            this.slipFilePreviewUrl = this.apiOrigin + order.transferSlipUrl;
          } else {
            this.slipFilePreviewUrl = null;
          }

          order.items.forEach((item) => {
            const itemForm = this.fb.group({
              variantId: [item.variantId, Validators.required],
              sku: [item.sku],
              imageUrl: [item.imageUrl || null],
              productName: [item.productName],
              variantName: [item.variantName || ''],
              quantity: [item.quantity, [Validators.required, Validators.min(1)]],
              totalPrice: [Number(item.totalPrice ?? 0).toFixed(2), [Validators.required, Validators.min(0)]],
            });
            this.items.push(itemForm);
          });
          this.calculateTotal();
        }
        this.isLoadingData = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.swal.error('โหลดข้อมูลออเดอร์ไม่สำเร็จ');
        this.router.navigate(['/procurement/vcb-orders']);
      },
    });
  }

  private formatDate(date: Date): string {
    const tzoffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzoffset).toISOString().slice(0, 16);
  }

  private setupProductSearch(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        switchMap((term) => {
          if (!term) return [];
          this.isSearching = true;
          return this.productService
            .getProducts({
              pageNumber: 1,
              pageSize: 20,
              searchTerm: term,
            })
            .pipe(
              finalize(() => {
                this.isSearching = false;
                this.cdr.markForCheck();
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res: any) => {
        this.searchResults = res?.items || [];
        this.cdr.markForCheck();
      });
  }

  onSearchChange(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    if (term.length > 2) {
      this.searchSubject.next(term);
    } else {
      this.searchResults = [];
      this.selectedProductVariants = [];
    }
  }

  selectProduct(prod: ProductListItem): void {
    this.selectedProduct = prod;
    this.isFetchingVariants = true;
    this.productService.getProductById(prod.productId).subscribe({
      next: (res) => {
        this.selectedProductVariants = res.variants || [];
        this.isFetchingVariants = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isFetchingVariants = false;
        this.cdr.markForCheck();
        this.swal.error('ไม่สามารถดึงข้อมูลสินค้านี้ได้');
      },
    });
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
      totalPrice: [Number(variant.basePrice ?? 0).toFixed(2), [Validators.required, Validators.min(0)]],
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

  onFileChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedSlipFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.slipFilePreviewUrl = reader.result as string;
        this.cdr.markForCheck();
      };
      reader.readAsDataURL(file);
    }
  }

  clearFile(): void {
    this.selectedSlipFile = null;
    this.slipFilePreviewUrl = null;
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

    this.isSubmitting = true;
    const formValue = this.orderForm.value;
    const dto: VcbOrderCreate = {
      orderNo: formValue.orderNo,
      orderDate: new Date(formValue.orderDate).toISOString(),
      totalAmount: Number(formValue.totalAmount || 0).toFixed(2),
      branchId: formValue.branchId,
      notes: formValue.notes,
      items: formValue.items.map((i: any) => ({
        variantId: i.variantId,
        quantity: i.quantity,
        totalPrice: Number(i.totalPrice || 0).toFixed(2),
      })),
    };

    const formData = new FormData();
    formData.append('data', JSON.stringify(dto));
    if (this.selectedSlipFile) {
      formData.append('slipFile', this.selectedSlipFile);
    }

    if (this.isEditMode && this.editOrderId) {
      this.procurementService.updateVcbOrder(this.editOrderId, formData).subscribe({
        next: (res) => {
          this.cdr.markForCheck();
          if (res.isSuccess) {
            this.orderForm.markAsPristine();
            this.swal.success('แก้ไขออเดอร์ VCANBUY สำเร็จ').then(() => {
              this.router.navigate(['/procurement/vcb-orders']);
            });
          } else {
            this.isSubmitting = false;
            this.swal.error(res.error || res.errorMessage || 'เกิดข้อผิดพลาด');
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
          this.swal.error(
            err.error?.errorMessage || err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ',
          );
        },
      });
    } else {
      this.procurementService.createVcbOrder(formData).subscribe({
        next: (res) => {
          this.cdr.markForCheck();
          if (res.isSuccess) {
            this.orderForm.markAsPristine();
            this.swal.success('สร้างออเดอร์ VCANBUY สำเร็จ').then(() => {
              this.router.navigate(['/procurement/vcb-orders']);
            });
          } else {
            this.isSubmitting = false;
            this.swal.error(res.error || res.errorMessage || 'เกิดข้อผิดพลาด');
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
          this.swal.error(
            err.error?.errorMessage || err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ',
          );
        },
      });
    }
  }
}
