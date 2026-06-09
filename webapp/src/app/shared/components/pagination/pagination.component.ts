import { DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [DecimalPipe],
  template: `
    @if (totalCount > 0 && totalCount > pageSize) {
      <div class="flex flex-col sm:flex-row items-center justify-between px-4 py-3 border-t border-base-200 bg-base-50/50 gap-4">
        <span class="text-sm text-base-content/60">
          แสดงข้อมูลทั้งหมด
          <span class="font-semibold text-base-content">{{ totalCount | number }}</span> รายการ
          (หน้า {{ page }} จาก {{ totalPages }})
        </span>
        <div class="join">
          <button
            class="join-item btn btn-sm"
            [disabled]="page === 1"
            (click)="onPageChange(page - 1)"
          >
            « ก่อนหน้า
          </button>
          <button class="join-item btn btn-sm bg-base-100 pointer-events-none text-base-content/70">
            หน้า {{ page }}
          </button>
          <button
            class="join-item btn btn-sm"
            [disabled]="page >= totalPages"
            (click)="onPageChange(page + 1)"
          >
            ถัดไป »
          </button>
        </div>
      </div>
    }
  `,
})
export class PaginationComponent {
  @Input({ required: true }) page!: number;
  @Input({ required: true }) pageSize!: number;
  @Input({ required: true }) totalCount!: number;

  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize) || 1;
  }

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.pageChange.emit(newPage);
    }
  }
}
