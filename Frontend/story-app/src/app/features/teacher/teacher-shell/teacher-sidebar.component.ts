import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-teacher-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside class="teacher-sidebar" dir="rtl">
      <div class="ts-profile">
        <div class="ts-avatar">🦁</div>
        <div>
          <div class="ts-name">{{ state.currentUserName() || 'أ. فاطمة' }}</div>
          <div class="ts-role">معلمة عربية</div>
        </div>
      </div>
      <nav class="ts-nav">
        @for (item of navItems; track item.route) {
          <a class="ts-link" [routerLink]="item.route" routerLinkActive="active">
            <span>{{ item.icon }}</span>
            <span>{{ item.label }}</span>
          </a>
        }
      </nav>
      <div class="ts-footer">
        <button class="ts-logout" type="button" (click)="logout()">
          <span>🚪</span><span>تسجيل الخروج</span>
        </button>
      </div>
    </aside>
  `,
  styles: [`
    .teacher-sidebar {
      width: 220px; flex-shrink: 0; background: var(--bg-card);
      border-left: 1.5px solid rgba(244,120,138,.1); padding: 24px 14px;
      display: flex; flex-direction: column; gap: 20px;
      height: 100vh; position: sticky; top: 0; overflow-y: auto;
    }
    .ts-profile {
      display: flex; align-items: center; gap: 10px; padding: 10px;
      background: rgba(244,120,138,.05); border-radius: 14px;
    }
    .ts-avatar {
      width: 44px; height: 44px; border-radius: 50%;
      background: linear-gradient(135deg, #FFE4E8, #F3E8FF);
      display: flex; align-items: center; justify-content: center; font-size: 22px;
    }
    .ts-name { font-size: 14px; font-weight: 800; }
    .ts-role { font-size: 12px; color: var(--text-muted); margin-top: 2px; }
    .ts-nav { display: flex; flex-direction: column; gap: 4px; }
    .ts-link {
      display: flex; align-items: center; gap: 10px; padding: 11px 12px;
      border-radius: 12px; text-decoration: none; font-size: 14px; font-weight: 700;
      color: var(--text-muted); transition: .2s;
    }
    .ts-link:hover { background: rgba(244,120,138,.08); color: var(--primary); }
    .ts-link.active { background: rgba(244,120,138,.08); color: var(--primary); font-weight: 800; }
    .ts-footer { margin-top: auto; }
    .ts-logout {
      display: flex; align-items: center; gap: 10px; width: 100%;
      padding: 11px 12px; border-radius: 12px; border: none; background: none;
      font-family: 'Cairo', sans-serif; font-size: 14px; font-weight: 700;
      color: #EF4444; cursor: pointer; transition: .2s;
    }
    .ts-logout:hover { background: #FFF5F5; }
    @media (max-width: 900px) { .teacher-sidebar { display: none; } }
  `]
})
export class TeacherSidebarComponent {
  readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly navItems = [
    { icon: '👥', label: 'الطلاب',           route: '/teacher/students' },
    { icon: '➕', label: 'إضافة طالب',       route: '/auth/create-student' },
    { icon: '📚', label: 'الدروس',            route: '/teacher/lessons' },
    { icon: '✨', label: 'المولّد الذكي',     route: '/teacher/ai-generator' },
    { icon: '📝', label: 'إنشاء درس',        route: '/teacher/lessons/create' },
    { icon: '📊', label: 'التقارير',          route: '/teacher/reports' },
  ];

  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
