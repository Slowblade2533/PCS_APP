import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { ProductListItem, ProductSearchParams } from '../../../shared/models/product.models';
import { ProductService } from '../../../shared/services/product.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { environment } from '../../../../environments/environment';

import { ProductSearchFilter } from '../../../shared/components/product-search-filter/product-search-filter';

@Component({
  selector: 'app-products-list',
  imports: [FormsModule, RouterLink, PaginationComponent, ImageHoverPreview, ProductSearchFilter],
  templateUrl: './products-list.html',
  styleUrl: './products-list.css',
})
export class ProductsList implements OnInit {
  apiOrigin = environment.apiUrl;
  private productService = inject(ProductService);
  private destroyRef = inject(DestroyRef);

  searchTerm = signal<string>('');
  selectedCategory = signal<string>('');
  selectedStatus = signal<string>('');
  selectedType = signal<string>('');
  selectedInventoryGroup = signal<string>('');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(10);

  private readonly search$ = new Subject<string>();

  products = signal<ProductListItem[]>([]);
  totalCount = signal<number>(0);
  totalPages = signal<number>(1);

  private currentReq?: import('rxjs').Subscription;

  ngOnInit() {
    this.loadProducts();

    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.pageNumber.set(1);
        this.loadProducts();
      });
  }

  loadProducts() {
    if (this.currentReq) {
      this.currentReq.unsubscribe();
    }

    const filterParams: ProductSearchParams = {
      searchTerm: this.searchTerm(),
      categoryId: this.selectedCategory() ? Number(this.selectedCategory()) : null,
      productStatus: this.selectedStatus(),
      productType: this.selectedType(),
      inventoryGroup: this.selectedInventoryGroup(),
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
    };

    this.currentReq = this.productService
      .getProducts(filterParams)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.products.set(res.items);
          this.totalCount.set(res.totalCount);
          this.totalPages.set(res.totalPages);
        },
        error: (err) => console.error('Error fetching products:', err),
      });
  }

  onSearch() {
    this.pageNumber.set(1);
    this.loadProducts();
  }

  onSearchChange(value: string) {
    this.search$.next(value);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageNumber.set(page);
      this.loadProducts();
    }
  }
}
