import { Component, signal, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AppStateService } from '../../../services/app-state-service';
import { AuthService } from '../../../services/auth.service';

type LoginFlow = 'idle' | 'adult' | 'student';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly router = inject(Router);
  private readonly state  = inject(AppStateService);
  private readonly auth   = inject(AuthService);

  readonly flow      = signal<LoginFlow>('idle');
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);

  // ── Adult form ──────────────────────────────────────────────────────────────
  adultForm = { email: '', password: '', showPassword: false };

  // ── Student username ────────────────────────────────────────────────────────
  studentUsername = '';

  // ── Image PIN ───────────────────────────────────────────────────────────────
  readonly imageOptions = [
    { id:  1, emoji: '🐰' }, { id:  2, emoji: '🦆' },
    { id:  3, emoji: '🐟' }, { id:  4, emoji: '🐢' },
    { id:  5, emoji: '🐱' }, { id:  6, emoji: '🦎' },
    { id:  7, emoji: '🚗' }, { id:  8, emoji: '🚕' },
    { id:  9, emoji: '🚀' }, { id: 10, emoji: '🚂' },
    { id: 11, emoji: '🦈' }, { id: 12, emoji: '⛵' },
    { id: 13, emoji: '🍓' }, { id: 14, emoji: '🍎' },
    { id: 15, emoji: '🥕' }, { id: 16, emoji: '🦋' },
    { id: 17, emoji: '🌸' }, { id: 18, emoji: '⭐' },
    { id: 19, emoji: '🎈' }, { id: 20, emoji: '🌙' },
  ];
  readonly selectedPins = signal<number[]>([]);

  togglePin(id: number): void {
    const pins = this.selectedPins();
    if (pins.includes(id)) {
      this.selectedPins.update(s => s.filter(x => x !== id));
    } else if (pins.length < 2) {
      this.selectedPins.update(s => [...s, id]);
    }
  }
  pinSelected(id: number): boolean { return this.selectedPins().includes(id); }
  pinOrder(id: number):    number   { return this.selectedPins().indexOf(id) + 1; }

  // ── Flow control ────────────────────────────────────────────────────────────
  checkUsername(): void {
    if (!this.studentUsername.trim()) { this.error.set('يرجى إدخال اسم المستخدم.'); return; }
    this.flow.set(this.studentUsername.includes('@') ? 'adult' : 'student');
    if (this.studentUsername.includes('@')) this.adultForm.email = this.studentUsername;
    this.error.set(null);
  }

  backToUsername(): void { this.flow.set('idle'); this.selectedPins.set([]); this.error.set(null); }

  // ── Adult submit ────────────────────────────────────────────────────────────
  submitAdult(): void {
    if (!this.adultForm.email || !this.adultForm.password) {
      this.error.set('يرجى ملء جميع الحقول.'); return;
    }
    this.isLoading.set(true);
    this.error.set(null);

    this.auth.login(this.adultForm.email, this.adultForm.password).subscribe({
      next: res => {
        this.state.setUser({ id: res.userId, name: res.name, role: res.role as any, schoolCode: res.schoolCode });
        this.isLoading.set(false);
        this.redirectByRole(res.role);
      },
      error: err => {
        this.isLoading.set(false);
        this.error.set(err?.error?.error ?? 'فشل تسجيل الدخول. حاول مرة أخرى.');
      }
    });
  }

  // ── Student PIN submit ──────────────────────────────────────────────────────
  submitStudentPin(): void {
    const pins = this.selectedPins();
    if (pins.length < 1) { this.error.set('يرجى اختيار رمز صورة واحد على الأقل.'); return; }

    this.isLoading.set(true);
    this.error.set(null);

    this.auth.studentLogin(this.studentUsername.trim(), pins[0], pins[1] ?? null).subscribe({
      next: res => {
        this.state.setUser({ id: res.studentId, name: res.name, role: 'student', level: res.level });
        this.state.setChildName(res.name);
        this.isLoading.set(false);
        if (!res.placementDone) {
          this.router.navigate(['/test']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: err => {
        this.isLoading.set(false);
        this.error.set(err?.error?.error ?? 'اسم المستخدم أو رمز الصورة غير صحيح.');
      }
    });
  }

  private redirectByRole(role: string): void {
    const map: Record<string, string> = {
      parent:      '/parent/dashboard',
      teacher:     '/teacher/students',
      schooladmin: '/school/dashboard',
      systemadmin: '/admin/content',
      admin:       '/admin/content',
    };
    this.router.navigate([map[role.toLowerCase()] ?? '/dashboard']);
  }
}
