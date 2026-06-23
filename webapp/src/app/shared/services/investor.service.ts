import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Investor, InvestorCreateRequest, InvestorDetailResponse } from '../models/investor.models';

@Injectable({ providedIn: 'root' })
export class InvestorService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/investors`;

  getInvestors(): Observable<Investor[]> {
    return this.http.get<Investor[]>(this.apiUrl);
  }

  getInvestorById(id: string): Observable<InvestorDetailResponse> {
    return this.http.get<InvestorDetailResponse>(`${this.apiUrl}/${id}`);
  }

  createInvestor(payload: InvestorCreateRequest): Observable<{ investorId: string }> {
    return this.http.post<{ investorId: string }>(this.apiUrl, payload);
  }

  updateInvestor(id: string, payload: InvestorCreateRequest): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.apiUrl}/${id}`, payload);
  }

  deleteInvestor(id: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${id}`);
  }
}
