import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  ProductCreatePayload,
  ProductCreateResponse,
  ProductDetail,
  ProductListItem,
  ProductSearchParams,
} from '../models/product.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/products`;

  createProduct(payload: ProductCreatePayload): Observable<ProductCreateResponse> {
    return this.http.post<ProductCreateResponse>(this.apiUrl, payload);
  }

  getProductById(id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.apiUrl}/${id}`);
  }

  getProducts(params: ProductSearchParams): Observable<PagedResult<ProductListItem>> {
    let httpParams = new HttpParams();

    if (params.searchTerm) {
      httpParams = httpParams.set('searchTerm', params.searchTerm);
    }

    if (params.categoryId) {
      httpParams = httpParams.set('categoryId', params.categoryId.toString());
    }

    if (params.productStatus) {
      httpParams = httpParams.set('productStatus', params.productStatus);
    }

    if (params.productType) {
      httpParams = httpParams.set('productType', params.productType);
    }

    if (params.inventoryGroup) {
      httpParams = httpParams.set('inventoryGroup', params.inventoryGroup);
    }

    httpParams = httpParams.set('pageNumber', params.pageNumber.toString());
    httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResult<ProductListItem>>(this.apiUrl, {
      params: httpParams,
    });
  }

  updateProduct(id: number, payload: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, payload);
  }

  uploadImage(file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string }>(
      `${environment.apiUrl}/upload/product-image`,
      formData,
    );
  }
}
