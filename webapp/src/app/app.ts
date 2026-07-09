import { Component, HostListener, signal, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import flatpickr from 'flatpickr';
import { Thai } from 'flatpickr/dist/l10n/th.js';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit, OnDestroy {
  protected readonly title = signal('webapp');
  private observer: MutationObserver | null = null;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.initGlobalFlatpickr();
    }
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }

  private initGlobalFlatpickr() {
    const applyToInputs = (node: Element) => {
      const initFlatpickr = (input: HTMLInputElement) => {
        if (!input.classList.contains('flatpickr-input') && (input.type === 'date' || input.type === 'datetime-local')) {
          const isDatetime = input.type === 'datetime-local';

          // Set default placeholder if none exists
          const defaultPlaceholder = isDatetime ? 'วัน/เดือน/ปี เวลา' : 'วัน/เดือน/ปี';
          if (!input.placeholder) {
            input.placeholder = defaultPlaceholder;
          }

          let isSyncing = false;
          const descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
          const originalValueGetter = descriptor?.get;
          const originalValueSetter = descriptor?.set;
          if (originalValueGetter && originalValueSetter) {
            Object.defineProperty(input, 'value', {
              get() {
                return originalValueGetter.call(input);
              },
              set(val) {
                originalValueSetter.call(input, val);
                if (isSyncing) return;
                const fp = (input as any)._flatpickr;
                if (fp) {
                  isSyncing = true;
                  try {
                    fp.setDate(val, false);
                  } finally {
                    isSyncing = false;
                  }
                }
              },
              configurable: true
            });
          }

          flatpickr(input, {
            locale: Thai,
            enableTime: isDatetime,
            dateFormat: isDatetime ? 'Y-m-d\\TH:i' : 'Y-m-d',
            altInput: true,
            altFormat: isDatetime ? 'd/m/Y H:i' : 'd/m/Y',
            allowInput: true,
            onReady: (selectedDates, dateStr, instance) => {
              if (instance.altInput && input.className) {
                // Copy original classes to the visible alternate input so daisyUI styles still apply
                instance.altInput.className = input.className + ' flatpickr-alt-input';
                // Flatpickr creates its own flatpickr-input class, remove it from alt so it doesn't duplicate loops
                instance.altInput.classList.remove('flatpickr-input');
              }
            },
            onChange: () => {
              // Trigger events for Angular ReactiveFormsModule to pick up
              input.dispatchEvent(new Event('input', { bubbles: true }));
              input.dispatchEvent(new Event('change', { bubbles: true }));
            }
          });

          // Sync initial value if it exists
          const fpInstance = (input as any)._flatpickr;
          if (fpInstance && input.value) {
            isSyncing = true;
            try {
              fpInstance.setDate(input.value, false);
            } finally {
              isSyncing = false;
            }
          }

          // Sync 'disabled' state from original input to altInput
          if (fpInstance && fpInstance.altInput) {
            const altInput = fpInstance.altInput;
            altInput.disabled = input.disabled; // Initial sync

            const attrObserver = new MutationObserver(() => {
              altInput.disabled = input.disabled;
            });
            attrObserver.observe(input, { attributes: true, attributeFilter: ['disabled'] });
          }
        }
      };

      if (node.tagName === 'INPUT') {
        initFlatpickr(node as HTMLInputElement);
      }
      
      const inputs = node.querySelectorAll?.('input[type="date"], input[type="datetime-local"]');
      if (inputs) {
        inputs.forEach(input => initFlatpickr(input as HTMLInputElement));
      }
    };

    applyToInputs(document.body);

    this.observer = new MutationObserver(mutations => {
      mutations.forEach(mutation => {
        mutation.addedNodes.forEach(node => {
          if (node.nodeType === 1) { // ELEMENT_NODE
            applyToInputs(node as Element);
          }
        });
      });
    });

    this.observer.observe(document.body, { childList: true, subtree: true });
  }

  @HostListener('window:keydown.enter', ['$event'])
  handleEnterKey(event: Event) {
    const target = event.target as HTMLElement;

    if (target && target.tagName === 'INPUT') {
      const inputType = (target as HTMLInputElement).type;
      if (inputType !== 'submit' && inputType !== 'button' && inputType !== 'reset') {
        event.preventDefault();
      }
    }
  }
}
