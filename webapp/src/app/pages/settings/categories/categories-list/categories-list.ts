import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategoryService } from '../../../../shared/services/category.service';
import { CategoryDto } from '../../../../shared/models/category.models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-categories-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './categories-list.html',
})
export class CategoriesList implements OnInit {
  private categoryService = inject(CategoryService);
  private destroyRef = inject(DestroyRef);

  categories = signal<CategoryDto[]>([]);
  isLoading = signal<boolean>(true);
  
  // Filter
  selectedLevel = signal<number | null>(3); // Default to Level 3

  filteredCategories = computed(() => {
    const all = this.categories();
    const level = this.selectedLevel();
    if (level === null) return all;
    return all.filter(c => c.level === level);
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading.set(true);
    this.categoryService.getAllCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.categories.set(data);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error(err);
          this.isLoading.set(false);
        }
      });
  }

  deleteCategory(id: number): void {
    if (confirm('คุณต้องการลบหมวดหมู่นี้ใช่หรือไม่?')) {
      this.categoryService.deleteCategory(id).subscribe({
        next: () => {
          this.loadCategories();
        },
        error: (err) => {
          alert(err.error?.message || 'เกิดข้อผิดพลาดในการลบข้อมูล');
        }
      });
    }
  }
}
