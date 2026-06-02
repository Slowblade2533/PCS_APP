import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import {
  PagedResult,
  ProductCreatePayload,
  ProductCreateResponse,
  ProductListItem,
  ProductSearchParams,
} from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/products`;
  private readonly httpOptions = { withCredentials: true };

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

    httpParams = httpParams.set('pageNumber', params.pageNumber.toString());
    httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResult<ProductListItem>>(this.apiUrl, {
      params: httpParams,
      withCredentials: true,
    });
  }

  createProduct(payload: ProductCreatePayload): Observable<ProductCreateResponse> {
    return this.http.post<ProductCreateResponse>(this.apiUrl, payload, this.httpOptions);
  }
}
