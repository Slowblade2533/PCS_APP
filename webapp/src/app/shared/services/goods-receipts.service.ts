import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  GoodsReceiptCreatePayload,
  GoodsReceiptDetail,
  GoodsReceiptListItem,
  GoodsReceiptSearchParams,
} from '../models/goods-receipts.models';

@Injectable({ providedIn: 'root' })
export class GoodsReceiptsService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/goods-receipts`;

  getAll(params: GoodsReceiptSearchParams): Observable<PagedResult<GoodsReceiptListItem>> {
    let p = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());
    if (params.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params.status) p = p.set('status', params.status);
    if (params.purchaseOrderId) p = p.set('purchaseOrderId', params.purchaseOrderId.toString());
    return this.http.get<PagedResult<GoodsReceiptListItem>>(this.apiUrl, { params: p });
  }

  getById(id: number): Observable<GoodsReceiptDetail> {
    return this.http.get<GoodsReceiptDetail>(`${this.apiUrl}/${id}`);
  }

  create(payload: GoodsReceiptCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(this.apiUrl, payload);
  }

  complete(id: number, updatedBy?: number): Observable<unknown> {
    let p = new HttpParams();
    if (updatedBy) p = p.set('updatedBy', updatedBy.toString());
    return this.http.post(`${this.apiUrl}/${id}/complete`, {}, { params: p });
  }
}
