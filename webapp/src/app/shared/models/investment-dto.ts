// src/app/models/investment-dto.ts
export interface InvestmentDto {
  investmentId: string;
  investorId: string;
  investmentType: number; // 0 = Equity, 1 = Loan
  principalAmount: number;
  currency: string;
  interestRate?: number;
  startDate: string;
  maturityDate?: string;
  status: number; // 0 = Active, 1 = Repaid, 2 = Defaulted, 3 = Cancelled
  createdAt: string;
  updatedAt: string;
}
