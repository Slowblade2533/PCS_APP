import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  PurchaseOrderCreatePayload,
  PurchaseOrderDetail,
  PurchaseOrderListItem,
  PurchaseOrderSearchParams,
} from '../models/purchase-orders.models';

@Injectable({ providedIn: 'root' })
export class PurchaseOrdersService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/purchase-orders`;

  getAll(params: PurchaseOrderSearchParams): Observable<PagedResult<PurchaseOrderListItem>> {
    let p = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());
    if (params.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params.status) p = p.set('status', params.status);
    if (params.dateFrom) p = p.set('dateFrom', params.dateFrom);
    if (params.dateTo) p = p.set('dateTo', params.dateTo);
    return this.http.get<PagedResult<PurchaseOrderListItem>>(this.apiUrl, { params: p });
  }

  getById(id: number): Observable<PurchaseOrderDetail> {
    return this.http.get<PurchaseOrderDetail>(`${this.apiUrl}/${id}`);
  }

  create(payload: PurchaseOrderCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(this.apiUrl, payload);
  }

  updateStatus(id: number, status: string, updatedBy?: number): Observable<unknown> {
    return this.http.patch(`${this.apiUrl}/${id}/status`, { status, updatedBy });
  }

  updateSlip(id: number, slipUrl: string, updatedBy?: number): Observable<unknown> {
    return this.http.patch(`${this.apiUrl}/${id}/slip`, { slipUrl, updatedBy });
  }
}
