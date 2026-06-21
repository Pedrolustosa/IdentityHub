import { Directive, ElementRef, Input, OnInit } from '@angular/core';

@Directive({
  selector: '[appTooltip]',
  standalone: true
})
export class TooltipDirective implements OnInit {
  @Input('appTooltip') tooltipText: string = '';
  @Input() tooltipPosition: 'top' | 'bottom' | 'left' | 'right' = 'top';
  @Input() tooltipDelay: number = 200;

  private tooltipElement: HTMLDivElement | null = null;
  private hideTimeout: any;

  constructor(private el: ElementRef) {}

  ngOnInit(): void {
    this.el.nativeElement.addEventListener('mouseenter', () => this.showTooltip());
    this.el.nativeElement.addEventListener('mouseleave', () => this.hideTooltip());
    this.el.nativeElement.style.cursor = 'help';
  }

  private showTooltip(): void {
    if (this.hideTimeout) {
      clearTimeout(this.hideTimeout);
    }

    setTimeout(() => {
      if (this.tooltipElement) {
        this.tooltipElement.remove();
      }

      this.tooltipElement = document.createElement('div');
      this.tooltipElement.className = this.getTooltipClasses();
      this.tooltipElement.textContent = this.tooltipText;
      this.tooltipElement.style.position = 'absolute';
      this.tooltipElement.style.zIndex = '9999';
      this.tooltipElement.style.pointerEvents = 'none';

      document.body.appendChild(this.tooltipElement);
      this.positionTooltip();
    }, this.tooltipDelay);
  }

  private hideTooltip(): void {
    this.hideTimeout = setTimeout(() => {
      if (this.tooltipElement) {
        this.tooltipElement.remove();
        this.tooltipElement = null;
      }
    }, 100);
  }

  private positionTooltip(): void {
    if (!this.tooltipElement) return;

    const rect = this.el.nativeElement.getBoundingClientRect();
    const tooltipRect = this.tooltipElement.getBoundingClientRect();
    const gap = 8;

    let top = 0;
    let left = 0;

    switch (this.tooltipPosition) {
      case 'top':
        top = rect.top - tooltipRect.height - gap;
        left = rect.left + rect.width / 2 - tooltipRect.width / 2;
        break;
      case 'bottom':
        top = rect.bottom + gap;
        left = rect.left + rect.width / 2 - tooltipRect.width / 2;
        break;
      case 'left':
        top = rect.top + rect.height / 2 - tooltipRect.height / 2;
        left = rect.left - tooltipRect.width - gap;
        break;
      case 'right':
        top = rect.top + rect.height / 2 - tooltipRect.height / 2;
        left = rect.right + gap;
        break;
    }

    // Clamp to viewport
    left = Math.max(0, Math.min(left, window.innerWidth - tooltipRect.width - 10));
    top = Math.max(0, Math.min(top, window.innerHeight - tooltipRect.height - 10));

    this.tooltipElement.style.top = `${top + window.scrollY}px`;
    this.tooltipElement.style.left = `${left + window.scrollX}px`;
  }

  private getTooltipClasses(): string {
    return 'rounded-lg bg-slate-900 px-2 py-1 text-xs font-medium text-white shadow-lg whitespace-nowrap';
  }
}
