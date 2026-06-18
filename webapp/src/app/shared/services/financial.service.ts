import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  ChartOfAccount,
  ChartOfAccountCreatePayload,
  FinancialTransaction,
  FinancialTransactionCreatePayload,
  FinancialTransactionSearchParams,
  TaxInvoice,
  TaxInvoiceCreatePayload,
  TaxInvoiceSearchParams,
  TrialBalanceRow,
} from '../models/financial.models';
import { Branch } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class FinancialService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/financial`;

  // ─── Chart of Accounts ──────────────────────────────────────────────────────
  getAccounts(): Observable<ChartOfAccount[]> {
    return this.http.get<ChartOfAccount[]>(`${this.apiUrl}/accounts`);
  }

  getBranches(): Observable<Branch[]> {
    return this.http.get<Branch[]>(`${environment.apiUrl}/branches`);
  }

  getAccountById(id: number): Observable<ChartOfAccount> {
    return this.http.get<ChartOfAccount>(`${this.apiUrl}/accounts/${id}`);
  }

  createAccount(payload: ChartOfAccountCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(`${this.apiUrl}/accounts`, payload);
  }

  // ─── Tax Invoices ────────────────────────────────────────────────────────────
  getTaxInvoices(params: TaxInvoiceSearchParams): Observable<PagedResult<TaxInvoice>> {
    let p = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());
    if (params.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params.taxType) p = p.set('taxType', params.taxType);
    if (params.dateFrom) p = p.set('dateFrom', params.dateFrom);
    if (params.dateTo) p = p.set('dateTo', params.dateTo);
    return this.http.get<PagedResult<TaxInvoice>>(`${this.apiUrl}/tax-invoices`, { params: p });
  }

  getTaxInvoiceById(id: number): Observable<TaxInvoice> {
    return this.http.get<TaxInvoice>(`${this.apiUrl}/tax-invoices/${id}`);
  }

  createTaxInvoice(payload: TaxInvoiceCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(`${this.apiUrl}/tax-invoices`, payload);
  }

  // ─── Financial Transactions ──────────────────────────────────────────────────
  getTransactions(params: FinancialTransactionSearchParams): Observable<PagedResult<FinancialTransaction>> {
    let p = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());
    if (params.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params.transactionType) p = p.set('transactionType', params.transactionType);
    if (params.dateFrom) p = p.set('dateFrom', params.dateFrom);
    if (params.dateTo) p = p.set('dateTo', params.dateTo);
    return this.http.get<PagedResult<FinancialTransaction>>(`${this.apiUrl}/transactions`, { params: p });
  }

  getTransactionById(id: number): Observable<FinancialTransaction> {
    return this.http.get<FinancialTransaction>(`${this.apiUrl}/transactions/${id}`);
  }

  createTransaction(payload: FinancialTransactionCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(`${this.apiUrl}/transactions`, payload);
  }

  uploadAttachment(file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string }>(
      `${environment.apiUrl}/upload/transaction-attachment`,
      formData
    );
  }

  // ─── Reports ────────────────────────────────────────────────────────────────
  getTrialBalance(dateFrom?: string, dateTo?: string): Observable<TrialBalanceRow[]> {
    let p = new HttpParams();
    if (dateFrom) p = p.set('dateFrom', dateFrom);
    if (dateTo) p = p.set('dateTo', dateTo);
    return this.http.get<TrialBalanceRow[]>(`${this.apiUrl}/reports/trial-balance`, { params: p });
  }
}
