import { Directive, ElementRef, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import flatpickr from 'flatpickr';
import { Thai } from 'flatpickr/dist/l10n/th.js';
import Instance = flatpickr.Instance;

@Directive({
  selector: 'input[type="date"], input[type="datetime-local"], input[appDatepicker]',
  standalone: true,
})
export class DatepickerDirective implements OnInit, OnDestroy {
  private fpInstance?: Instance;
  private attrObserver?: MutationObserver;

  constructor(
    private el: ElementRef<HTMLInputElement>,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;

    const input = this.el.nativeElement;
    if (input.classList.contains('flatpickr-input')) return;

    const isDatetime = input.type === 'datetime-local';
    const defaultPlaceholder = isDatetime ? 'วัน/เดือน/ปี เวลา' : 'วัน/เดือน/ปี';
    if (!input.placeholder) {
      input.placeholder = defaultPlaceholder;
    }

    this.fpInstance = flatpickr(input, {
      locale: Thai,
      enableTime: isDatetime,
      dateFormat: isDatetime ? 'Y-m-d\\TH:i' : 'Y-m-d',
      altInput: true,
      altFormat: isDatetime ? 'd/m/Y H:i' : 'd/m/Y',
      allowInput: true,
      onReady: (selectedDates, dateStr, instance) => {
        if (instance.altInput && input.className) {
          instance.altInput.className = input.className + ' flatpickr-alt-input';
          instance.altInput.classList.remove('flatpickr-input');
        }
      },
      onChange: () => {
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
      }
    });

    if (this.fpInstance && input.value) {
      this.fpInstance.setDate(input.value, false);
    }

    if (this.fpInstance?.altInput) {
      const altInput = this.fpInstance.altInput;
      altInput.disabled = input.disabled;

      this.attrObserver = new MutationObserver(() => {
        altInput.disabled = input.disabled;
      });
      this.attrObserver.observe(input, { attributes: true, attributeFilter: ['disabled'] });
    }
  }

  ngOnDestroy() {
    this.attrObserver?.disconnect();
    this.fpInstance?.destroy();
  }
}
