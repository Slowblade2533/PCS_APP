import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CategoryService } from '../../../../shared/services/category.service';
import { CategorySearchComponent } from '../../../../shared/components/category-search/category-search';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-category-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CategorySearchComponent],
  templateUrl: './category-create.html',
})
export class CategoryCreate implements OnInit {
  private fb = inject(FormBuilder);
  private categoryService = inject(CategoryService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  categoryId = signal<number | null>(null);
  isEditMode = signal<boolean>(false);
  submitting = signal<boolean>(false);
  errorMsg = signal<string>('');

  form: FormGroup = this.fb.group({
    // Standard edit fields
    categoryName: ['', [Validators.maxLength(200)]],
    parentId: [null],
    // Batch create fields
    level1Name: ['', [Validators.maxLength(200)]],
    level2Name: ['', [Validators.maxLength(200)]],
    level3Name: ['', [Validators.maxLength(200)]],
    
    description: ['', [Validators.maxLength(1000)]],
    sortOrder: [0, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.categoryId.set(Number(idParam));
      this.isEditMode.set(true);
      this.loadCategory(this.categoryId()!);
    }
  }

  loadCategory(id: number): void {
    this.categoryService.getCategoryById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (cat) => {
          this.form.patchValue({
            categoryName: cat.categoryName,
            description: cat.description,
            parentId: cat.parentId,
            sortOrder: cat.sortOrder,
            isActive: cat.isActive
          });
        },
        error: (err) => {
          this.errorMsg.set('ไม่พบข้อมูลหมวดหมู่ที่ระบุ');
          console.error(err);
        }
      });
  }

  isInvalid(controlName: string): boolean {
    const ctrl = this.form.get(controlName);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    // Custom validation
    if (!this.isEditMode() && !this.form.value.level1Name?.trim()) {
      this.errorMsg.set('กรุณาระบุชื่อหมวดหมู่หลัก (กลุ่ม 1)');
      return;
    }
    if (this.isEditMode() && !this.form.value.categoryName?.trim()) {
      this.errorMsg.set('กรุณาระบุชื่อหมวดหมู่');
      return;
    }

    this.submitting.set(true);
    this.errorMsg.set('');

    let req$;
    if (this.isEditMode()) {
        const updateData = {
           categoryName: this.form.value.categoryName,
           description: this.form.value.description,
           parentId: this.form.value.parentId,
           sortOrder: this.form.value.sortOrder,
           isActive: this.form.value.isActive
        };
        req$ = this.categoryService.updateCategory(this.categoryId()!, updateData);
    } else {
        const batchData = {
           level1Name: this.form.value.level1Name,
           level2Name: this.form.value.level2Name,
           level3Name: this.form.value.level3Name,
           description: this.form.value.description,
           sortOrder: this.form.value.sortOrder,
           isActive: this.form.value.isActive
        };
        req$ = this.categoryService.createCategoryBatch(batchData);
    }

    req$.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.router.navigate(['/settings/categories']);
        },
        error: (err) => {
          this.errorMsg.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกข้อมูล');
          this.submitting.set(false);
        }
      });
  }
}
