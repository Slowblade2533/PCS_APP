import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  ChartOfAccount,
  ChartOfAccountCreatePayload,
  FinancialTransaction,
  FinancialTransactionPagedResult,
  FinancialTransactionCreatePayload,
  FinancialTransactionSearchParams,
  TaxInvoice,
  TaxInvoiceCreatePayload,
  TaxInvoiceSearchParams,
  TrialBalanceRow,
  GeneralJournalRow,
  GeneralLedgerRow,
  ProfitAndLossReport,
  BalanceSheetReport,
  PartnerBankAccount,
  PartnerBankAccountCreatePayload,
  CompanyBankAccount,
  CompanyBankAccountCreatePayload,
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
  getTransactions(
    params: FinancialTransactionSearchParams,
  ): Observable<FinancialTransactionPagedResult> {
    let p = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());
    if (params.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params.transactionType) p = p.set('transactionType', params.transactionType);
    if (params.dateFrom) p = p.set('dateFrom', params.dateFrom);
    if (params.dateTo) p = p.set('dateTo', params.dateTo);
    return this.http.get<FinancialTransactionPagedResult>(`${this.apiUrl}/transactions`, {
      params: p,
    });
  }

  getTransactionById(id: number): Observable<FinancialTransaction> {
    return this.http.get<FinancialTransaction>(`${this.apiUrl}/transactions/${id}`);
  }

  createTransaction(payload: FinancialTransactionCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(`${this.apiUrl}/transactions`, payload);
  }

  updateTransaction(
    id: number,
    payload: FinancialTransactionCreatePayload,
  ): Observable<{ value: number }> {
    return this.http.put<{ value: number }>(`${this.apiUrl}/transactions/${id}`, payload);
  }

  uploadAttachment(file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string }>(
      `${environment.apiUrl}/upload/transaction-attachment`,
      formData,
    );
  }

  // ─── Bank Accounts ──────────────────────────────────────────────────────────
  getPartnerBankAccounts(
    partnerName?: string,
    supplierId?: number,
  ): Observable<PartnerBankAccount[]> {
    let p = new HttpParams();
    if (partnerName) p = p.set('partnerName', partnerName);
    if (supplierId) p = p.set('supplierId', supplierId.toString());
    return this.http.get<PartnerBankAccount[]>(`${environment.apiUrl}/bank-accounts/partner`, {
      params: p,
    });
  }

  savePartnerBankAccount(payload: PartnerBankAccountCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(
      `${environment.apiUrl}/bank-accounts/partner`,
      payload,
    );
  }

  deletePartnerBankAccount(id: number): Observable<{ value: boolean }> {
    return this.http.delete<{ value: boolean }>(
      `${environment.apiUrl}/bank-accounts/partner/${id}`,
    );
  }

  getCompanyBankAccounts(activeOnly: boolean = false): Observable<CompanyBankAccount[]> {
    let params = new HttpParams();
    if (activeOnly) {
      params = params.set('activeOnly', 'true');
    }
    return this.http.get<CompanyBankAccount[]>(`${environment.apiUrl}/bank-accounts/company`, {
      params,
    });
  }

  saveCompanyBankAccount(payload: CompanyBankAccountCreatePayload): Observable<{ value: number }> {
    return this.http.post<{ value: number }>(
      `${environment.apiUrl}/bank-accounts/company`,
      payload,
    );
  }

  updateCompanyBankAccount(
    id: number,
    payload: CompanyBankAccountCreatePayload,
  ): Observable<{ value: boolean }> {
    return this.http.put<{ value: boolean }>(
      `${environment.apiUrl}/bank-accounts/company/${id}`,
      payload,
    );
  }

  deleteCompanyBankAccount(id: number): Observable<{ value: boolean }> {
    return this.http.delete<{ value: boolean }>(
      `${environment.apiUrl}/bank-accounts/company/${id}`,
    );
  }

  // ─── Reports ────────────────────────────────────────────────────────────────
  getTrialBalance(dateFrom?: string, dateTo?: string): Observable<TrialBalanceRow[]> {
    let p = new HttpParams();
    if (dateFrom) p = p.set('dateFrom', dateFrom);
    if (dateTo) p = p.set('dateTo', dateTo);
    return this.http.get<TrialBalanceRow[]>(`${this.apiUrl}/reports/trial-balance`, { params: p });
  }

  getGeneralJournal(dateFrom?: string, dateTo?: string): Observable<GeneralJournalRow[]> {
    let p = new HttpParams();
    if (dateFrom) p = p.set('dateFrom', dateFrom);
    if (dateTo) p = p.set('dateTo', dateTo);
    return this.http.get<GeneralJournalRow[]>(`${this.apiUrl}/reports/general-journal`, {
      params: p,
    });
  }

  getCashBroughtForward(dateFrom?: string): Observable<number> {
    let p = new HttpParams();
    if (dateFrom) p = p.set('dateFrom', dateFrom);
    return this.http.get<number>(`${this.apiUrl}/reports/general-journal/cash-brought-forward`, {
      params: p,
    });
  }

  getGeneralLedger(
    accountId: number,
    dateFrom?: string,
    dateTo?: string,
  ): Observable<GeneralLedgerRow[]> {
    let p = new HttpParams().set('accountId', accountId.toString());
    if (dateFrom) p = p.set('dateFrom', dateFrom);
    if (dateTo) p = p.set('dateTo', dateTo);
    return this.http.get<GeneralLedgerRow[]>(`${this.apiUrl}/reports/general-ledger`, {
      params: p,
    });
  }

  getProfitAndLoss(dateFrom?: string, dateTo?: string): Observable<ProfitAndLossReport> {
    let p = new HttpParams();
    if (dateFrom) p = p.set('dateFrom', dateFrom);
    if (dateTo) p = p.set('dateTo', dateTo);
    return this.http.get<ProfitAndLossReport>(`${this.apiUrl}/reports/profit-loss`, { params: p });
  }

  getBalanceSheet(asOfDate?: string): Observable<BalanceSheetReport> {
    let p = new HttpParams();
    if (asOfDate) p = p.set('asOfDate', asOfDate);
    return this.http.get<BalanceSheetReport>(`${this.apiUrl}/reports/balance-sheet`, { params: p });
  }
}
