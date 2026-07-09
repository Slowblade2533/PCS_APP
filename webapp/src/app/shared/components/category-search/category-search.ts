import {
  Component,
  DestroyRef,
  ElementRef,
  forwardRef,
  HostListener,
  inject,
  Input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ControlValueAccessor,
  FormControl,
  FormsModule,
  NG_VALUE_ACCESSOR,
  ReactiveFormsModule,
} from '@angular/forms';
import { BehaviorSubject, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
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

  @Input() disabled: boolean = false;
  @Input() placeholder: string = 'ค้นหาหมวดหมู่ (เช่น เตาปิ้งย่าง)';
  @Input() restrictLevel?: number; // Optional: restrict dropdown to specific level (e.g. 3)

  categories = signal<CategoryDto[]>([]);
  isLoading = signal<boolean>(false);
  selectedCategory = signal<CategoryDto | null>(null);
  showDropdown = signal<boolean>(false);

  private searchCache = new Map<string, CategoryDto[]>();
  private searchTrigger$ = new BehaviorSubject<string | null>(null);

  searchControl = new FormControl('');

  onChange: any = () => {};
  onTouched: any = () => {};

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event): void {
    if (!this.elRef.nativeElement.contains(event.target)) {
      this.showDropdown.set(false);
      this.onTouched();

      if (
        this.selectedCategory() &&
        this.searchControl.value !== this.selectedCategory()?.categoryName
      ) {
        this.searchControl.setValue(this.selectedCategory()!.categoryName, { emitEvent: false });
      } else if (!this.selectedCategory() && this.searchControl.value) {
        this.searchControl.setValue('', { emitEvent: false });
      }
    }
  }

  ngOnInit(): void {
    this.searchControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((query) => {
      if (typeof query === 'string') {
        this.searchTrigger$.next(query);
      }
    });

    this.searchTrigger$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
        tap((query) => {
          if (query !== null) {
            const trimmedQuery = query.trim();
            if (trimmedQuery.length >= 3 || trimmedQuery.length === 0) {
              if (!(this.selectedCategory() && query === this.selectedCategory()?.categoryName)) {
                this.isLoading.set(true);
              }
            }
          }
        }),
        switchMap((query) => {
          if (query === null) return of(null);

          if (this.selectedCategory() && query === this.selectedCategory()?.categoryName) {
            this.isLoading.set(false);
            return of(null);
          }

          const trimmedQuery = query.trim();
          if (trimmedQuery.length > 0 && trimmedQuery.length < 3) {
            this.categories.set([]);
            this.isLoading.set(false);
            return of(null);
          }

          if (this.searchCache.has(trimmedQuery)) {
            return of(this.searchCache.get(trimmedQuery)!);
          }

          return this.categoryService.searchCategories(trimmedQuery).pipe(
            tap((res) => this.searchCache.set(trimmedQuery, res)),
            catchError(() => {
              return of([] as CategoryDto[]);
            }),
          );
        }),
      )
      .subscribe((res) => {
        if (res !== null) {
          const filtered = this.restrictLevel
            ? res.filter((c: CategoryDto) => c.level === this.restrictLevel)
            : res;
          this.categories.set(filtered);
          this.isLoading.set(false);
          this.showDropdown.set(true);
        }
      });
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
    this.triggerSearch('');
  }

  onInputClick(): void {
    if (this.disabled) return;
    if (this.categories().length === 0) {
      this.triggerSearch(this.searchControl.value || '');
    } else {
      this.showDropdown.set(true);
    }
  }

  onSelectCategory(category: CategoryDto): void {
    this.selectedCategory.set(category);
    this.searchControl.setValue(category.categoryName, { emitEvent: false });
    this.showDropdown.set(false);
    this.onChange(category.categoryId);
    this.onTouched();
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

  triggerSearch(query: string = '') {
    if (this.disabled) return;
    this.searchTrigger$.next(query);
  }

  writeValue(obj: any): void {
    if (obj) {
      this.categoryService.getCategoryById(obj).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (cat) => {
          this.selectedCategory.set(cat);
          this.searchControl.setValue(cat.categoryName, { emitEvent: false });
        },
      });
    } else {
      this.selectedCategory.set(null);
      this.searchControl.setValue('', { emitEvent: false });
    }
  }
}
