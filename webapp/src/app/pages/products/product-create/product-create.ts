import { Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ProductCreatePayload } from '../../../shared/models/product.model';
import { AuthService } from '../../../shared/services/auth.service';
import { ProductService } from '../../../shared/services/product.service';

@Component({
  selector: 'app-product-create',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-create.html',
  styleUrl: './product-create.css',
})
export class ProductCreate implements OnInit {
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private authService = inject(AuthService);
  private router = inject(Router);

  isFormReady = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  submitError = signal<string | null>(null);

  categories = signal([
    { id: 1, name: 'เตาปิ้งย่าง' },
    { id: 2, name: 'ตะแกรงปิ้งย่าง' },
    { id: 3, name: 'กระทะย่าง' },
  ]);

  productForm: FormGroup = this.fb.group({
    productNameTh: ['', [Validators.required]],
    productNameEn: [''],
    description: [''],
    brandName: [''],
    categoryId: ['', [Validators.required]],
    productType: ['Product', [Validators.required]],
    productStatus: ['Available', [Validators.required]],
    variants: this.fb.array([]),
  });

  get variants(): FormArray {
    return this.productForm.get('variants') as FormArray;
  }

  ngOnInit(): void {
    this.productForm.updateValueAndValidity();
    this.isFormReady.set(true);
  }

  addVariant(): void {
    const variantForm = this.fb.group({
      sku: ['', [Validators.required]],
      barcode: [''],
      unitOfMeasure: ['อัน', [Validators.required]],
      width: [0.01, [Validators.required, Validators.min(0)]],
      length: [0.01, [Validators.required, Validators.min(0)]],
      height: [0.01, [Validators.required, Validators.min(0)]],
      weight: [0.01, [Validators.required, Validators.min(0)]],
      basePrice: [0.0, [Validators.required, Validators.min(0)]],
      discountPrice: [0.0, [Validators.min(0)]],
      currentQuantity: [0, [Validators.required, Validators.min(0)]],
      reorderPoint: [0, [Validators.required, Validators.min(0)]],
    });

    this.variants.push(variantForm);
  }

  removeVariant(index: number): void {
    this.variants.removeAt(index);
  }

  onSubmit(): void {
    this.submitError.set(null);

    if (!this.productForm.valid) {
      this.productForm.markAllAsTouched();
      return;
    }

    if (this.variants.length === 0) {
      this.submitError.set('กรุณาเพิ่มอย่างน้อย 1 SKU ก่อนบันทึก');
      return;
    }

    const payload = this.buildPayload();
    this.isSubmitting.set(true);

    this.productService.createProduct(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/products']);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.submitError.set(err?.error?.message ?? 'ไม่สามารถบันทึกสินค้าได้ในขณะนี้');
      },
    });
  }

  private buildPayload(): ProductCreatePayload {
    type RawVariant = {
      sku?: string;
      barcode?: string;
      unitOfMeasure?: string;
      width?: number;
      length?: number;
      height?: number;
      weight?: number;
      basePrice?: number;
      discountPrice?: number;
      currentQuantity?: number;
      reorderPoint?: number;
    };

    type RawProductForm = {
      productNameTh?: string;
      productNameEn?: string;
      description?: string;
      brandName?: string;
      categoryId?: number | string;
      productType: ProductCreatePayload['productType'];
      productStatus: ProductCreatePayload['productStatus'];
      variants?: RawVariant[];
    };

    const raw = this.productForm.getRawValue() as RawProductForm;
    const currentUserId = Number(this.authService.currentUser()?.id);

    return {
      productNameTh: String(raw.productNameTh ?? '').trim(),
      productNameEn: String(raw.productNameEn ?? '').trim() || undefined,
      description: String(raw.description ?? '').trim() || undefined,
      brandName: String(raw.brandName ?? '').trim() || undefined,
      categoryId: Number(raw.categoryId),
      productType: raw.productType,
      productStatus: raw.productStatus,
      createdBy: Number.isFinite(currentUserId) ? currentUserId : undefined,
      variants: (raw.variants ?? []).map((variant) => ({
        sku: String(variant.sku ?? '').trim(),
        barcode: String(variant.barcode ?? '').trim() || undefined,
        unitOfMeasure: String(variant.unitOfMeasure ?? 'อัน'),
        width: Number(variant.width ?? 0),
        length: Number(variant.length ?? 0),
        height: Number(variant.height ?? 0),
        weight: Number(variant.weight ?? 0),
        basePrice: Number(variant.basePrice ?? 0),
        discountPrice: Number(variant.discountPrice ?? 0),
        currentQuantity: Number(variant.currentQuantity ?? 0),
        reorderPoint: Number(variant.reorderPoint ?? 0),
      })),
    };
  }
}
