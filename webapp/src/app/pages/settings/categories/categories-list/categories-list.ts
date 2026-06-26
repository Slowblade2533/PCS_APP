import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { CategoryDto } from '../../../../shared/models/category.models';
import { CategoryService } from '../../../../shared/services/category.service';
import { SweetAlertService } from '../../../../shared/services/sweet-alert.service';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-categories-list',
  standalone: true,
  imports: [RouterLink, PaginationComponent],
  templateUrl: './categories-list.html',
})
export class CategoriesList implements OnInit {
  private categoryService = inject(CategoryService);
  private destroyRef = inject(DestroyRef);
  private swal = inject(SweetAlertService);

  categories = signal<CategoryDto[]>([]);
  isLoading = signal<boolean>(true);
  selectedLevel = signal<number | null>(3); // Default to Level 3

  pageNumber = signal<number>(1);
  pageSize = signal<number>(20);

  filteredCategories = computed(() => {
    const all = this.categories();
    const level = this.selectedLevel();
    if (level === null) return all;
    return all.filter((c) => c.level === level);
  });

  paginatedCategories = computed(() => {
    const filtered = this.filteredCategories();
    const start = (this.pageNumber() - 1) * this.pageSize();
    return filtered.slice(start, start + this.pageSize());
  });

  totalCount = computed(() => this.filteredCategories().length);

  ngOnInit(): void {
    this.loadCategories();
  }

  deleteCategory(id: number): void {
    this.swal.confirm('คุณต้องการลบหมวดหมู่นี้ใช่หรือไม่?').then((result) => {
      if (result.isConfirmed) {
        this.categoryService
          .deleteCategory(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.loadCategories();
              this.swal.success('ลบหมวดหมู่เรียบร้อย');
            },
            error: (err) => {
              this.swal.error(err.error?.message || 'เกิดข้อผิดพลาดในการลบข้อมูล');
            },
          });
      }
    });
  }

  loadCategories(): void {
    this.isLoading.set(true);
    this.categoryService
      .getAllCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.categories.set(data);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error(err);
          this.isLoading.set(false);
        },
      });
  }

  onLevelChange(event: any): void {
    const val = event.target.value === 'null' ? null : +event.target.value;
    this.selectedLevel.set(val);
    this.pageNumber.set(1);
  }

  onPageChange(page: number): void {
    this.pageNumber.set(page);
  }
}
