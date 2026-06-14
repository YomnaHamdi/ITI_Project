import { Component, signal, computed, inject, OnInit } from '@angular/core';
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
export class AdminSchoolsComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading   = signal(false);
  readonly error       = signal<string | null>(null);
  readonly created     = signal<any>(null);
  readonly schools     = signal<any[]>([]);
  readonly searchTerm  = signal('');
  readonly showForm    = signal(false);

  readonly filtered = computed(() => {
    const q = this.searchTerm().toLowerCase();
    return q ? this.schools().filter(s =>
      s.schoolName?.toLowerCase().includes(q) || s.adminEmail?.toLowerCase().includes(q)
    ) : this.schools();
  });

  form = { schoolName: '', adminEmail: '', adminPassword: '' };

  ngOnInit(): void { this.loadSchools(); }

  loadSchools(): void {
    this.isLoading.set(true);
    this.service.getSchools().subscribe({
      next:  s => { this.schools.set(s); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

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
        this.loadSchools();
      },
      error: err => {
        this.error.set(err?.error?.error ?? 'فشل إنشاء المدرسة.');
        this.isLoading.set(false);
      }
    });
  }

  reset(): void { this.created.set(null); this.error.set(null); this.showForm.set(false); }
  setSearch(v: string): void { this.searchTerm.set(v); }
}
