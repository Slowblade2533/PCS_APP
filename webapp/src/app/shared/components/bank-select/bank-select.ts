import { Component, Input, signal, computed, ElementRef, HostListener, model } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { bankLists, Bank } from '../../constants/banks.constants';

@Component({
  selector: 'app-bank-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bank-select.html',
  styles: [
    `
      :host {
        display: block;
        width: 100%;
      }
    `,
  ],
})
export class BankSelectComponent {
  value = model<string>('');
  @Input() placeholder: string = 'เลือกธนาคาร';

  banks = Object.values(bankLists);

  isOpen = signal(false);
  searchQuery = signal('');
  forceCustomText = signal(false);

  isCustomText = computed(() => {
    if (this.forceCustomText()) return true;
    const val = this.value();
    return !!val && !this.banks.find((b) => b.symbol === val);
  });

  customText = computed(() => {
    const val = this.value();
    const isCustom = this.forceCustomText() || (!!val && !this.banks.find((b) => b.symbol === val));
    return isCustom ? val : '';
  });

  filteredBanks = computed(() => {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.banks;
    return this.banks.filter(
      (b) =>
        b.name.toLowerCase().includes(query) ||
        b.fullname.toLowerCase().includes(query) ||
        b.nameEN.toLowerCase().includes(query) ||
        b.symbol.toLowerCase().includes(query),
    );
  });

  selectedBank = computed(() => {
    const val = this.value();
    return this.banks.find((b) => b.symbol === val) || null;
  });

  constructor(private eRef: ElementRef) {}

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if (!this.eRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  toggleDropdown(event: Event) {
    event.stopPropagation();
    this.isOpen.update((v) => !v);
    if (this.isOpen()) {
      this.searchQuery.set('');
    }
  }

  selectBank(bank: Bank) {
    this.value.set(bank.symbol);
    this.forceCustomText.set(false);
    this.isOpen.set(false);
  }

  selectOther() {
    this.forceCustomText.set(true);
    this.value.set('');
    this.isOpen.set(false);
  }

  onCustomTextChange(val: string) {
    this.value.set(val);
    if (!val) {
      this.forceCustomText.set(false);
    }
  }

  clearSelection(event: Event) {
    event.stopPropagation();
    this.value.set('');
    this.forceCustomText.set(false);
    this.isOpen.set(false);
  }
}
