import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import { VcbOrder, VcbOrderSearch, VcbOrderCreate } from '../models/vcb-orders.models';
import { Result } from '../models/result.models';

@Injectable({
  providedIn: 'root',
})
export class VcbOrdersService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  createVcbOrder(formData: FormData): Observable<Result<number>> {
    return this.http.post<Result<number>>(`${this.apiUrl}/vcb-orders`, formData);
  }

  getVcbOrderById(id: number): Observable<Result<VcbOrder>> {
    return this.http.get<Result<VcbOrder>>(`${this.apiUrl}/vcb-orders/${id}`);
  }

  getVcbOrders(search: VcbOrderSearch): Observable<PagedResult<VcbOrder>> {
    let params = new HttpParams()
      .set('page', search.page.toString())
      .set('pageSize', search.pageSize.toString());

    if (search.searchTerm) params = params.set('searchTerm', search.searchTerm);
    if (search.status) params = params.set('status', search.status);
    if (search.branchId) params = params.set('branchId', search.branchId.toString());

    return this.http
      .get<any>(`${this.apiUrl}/vcb-orders`, { params })
      .pipe(map((res) => (res.value ? res.value : res.data ? res.data : res)));
  }

  updateVcbOrder(id: number, formData: FormData): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(`${this.apiUrl}/vcb-orders/${id}`, formData);
  }

  updateVcbOrderStatus(id: number, status: string): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(`${this.apiUrl}/vcb-orders/${id}/status`, `"${status}"`, {
      headers: { 'Content-Type': 'application/json' },
    });
  }
}
