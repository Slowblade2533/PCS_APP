import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import { VcbShipment, VcbShipmentCreate, VcbShipmentSearch } from '../models/vcb-shipments.models';
import { Result } from '../models/result.models';

@Injectable({
  providedIn: 'root',
})
export class VcbShipmentsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  createVcbShipment(dto: VcbShipmentCreate): Observable<Result<number>> {
    return this.http.post<Result<number>>(`${this.apiUrl}/vcb-shipments`, dto);
  }

  updateVcbShipment(id: number, dto: VcbShipmentCreate): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(`${this.apiUrl}/vcb-shipments/${id}`, dto);
  }

  getVcbShipmentById(id: number): Observable<Result<VcbShipment>> {
    return this.http.get<Result<VcbShipment>>(`${this.apiUrl}/vcb-shipments/${id}`);
  }

  getVcbShipments(search: VcbShipmentSearch): Observable<PagedResult<VcbShipment>> {
    let params = new HttpParams()
      .set('page', search.page.toString())
      .set('pageSize', search.pageSize.toString());

    if (search.searchTerm) params = params.set('searchTerm', search.searchTerm);
    if (search.status) params = params.set('status', search.status);
    if (search.deliveryId) params = params.set('deliveryId', search.deliveryId.toString());

    return this.http
      .get<any>(`${this.apiUrl}/vcb-shipments`, { params })
      .pipe(map((res) => (res.value ? res.value : res.data ? res.data : res)));
  }

  updateVcbShipmentStatus(id: number, status: string): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(
      `${this.apiUrl}/vcb-shipments/${id}/status`,
      `"${status}"`,
      {
        headers: { 'Content-Type': 'application/json' },
      },
    );
  }
}
