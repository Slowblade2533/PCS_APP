import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProductListItem, ProductSearchParams } from '../../../shared/models/product.model';
import { ProductService } from '../../../shared/services/product.service';

@Component({
  selector: 'app-products-list',
  imports: [FormsModule, DecimalPipe, RouterLink],
  templateUrl: './products-list.html',
  styleUrl: './products-list.css',
})
export class ProductsList implements OnInit {
  private productService = inject(ProductService);

  // State ของการค้นหาและ Pagination ควบคุมด้วย Signalsทั้งหมด
  searchTerm = signal<string>('');
  selectedCategory = signal<string>('');
  selectedStatus = signal<string>('');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(10);

  // ข้อมูลที่ได้มาจาก API
  products = signal<ProductListItem[]>([]);
  totalCount = signal<number>(0);
  totalPages = signal<number>(1);

  ngOnInit() {
    this.loadProducts();
  }

  // ฟังก์ชันยิง API ดึงข้อมูล
  loadProducts() {
    const filterParams: ProductSearchParams = {
      searchTerm: this.searchTerm(),
      categoryId: this.selectedCategory() ? Number(this.selectedCategory()) : null,
      productStatus: this.selectedStatus(),
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
    };

    this.productService.getProducts(filterParams).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
      },
      error: (err) => console.error('Error fetching products:', err),
    });
  }

  // เมื่อกดยึดตัวกรอง/ค้นหา ให้กลับไปหน้า 1 เสมอ
  onSearch() {
    this.pageNumber.set(1);
    this.loadProducts();
  }

  // เปลี่ยนหน้า Pagination
  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageNumber.set(page);
      this.loadProducts();
    }
  }
}
