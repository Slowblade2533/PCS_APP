import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  SalesOrderCreatePayload,
  SalesOrderDetail,
  SalesOrderListItem,
  SalesOrderSearchParams,
} from '../models/sales-orders.models';

@Injectable({ providedIn: 'root' })
export class SalesOrdersService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/sales-orders`;

  getAll(params: SalesOrderSearchParams): Observable<PagedResult<SalesOrderListItem>> {
    let p = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());
    if (params.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params.status) p = p.set('status', params.status);
    if (params.dateFrom) p = p.set('dateFrom', params.dateFrom);
    if (params.dateTo) p = p.set('dateTo', params.dateTo);
    return this.http.get<PagedResult<SalesOrderListItem>>(this.apiUrl, { params: p });
  }

  getById(id: number): Observable<SalesOrderDetail> {
    return this.http.get<SalesOrderDetail>(`${this.apiUrl}/${id}`);
  }

  create(payload: SalesOrderCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(this.apiUrl, payload);
  }

  complete(id: number, updatedBy?: number): Observable<unknown> {
    let p = new HttpParams();
    if (updatedBy) p = p.set('updatedBy', updatedBy.toString());
    return this.http.post(`${this.apiUrl}/${id}/complete`, {}, { params: p });
  }

  cancel(id: number, updatedBy?: number): Observable<unknown> {
    let p = new HttpParams();
    if (updatedBy) p = p.set('updatedBy', updatedBy.toString());
    return this.http.post(`${this.apiUrl}/${id}/cancel`, {}, { params: p });
  }
}
