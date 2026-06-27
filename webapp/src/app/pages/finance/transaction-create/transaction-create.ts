import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed, effect, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';
import {
  ChartOfAccount,
  FinancialTransaction,
  FinancialTransactionCreatePayload,
  LedgerEntryCreatePayload,
  TransactionType,
  PartnerBankAccount,
  CompanyBankAccount,
} from '../../../shared/models/financial.models';
import { Branch } from '../../../shared/models/user.models';
import { FinancialService } from '../../../shared/services/financial.service';
import { BankSelectComponent } from '../../../shared/components/bank-select/bank-select';
import { bankLists, Bank } from '../../../shared/constants/banks.constants';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';

@Component({
  selector: 'app-transaction-create',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe, BankSelectComponent, ImageHoverPreview],
  templateUrl: './transaction-create.html',
})
export class TransactionCreate implements OnInit {
  private financialService = inject(FinancialService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  apiOrigin = environment.apiUrl;
  selectedFile = signal<File | null>(null);
  selectedFileName = signal<string>('');
  imagePreview = signal<string | null>(null);

  transactionId = signal<number | null>(null);
  isViewMode = signal<boolean>(false);

  accountsResource = rxResource({
    stream: () => this.financialService.getAccounts().pipe(catchError(() => of([]))),
  });
  accounts = computed(() => this.accountsResource.value()?.filter((a) => a.isActive) || []);

  branchesResource = rxResource({
    stream: () => this.financialService.getBranches().pipe(catchError(() => of([]))),
  });
  branches = computed(() => this.branchesResource.value()?.filter((b) => b.isActive) || []);

  // Form State
  transactionDate = signal<string>(new Date().toLocaleDateString('en-CA'));
  transactionType = signal<TransactionType>('EXPENSE');
  description = signal<string>('');
  paymentMethod = signal<string>('CASH');
  paymentRefNo = signal<string>('');
  sourceAccountInfo = signal<string>('');
  receiverAccountId = signal<number | null>(null);
  attachmentUrl = signal<string>('');

  // New audit and document fields
  documentNo = signal<string>('');
  partnerName = signal<string>('');
  status = signal<string>('POSTED'); // default to POSTED
  slipDateTime = signal<string>('');
  originBank = signal<string>('');
  destinationBank = signal<string>('');
  branchId = signal<number | null>(null);

  // Bank Account State
  sourceAccountNo = signal<string>('');
  destinationAccountNo = signal<string>('');
  sourceAccountName = signal<string>('');
  destinationAccountName = signal<string>('');
  partnerBankAccountsResource = rxResource({
    params: () => this.partnerName(),
    stream: ({ params }) => {
      if (!params || params.trim().length === 0) return of([]);
      return this.financialService
        .getPartnerBankAccounts(params.trim())
        .pipe(catchError(() => of([])));
    },
  });
  partnerBankAccounts = computed(() => this.partnerBankAccountsResource.value() || []);

  companyBankAccountsResource = rxResource({
    stream: () => this.financialService.getCompanyBankAccounts().pipe(catchError(() => of([]))),
  });
  companyBankAccounts = computed(() => this.companyBankAccountsResource.value() || []);
  saveSourceAccountOnTheFly = signal<boolean>(false);
  saveDestinationAccountOnTheFly = signal<boolean>(false);

  // Template and calculation state
  inputAmount = signal<number>(0);
  autoGenerateLedger = signal<boolean>(true);

  ledgerEntries = signal<LedgerEntryCreatePayload[]>([
    { accountId: 0, debitAmount: 0, creditAmount: 0, memo: '' },
    { accountId: 0, debitAmount: 0, creditAmount: 0, memo: '' },
  ]);

  detailResource = rxResource({
    params: () => this.transactionId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.financialService.getTransactionById(params).pipe(catchError(() => of(null)));
    },
  });
  detail = computed(() => this.detailResource.value());

  loading = computed(
    () =>
      this.accountsResource.isLoading() ||
      this.branchesResource.isLoading() ||
      this.companyBankAccountsResource.isLoading() ||
      this.detailResource.isLoading() ||
      this.partnerBankAccountsResource.isLoading(),
  );
  submitting = signal<boolean>(false);
  error = signal<string | null>(null);

  typeOptions: { value: TransactionType; label: string }[] = [
    { value: 'INVESTMENT', label: 'รับเงินลงทุน' },
    { value: 'SALES', label: 'รายได้จากการขาย' },
    { value: 'PURCHASE_GENERAL', label: 'ซื้อสินค้าทั่วไป' },
    { value: 'PURCHASE_VCB', label: 'สั่งซื้อ VCANBUY' },
    { value: 'FREIGHT_VCB', label: 'ค่าขนส่ง VCANBUY' },
    { value: 'FREIGHT_GENERAL', label: 'ค่าขนส่งทั่วไป' },
    { value: 'EXPENSE', label: 'ค่าใช้จ่าย' },
    { value: 'STOCK_LOSS', label: 'สินค้าสูญหาย' },
    { value: 'SCRAP', label: 'ตัดจำหน่ายทิ้ง' },
    { value: 'TRANSFER_IN', label: 'รับโอนเงิน' },
    { value: 'RECEIPT', label: 'รับเงินเข้า' },
    { value: 'GOODS_RECEIPT', label: 'ใบรับสินค้า' },
  ];

  constructor() {
    effect(() => {
      const accs = this.accounts();
      if (accs.length > 0 && !this.transactionId() && this.autoGenerateLedger()) {
        untracked(() => this.applyTemplate());
      }
    });

    effect(() => {
      const brs = this.branches();
      if (!this.isViewMode() && brs.length > 0 && !this.branchId()) {
        this.branchId.set(brs[0].id);
      }
    });
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.transactionId.set(Number(id));
      this.isViewMode.set(true);
    } else {
      this.slipDateTime.set(this.getCurrentLocalDateTimeString());
    }
  }

  editTransaction() {
    const d = this.detail();
    if (!d) return;

    this.transactionDate.set(d.transactionDate.split('T')[0]);
    this.transactionType.set(d.transactionType);
    this.description.set(d.description || '');
    this.paymentMethod.set(d.paymentMethod || 'CASH');
    this.paymentRefNo.set(d.paymentRefNo || '');
    this.sourceAccountInfo.set(d.sourceAccountInfo || '');
    this.attachmentUrl.set(d.attachmentUrl || '');

    this.documentNo.set(d.documentNo || '');
    this.partnerName.set(d.partnerName || '');
    this.status.set(d.status || 'POSTED');
    if (d.slipDateTime) {
      this.slipDateTime.set(d.slipDateTime.substring(0, 16));
    } else {
      this.slipDateTime.set(this.getCurrentLocalDateTimeString());
    }
    this.originBank.set(d.originBank || '');
    this.destinationBank.set(d.destinationBank || '');
    this.sourceAccountNo.set(d.sourceAccountNo || '');
    this.destinationAccountNo.set(d.destinationAccountNo || '');
    this.sourceAccountName.set(d.sourceAccountName || '');
    this.destinationAccountName.set(d.destinationAccountName || '');
    this.branchId.set(d.branchId || null);
    if (d.partnerName) {
      this.onPartnerNameChange(d.partnerName);
    }

    this.inputAmount.set(d.totalAmount);
    this.autoGenerateLedger.set(false);

    if (d.ledgerEntries && d.ledgerEntries.length > 0) {
      this.ledgerEntries.set(
        d.ledgerEntries.map((e) => ({
          accountId: e.accountId,
          debitAmount: e.debitAmount,
          creditAmount: e.creditAmount,
          memo: e.memo || '',
        })),
      );
    }

    this.isViewMode.set(false);
  }

  cancel() {
    if (this.transactionId()) {
      this.isViewMode.set(true);
    } else {
      this.router.navigate(['/finance/transactions']);
    }
  }

  getBank(symbol: string): Bank | null {
    if (!symbol) return null;
    return bankLists[symbol] || null;
  }

  // Template auto-generation logic
  onTypeChange(newType: TransactionType) {
    this.transactionType.set(newType);
    if (this.autoGenerateLedger()) {
      this.applyTemplate();
    }
  }

  onPaymentMethodChange(newMethod: string) {
    this.paymentMethod.set(newMethod);
    if (this.autoGenerateLedger()) {
      this.applyTemplate();
    }
  }

  onAmountChange(newAmount: number) {
    this.inputAmount.set(newAmount);
    if (this.autoGenerateLedger()) {
      this.syncLedgerAmounts(newAmount);
    }
  }

  onDescriptionChange(newDesc: string) {
    this.description.set(newDesc);
    if (this.autoGenerateLedger()) {
      this.ledgerEntries.update((entries) => entries.map((e) => ({ ...e, memo: newDesc })));
    }
  }

  applyTemplate() {
    if (this.accounts().length === 0) return;

    const type = this.transactionType();
    const method = this.paymentMethod();
    const amount = this.inputAmount();
    const memoText = this.description() || this.getTypeLabel(type);

    // Default Debit: 1000 Cash, 1001 Bank Deposit
    let debitCode = '1000';
    if (method === 'TRANSFER') {
      debitCode = '1001';
    }

    // Default Credit: 4000 Sales
    let creditCode = '4000';
    let debitCodeOverride: string | null = null;
    let creditCodeOverride: string | null = null;

    switch (type) {
      case 'INVESTMENT':
        creditCode = '3000'; // Capital/Equity
        break;
      case 'SALES':
        creditCode = '4000'; // Sales Revenue
        break;
      case 'TRANSFER_IN':
      case 'RECEIPT':
        creditCode = '4100'; // Other Revenue
        break;
      case 'PURCHASE_GENERAL':
      case 'PURCHASE_VCB':
      case 'GOODS_RECEIPT':
        debitCodeOverride = '1100'; // Inventory
        creditCodeOverride = method === 'TRANSFER' ? '1001' : '1000';
        break;
      case 'FREIGHT_VCB':
      case 'FREIGHT_GENERAL':
        debitCodeOverride = '5100'; // Freight In
        creditCodeOverride = method === 'TRANSFER' ? '1001' : '1000';
        break;
      case 'EXPENSE':
        debitCodeOverride = '6000'; // Admin Expense
        creditCodeOverride = method === 'TRANSFER' ? '1001' : '1000';
        break;
      case 'STOCK_LOSS':
      case 'SCRAP':
        debitCodeOverride = '5200'; // Stock Loss / Scrap
        creditCodeOverride = '1100'; // Inventory
        break;
    }

    const finalDebitCode = debitCodeOverride || debitCode;
    const finalCreditCode = creditCodeOverride || creditCode;

    const dbAcc = this.accounts().find((a) => a.accountCode === finalDebitCode);
    const crAcc = this.accounts().find((a) => a.accountCode === finalCreditCode);

    if (dbAcc && crAcc) {
      this.ledgerEntries.set([
        { accountId: dbAcc.accountId, debitAmount: amount, creditAmount: 0, memo: memoText },
        { accountId: crAcc.accountId, debitAmount: 0, creditAmount: amount, memo: memoText },
      ]);
    }
  }

  syncLedgerAmounts(amount: number) {
    this.ledgerEntries.update((entries) => {
      if (entries.length >= 2) {
        const newEntries = [...entries];
        newEntries[0] = { ...newEntries[0], debitAmount: amount, creditAmount: 0 };
        newEntries[1] = { ...newEntries[1], debitAmount: 0, creditAmount: amount };
        return newEntries;
      }
      return entries;
    });
  }

  toggleAutoGenerate(checked: boolean) {
    this.autoGenerateLedger.set(checked);
    if (checked) {
      this.applyTemplate();
    }
  }

  addLedgerEntry() {
    this.autoGenerateLedger.set(false);
    this.ledgerEntries.update((entries) => [
      ...entries,
      { accountId: 0, debitAmount: 0, creditAmount: 0, memo: '' },
    ]);
  }

  removeLedgerEntry(index: number) {
    this.autoGenerateLedger.set(false);
    this.ledgerEntries.update((entries) => entries.filter((_, i) => i !== index));
  }

  updateLedgerEntry(index: number, field: keyof LedgerEntryCreatePayload, value: any) {
    this.autoGenerateLedger.set(false);
    this.ledgerEntries.update((entries) => {
      const newEntries = [...entries];
      (newEntries[index] as any)[field] = value;
      return newEntries;
    });
  }

  getAccountCode(accountId: number): string {
    const acc = this.accounts().find((a) => a.accountId === Number(accountId));
    return acc ? acc.accountCode : '';
  }

  get totalDebit(): number {
    return this.ledgerEntries().reduce((sum, e) => sum + (Number(e.debitAmount) || 0), 0);
  }

  get totalCredit(): number {
    return this.ledgerEntries().reduce((sum, e) => sum + (Number(e.creditAmount) || 0), 0);
  }

  get isBalanced(): boolean {
    return this.totalDebit === this.totalCredit && this.totalDebit > 0;
  }

  totalAmount = computed(() => this.totalDebit);

  onSubmit() {
    if (!this.isBalanced) {
      this.error.set('ยอดเดบิตและเครดิตต้องเท่ากัน และมากกว่า 0');
      return;
    }

    // Validate bank account numbers if filled (must be 10-15 digits)
    const srcAcc = this.sourceAccountNo();
    const destAcc = this.destinationAccountNo();
    if (srcAcc && (srcAcc.length < 10 || srcAcc.length > 15)) {
      this.error.set('เลขที่บัญชีธนาคารต้นทางต้องเป็นตัวเลขความยาว 10 ถึง 15 หลัก');
      return;
    }
    if (destAcc && (destAcc.length < 10 || destAcc.length > 15)) {
      this.error.set('เลขที่บัญชีธนาคารปลายทางต้องเป็นตัวเลขความยาว 10 ถึง 15 หลัก');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const submitPayload = (finalAttachmentUrl: string) => {
      const payload: FinancialTransactionCreatePayload = {
        transactionDate: this.transactionDate(),
        transactionType: this.transactionType(),
        description: this.description(),
        totalAmount: this.totalAmount(),
        paymentMethod: this.paymentMethod(),
        paymentRefNo: this.paymentRefNo(),
        sourceAccountInfo: this.sourceAccountInfo(),
        receiverAccountId: this.receiverAccountId() ? Number(this.receiverAccountId()) : undefined,
        attachmentUrl: finalAttachmentUrl,

        // New audit and document fields
        status: this.status(),
        branchId: this.branchId() ? Number(this.branchId()) : undefined,
        documentNo: this.documentNo() || undefined,
        partnerName: this.partnerName() || undefined,
        slipDateTime: this.slipDateTime() || undefined,
        originBank: this.originBank() || undefined,
        destinationBank: this.destinationBank() || undefined,
        sourceAccountNo: this.sourceAccountNo() || undefined,
        destinationAccountNo: this.destinationAccountNo() || undefined,
        sourceAccountName: this.sourceAccountName() || undefined,
        destinationAccountName: this.destinationAccountName() || undefined,

        ledgerEntries: this.ledgerEntries().map((e) => ({
          accountId: Number(e.accountId),
          debitAmount: Number(e.debitAmount),
          creditAmount: Number(e.creditAmount),
          memo: e.memo,
        })),
      };

      const id = this.transactionId();
      const request$ = id
        ? this.financialService.updateTransaction(id, payload)
        : this.financialService.createTransaction(payload);

      this.saveOnTheFlyIfNeeded(() => {
        request$.subscribe({
          next: () => {
            this.router.navigate(['/finance/transactions']);
          },
          error: (err) => {
            this.error.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกรายการ');
            this.submitting.set(false);
          },
        });
      });
    };

    const file = this.selectedFile();
    if (file) {
      this.financialService
        .uploadAttachment(file)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            submitPayload(res.imageUrl);
          },
          error: (err) => {
            console.error('Upload failed', err);
            this.error.set(err.error?.message || 'ไม่สามารถอัปโหลดไฟล์แนบได้');
            this.submitting.set(false);
          },
        });
    } else {
      submitPayload(this.attachmentUrl());
    }
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      this.error.set('ขนาดไฟล์ต้องไม่เกิน 5MB');
      return;
    }

    this.selectedFile.set(file);
    this.selectedFileName.set(file.name);
    this.attachmentUrl.set(''); // Clear text input if a file is staged
    this.error.set(null);

    if (file.type.startsWith('image/')) {
      const oldUrl = this.imagePreview();
      if (oldUrl && oldUrl.startsWith('blob:')) {
        URL.revokeObjectURL(oldUrl);
      }
      this.imagePreview.set(URL.createObjectURL(file));
    } else {
      this.imagePreview.set(null);
    }
  }

  removeAttachment(): void {
    const oldUrl = this.imagePreview();
    if (oldUrl && oldUrl.startsWith('blob:')) {
      URL.revokeObjectURL(oldUrl);
    }
    this.selectedFile.set(null);
    this.selectedFileName.set('');
    this.attachmentUrl.set('');
    this.imagePreview.set(null);
  }

  isImageUrl(url: string | null | undefined): boolean {
    if (!url) return false;
    const lower = url.toLowerCase();
    return (
      lower.endsWith('.jpg') ||
      lower.endsWith('.jpeg') ||
      lower.endsWith('.png') ||
      lower.endsWith('.webp')
    );
  }

  getAttachmentUrl(url: string | undefined): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    return `${this.apiOrigin}${url}`;
  }

  getTypeLabel(type: string | undefined): string {
    if (!type) return '-';
    const found = this.typeOptions.find((o) => o.value === type);
    return found ? found.label : type;
  }

  getBranchName(id: number | undefined): string {
    if (!id) return '-';
    const b = this.branches().find((x) => x.id === id);
    return b ? b.branchName : id.toString();
  }

  onPartnerNameChange(name: string) {
    this.partnerName.set(name);
  }

  selectPartnerBankAccount(account: PartnerBankAccount, type: 'source' | 'destination') {
    if (type === 'source') {
      this.originBank.set(account.bankName);
      this.sourceAccountNo.set(account.accountNo);
      this.sourceAccountName.set(account.accountName);
    } else {
      this.destinationBank.set(account.bankName);
      this.destinationAccountNo.set(account.accountNo);
      this.destinationAccountName.set(account.accountName);
    }
  }

  selectCompanyBankAccount(account: CompanyBankAccount, type: 'source' | 'destination') {
    if (type === 'source') {
      this.originBank.set(account.bankName);
      this.sourceAccountNo.set(account.accountNo);
      this.sourceAccountName.set(account.accountName);
    } else {
      this.destinationBank.set(account.bankName);
      this.destinationAccountNo.set(account.accountNo);
      this.destinationAccountName.set(account.accountName);
    }
  }

  isPartnerAccountSaved(accountNo: string): boolean {
    return this.partnerBankAccounts().some((a) => a.accountNo === accountNo);
  }

  onAccountNoInput(event: Event, type: 'source' | 'destination') {
    const input = event.target as HTMLInputElement;
    const cleaned = input.value.replace(/[^0-9]/g, '');
    if (type === 'source') {
      this.sourceAccountNo.set(cleaned);
    } else {
      this.destinationAccountNo.set(cleaned);
    }
    input.value = cleaned;
  }

  private saveOnTheFlyIfNeeded(callback: () => void) {
    const partner = this.partnerName()?.trim();
    if (!partner) {
      callback();
      return;
    }

    const promises: any[] = [];

    if (this.saveSourceAccountOnTheFly() && this.originBank() && this.sourceAccountNo()) {
      promises.push(
        this.financialService.savePartnerBankAccount({
          partnerName: partner,
          bankName: this.originBank(),
          accountNo: this.sourceAccountNo(),
          accountName: this.sourceAccountName() || partner,
        }),
      );
    }

    if (
      this.saveDestinationAccountOnTheFly() &&
      this.destinationBank() &&
      this.destinationAccountNo()
    ) {
      promises.push(
        this.financialService.savePartnerBankAccount({
          partnerName: partner,
          bankName: this.destinationBank(),
          accountNo: this.destinationAccountNo(),
          accountName: this.destinationAccountName() || partner,
        }),
      );
    }

    if (promises.length === 0) {
      callback();
      return;
    }

    import('rxjs').then(({ forkJoin }) => {
      forkJoin(promises).subscribe({
        next: () => {
          callback();
        },
        error: (err) => {
          console.error('Failed to save bank account on the fly', err);
          callback();
        },
      });
    });
  }

  getCurrentLocalDateTimeString(): string {
    const now = new Date();
    const offset = now.getTimezoneOffset() * 60000;
    return new Date(now.getTime() - offset).toISOString().substring(0, 16);
  }

  protected readonly Math = Math;
  protected readonly Number = Number;
}
