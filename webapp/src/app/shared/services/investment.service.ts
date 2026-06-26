import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Investment,
  InvestmentCreateRequest,
  InvestmentDetailResponse,
  InvestmentRepaymentRequest,
} from '../models/investment.models';

@Injectable({ providedIn: 'root' })
export class InvestmentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/investments`;

  getInvestments(): Observable<Investment[]> {
    return this.http.get<Investment[]>(this.apiUrl);
  }

  getInvestmentById(id: string): Observable<InvestmentDetailResponse> {
    return this.http.get<InvestmentDetailResponse>(`${this.apiUrl}/${id}`);
  }

  createInvestment(payload: InvestmentCreateRequest): Observable<{ investmentId: string }> {
    return this.http.post<{ investmentId: string }>(this.apiUrl, payload);
  }

  updateInvestment(id: string, payload: InvestmentCreateRequest): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.apiUrl}/${id}`, payload);
  }

  deleteInvestment(id: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${id}`);
  }

  payInstallment(
    investmentId: string,
    scheduleId: string,
    payload: InvestmentRepaymentRequest,
  ): Observable<{ success: boolean }> {
    return this.http.post<{ success: boolean }>(
      `${this.apiUrl}/${investmentId}/installments/${scheduleId}/pay`,
      payload,
    );
  }
}
