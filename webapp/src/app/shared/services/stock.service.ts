import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  StockItem,
  StockListQuery,
  StockTransaction,
  StockTransactionQuery,
  StockTransactionRequest,
} from '../models/stock.models';
import { Branch } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/stock`;

  getStocks(query: StockListQuery): Observable<PagedResult<StockItem>> {
    let params = new HttpParams().set('pageNumber', query.page).set('pageSize', query.pageSize);
    if (query.branchId) params = params.set('branchId', query.branchId);
    if (query.search) params = params.set('searchTerm', query.search);
    if (query.productStatus) params = params.set('productStatus', query.productStatus);
    if (query.productType) params = params.set('productType', query.productType);
    if (query.inventoryGroup) params = params.set('inventoryGroup', query.inventoryGroup);
    return this.http.get<PagedResult<StockItem>>(this.apiUrl, { params });
  }

  getTransactions(query: StockTransactionQuery): Observable<PagedResult<StockTransaction>> {
    let params = new HttpParams().set('pageNumber', query.page).set('pageSize', query.pageSize);
    if (query.variantId) params = params.set('variantId', query.variantId);
    if (query.branchId) params = params.set('branchId', query.branchId);
    if (query.transactionType) params = params.set('transactionType', query.transactionType);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    return this.http.get<PagedResult<StockTransaction>>(`${this.apiUrl}/transactions`, { params });
  }

  createTransaction(payload: StockTransactionRequest): Observable<StockTransaction> {
    return this.http.post<StockTransaction>(`${this.apiUrl}/transaction`, payload);
  }

  getBranches(): Observable<Branch[]> {
    return this.http.get<Branch[]>(`${environment.apiUrl}/branches`);
  }
}
