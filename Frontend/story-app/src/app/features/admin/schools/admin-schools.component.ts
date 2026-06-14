import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-admin-schools',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-schools.component.html',
})
export class AdminSchoolsComponent {
  private readonly service = inject(StoryService);

  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);
  readonly created   = signal<any>(null);

  form = { schoolName: '', adminEmail: '', adminPassword: '' };

  submit(): void {
    this.error.set(null);
    this.created.set(null);
    if (!this.form.schoolName.trim()) { this.error.set('يرجى إدخال اسم المدرسة.'); return; }
    if (!this.form.adminEmail.trim()) { this.error.set('يرجى إدخال البريد الإلكتروني.'); return; }
    if (this.form.adminPassword.length < 6) { this.error.set('كلمة المرور 6 أحرف على الأقل.'); return; }

    this.isLoading.set(true);
    this.service.createSchool({
      schoolName:    this.form.schoolName.trim(),
      adminEmail:    this.form.adminEmail.trim(),
      adminPassword: this.form.adminPassword,
    }).subscribe({
      next: res => {
        this.created.set(res);
        this.isLoading.set(false);
        this.form = { schoolName: '', adminEmail: '', adminPassword: '' };
      },
      error: err => {
        this.error.set(err?.error?.error ?? 'فشل إنشاء المدرسة.');
        this.isLoading.set(false);
      }
    });
  }

  reset(): void { this.created.set(null); this.error.set(null); }
}
