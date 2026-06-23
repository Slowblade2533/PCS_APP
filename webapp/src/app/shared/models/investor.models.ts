export interface Investor {
  investorId: string;
  title: string;
  firstName: string;
  lastName: string;
  taxId: string;
  address: string;
  phone: string;
  email: string;
  createdAt: string;
}

export interface InvestorBankAccount {
  bankAccountId: string;
  investorId: string;
  bankName: string;
  accountNumber: string;
  accountType: string;
  isDefault: boolean;
  createdAt: string;
}

export interface InvestorDetailResponse {
  investor: Investor;
  bankAccounts: InvestorBankAccount[];
}

export interface InvestorCreateRequest {
  title: string;
  firstName: string;
  lastName: string;
  taxId: string;
  address: string;
  phone: string;
  email: string;
  bankAccounts: {
    bankAccountId?: string;
    bankName: string;
    accountNumber: string;
    accountType: string;
    isDefault: boolean;
  }[];
}
