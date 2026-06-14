import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [NavbarComponent],
  template: `
    <app-navbar />
    <div class="nf-page" dir="rtl">
      <div class="nf-card card animate-pop">
        <div class="nf-mascot">🦁</div>
        <h1 class="nf-code">404</h1>
        <h2>عفواً! الصفحة غير موجودة</h2>
        <p>يبدو أن هذه الصفحة ذهبت في مغامرة بعيدة!</p>
        <button class="nf-btn" type="button" (click)="goHome()">
          🏠 العودة للرئيسية
        </button>
      </div>
    </div>
  `,
  styles: [`
    .nf-page {
      min-height: 100vh;
      padding-top: 68px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-base);
    }
    .nf-card {
      padding: 60px 40px;
      text-align: center;
      max-width: 440px;
      width: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
    }
    .nf-mascot { font-size: 80px; animation: bounce 2s ease infinite; }
    .nf-code {
      font-size: 64px; font-weight: 800;
      background: var(--primary-gradient);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }
    h2 { font-size: 22px; font-weight: 800; }
    p  { font-size: 16px; color: var(--text-muted); }
    .nf-btn {
      margin-top: 8px;
      display: inline-flex; align-items: center; justify-content: center; gap: 8px;
      border-radius: 999px;
      font-family: 'Cairo', sans-serif; font-weight: 700; font-size: 16px;
      padding: 14px 32px; cursor: pointer; transition: all .2s ease;
      border: none;
      background: var(--primary-gradient); color: #fff;
      box-shadow: 0 4px 16px rgba(244,120,138,.35);
    }
    .nf-btn:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(244,120,138,.45); }
  `],
})
export class NotFoundComponent {
  private readonly router = inject(Router);
  goHome(): void { this.router.navigate(['/']); }
}
