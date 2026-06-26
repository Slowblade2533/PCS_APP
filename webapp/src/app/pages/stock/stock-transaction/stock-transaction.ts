import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { PagedResult } from '../../../shared/models/pagination.models';
import {
  StockTransaction,
  StockTransactionQuery,
  StockTransactionRequest,
  TransactionType,
} from '../../../shared/models/stock.models';
import { Branch } from '../../../shared/models/user.models';
import { StockService } from '../../../shared/services/stock.service';

@Component({
  selector: 'app-stock-transaction',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe, NgClass, PaginationComponent],
  templateUrl: './stock-transaction.html',
})
export class StockTransactionList implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly stockService = inject(StockService);

  branches = signal<Branch[]>([]);
  errorMsg = signal('');
  loading = signal(false);
  submitting = signal(false);
  successMsg = signal('');
  txLoading = signal(false);
  txResult = signal<PagedResult<StockTransaction> | null>(null);

  txQuery: StockTransactionQuery = { page: 1, pageSize: 15 };

  private currentReq?: import('rxjs').Subscription;

  transactionTypes = [
    { value: 'IN' as TransactionType, label: 'รับเข้า (IN)', class: 'peer-checked:btn-success' },
    { value: 'OUT' as TransactionType, label: 'เบิกออก (OUT)', class: 'peer-checked:btn-error' },
    {
      value: 'ADJUST' as TransactionType,
      label: 'ปรับ (ADJUST)',
      class: 'peer-checked:btn-warning',
    },
    { value: 'RESERVE' as TransactionType, label: 'จอง (RESERVE)', class: 'peer-checked:btn-info' },
    {
      value: 'UNRESERVE' as TransactionType,
      label: 'ยกเลิกจอง (UNRESERVE)',
      class: 'peer-checked:btn-neutral',
    },
    {
      value: 'DAMAGE' as TransactionType,
      label: 'ชำรุด (DAMAGE)',
      class: 'peer-checked:bg-orange-600 peer-checked:text-white',
    },
    {
      value: 'LOST' as TransactionType,
      label: 'สูญหาย (LOST)',
      class: 'peer-checked:bg-purple-600 peer-checked:text-white',
    },
  ];

  form!: FormGroup;
  filterType = new FormControl<TransactionType | undefined>(undefined);

  get selectedType(): TransactionType {
    return this.form.get('transactionType')?.value ?? 'IN';
  }

  ngOnInit(): void {
    const variantId = this.route.snapshot.queryParamMap.get('variantId');
    const branchId = this.route.snapshot.queryParamMap.get('branchId');

    this.form = this.fb.group({
      transactionType: ['IN', Validators.required],
      condition: ['Normal', Validators.required],
      branchId: [branchId ? +branchId : null, Validators.required],
      variantId: [variantId ? +variantId : null, [Validators.required, Validators.min(1)]],
      quantity: [null, [Validators.required, Validators.min(1)]],
      referenceNo: [''],
      note: [''],
    });

    this.stockService
      .getBranches()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((branches) => {
        this.branches.set(branches);
      });

    this.filterType.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((val) => {
      this.txQuery.transactionType = val ?? undefined;
      this.txQuery.page = 1;
      this.loadTransactions();
    });

    this.loadTransactions();
  }

  changeTxPage(page: number): void {
    this.txQuery = { ...this.txQuery, page };
    this.loadTransactions();
  }

  hasTxNextPage(): boolean {
    const total = this.txResult()?.totalCount ?? 0;
    return this.txQuery.page * this.txQuery.pageSize < total;
  }

  loadTransactions(): void {
    if (this.currentReq) {
      this.currentReq.unsubscribe();
    }

    this.txLoading.set(true);
    this.currentReq = this.stockService
      .getTransactions(this.txQuery)
      .pipe(
        finalize(() => {
          this.txLoading.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => this.txResult.set(res));
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { transactionType, condition, branchId, variantId, quantity, referenceNo, note } =
      this.form.value;
    const payload: StockTransactionRequest = {
      transactionType,
      condition,
      branchId,
      variantId,
      quantity,
      referenceNo: referenceNo || undefined,
      requestId: crypto.randomUUID(),
      note: note || undefined,
    };

    this.submitting.set(true);
    this.errorMsg.set('');
    this.successMsg.set('');

    this.stockService
      .createTransaction(payload)
      .pipe(
        finalize(() => {
          this.submitting.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.successMsg.set('บันทึกรายการสำเร็จ');
          this.form.patchValue({ quantity: null, referenceNo: '', note: '' });
          this.loadTransactions();
        },
        error: (err) => {
          this.errorMsg.set(err.error?.message ?? 'เกิดข้อผิดพลาด กรุณาลองใหม่');
        },
      });
  }
}
