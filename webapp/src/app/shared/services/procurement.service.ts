import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  VcbOrder,
  VcbOrderCreate,
  VcbOrderSearch,
  VcbShipment,
  VcbShipmentCreate,
  VcbShipmentSearch,
  VcbDelivery,
  VcbDeliverySearch,
} from '../models/procurement.models';
import { Result } from '../models/result.models';

@Injectable({
  providedIn: 'root',
})
export class ProcurementService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = environment.apiUrl;

  createVcbOrder(formData: FormData): Observable<Result<number>> {
    return this.http.post<Result<number>>(`${this.apiUrl}/VcbOrders`, formData);
  }

  createVcbShipment(dto: VcbShipmentCreate): Observable<Result<number>> {
    return this.http.post<Result<number>>(`${this.apiUrl}/VcbShipments`, dto);
  }

  updateVcbShipment(id: number, dto: VcbShipmentCreate): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(`${this.apiUrl}/VcbShipments/${id}`, dto);
  }

  getVcbOrderById(id: number): Observable<Result<VcbOrder>> {
    return this.http.get<Result<VcbOrder>>(`${this.apiUrl}/VcbOrders/${id}`);
  }

  getVcbOrders(search: VcbOrderSearch): Observable<PagedResult<VcbOrder>> {
    let params = new HttpParams()
      .set('page', search.page.toString())
      .set('pageSize', search.pageSize.toString());

    if (search.searchTerm) params = params.set('searchTerm', search.searchTerm);
    if (search.status) params = params.set('status', search.status);
    if (search.branchId) params = params.set('branchId', search.branchId.toString());

    return this.http
      .get<any>(`${this.apiUrl}/VcbOrders`, { params })
      .pipe(map((res) => (res.value ? res.value : res.data ? res.data : res)));
  }

  getVcbShipmentById(id: number): Observable<Result<VcbShipment>> {
    return this.http.get<Result<VcbShipment>>(`${this.apiUrl}/VcbShipments/${id}`);
  }

  getVcbShipments(search: VcbShipmentSearch): Observable<PagedResult<VcbShipment>> {
    let params = new HttpParams()
      .set('page', search.page.toString())
      .set('pageSize', search.pageSize.toString());

    if (search.searchTerm) params = params.set('searchTerm', search.searchTerm);
    if (search.status) params = params.set('status', search.status);
    if (search.deliveryId) params = params.set('deliveryId', search.deliveryId.toString());

    return this.http
      .get<any>(`${this.apiUrl}/VcbShipments`, { params })
      .pipe(map((res) => (res.value ? res.value : res.data ? res.data : res)));
  }

  updateVcbOrder(id: number, formData: FormData): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(`${this.apiUrl}/VcbOrders/${id}`, formData);
  }

  updateVcbOrderStatus(id: number, status: string): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(`${this.apiUrl}/VcbOrders/${id}/status`, `"${status}"`, {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  updateVcbShipmentStatus(id: number, status: string): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(
      `${this.apiUrl}/VcbShipments/${id}/status`,
      `"${status}"`,
      {
        headers: { 'Content-Type': 'application/json' },
      },
    );
  }

  createVcbDelivery(formData: FormData): Observable<Result<number>> {
    return this.http.post<Result<number>>(`${this.apiUrl}/VcbDeliveries`, formData);
  }

  updateVcbDelivery(id: number, formData: FormData): Observable<Result<number>> {
    return this.http.put<Result<number>>(`${this.apiUrl}/VcbDeliveries/${id}`, formData);
  }

  getVcbDeliveryById(id: number): Observable<Result<VcbDelivery>> {
    return this.http.get<Result<VcbDelivery>>(`${this.apiUrl}/VcbDeliveries/${id}`);
  }

  getVcbDeliveries(search: VcbDeliverySearch): Observable<PagedResult<VcbDelivery>> {
    let params = new HttpParams()
      .set('page', search.page.toString())
      .set('pageSize', search.pageSize.toString());

    if (search.searchTerm) params = params.set('searchTerm', search.searchTerm);
    if (search.status) params = params.set('status', search.status);

    return this.http
      .get<any>(`${this.apiUrl}/VcbDeliveries`, { params })
      .pipe(map((res) => (res.value ? res.value : res.data ? res.data : res)));
  }

  updateVcbDeliveryStatus(id: number, status: string): Observable<Result<boolean>> {
    return this.http.put<Result<boolean>>(
      `${this.apiUrl}/VcbDeliveries/${id}/status`,
      `"${status}"`,
      {
        headers: { 'Content-Type': 'application/json' },
      },
    );
  }
}
