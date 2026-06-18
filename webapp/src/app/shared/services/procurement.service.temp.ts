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
  PurchaseOrderCreatePayload,
  PurchaseOrderDetail,
  PurchaseOrderListItem,
  PurchaseOrderSearchParams,
  SalesOrderCreatePayload,
  SalesOrderDetail,
  SalesOrderListItem,
  SalesOrderSearchParams,
  StockConditionTransferPayload,
  StockScrapPayload,
} from '../models/procurement.models';

// ─── Sales Order Service ───────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class SalesOrderService {
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

// ─── Purchase Order Service ────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class PurchaseOrderService {
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

// ─── Goods Receipt Service ─────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class GoodsReceiptService {
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

// ─── Stock Adjustment Service ──────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class StockAdjustmentService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/stock`;

  transferCondition(payload: StockConditionTransferPayload): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/transfer-condition`, payload);
  }

  scrap(payload: StockScrapPayload): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/scrap`, payload);
  }
}
