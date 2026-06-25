export interface Investment {
  investmentId: string;
  investorId: string;
  investorName?: string;
  investmentType: number; // 0 = Equity, 1 = Loan
  principalAmount: number;
  currency: string;
  interestRate?: number;
  startDate: string;
  maturityDate?: string;
  status: number; // 0 = Active, 1 = Repaid, 2 = Defaulted, 3 = Cancelled
  contractUrl?: string;
  paymentProofUrl?: string;
  companyBankAccountId?: number;
  isCash: boolean;
  investorBankAccountId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface InvestmentSchedule {
  scheduleId: string;
  investmentId: string;
  installmentNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  paidAmount: number;
  status: number; // 0 = Pending, 1 = Paid, 2 = Overdue
  paymentDate?: string;
  companyBankAccountId?: number;
  isCash: boolean;
  slipUrl?: string;
  transactionId?: number;
  createdAt: string;
}

export interface InvestmentInterestSchedule {
  scheduleId: string;
  investmentId: string;
  startMonth: number;
  endMonth: number;
  interestRate: number;
}

export interface InvestmentDetailResponse {
  investment: Investment;
  schedules: InvestmentSchedule[];
  interestSchedules: InvestmentInterestSchedule[];
}

export interface InvestmentCreateRequest {
  investorId: string;
  investmentType: number;
  principalAmount: number;
  currency: string;
  interestRate?: number;
  startDate: string;
  maturityDate?: string;
  contractUrl?: string;
  paymentProofUrl?: string;
  companyBankAccountId?: number;
  isCash: boolean;
  investorBankAccountId?: string;
  schedules: {
    installmentNumber: number;
    dueDate: string;
    principalAmount: number;
    interestAmount: number;
  }[];
  interestSchedules: {
    startMonth: number;
    endMonth: number;
    interestRate: number;
  }[];
}

export interface InvestmentRepaymentRequest {
  paidAmount: number;
  companyBankAccountId?: number;
  isCash: boolean;
  slipUrl?: string;
}
