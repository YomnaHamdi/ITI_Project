import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading   = signal(false);
  readonly users       = signal<any[]>([]);
  readonly searchTerm  = signal('');
  readonly roleFilter  = signal('');
  readonly currentPage = signal(1);

  readonly roleOptions = [
    { value: '',        label: 'جميع الأدوار' },
    { value: 'teacher', label: 'معلم' },
    { value: 'parent',  label: 'ولي أمر' },
    { value: 'student', label: 'طالب' },
    { value: 'school',  label: 'مدرسة' },
  ];

  readonly filteredUsers = computed(() => {
    const q    = this.searchTerm().toLowerCase();
    const role = this.roleFilter();
    return this.users()
      .filter(u => u.role !== 'admin' && u.role !== 'systemadmin')
      .filter(u => !role || u.role === role)
      .filter(u => !q || u.name?.toLowerCase().includes(q) || u.email?.toLowerCase().includes(q) || u.role?.toLowerCase().includes(q));
  });

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredUsers().length / PAGE_SIZE)));

  readonly pagedUsers = computed(() => {
    const page = this.currentPage();
    const start = (page - 1) * PAGE_SIZE;
    return this.filteredUsers().slice(start, start + PAGE_SIZE);
  });

  readonly pages = computed(() => Array.from({ length: this.totalPages() }, (_, i) => i + 1));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.service.getAllUsers().subscribe({
      next:  u => { this.users.set(u); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  setSearch(v: string): void  { this.searchTerm.set(v);  this.currentPage.set(1); }
  setRole(v: string): void    { this.roleFilter.set(v);  this.currentPage.set(1); }
  goToPage(p: number): void   { this.currentPage.set(p); }

  toggleBlock(user: any): void {
    const action = user.isBlocked ? this.service.unblockUser(user.id) : this.service.blockUser(user.id);
    action.subscribe({ next: () => this.load() });
  }

  roleLabel(role: string): string {
    const map: Record<string,string> = { admin: 'مدير', teacher: 'معلم', parent: 'ولي أمر', student: 'طالب', school: 'مدرسة' };
    return map[role] ?? role;
  }

  roleColor(role: string): string {
    const map: Record<string,string> = { teacher: '#0EA5E9', parent: '#10B981', student: '#F59E0B', school: '#7C3AED' };
    return map[role] ?? '#9CA3AF';
  }
}
