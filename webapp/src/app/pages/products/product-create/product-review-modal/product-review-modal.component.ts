import { Component, input, output } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-product-review-modal',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './product-review-modal.component.html'
})
export class ProductReviewModalComponent {
  showModal = input<boolean>(false);
  comparisonReport = input<any[]>([]);
  variantsComparison = input<any[]>([]);
  isSubmitting = input<boolean>(false);
  apiOrigin = input<string>('');

  onCancel = output<void>();
  onConfirm = output<void>();
}
