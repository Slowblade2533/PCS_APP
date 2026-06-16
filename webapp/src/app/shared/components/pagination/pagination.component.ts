import { DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './pagination.component.html',
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
