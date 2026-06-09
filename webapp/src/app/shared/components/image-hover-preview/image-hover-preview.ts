import { Component, Input, signal } from '@angular/core';

@Component({
  selector: 'app-image-hover-preview',
  standalone: true,
  templateUrl: './image-hover-preview.html',
  styles: []
})
export class ImageHoverPreview {
  @Input({ required: true }) imageUrl!: string;
  @Input() altText: string = 'Image Preview';
  @Input() imageClass: string = 'w-12 h-12 object-cover rounded-md border border-base-200 cursor-pointer';

  showPreview = signal<boolean>(false);
  previewStyle = signal<any>({});

  onMouseEnter(event: MouseEvent) {
    this.showPreview.set(true);
    this.updatePreviewPosition(event);
  }

  onMouseMove(event: MouseEvent) {
    this.updatePreviewPosition(event);
  }

  onMouseLeave() {
    this.showPreview.set(false);
  }

  private updatePreviewPosition(event: MouseEvent) {
    if (!this.showPreview()) return;
    
    const x = event.clientX;
    const y = event.clientY;
    const w = window.innerWidth;
    const h = window.innerHeight;

    let left = x + 20;
    let top = y - 250; // default center around cursor

    // Bounds checking
    if (top < 10) top = 10;
    else if (top + 500 > h - 10) top = h - 510;
    
    if (left + 500 > w - 10) left = x - 520; // Flip to left of cursor if too close to right edge

    this.previewStyle.set({
      left: `${left}px`,
      top: `${top}px`
    });
  }
}
