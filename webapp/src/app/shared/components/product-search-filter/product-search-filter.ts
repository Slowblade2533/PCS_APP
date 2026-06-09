import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-product-search-filter',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './product-search-filter.html',
  styles: []
})
export class ProductSearchFilter {
  @Input() searchTerm: string = '';
  @Output() searchTermChange = new EventEmitter<string>();

  @Input() selectedStatus: string = '';
  @Output() selectedStatusChange = new EventEmitter<string>();

  @Input() selectedType: string = '';
  @Output() selectedTypeChange = new EventEmitter<string>();

  @Input() selectedInventoryGroup: string = '';
  @Output() selectedInventoryGroupChange = new EventEmitter<string>();

  @Output() filterChange = new EventEmitter<void>();

  onSearchTextChange(value: string) {
    this.searchTermChange.emit(value);
    // Usually debounce is handled by parent, so we just emit the value.
  }

  onSelectChange() {
    this.filterChange.emit();
  }
}
