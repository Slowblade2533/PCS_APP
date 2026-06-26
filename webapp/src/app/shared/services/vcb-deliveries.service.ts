import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import { VcbDelivery, VcbDeliverySearch, VcbDeliveryCreate } from '../models/vcb-deliveries.models';
import { Result } from '../models/result.models';

@Injectable({
  providedIn: 'root',
})
export class VcbDeliveriesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  createVcbDelivery(formData: FormData): Observable<Result<number>> {
    return this.http.post<Result<number>>(`${this.apiUrl}/vcb-deliveries`, formData);
  }

  updateVcbDelivery(id: number, formData: FormData): Observable<Result<number>> {
    return this.http.put<Result<number>>(`${this.apiUrl}/vcb-deliveries/${id}`, formData);
  }

  getVcbDeliveryById(id: number): Observable<Result<VcbDelivery>> {
    return this.http.get<Result<VcbDelivery>>(`${this.apiUrl}/vcb-deliveries/${id}`);
  }

  getVcbDeliveries(search: VcbDeliverySearch): Observable<PagedResult<VcbDelivery>> {
    let params = new HttpParams()
      .set('page', search.page.toString())
      .set('pageSize', search.pageSize.toString());

    if (search.searchTerm) params = params.set('searchTerm', search.searchTerm);
    if (search.status) params = params.set('status', search.status);

    return this.http
      .get<any>(`${this.apiUrl}/vcb-deliveries`, { params })
      .pipe(map((res) => (res.value ? res.value : res.data ? res.data : res)));
  }

  updateVcbDeliveryStatus(id: number, status: string): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(
      `${this.apiUrl}/vcb-deliveries/${id}/status`,
      `"${status}"`,
      {
        headers: { 'Content-Type': 'application/json' },
      },
    );
  }
}
