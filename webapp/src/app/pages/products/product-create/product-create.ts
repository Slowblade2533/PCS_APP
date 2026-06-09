import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../shared/services/auth.service';
import { ProductService } from '../../../shared/services/product.service';

import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { CategorySearchComponent } from '../../../shared/components/category-search/category-search';

@Component({
  selector: 'app-product-create',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, ImageHoverPreview, CategorySearchComponent],
  templateUrl: './product-create.html',
  styleUrl: './product-create.css',
})
export class ProductCreate implements OnInit {
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  isFormReady = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  submitError = signal<string | null>(null);
  isUploadingImage = signal<boolean>(false);

  productId = signal<number | null>(null);
  currentMode = signal<'create' | 'edit' | 'view'>('create');
  showReviewModal = signal<boolean>(false);

  isModified = signal<boolean>(false);
  comparisonReport = signal<any[]>([]);
  variantsComparison = signal<any[]>([]);
  private originalFormValue: any = null;

  readonly apiOrigin = environment.apiUrl;

  productForm: FormGroup = this.fb.group({
    productNameTh: ['', [Validators.required]],
    productNameEn: [''],
    description: [''],
    brandName: [''],
    categoryId: ['', [Validators.required]],
    productType: ['Product', [Validators.required]],
    productStatus: ['Available', [Validators.required]],
    isStockTracked: [true, [Validators.required]],
    inventoryGroup: ['ForSale', [Validators.required]],
    variants: this.fb.array([]),
  });

  get variants(): FormArray {
    return this.productForm.get('variants') as FormArray;
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const modeParam = this.route.snapshot.queryParamMap.get('mode');

    if (idParam) {
      this.productId.set(Number(idParam));
      this.currentMode.set(modeParam === 'view' ? 'view' : 'edit');
      this.loadProductDetail(this.productId()!);
    } else {
      this.currentMode.set('create');
      this.isFormReady.set(true);
      this.isModified.set(false);
      this.trackFormChanges();
    }
  }

  loadProductDetail(id: number) {
    this.productService
      .getProductById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (product) => {
          if (!product) {
            this.submitError.set('ไม่พบข้อมูลสินค้าชิ้นนี้ในระบบ');
            this.isFormReady.set(true);
            return;
          }

          const nameTh = product.productNameTh;
          const nameEn = product.productNameEn;
          const desc = product.description;
          const brand = product.brandName;
          const catId = product.categoryId;
          const type = product.productType;
          const status = product.productStatus;
          const isTracked = product.isStockTracked ?? true;
          const invGroup = product.inventoryGroup ?? 'ForSale';

          this.productForm.patchValue({
            productNameTh: nameTh,
            productNameEn: nameEn,
            description: desc,
            brandName: brand,
            categoryId: catId,
            productType: type || 'Product',
            productStatus: status || 'Available',
            isStockTracked: isTracked,
            inventoryGroup: invGroup,
          });

          this.variants.clear();
          const variantsData = product.variants;

          if (variantsData && Array.isArray(variantsData)) {
            variantsData.forEach((v: any) => {
              this.addVariantWithData(v);
            });
          }

          if (this.currentMode() === 'view') {
            this.productForm.disable();
          }

          this.isFormReady.set(true);
          this.originalFormValue = this.productForm.getRawValue();
          this.isModified.set(false); // เริ่มต้นยังไม่ได้แก้ไขอะไร
          this.trackFormChanges();
        },
        error: (err) => {
          console.error('Error loading product:', err);
          this.submitError.set('ไม่สามารถดึงข้อมูลรายละเอียดสินค้าได้');
          this.isFormReady.set(true);
        },
      });
  }

  trackFormChanges(): void {
    this.productForm.valueChanges
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.currentMode() === 'create') {
          this.isModified.set(this.productForm.dirty);
        } else if (this.currentMode() === 'edit') {
          const currentFormValue = this.productForm.getRawValue();
          const hasChanged =
            JSON.stringify(this.originalFormValue) !== JSON.stringify(currentFormValue);
          this.isModified.set(hasChanged);
        }
      });
  }

  getComparisonReport() {
    const current = this.productForm.getRawValue();
    const old = this.originalFormValue || {};
    const getCatName = (id: any) => id ? `รหัสหมวดหมู่: ${id}` : '-';
    const oldNameTh = (old.productNameTh || '').trim();
    const newNameTh = (current.productNameTh || '').trim();
    const oldNameEn = (old.productNameEn || '').trim();
    const newNameEn = (current.productNameEn || '').trim();
    const oldBrand = (old.brandName || '').trim();
    const newBrand = (current.brandName || '').trim();
    const oldDesc = (old.description || '').trim();
    const newDesc = (current.description || '').trim();

    return [
      {
        label: 'ชื่อสินค้า (ไทย)',
        old: oldNameTh || '-',
        new: newNameTh || '-',
        isChanged: oldNameTh !== newNameTh,
      },
      {
        label: 'ชื่อสินค้า (อังกฤษ)',
        old: oldNameEn || '-',
        new: newNameEn || '-',
        isChanged: oldNameEn !== newNameEn,
      },
      {
        label: 'แบรนด์สินค้า',
        old: oldBrand || '-',
        new: newBrand || '-',
        isChanged: oldBrand !== newBrand,
      },
      {
        label: 'หมวดหมู่สินค้า',
        old: old.categoryId ? getCatName(old.categoryId) : '-',
        new: current.categoryId ? getCatName(current.categoryId) : '-',
        isChanged: Number(old.categoryId) !== Number(current.categoryId),
      },
      {
        label: 'ประเภทสินค้า',
        old: old.productType || '-',
        new: current.productType || '-',
        isChanged: old.productType !== current.productType,
      },
      {
        label: 'สถานะสินค้า',
        old: old.productStatus || '-',
        new: current.productStatus || '-',
        isChanged: old.productStatus !== current.productStatus,
      },
      {
        label: 'การนับสต็อก',
        old: old.isStockTracked ? 'นับสต็อก' : 'ไม่นับสต็อก',
        new: current.isStockTracked ? 'นับสต็อก' : 'ไม่นับสต็อก',
        isChanged: old.isStockTracked !== current.isStockTracked,
      },
      {
        label: 'กลุ่มคลังสินค้า',
        old: old.inventoryGroup === 'Internal' ? 'ใช้ภายในองค์กร' : 'สินค้าจำหน่าย',
        new: current.inventoryGroup === 'Internal' ? 'ใช้ภายในองค์กร' : 'สินค้าจำหน่าย',
        isChanged: old.inventoryGroup !== current.inventoryGroup,
      },
      {
        label: 'รายละเอียดสินค้า',
        old: oldDesc || '-',
        new: newDesc || '-',
        isChanged: oldDesc !== newDesc,
      },
    ];
  }

  getVariantsComparison() {
    const oldVariants = this.originalFormValue?.variants || [];
    const currentVariants = this.productForm.getRawValue().variants || [];

    return currentVariants.map((v: any, index: number) => {
      const oldV = oldVariants[index];
      const getDimText = (item: any) => {
        if (!item) return '-';
        const w = item.width ?? 0;
        const l = item.length ?? 0;
        const h = item.height ?? 0;
        return `${w} x ${l} x ${h} ซม.`;
      };

      if (!oldV) {
        return {
          skuName: v.sku || `SKU ใหม่ลำดับที่ #${index + 1}`,
          isNew: true,
          details: [
            { field: 'รหัส SKU', old: '-', new: v.sku || '-', isChanged: true },
            { field: 'บาร์โค้ด', old: '-', new: v.barcode || '-', isChanged: true },
            { field: 'ชื่อตัวเลือก (ไทย)', old: '-', new: v.variantNameTh || '-', isChanged: true },
            { field: 'ชื่อตัวเลือก (Eng)', old: '-', new: v.variantNameEn || '-', isChanged: true },
            { field: 'สี (Color)', old: '-', new: v.color || '-', isChanged: true },
            { field: 'ขนาด (Size)', old: '-', new: v.sizeLabel || '-', isChanged: true },
            { field: 'ลวดลาย (Style)', old: '-', new: v.stylePattern || '-', isChanged: true },
            {
              field: 'รูปภาพประกอบ SKU',
              old: null,
              new: v.imageUrl || null,
              isChanged: !!v.imageUrl,
              isImage: true,
            },
            { field: 'หน่วยนับ', old: '-', new: v.unitOfMeasure || 'อัน', isChanged: true },
            { field: 'ขนาด (กว้างxยาวxสูง)', old: '-', new: getDimText(v), isChanged: true },
            { field: 'น้ำหนัก (กรัม)', old: '-', new: v.weight ?? 0, isChanged: true },
            { field: 'ราคาขาย (บาท)', old: '-', new: v.basePrice ?? 0, isChanged: true },
            { field: 'สต็อกเริ่มต้น', old: '-', new: v.currentQuantity ?? 0, isChanged: true },
            { field: 'จุดเตือนสต็อกต่ำ', old: '-', new: v.reorderPoint ?? 0, isChanged: true },
          ],
        };
      }

      const isDimChanged =
        Number(oldV.width) !== Number(v.width) ||
        Number(oldV.length) !== Number(v.length) ||
        Number(oldV.height) !== Number(v.height);

      const detailsReport = [
        {
          field: 'รหัส SKU',
          old: oldV.sku || '-',
          new: v.sku || '-',
          isChanged: (oldV.sku || '').trim() !== (v.sku || '').trim(),
        },
        {
          field: 'รูปภาพประกอบ SKU', // ✨ เพิ่มบล็อกนี้เข้าไปในอาร์เรย์เปรียบเทียบข้อมูลเดิม
          old: oldV.imageUrl || null,
          new: v.imageUrl || null,
          isChanged: (oldV.imageUrl || '') !== (v.imageUrl || ''),
          isImage: true,
        },
        {
          field: 'บาร์โค้ด',
          old: oldV.barcode || '-',
          new: v.barcode || '-',
          isChanged: (oldV.barcode || '').trim() !== (v.barcode || '').trim(),
        },
        {
          field: 'ชื่อตัวเลือก (ไทย)',
          old: oldV.variantNameTh || '-',
          new: v.variantNameTh || '-',
          isChanged: (oldV.variantNameTh || '').trim() !== (v.variantNameTh || '').trim(),
        },
        {
          field: 'ชื่อตัวเลือก (Eng)',
          old: oldV.variantNameEn || '-',
          new: v.variantNameEn || '-',
          isChanged: (oldV.variantNameEn || '').trim() !== (v.variantNameEn || '').trim(),
        },
        {
          field: 'สี (Color)',
          old: oldV.color || '-',
          new: v.color || '-',
          isChanged: (oldV.color || '').trim() !== (v.color || '').trim(),
        },
        {
          field: 'ขนาด (Size)',
          old: oldV.sizeLabel || '-',
          new: v.sizeLabel || '-',
          isChanged: (oldV.sizeLabel || '').trim() !== (v.sizeLabel || '').trim(),
        },
        {
          field: 'ลวดลาย (Style)',
          old: oldV.stylePattern || '-',
          new: v.stylePattern || '-',
          isChanged: (oldV.stylePattern || '').trim() !== (v.stylePattern || '').trim(),
        },
        {
          field: 'หน่วยนับ',
          old: oldV.unitOfMeasure || 'อัน',
          new: v.unitOfMeasure || 'อัน',
          isChanged: (oldV.unitOfMeasure || '').trim() !== (v.unitOfMeasure || '').trim(),
        },
        {
          field: 'ขนาด (กว้างxยาวxสูง)',
          old: getDimText(oldV),
          new: getDimText(v),
          isChanged: isDimChanged,
        },
        {
          field: 'น้ำหนัก (กรัม)',
          old: oldV.weight ?? 0,
          new: v.weight ?? 0,
          isChanged: Number(oldV.weight) !== Number(v.weight),
        },
        {
          field: 'ราคาขาย (บาท)',
          old: oldV.basePrice ?? 0,
          new: v.basePrice ?? 0,
          isChanged: Number(oldV.basePrice) !== Number(v.basePrice),
        },
        {
          field: 'สต็อกสินค้า',
          old: oldV.currentQuantity ?? 0,
          new: v.currentQuantity ?? 0,
          isChanged: Number(oldV.currentQuantity) !== Number(v.currentQuantity),
        },
        {
          field: 'จุดเตือนสต็อกต่ำ',
          old: oldV.reorderPoint ?? 0,
          new: v.reorderPoint ?? 0,
          isChanged: Number(oldV.reorderPoint) !== Number(v.reorderPoint),
        },
      ];

      const hasAnyChange = detailsReport.some((d) => d.isChanged);

      return {
        skuName: oldV.sku || v.sku || `SKU ลำดับที่ #${index + 1}`,
        isNew: false,
        isChanged: hasAnyChange,
        details: detailsReport,
      };
    });
  }

  addVariantWithData(v: any): void {
    const variantForm = this.fb.group({
      variantId: [v.variantId],
      sku: [v.sku, [Validators.required]],
      barcode: [v.barcode],
      variantNameTh: [v.variantNameTh],
      variantNameEn: [v.variantNameEn],
      color: [v.color],
      sizeLabel: [v.sizeLabel],
      stylePattern: [v.stylePattern],
      imageUrl: [v.imageUrl ?? null],
      unitOfMeasure: [v.unitOfMeasure ?? 'อัน', [Validators.required]],
      width: [v.width ?? 0.01, [Validators.required, Validators.min(0)]],
      length: [v.length ?? 0.01, [Validators.required, Validators.min(0)]],
      height: [v.height ?? 0.01, [Validators.required, Validators.min(0)]],
      weight: [v.weight ?? 0.01, [Validators.required, Validators.min(0)]],
      basePrice: [v.basePrice ?? 0.0, [Validators.required, Validators.min(0)]],
      discountPrice: [v.discountPrice ?? 0.0, [Validators.min(0)]],
      currentQuantity: [v.currentQuantity ?? 0, [Validators.required, Validators.min(0)]],
      reorderPoint: [v.reorderPoint ?? 0, [Validators.required, Validators.min(0)]],
    });

    if (this.currentMode() === 'view') {
      variantForm.disable();
    }
    this.variants.push(variantForm);
  }

  addVariant(): void {
    const variantForm = this.fb.group({
      variantId: [null],
      sku: ['', [Validators.required]],
      barcode: [''],
      variantNameTh: [''],
      variantNameEn: [''],
      color: [''],
      sizeLabel: [''],
      stylePattern: [''],
      imageUrl: [null],
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

  onVariantImageSelected(event: any, variantIndex: number): void {
    const file = event.target.files[0];
    if (!file) return;
    if (file.size > 2 * 1024 * 1024) {
      this.submitError.set('ขนาดไฟล์รูปภาพต้องไม่เกิน 2MB');
      return;
    }

    this.isUploadingImage.set(true);
    this.submitError.set(null);

    this.productService
      .uploadImage(file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: any) => {
          const variantForm = this.variants.at(variantIndex) as FormGroup;
          variantForm.patchValue({ imageUrl: res.imageUrl });
          variantForm.markAsDirty();
          this.isModified.set(true);
          this.isUploadingImage.set(false);
        },
        error: (err: any) => {
          console.error('Upload failed', err);
          this.submitError.set('ไม่สามารถอัปโหลดรูปภาพได้ กรุณาลองใหม่อีกครั้ง');
          this.isUploadingImage.set(false);
        },
      });
  }

  removeVariantImage(variantIndex: number): void {
    const variantForm = this.variants.at(variantIndex) as FormGroup;
    variantForm.patchValue({ imageUrl: null });
    variantForm.markAsDirty();
    this.isModified.set(true);
  }

  onPreSubmit(): void {
    this.submitError.set(null);
    if (!this.productForm.valid) {
      this.productForm.markAllAsTouched();
      return;
    }

    const rawVariants = this.variants.getRawValue();
    const skus = rawVariants.map((v) => (v.sku || '').trim().toLowerCase()).filter((s) => s !== '');
    const hasDuplicateSku = skus.some((item, index) => skus.indexOf(item) !== index);
    if (hasDuplicateSku) {
      this.submitError.set(
        '❌ พบรหัส SKU ซ้ำกันภายในรายการสินค้าหน่วยย่อย กรุณาแก้ไขไม่ให้ซ้ำกันก่อนทำการบันทึก',
      );
      return;
    }

    const barcodes = rawVariants
      .map((v) => (v.barcode || '').trim().toLowerCase())
      .filter((b) => b !== '');
    const hasDuplicateBarcode = barcodes.some((item, index) => barcodes.indexOf(item) !== index);
    if (hasDuplicateBarcode) {
      this.submitError.set(
        '❌ พบรหัสบาร์โค้ดซ้ำกันภายในรายการสินค้าหน่วยย่อย กรุณาแก้ไขไม่ให้ซ้ำกันก่อนทำการบันทึก',
      );
      return;
    }

    if (this.variants.length === 0) {
      this.submitError.set('กรุณาเพิ่มอย่างน้อย 1 SKU ก่อนบันทึก');
      return;
    }

    this.comparisonReport.set(this.getComparisonReport());
    this.variantsComparison.set(this.getVariantsComparison());
    this.showReviewModal.set(true);
  }

  executeSubmit(): void {
    this.showReviewModal.set(false);
    const payload = this.buildPayload();
    this.isSubmitting.set(true);

    if (this.currentMode() === 'edit') {
      this.productService
        .updateProduct(this.productId()!, payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.router.navigate(['/products']);
          },
          error: (err) => {
            this.isSubmitting.set(false);
            this.submitError.set(err?.error?.message ?? 'ไม่สามารถแก้ไขข้อมูลสินค้าได้');
          },
        });
    } else {
      this.productService
        .createProduct(payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
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
  }

  onSubmit() {
    this.onPreSubmit();
  }

  private buildPayload(): any {
    const raw = this.productForm.getRawValue();
    const currentUserId = Number(this.authService.currentUser()?.id);

    return {
      productNameTh: String(raw.productNameTh ?? '').trim(),
      productNameEn: String(raw.productNameEn ?? '').trim() || undefined,
      description: String(raw.description ?? '').trim() || undefined,
      brandName: String(raw.brandName ?? '').trim() || undefined,
      categoryId: Number(raw.categoryId),
      productType: raw.productType,
      productStatus: raw.productStatus,
      isStockTracked: Boolean(raw.isStockTracked),
      inventoryGroup: raw.inventoryGroup,
      updatedBy: this.currentMode() === 'edit' ? currentUserId : undefined,
      createdBy: this.currentMode() === 'create' ? currentUserId : undefined,
      variants: (raw.variants ?? []).map((variant: any) => ({
        variantId: variant.variantId ? Number(variant.variantId) : undefined,
        sku: String(variant.sku ?? '').trim(),
        barcode: String(variant.barcode ?? '').trim() || undefined,
        variantNameTh: String(variant.variantNameTh ?? '').trim() || undefined,
        variantNameEn: String(variant.variantNameEn ?? '').trim() || undefined,
        color: String(variant.color ?? '').trim() || undefined,
        sizeLabel: String(variant.sizeLabel ?? '').trim() || undefined,
        stylePattern: String(variant.stylePattern ?? '').trim() || undefined,
        imageUrl: variant.imageUrl || undefined,
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
