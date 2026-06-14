import { Component, input, output, OnInit, OnDestroy } from '@angular/core';

@Component({
  selector: 'app-error-toast',
  standalone: true,
  template: `
    <div class="toast" role="alert" aria-live="assertive">
      <span class="toast-icon">⚠️</span>
      <span class="toast-msg">{{ message() }}</span>
      <button class="toast-close" (click)="dismissed.emit()" aria-label="إغلاق">✕</button>
    </div>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Baloo+Bhaijaan+2:wght@600;700&display=swap');
    .toast {
      position: fixed; bottom: 1.5rem; right: 1.5rem; left: 1.5rem;
      max-width: 480px; margin: 0 auto;
      background: #fff5f5; border: 2px solid #fed7d7;
      border-radius: 16px; padding: 1rem 1.2rem;
      display: flex; align-items: center; gap: 0.8rem;
      box-shadow: 0 8px 30px rgba(0,0,0,0.15);
      font-family: 'Baloo Bhaijaan 2', sans-serif;
      direction: rtl;
      animation: slideUp 0.3s ease-out;
      z-index: 2000;
    }
    @keyframes slideUp {
      from { opacity: 0; transform: translateY(20px); }
      to   { opacity: 1; transform: translateY(0); }
    }
    .toast-icon { font-size: 1.3rem; flex-shrink: 0; }
    .toast-msg  { font-size: 1rem; font-weight: 700; color: #c53030; flex: 1; }
    .toast-close {
      background: none; border: none; cursor: pointer;
      font-size: 1rem; color: #c53030; padding: 2px 6px;
      border-radius: 8px; flex-shrink: 0;
      transition: background 0.15s;
    }
    .toast-close:hover { background: #fed7d7; }
  `]
})
export class ErrorToastComponent implements OnInit, OnDestroy {
  readonly message = input.required<string>();
  readonly dismissed = output<void>();

  private timer?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.timer = setTimeout(() => this.dismissed.emit(), 5000);
  }

  ngOnDestroy(): void {
    clearTimeout(this.timer);
  }
}
