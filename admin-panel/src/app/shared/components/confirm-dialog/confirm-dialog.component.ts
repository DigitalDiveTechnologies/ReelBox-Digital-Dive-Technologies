import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild,
} from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  template: `
    @if (open) {
      <div class="backdrop" (click)="cancel.emit()" role="presentation"></div>
      <div
        class="dialog"
        role="dialog"
        aria-modal="true"
        [attr.aria-labelledby]="titleId"
        [attr.aria-describedby]="messageId"
        #dialog
      >
        <h2 [id]="titleId">{{ title }}</h2>
        <p [id]="messageId">{{ message }}</p>
        <div class="actions">
          <button type="button" class="ghost" (click)="cancel.emit()" [disabled]="busy">
            Cancel
          </button>
          <button
            type="button"
            class="danger"
            #confirmBtn
            (click)="confirm.emit()"
            [disabled]="busy"
          >
            {{ busy ? 'Working…' : confirmLabel }}
          </button>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.55);
        z-index: 40;
      }
      .dialog {
        position: fixed;
        z-index: 50;
        left: 50%;
        top: 50%;
        transform: translate(-50%, -50%);
        width: min(420px, calc(100vw - 2rem));
        padding: 1.25rem 1.35rem;
        border-radius: var(--radius-lg);
        background: var(--surface-elevated);
        border: 1px solid var(--border);
        box-shadow: var(--shadow);
      }
      h2 {
        margin: 0 0 0.5rem;
        font-size: 1.05rem;
      }
      p {
        margin: 0 0 1.15rem;
        color: var(--text-muted);
        font-size: 0.9rem;
        line-height: 1.45;
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.6rem;
      }
      button {
        border: 1px solid var(--border);
        border-radius: 10px;
        padding: 0.55rem 0.9rem;
        cursor: pointer;
        background: transparent;
        color: var(--text-primary);
      }
      button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .ghost:hover:not(:disabled) {
        border-color: var(--text-muted);
      }
      .danger {
        background: color-mix(in srgb, var(--status-fail) 80%, transparent);
        border-color: transparent;
      }
    `,
  ],
})
export class ConfirmDialogComponent implements OnChanges {
  @Input() open = false;
  @Input() title = 'Confirm';
  @Input() message = 'Are you sure?';
  @Input() confirmLabel = 'Confirm';
  @Input() busy = false;
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  @ViewChild('confirmBtn') confirmBtn?: ElementRef<HTMLButtonElement>;

  readonly titleId = 'confirm-dialog-title';
  readonly messageId = 'confirm-dialog-message';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']?.currentValue === true) {
      queueMicrotask(() => this.confirmBtn?.nativeElement.focus());
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open && !this.busy) {
      this.cancel.emit();
    }
  }
}
