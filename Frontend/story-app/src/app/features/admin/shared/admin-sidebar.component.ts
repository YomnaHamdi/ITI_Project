import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="admin-sidebar" dir="rtl">
      <div class="sidebar-logo">🛡️ لوحة الإدارة</div>
      <ul class="sidebar-nav">
        <li>
          <a class="nav-link" routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">
            <span class="nav-icon">🏠</span> الصفحة الرئيسية
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/books" routerLinkActive="active">
            <span class="nav-icon">📚</span> إدارة الكتب
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/content" routerLinkActive="active">
            <span class="nav-icon">🗂️</span> المحتوى
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/ai-settings" routerLinkActive="active">
            <span class="nav-icon">🤖</span> إعدادات الذكاء الاصطناعي
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/subscriptions" routerLinkActive="active">
            <span class="nav-icon">💳</span> الاشتراكات
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/users" routerLinkActive="active">
            <span class="nav-icon">👥</span> المستخدمون
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/schools" routerLinkActive="active">
            <span class="nav-icon">🏫</span> إضافة مدرسة
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/stories" routerLinkActive="active">
            <span class="nav-icon">📖</span> القصص
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/rag-chunks" routerLinkActive="active">
            <span class="nav-icon">🧩</span> صفحات RAG
          </a>
        </li>
      </ul>
      <div class="sidebar-footer">
        <button class="logout-btn" (click)="logout()">🚪 تسجيل الخروج</button>
      </div>
    </nav>
  `,
  styles: [`
    .admin-sidebar {
      width: 220px; min-height: 100vh; background: #1E1B4B; color: #fff;
      display: flex; flex-direction: column; padding: 24px 0; flex-shrink: 0;
    }
    .sidebar-logo {
      font-size: 16px; font-weight: 800; padding: 0 20px 24px; border-bottom: 1px solid rgba(255,255,255,.1);
    }
    .sidebar-nav { list-style: none; margin: 16px 0 0; padding: 0; flex: 1; }
    .sidebar-nav li { margin: 2px 0; }
    .nav-link {
      display: flex; align-items: center; gap: 10px; padding: 12px 20px;
      font-size: 14px; font-weight: 700; color: rgba(255,255,255,.7);
      text-decoration: none; border-radius: 0 24px 24px 0; margin-left: 8px;
      transition: all .2s;
    }
    .nav-link:hover { background: rgba(255,255,255,.1); color: #fff; }
    .nav-link.active { background: #F4788A; color: #fff; }
    .nav-icon { font-size: 18px; }
    .sidebar-footer { padding: 16px 20px; border-top: 1px solid rgba(255,255,255,.1); }
    .logout-btn {
      width: 100%; padding: 10px 14px; background: rgba(255,255,255,.08);
      border: 1px solid rgba(255,255,255,.15); border-radius: 10px;
      color: rgba(255,255,255,.8); font-size: 13px; font-weight: 700;
      font-family: Cairo, sans-serif; cursor: pointer; transition: all .2s;
    }
    .logout-btn:hover { background: rgba(239,68,68,.3); border-color: #EF4444; color: #fff; }
  `]
})
export class AdminSidebarComponent {
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
