import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-copy-to-clipboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex items-center gap-2">
      <code class="rounded bg-slate-100 px-2 py-1 font-mono text-xs text-slate-800">
        @if (truncate && value.length > truncate) {
          {{ value.substring(0, truncate) }}...
        } @else {
          {{ value }}
        }
      </code>
      <button
        type="button"
        (click)="copy()"
        class="inline-flex items-center justify-center rounded p-1 text-muted hover:bg-muted-lighter hover:text-slate-700 transition-colors"
        [title]="isCopied ? 'Copied!' : 'Copy to clipboard'"
        [class.text-success-600]="isCopied"
      >
        @if (isCopied) {
          <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
          </svg>
        } @else {
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
        }
      </button>
    </div>
  `
})
export class CopyToClipboardComponent implements OnInit {
  @Input({ required: true }) value!: string;
  @Input() truncate: number | null = null;
  @Input() successMessage = 'Copied to clipboard!';

  isCopied = false;
  private copyResetTimeout: any;

  constructor(private toastr: ToastrService) {}

  ngOnInit(): void {
    if (!this.value) {
      console.warn('CopyToClipboardComponent: value is required');
    }
  }

  async copy(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.value);
      this.isCopied = true;
      this.toastr.success(this.successMessage);

      if (this.copyResetTimeout) {
        clearTimeout(this.copyResetTimeout);
      }

      this.copyResetTimeout = setTimeout(() => {
        this.isCopied = false;
      }, 2000);
    } catch (err) {
      this.toastr.error('Failed to copy to clipboard');
      console.error('Copy failed:', err);
    }
  }
}
