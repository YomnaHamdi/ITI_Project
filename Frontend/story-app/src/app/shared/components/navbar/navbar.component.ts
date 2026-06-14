import { Component, signal, inject, HostListener } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css',
})
export class NavbarComponent {
  private readonly router = inject(Router);
  readonly state          = inject(AppStateService);

  readonly menuOpen = signal(false);
  readonly scrolled = signal(false);
  readonly isArabic = signal(true);

  @HostListener('window:scroll')
  onScroll(): void { this.scrolled.set(window.scrollY > 20); }

  toggleMenu(): void { this.menuOpen.update(v => !v); }
  closeMenu():  void { this.menuOpen.set(false); }

  toggleLang(): void {
    this.isArabic.update(v => !v);
    document.documentElement.dir = this.isArabic() ? 'rtl' : 'ltr';
  }

  goToLogin():    void { this.router.navigate(['/auth/login']);    this.closeMenu(); }
  goToRegister(): void { this.router.navigate(['/auth/register']); this.closeMenu(); }

  goToDashboard(): void {
    const role = this.state.userRole();
    const map: Record<string, string> = {
      student: '/dashboard',
      parent:  '/parent/dashboard',
      teacher: '/teacher/students',
      school:  '/school/dashboard',
      admin:   '/admin/content',
    };
    this.router.navigate([map[role] ?? '/']);
    this.closeMenu();
  }

  logout(): void {
    this.state.logout();
    this.router.navigate(['/']);
    this.closeMenu();
  }

  private readonly allNavLinks = [
    { label: 'المستويات',      labelEn: 'Levels',       route: '/levels',     hideForParent: true  },
    { label: 'لوحتي',          labelEn: 'My Dashboard', route: '/dashboard',  hideForParent: false },
    { label: 'اختبار التحديد', labelEn: 'Placement',    route: '/test',       hideForParent: true  },
  ];

  get navLinks() {
    const isParent = this.state.userRole() === 'parent';
    return isParent ? this.allNavLinks.filter(l => !l.hideForParent) : this.allNavLinks;
  }
}
