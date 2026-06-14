import { Component, signal, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AppStateService } from '../../../services/app-state-service';
import { AuthService } from '../../../services/auth.service';

type Role = 'parent' | 'teacher';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, NavbarComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  private readonly router = inject(Router);
  private readonly state  = inject(AppStateService);
  private readonly auth   = inject(AuthService);

  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);
  readonly submitted = signal(false);
  readonly showPass  = signal(false);

  form = {
    fullName:   '',
    email:      '',
    password:   '',
    role:       'parent' as Role,
    schoolCode: '',
    langPref:   'ar' as 'ar' | 'en',
  };

  readonly roles = [
    { value: 'parent'  as Role, icon: '👪', label: 'ولي أمر',      sub: 'تابع أطفالك' },
    { value: 'teacher' as Role, icon: '👩‍🏫', label: 'معلمة/معلم', sub: 'أدر طلابك' },
  ];

  submit(): void {
    if (!this.form.fullName || !this.form.email || !this.form.password) {
      this.error.set('يرجى ملء جميع الحقول المطلوبة.'); return;
    }
    if (this.form.password.length < 8) {
      this.error.set('كلمة المرور يجب أن تكون 8 أحرف على الأقل.'); return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    this.auth.register({
      fullName:   this.form.fullName,
      email:      this.form.email,
      password:   this.form.password,
      role:       this.form.role,
      schoolCode: this.form.schoolCode || undefined,
    }).subscribe({
      next: res => {
        this.state.setUser({ id: res.userId, name: res.name, role: res.role as any });
        this.isLoading.set(false);
        this.submitted.set(true);
        setTimeout(() => {
          this.router.navigate(
            this.form.role === 'parent' ? ['/parent/dashboard'] : ['/teacher/students']
          );
        }, 1500);
      },
      error: err => {
        this.isLoading.set(false);
        this.error.set(err?.error?.error ?? 'فشل إنشاء الحساب. حاول مرة أخرى.');
      }
    });
  }

  goToLogin(): void { this.router.navigate(['/auth/login']); }
}
