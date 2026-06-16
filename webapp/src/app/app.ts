import { ChangeDetectionStrategy, Component, HostListener, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './app.css',
})
export class App {
  protected readonly title = signal('webapp');

  @HostListener('window:keydown.enter', ['$event'])
  handleEnterKey(event: any) {
    const target = event.target as HTMLElement;

    if (target && target.tagName === 'INPUT') {
      const inputType = (target as HTMLInputElement).type;
      if (inputType !== 'submit' && inputType !== 'button' && inputType !== 'reset') {
        event.preventDefault();
      }
    }
  }
}
