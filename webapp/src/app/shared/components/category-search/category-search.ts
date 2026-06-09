import { Component, DestroyRef, ElementRef, forwardRef, HostListener, inject, Input, OnInit, signal } from '@angular/core';
import { ControlValueAccessor, FormControl, FormsModule, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { CategoryDto } from '../../models/category.models';
import { CategoryService } from '../../services/category.service';

@Component({
  selector: 'app-category-search',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule],
  templateUrl: './category-search.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CategorySearchComponent),
      multi: true,
    },
  ],
})
export class CategorySearchComponent implements ControlValueAccessor, OnInit {
  private categoryService = inject(CategoryService);
  private destroyRef = inject(DestroyRef);
  private elRef = inject(ElementRef);

  @Input() placeholder: string = 'ค้นหาหมวดหมู่ (เช่น เตาปิ้งย่าง)';
  @Input() disabled: boolean = false;
  @Input() restrictLevel?: number; // Optional: restrict dropdown to specific level (e.g. 3)

  searchControl = new FormControl('');
  categories = signal<CategoryDto[]>([]);
  isLoading = signal<boolean>(false);
  showDropdown = signal<boolean>(false);
  
  selectedCategory = signal<CategoryDto | null>(null);

  onChange: any = () => {};
  onTouched: any = () => {};

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((query) => {
        if (!query || typeof query !== 'string') {
          this.categories.set([]);
          return;
        }
        
        // Don't search if the query is just the selected category name
        if (this.selectedCategory() && query === this.selectedCategory()?.categoryName) {
           return;
        }

        this.isLoading.set(true);
        this.categoryService.searchCategories(query).subscribe({
          next: (res) => {
            const filtered = this.restrictLevel ? res.filter(c => c.level === this.restrictLevel) : res;
            this.categories.set(filtered);
            this.isLoading.set(false);
            this.showDropdown.set(true);
          },
          error: () => {
            this.categories.set([]);
            this.isLoading.set(false);
          }
        });
      });
  }

  onSelectCategory(category: CategoryDto): void {
    this.selectedCategory.set(category);
    this.searchControl.setValue(category.categoryName, { emitEvent: false });
    this.showDropdown.set(false);
    this.onChange(category.categoryId);
    this.onTouched();
  }

  clearSelection(event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (this.disabled) return;
    
    this.selectedCategory.set(null);
    this.searchControl.setValue('', { emitEvent: false });
    this.categories.set([]);
    this.onChange(null);
    this.onTouched();
    
    // Auto trigger search to show recent or all when cleared
    this.triggerSearch('');
  }

  triggerSearch(query: string = '') {
      if (this.disabled) return;
      this.isLoading.set(true);
      this.categoryService.searchCategories(query).subscribe({
          next: (res) => {
              const filtered = this.restrictLevel ? res.filter(c => c.level === this.restrictLevel) : res;
              this.categories.set(filtered);
              this.isLoading.set(false);
              this.showDropdown.set(true);
          },
          error: () => {
              this.categories.set([]);
              this.isLoading.set(false);
          }
      });
  }

  onInputClick(): void {
    if (this.disabled) return;
    if (this.categories().length === 0) {
       this.triggerSearch(this.searchControl.value || '');
    } else {
       this.showDropdown.set(true);
    }
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event): void {
    if (!this.elRef.nativeElement.contains(event.target)) {
      this.showDropdown.set(false);
      this.onTouched();
      
      // Revert text to selected category if typing without selecting
      if (this.selectedCategory() && this.searchControl.value !== this.selectedCategory()?.categoryName) {
          this.searchControl.setValue(this.selectedCategory()!.categoryName, { emitEvent: false });
      } else if (!this.selectedCategory() && this.searchControl.value) {
          this.searchControl.setValue('', { emitEvent: false });
      }
    }
  }

  // ControlValueAccessor methods
  writeValue(obj: any): void {
    if (obj) {
      // Need to fetch category details to show the name
      this.categoryService.getCategoryById(obj).subscribe({
        next: (cat) => {
          this.selectedCategory.set(cat);
          this.searchControl.setValue(cat.categoryName, { emitEvent: false });
        }
      });
    } else {
      this.selectedCategory.set(null);
      this.searchControl.setValue('', { emitEvent: false });
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
    if (isDisabled) {
      this.searchControl.disable({ emitEvent: false });
    } else {
      this.searchControl.enable({ emitEvent: false });
    }
  }
}
