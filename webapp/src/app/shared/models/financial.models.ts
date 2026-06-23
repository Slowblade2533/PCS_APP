// ─── Chart of Accounts ────────────────────────────────────────────────────────
export type AccountType = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';

export interface ChartOfAccount {
  accountId: number;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  normalBalance: 'Debit' | 'Credit';
  isSystemAccount: boolean;
  description?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface ChartOfAccountCreatePayload {
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  normalBalance: 'Debit' | 'Credit';
  description?: string;
  sortOrder?: number;
}

// ─── Tax Invoice ──────────────────────────────────────────────────────────────
export type TaxType = 'INPUT' | 'OUTPUT';

export interface TaxInvoice {
  taxInvoiceId: number;
  taxInvoiceNo: string;
  taxInvoiceDate: string;
  taxType: TaxType;
  partnerName?: string;
  partnerTaxId?: string;
  partnerAddress?: string;
  baseAmount: number;
  vatRate: number;
  vatAmount: number;
  totalAmount: number;
  documentUrl?: string;
  notes?: string;
  createdAt: string;
}

export interface TaxInvoiceCreatePayload {
  taxInvoiceNo: string;
  taxInvoiceDate: string;
  taxType: TaxType;
  partnerName?: string;
  partnerTaxId?: string;
  partnerAddress?: string;
  baseAmount: number;
  vatRate?: number;
  vatAmount: number;
  totalAmount: number;
  documentUrl?: string;
  notes?: string;
  createdBy?: number;
}

export interface TaxInvoiceSearchParams {
  searchTerm?: string;
  taxType?: TaxType | '';
  dateFrom?: string;
  dateTo?: string;
  pageNumber: number;
  pageSize: number;
}

// ─── Financial Transaction ────────────────────────────────────────────────────
export type TransactionType =
  | 'INVESTMENT'
  | 'SALES'
  | 'PURCHASE_VCB'
  | 'FREIGHT_VCB'
  | 'PURCHASE_GENERAL'
  | 'FREIGHT_GENERAL'
  | 'EXPENSE'
  | 'STOCK_LOSS'
  | 'SCRAP'
  | 'TRANSFER_IN'
  | 'RECEIPT';

export interface FinancialTransaction {
  transactionId: number;
  transactionDate: string;
  transactionType: TransactionType;
  transactionTypeLabel?: string;
  referenceType?: string;
  referenceId?: number;
  description?: string;
  totalAmount: number;
  paymentMethod?: string;
  paymentRefNo?: string;
  sourceAccountInfo?: string;
  receiverAccountName?: string;
  attachmentUrl?: string;
  receivedByName?: string;
  createdAt: string;

  // New audit and document fields
  status?: string;
  branchId?: number;
  branchName?: string;
  postedAt?: string;
  postedBy?: number;
  postedByName?: string;
  voidedAt?: string;
  voidedBy?: number;
  voidedByName?: string;
  documentNo?: string;
  partnerName?: string;
  slipDateTime?: string;
  originBank?: string;
  destinationBank?: string;
  sourceAccountNo?: string;
  destinationAccountNo?: string;
  sourceAccountName?: string;
  destinationAccountName?: string;

  ledgerEntries: LedgerEntry[];
}

export interface LedgerEntry {
  entryId: number;
  accountId: number;
  accountCode: string;
  accountName: string;
  debitAmount: number;
  creditAmount: number;
  memo?: string;
}

export interface LedgerEntryCreatePayload {
  accountId: number;
  debitAmount: number;
  creditAmount: number;
  memo?: string;
}

export interface FinancialTransactionCreatePayload {
  transactionDate: string;
  transactionType: TransactionType;
  referenceType?: string;
  referenceId?: number;
  taxInvoiceId?: number;
  description?: string;
  totalAmount: number;
  paymentMethod?: string;      // CASH, TRANSFER, CREDIT
  sourceAccountInfo?: string;  // บัญชีต้นทาง
  paymentRefNo?: string;       // เลขสลิปโอนเงิน
  receiverAccountId?: number;
  attachmentUrl?: string;      // ไม่บังคับ
  receivedBy?: number;
  createdBy?: number;

  // New audit and document fields
  status?: string;
  branchId?: number;
  postedAt?: string;
  postedBy?: number;
  voidedAt?: string;
  voidedBy?: number;
  documentNo?: string;
  partnerName?: string;
  slipDateTime?: string;
  originBank?: string;
  destinationBank?: string;
  sourceAccountNo?: string;
  destinationAccountNo?: string;
  sourceAccountName?: string;
  destinationAccountName?: string;

  ledgerEntries: LedgerEntryCreatePayload[];
}

export interface FinancialTransactionSearchParams {
  searchTerm?: string;
  transactionType?: string;
  dateFrom?: string;
  dateTo?: string;
  pageNumber: number;
  pageSize: number;
}

// ─── Trial Balance (งบทดลอง) ─────────────────────────────────────────────────
export interface TrialBalanceRow {
  accountId: number;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  totalDebit: number;
  totalCredit: number;
  balance: number;
}

// ─── General Journal ───────────────────────────────────────────────────────
export interface GeneralJournalRow {
  entryId: number;
  transactionId: number;
  transactionDate: string;
  documentNo?: string;
  transactionType: string;
  transactionTypeLabel?: string;
  memo?: string;
  accountId: number;
  accountCode: string;
  accountName: string;
  debitAmount: number;
  creditAmount: number;
}

// ─── General Ledger ────────────────────────────────────────────────────────
export interface GeneralLedgerRow {
  transactionDate?: string;
  documentNo?: string;
  memo?: string;
  debitAmount: number;
  creditAmount: number;
  runningBalance: number;
  isBroughtForward: boolean;
}

// ─── Profit & Loss ─────────────────────────────────────────────────────────
export interface ProfitAndLossReport {
  dateFrom?: string;
  dateTo?: string;
  revenueLines: ProfitAndLossLine[];
  totalRevenue: number;
  expenseLines: ProfitAndLossLine[];
  totalExpense: number;
  netProfit: number;
}

export interface ProfitAndLossLine {
  accountId: number;
  accountCode: string;
  accountName: string;
  balance: number;
}

// ─── Balance Sheet ─────────────────────────────────────────────────────────
export interface BalanceSheetReport {
  asOfDate: string;
  assetLines: BalanceSheetLine[];
  totalAssets: number;
  liabilityLines: BalanceSheetLine[];
  totalLiabilities: number;
  equityLines: BalanceSheetLine[];
  totalEquity: number;
  totalLiabilitiesAndEquity: number;
}

export interface BalanceSheetLine {
  accountId: number;
  accountCode: string;
  accountName: string;
  balance: number;
}

// ─── Bank Accounts ────────────────────────────────────────────────────────────
export interface PartnerBankAccount {
  id: number;
  supplierId?: number;
  partnerName: string;
  bankName: string;
  accountNo: string;
  accountName: string;
  isDefault: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface PartnerBankAccountCreatePayload {
  supplierId?: number;
  partnerName: string;
  bankName: string;
  accountNo: string;
  accountName: string;
  isDefault?: boolean;
}

export interface CompanyBankAccount {
  id: number;
  bankName: string;
  accountNo: string;
  accountName: string;
  chartOfAccountId?: number;
  isActive: boolean;
  createdAt: string;
}

export interface CompanyBankAccountCreatePayload {
  bankName: string;
  accountNo: string;
  accountName: string;
  chartOfAccountId?: number;
}
