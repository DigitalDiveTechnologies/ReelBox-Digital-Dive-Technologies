import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination-bar',
  standalone: true,
  template: `
    <div class="bar">
      <span class="meta">
        @if (totalCount === 0) {
          No results
        } @else {
          Page {{ page }} of {{ totalPages }} · {{ totalCount }} total
        }
      </span>
      <div class="controls">
        <label>
          Rows
          <select [value]="pageSize" (change)="onPageSize($event)">
            @for (size of pageSizes; track size) {
              <option [value]="size">{{ size }}</option>
            }
          </select>
        </label>
        <button type="button" (click)="pageChange.emit(page - 1)" [disabled]="page <= 1">
          Prev
        </button>
        <button
          type="button"
          (click)="pageChange.emit(page + 1)"
          [disabled]="page >= totalPages || totalCount === 0"
        >
          Next
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .bar {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        padding-top: 0.85rem;
        border-top: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
        margin-top: 0.85rem;
      }
      .meta {
        font-size: 0.8rem;
        color: var(--text-muted);
      }
      .controls {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }
      label {
        display: flex;
        align-items: center;
        gap: 0.35rem;
        font-size: 0.8rem;
        color: var(--text-muted);
      }
      select,
      button {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 8px;
        padding: 0.35rem 0.55rem;
        font: inherit;
      }
      button {
        cursor: pointer;
      }
      button:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
    `,
  ],
})
export class PaginationBarComponent {
  @Input() page = 1;
  @Input() pageSize = 25;
  @Input() totalCount = 0;
  @Input() pageSizes: number[] = [10, 25, 50];
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get totalPages(): number {
    return this.pageSize <= 0
      ? 0
      : Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  onPageSize(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
