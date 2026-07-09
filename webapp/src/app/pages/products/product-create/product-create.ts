import {
  Component,
  DestroyRef,
  HostListener,
  inject,
  OnInit,
  signal,
  effect,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, debounceTime } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';
import { CategorySearchComponent } from '../../../shared/components/category-search/category-search';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { AuthService } from '../../../shared/services/auth.service';
import { ProductService } from '../../../shared/services/product.service';
import { ProductCreatePayload, ProductDetail, ProductVariantDetail, ProductVariantCreatePayload } from '../../../shared/models/product.models';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { ProductReviewModalComponent } from './product-review-modal/product-review-modal.component';
@Component({
  selector: 'app-product-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ImageHoverPreview,
    CategorySearchComponent,
    ProductReviewModalComponent,
  ],
  templateUrl: './product-create.html',
})
export class ProductCreate implements OnInit, HasUnsavedChanges {
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private swal = inject(SweetAlertService);

  comparisonReport = signal<any[]>([]);
  currentMode = signal<'create' | 'edit' | 'view'>('create');
  isFormReady = signal<boolean>(false);
  isModified = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  isUploadingImage = signal<boolean>(false);
  productId = signal<number | null>(null);
  showReviewModal = signal<boolean>(false);
  submitError = signal<string | null>(null);
  variantsComparison = signal<any[]>([]);

  private originalFormValue = signal<any>(null);

  readonly apiOrigin = environment.apiUrl;

  productDetailResource = rxResource({
    params: () => this.productId(),
    stream: ({ params }) => {
      if (!params) return of(undefined);
      return this.productService.getProductById(params).pipe(
        catchError((err) => {
          console.error('Error loading product:', err);
          return of(null);
        }),
      );
    },
  });

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

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  get variants(): FormArray {
    return this.productForm.get('variants') as FormArray;
  }

  constructor() {
    effect(() => {
      const product = this.productDetailResource.value();
      if (product) {
        untracked(() => {
          this.patchProductForm(product);
        });
      } else if (product === null && !this.productDetailResource.isLoading() && this.productId()) {
        untracked(() => {
          this.submitError.set('ไม่พบข้อมูลสินค้าชิ้นนี้ในระบบ หรือไม่สามารถดึงข้อมูลได้');
          this.isFormReady.set(true);
        });
      }
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const modeParam = this.route.snapshot.queryParamMap.get('mode');

    if (idParam) {
      this.productId.set(Number(idParam));
      this.currentMode.set(modeParam === 'view' ? 'view' : 'edit');
    } else {
      this.currentMode.set('create');
      this.isFormReady.set(true);
      this.isModified.set(false);
      this.trackFormChanges();
    }
  }

  hasUnsavedChanges(): boolean {
    // Only check if we are in create or edit mode
    if (this.currentMode() === 'view') return false;
    return this.productForm.dirty && !this.isSubmitting();
  }

  patchProductForm(product: ProductDetail) {
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
      variantsData.forEach((v: ProductVariantDetail) => {
        this.addVariantWithData(v);
      });
    }

    if (this.currentMode() === 'view') {
      this.productForm.disable();
    }

    this.isFormReady.set(true);
    this.originalFormValue.set(this.productForm.getRawValue());
    this.isModified.set(false); // เริ่มต้นยังไม่ได้แก้ไขอะไร
    this.trackFormChanges();
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
            JSON.stringify(this.originalFormValue()) !== JSON.stringify(currentFormValue);
          this.isModified.set(hasChanged);
        }
      });
  }

  getComparisonReport() {
    const current = this.productForm.getRawValue();
    const old = this.originalFormValue() || {};
    const getCatName = (id: number | null | undefined) => (id ? `รหัสหมวดหมู่: ${id}` : '-');
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
    const oldVariants = this.originalFormValue()?.variants || [];
    const currentVariants = this.productForm.getRawValue().variants || [];

    return currentVariants.map((v: ProductVariantDetail, index: number) => {
      const oldV = oldVariants[index];
      const getDimText = (item: Partial<ProductVariantDetail> | null | undefined) => {
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
            { field: 'น้ำหนัก (กก.)', old: '-', new: v.weight ?? 0, isChanged: true },
            { field: 'ราคาขาย (บาท)', old: '-', new: v.basePrice ?? 0, isChanged: true },
            { field: 'สต็อกเริ่มต้น', old: '-', new: v.currentQuantity ?? 0, isChanged: true },
            { field: 'จุดเตือนสต็อกต่ำ', old: '-', new: v.reorderPoint ?? 0, isChanged: true },
            { field: 'สภาพสินค้า', old: '-', new: v.condition === 'Normal' ? 'ปกติ' : v.condition === 'Defect' ? 'ตำหนิ' : v.condition === 'Damage' ? 'ชำรุด' : v.condition || 'ปกติ', isChanged: true },
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
          field: 'น้ำหนัก (กก.)',
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
        {
          field: 'สภาพสินค้า',
          old: oldV.condition === 'Normal' ? 'ปกติ' : oldV.condition === 'Defect' ? 'ตำหนิ' : oldV.condition === 'Damage' ? 'ชำรุด' : oldV.condition || 'ปกติ',
          new: v.condition === 'Normal' ? 'ปกติ' : v.condition === 'Defect' ? 'ตำหนิ' : v.condition === 'Damage' ? 'ชำรุด' : v.condition || 'ปกติ',
          isChanged: oldV.condition !== v.condition,
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

  addVariantWithData(v: ProductVariantDetail): void {
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
      stagedFile: [null],
      stagedPreviewUrl: [null],
      unitOfMeasure: [v.unitOfMeasure ?? 'อัน', [Validators.required]],
      width: [
        v.width != null && Math.round(Number(v.width)) >= 1 ? Math.round(Number(v.width)) : 1,
        [Validators.required, Validators.min(1), Validators.pattern('^[0-9]+$')],
      ],
      length: [
        v.length != null && Math.round(Number(v.length)) >= 1 ? Math.round(Number(v.length)) : 1,
        [Validators.required, Validators.min(1), Validators.pattern('^[0-9]+$')],
      ],
      height: [
        v.height != null && Math.round(Number(v.height)) >= 1 ? Math.round(Number(v.height)) : 1,
        [Validators.required, Validators.min(1), Validators.pattern('^[0-9]+$')],
      ],
      weight: [
        v.weight ? Number(v.weight).toFixed(2) : '0.00',
        [Validators.required, Validators.min(0)],
      ],
      basePrice: [
        v.basePrice ? Number(v.basePrice).toFixed(2) : '0.00',
        [Validators.required, Validators.min(0)],
      ],
      discountPrice: [
        v.discountPrice ? Number(v.discountPrice).toFixed(2) : '0.00',
        [Validators.min(0)],
      ],
      currentQuantity: [v.currentQuantity ?? 0, [Validators.required, Validators.min(0)]],
      reorderPoint: [v.reorderPoint ?? 0, [Validators.required, Validators.min(0)]],
      condition: [v.condition ?? 'Normal', [Validators.required]],
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
      stagedFile: [null],
      stagedPreviewUrl: [null],
      unitOfMeasure: ['อัน', [Validators.required]],
      width: [1, [Validators.required, Validators.min(1), Validators.pattern('^[0-9]+$')]],
      length: [1, [Validators.required, Validators.min(1), Validators.pattern('^[0-9]+$')]],
      height: [1, [Validators.required, Validators.min(1), Validators.pattern('^[0-9]+$')]],
      weight: ['0.00', [Validators.required, Validators.min(0)]],
      basePrice: ['0.00', [Validators.required, Validators.min(0)]],
      discountPrice: ['0.00', [Validators.min(0)]],
      currentQuantity: [0, [Validators.required, Validators.min(0)]],
      reorderPoint: [0, [Validators.required, Validators.min(0)]],
      condition: ['Normal', [Validators.required]],
    });
    this.variants.push(variantForm);
  }

  formatFinancial(index: number, controlName: string): void {
    const variantForm = this.variants.at(index) as FormGroup;
    const control = variantForm.get(controlName);
    if (control) {
      const val = control.value;
      if (val !== null && val !== undefined && val !== '') {
        const num = parseFloat(val);
        if (!isNaN(num)) {
          control.setValue(num.toFixed(2), { emitEvent: false });
        } else {
          control.setValue('0.00', { emitEvent: false });
        }
      } else {
        control.setValue('0.00', { emitEvent: false });
      }
    }
  }

  formatWeight(index: number): void {
    const variantForm = this.variants.at(index) as FormGroup;
    const control = variantForm.get('weight');
    if (control) {
      const val = control.value;
      if (val !== null && val !== undefined && val !== '') {
        const num = parseFloat(val);
        if (!isNaN(num)) {
          control.setValue(num.toFixed(2), { emitEvent: false });
        } else {
          control.setValue('0.00', { emitEvent: false });
        }
      } else {
        control.setValue('0.00', { emitEvent: false });
      }
    }
  }

  removeVariant(index: number): void {
    this.variants.removeAt(index);
  }

  onVariantImageSelected(event: Event, variantIndex: number): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (file.size > 2 * 1024 * 1024) {
      this.submitError.set('ขนาดไฟล์รูปภาพต้องไม่เกิน 2MB');
      return;
    }

    const variantForm = this.variants.at(variantIndex) as FormGroup;
    variantForm.patchValue({ stagedFile: file });

    const reader = new FileReader();
    reader.onload = () => {
      variantForm.patchValue({ stagedPreviewUrl: reader.result as string });
      variantForm.markAsDirty();
      this.isModified.set(true);
    };
    reader.readAsDataURL(file);
    this.submitError.set(null);
  }

  removeVariantImage(variantIndex: number): void {
    const variantForm = this.variants.at(variantIndex) as FormGroup;
    variantForm.patchValue({ imageUrl: null, stagedFile: null, stagedPreviewUrl: null });
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
    this.isSubmitting.set(true);

    const uploadObservables: Observable<any>[] = [];
    const variantFormsWithUploads: FormGroup[] = [];

    this.variants.controls.forEach((control) => {
      const fg = control as FormGroup;
      const file = fg.get('stagedFile')?.value;
      if (file) {
        uploadObservables.push(this.productService.uploadImage(file));
        variantFormsWithUploads.push(fg);
      }
    });

    const saveProduct = () => {
      const payload = this.buildPayload();
      if (this.currentMode() === 'edit') {
        this.productService
          .updateProduct(this.productId()!, payload)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.productForm.markAsPristine();
              this.swal.success('แก้ไขข้อมูลสินค้าสำเร็จ').then(() => {
                this.router.navigate(['/products']);
              });
            },
            error: (err) => {
              this.isSubmitting.set(false);
              const errMsg = err?.error?.message ?? 'ไม่สามารถแก้ไขข้อมูลสินค้าได้';
              this.submitError.set(errMsg);
              this.swal.error(errMsg);
            },
          });
      } else {
        this.productService
          .createProduct(payload)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.productForm.markAsPristine();
              this.swal.success('บันทึกข้อมูลสินค้าสำเร็จ').then(() => {
                this.router.navigate(['/products']);
              });
            },
            error: (err) => {
              this.isSubmitting.set(false);
              const errMsg = err?.error?.message ?? 'ไม่สามารถบันทึกสินค้าได้ในขณะนี้';
              this.submitError.set(errMsg);
              this.swal.error(errMsg);
            },
          });
      }
    };

    if (uploadObservables.length > 0) {
      forkJoin(uploadObservables)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (responses: { imageUrl: string }[]) => {
            responses.forEach((res, index) => {
              const fg = variantFormsWithUploads[index];
              fg.patchValue({
                imageUrl: res.imageUrl,
                stagedFile: null,
                stagedPreviewUrl: null,
              });
            });
            saveProduct();
          },
          error: (err) => {
            console.error('Variant image upload failed', err);
            this.isSubmitting.set(false);
            const errMsg = 'ไม่สามารถอัปโหลดรูปภาพสินค้าได้ กรุณาลองใหม่อีกครั้ง';
            this.submitError.set(errMsg);
            this.swal.error(errMsg);
          },
        });
    } else {
      saveProduct();
    }
  }

  onSubmit() {
    this.onPreSubmit();
  }

  private buildPayload(): ProductCreatePayload {
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
      variants: (raw.variants ?? []).map((variant: ProductVariantCreatePayload) => ({
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
        width: Math.round(Number(variant.width ?? 1)),
        length: Math.round(Number(variant.length ?? 1)),
        height: Math.round(Number(variant.height ?? 1)),
        weight: Number(variant.weight ?? 0).toFixed(2),
        basePrice: Number(variant.basePrice ?? 0).toFixed(2),
        discountPrice: Number(variant.discountPrice ?? 0).toFixed(2),
        currentQuantity: Number(variant.currentQuantity ?? 0),
        reorderPoint: Number(variant.reorderPoint ?? 0),
      })),
    };
  }
}
