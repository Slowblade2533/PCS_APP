import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { API_ORIGIN } from '../../../shared/config/api.config';
import { AuthService } from '../../../shared/services/auth.service';
import { ProductService } from '../../../shared/services/product.service';

@Component({
  selector: 'app-product-create',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-create.html',
  styleUrl: './product-create.css',
})
export class ProductCreate implements OnInit {
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isFormReady = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  submitError = signal<string | null>(null);
  isUploadingImage = signal<boolean>(false);

  productId = signal<number | null>(null);
  currentMode = signal<'create' | 'edit' | 'view'>('create');
  showReviewModal = signal<boolean>(false);

  isModified = signal<boolean>(false);
  private originalFormValue: any = null;
  readonly apiOrigin = API_ORIGIN;

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
    this.productService.getProductById(id).subscribe({
      next: (product) => {
        console.log('API Response:', product);
        if (!product) {
          this.submitError.set('ไม่พบข้อมูลสินค้าชิ้นนี้ในระบบ');
          this.isFormReady.set(true);
          return;
        }

        const nameTh = product.ProductNameTh ?? product.productNameTh;
        const nameEn = product.ProductNameEn ?? product.productNameEn;
        const desc = product.Description ?? product.description;
        const brand = product.BrandName ?? product.brandName;
        const catId = product.CategoryId ?? product.categoryId;
        const type = product.ProductType ?? product.productType;
        const status = product.ProductStatus ?? product.productStatus;

        this.productForm.patchValue({
          productNameTh: nameTh,
          productNameEn: nameEn,
          description: desc,
          brandName: brand,
          categoryId: catId,
          productType: type || 'Product',
          productStatus: status || 'Available',
        });

        this.variants.clear();
        const variantsData = product.variants ?? product.Variants;
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
    this.productForm.valueChanges.subscribe(() => {
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
    const getCatName = (id: any) => this.categories().find((c) => c.id === Number(id))?.name || id;

    // เตรียมค่าเพื่อทำการเปรียบเทียบแบบ Trim ข้อมูลป้องกันสเปซว่าง
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

      // ฟังก์ชันตัวช่วยดึงข้อความขนาด กว้าง x ยาว x สูง
      const getDimText = (item: any) => {
        if (!item) return '-';
        const w = item.width ?? 0;
        const l = item.length ?? 0;
        const h = item.height ?? 0;
        return `${w} x ${l} x ${h} ซม.`;
      };

      // กรณี 1: เป็น SKU ที่เพิ่มเข้ามาใหม่
      // 1. เพิ่มรายการตรวจเช็ค "รูปภาพประกอบ" ในฝั่งกรณี SKU ใหม่ (oldV ไม่มีค่า)
      if (!oldV) {
        return {
          skuName: v.sku || `SKU ใหม่ลำดับที่ #${index + 1}`,
          isNew: true,
          details: [
            { field: 'รหัส SKU', old: '-', new: v.sku || '-', isChanged: true },
            { field: 'บาร์โค้ด', old: '-', new: v.barcode || '-', isChanged: true },
            {
              field: 'รูปภาพประกอบ SKU',
              old: null,
              new: v.imageUrl || null,
              isChanged: !!v.imageUrl,
              isImage: true,
            }, // ✨ เพิ่มบรรทัดนี้
            { field: 'หน่วยนับ', old: '-', new: v.unitOfMeasure || 'อัน', isChanged: true },
            { field: 'ขนาด (กว้างxยาวxสูง)', old: '-', new: getDimText(v), isChanged: true },
            { field: 'น้ำหนัก (กรัม)', old: '-', new: v.weight ?? 0, isChanged: true },
            { field: 'ราคาขาย (บาท)', old: '-', new: v.basePrice ?? 0, isChanged: true },
            { field: 'สต็อกเริ่มต้น', old: '-', new: v.currentQuantity ?? 0, isChanged: true },
            { field: 'จุดเตือนสต็อกต่ำ', old: '-', new: v.reorderPoint ?? 0, isChanged: true },
          ],
        };
      }

      // ตรวจเช็กการเปลี่ยนแปลงของขนาด (เช็กแยกทีละแกน)
      const isDimChanged =
        Number(oldV.width) !== Number(v.width) ||
        Number(oldV.length) !== Number(v.length) ||
        Number(oldV.height) !== Number(v.height);

      // กรณี 2: เป็น SKU เดิมที่มีอยู่แล้ว แต่นำมาเช็กความเปลี่ยนแปลง
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

      // เช็กภาพรวมว่า SKU ตัวนี้มีฟิลด์ใดฟิลด์หนึ่งเปลี่ยนไปหรือไม่
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
    // 💡 ดักจับแบบยืดหยุ่นสำหรับฝั่ง SKU ตัวย่อย
    const variantForm = this.fb.group({
      variantId: [v.VariantId ?? v.variantId],
      sku: [v.Sku ?? v.sku, [Validators.required]],
      barcode: [v.Barcode ?? v.barcode],
      imageUrl: [v.ImageUrl ?? v.imageUrl ?? null],
      unitOfMeasure: [v.UnitOfMeasure ?? v.unitOfMeasure ?? 'อัน', [Validators.required]],
      width: [v.Width ?? v.width ?? 0.01, [Validators.required, Validators.min(0)]],
      length: [v.Length ?? v.length ?? 0.01, [Validators.required, Validators.min(0)]],
      height: [v.Height ?? v.height ?? 0.01, [Validators.required, Validators.min(0)]],
      weight: [v.Weight ?? v.weight ?? 0.01, [Validators.required, Validators.min(0)]],
      basePrice: [v.BasePrice ?? v.basePrice ?? 0.0, [Validators.required, Validators.min(0)]],
      discountPrice: [v.DiscountPrice ?? v.discountPrice ?? 0.0, [Validators.min(0)]],
      currentQuantity: [
        v.CurrentQuantity ?? v.currentQuantity ?? 0,
        [Validators.required, Validators.min(0)],
      ],
      reorderPoint: [
        v.ReorderPoint ?? v.reorderPoint ?? 0,
        [Validators.required, Validators.min(0)],
      ],
    });

    if (this.currentMode() === 'view') {
      variantForm.disable();
    }
    this.variants.push(variantForm);
  }

  // ฟังก์ชันกดเพิ่ม SKU เปล่าใหม่ (กรณีโหมดสร้าง/แก้ไข)
  addVariant(): void {
    const variantForm = this.fb.group({
      variantId: [null],
      sku: ['', [Validators.required]],
      barcode: [''],
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

  // ✨ 1. ฟังก์ชันจัดการเมื่อเลือกไฟล์รูปภาพ
  onVariantImageSelected(event: any, variantIndex: number): void {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      this.submitError.set('ขนาดไฟล์รูปภาพต้องไม่เกิน 2MB');
      return;
    }

    this.isUploadingImage.set(true);
    this.submitError.set(null);

    this.productService.uploadImage(file).subscribe({
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

  // ✨ 2. ฟังก์ชันลบรูปภาพออกจากฟอร์ม
  removeVariantImage(variantIndex: number): void {
    const variantForm = this.variants.at(variantIndex) as FormGroup;
    variantForm.patchValue({ imageUrl: null });
    variantForm.markAsDirty();
    this.isModified.set(true);
  }

  // กดปุ่มบันทึก ให้เด้งหน้าต่าง Review ก่อน
  onPreSubmit(): void {
    this.submitError.set(null);
    if (!this.productForm.valid) {
      this.productForm.markAllAsTouched();
      return;
    }

    // ✨ [เพิ่มจุดนี้] ดักจับรหัส SKU และ Barcode ซ้ำกันเองภายในฟอร์มหน้าเว็บ
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

    // เปิด Modal ให้ Review ข้อมูล
    this.showReviewModal.set(true);
  }

  // ยืนยันการบันทึกข้อมูลจริงจากหน้าต่าง Review
  executeSubmit(): void {
    this.showReviewModal.set(false);
    const payload = this.buildPayload();
    this.isSubmitting.set(true);

    if (this.currentMode() === 'edit') {
      // เรียกใช้ API อัปเดตสินค้า
      this.productService.updateProduct(this.productId()!, payload).subscribe({
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
      // เรียกใช้ API สร้างสินค้าอันเดิม
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
  }

  // เปลี่ยนชื่อฟังก์ชันเดิมเล็กน้อยเพื่อให้ล้อไปกับระบบใหม่
  onSubmit() {
    this.onPreSubmit();
  }

  private buildPayload(): any {
    // ปรับโครงสร้าง payload ให้แนบ `variantId` ไปด้วย (กรณี Edit)
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
      updatedBy: this.currentMode() === 'edit' ? currentUserId : undefined,
      createdBy: this.currentMode() === 'create' ? currentUserId : undefined,
      variants: (raw.variants ?? []).map((variant: any) => ({
        variantId: variant.variantId ? Number(variant.variantId) : undefined,
        sku: String(variant.sku ?? '').trim(),
        barcode: String(variant.barcode ?? '').trim() || undefined,
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
