import {
  Component,
  DestroyRef,
  HostListener,
  inject,
  OnInit,
  signal,
  WritableSignal,
  effect,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { CategorySearchComponent } from '../../../../shared/components/category-search/category-search';
import { HasUnsavedChanges } from '../../../../shared/guards/has-unsaved-changes.interface';
import { CategoryDto } from '../../../../shared/models/category.models';
import { CategoryService } from '../../../../shared/services/category.service';

@Component({
  selector: 'app-category-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CategorySearchComponent],
  templateUrl: './category-create.html',
})
export class CategoryCreate implements OnInit, HasUnsavedChanges {
  private categoryService = inject(CategoryService);
  private destroyRef = inject(DestroyRef);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  categoryId = signal<number | null>(null);
  errorMsg = signal<string>('');
  isEditMode = signal<boolean>(false);
  submitting = signal<boolean>(false);

  level1Suggestions = signal<CategoryDto[]>([]);
  level2Suggestions = signal<CategoryDto[]>([]);
  level3Suggestions = signal<CategoryDto[]>([]);

  form: FormGroup = this.fb.group({
    categoryName: ['', [Validators.maxLength(200)]],
    parentId: [null],
    level1Name: ['', [Validators.maxLength(200)]],
    level2Name: ['', [Validators.maxLength(200)]],
    level3Name: ['', [Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    sortOrder: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
  });

  categoryResource = rxResource({
    params: () => this.categoryId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.categoryService.getCategoryById(params).pipe(
        catchError((err) => {
          console.error(err);
          return of(null);
        }),
      );
    },
  });

  constructor() {
    effect(() => {
      const cat = this.categoryResource.value();
      if (cat) {
        untracked(() => {
          this.form.patchValue({
            categoryName: cat.categoryName,
            description: cat.description,
            parentId: cat.parentId,
            sortOrder: cat.sortOrder,
            isActive: cat.isActive,
          });
        });
      } else if (cat === null && !this.categoryResource.isLoading() && this.categoryId()) {
        untracked(() => {
          this.errorMsg.set('ไม่พบข้อมูลหมวดหมู่ที่ระบุ');
        });
      }
    });
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.categoryId.set(Number(idParam));
      this.isEditMode.set(true);
    }

    this.setupAutocomplete('level1Name', this.level1Suggestions);
    this.setupAutocomplete('level2Name', this.level2Suggestions);
    this.setupAutocomplete('level3Name', this.level3Suggestions);
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.submitting();
  }

  hideSuggestions(targetSignal: WritableSignal<CategoryDto[]>): void {
    targetSignal.set([]);
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
        isActive: this.form.value.isActive,
      };
      req$ = this.categoryService.updateCategory(this.categoryId()!, updateData);
    } else {
      const batchData = {
        level1Name: this.form.value.level1Name,
        level2Name: this.form.value.level2Name,
        level3Name: this.form.value.level3Name,
        description: this.form.value.description,
        sortOrder: this.form.value.sortOrder,
        isActive: this.form.value.isActive,
      };
      req$ = this.categoryService.createCategoryBatch(batchData);
    }

    req$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.router.navigate(['/settings/categories']);
      },
      error: (err) => {
        this.errorMsg.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกข้อมูล');
        this.submitting.set(false);
      },
    });
  }

  selectSuggestion(
    controlName: string,
    value: string,
    targetSignal: WritableSignal<CategoryDto[]>,
  ): void {
    this.form.get(controlName)?.setValue(value);
    targetSignal.set([]);
  }

  private setupAutocomplete(
    controlName: string,
    targetSignal: WritableSignal<CategoryDto[]>,
  ): void {
    this.form
      .get(controlName)
      ?.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((val: string) => {
          if (!val || val.length < 2) {
            return of([]);
          }
          return this.categoryService.searchCategories(val).pipe(catchError(() => of([])));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        targetSignal.set(res);
      });
  }
}
