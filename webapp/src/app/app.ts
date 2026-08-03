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
  }

  ngOnDestroy() {
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
