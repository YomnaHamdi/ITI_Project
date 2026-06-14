import { Component, input } from '@angular/core';

@Component({
  selector: 'app-simple-loading',
  standalone: true,
  template: `
    <div class="s-overlay" role="status">
      <div class="s-card">
        <div class="s-ring"></div>
        <p class="s-msg">{{ message() }}</p>
      </div>
    </div>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Baloo+Bhaijaan+2:wght@700&display=swap');
    .s-overlay {
      position: fixed; inset: 0;
      background: rgba(15, 20, 50, .75);
      display: flex; align-items: center; justify-content: center;
      z-index: 9999; backdrop-filter: blur(4px);
    }
    .s-card {
      background: #fff; border-radius: 20px;
      padding: 1.8rem 2.2rem;
      display: flex; flex-direction: column; align-items: center; gap: .8rem;
      font-family: 'Baloo Bhaijaan 2', sans-serif;
      box-shadow: 0 16px 48px rgba(0,0,0,.35);
      min-width: 180px;
    }
    .s-ring {
      width: 46px; height: 46px;
      border: 4px solid #e0e7ff;
      border-top-color: #667eea;
      border-radius: 50%;
      animation: spin .7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .s-msg {
      font-size: 1rem; font-weight: 700;
      color: #333; text-align: center;
    }
  `]
})
export class SimpleLoadingComponent {
  readonly message = input<string>('جارٍ التحميل...');
}
