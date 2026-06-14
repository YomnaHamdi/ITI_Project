import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading  = signal(false);
  readonly users      = signal<any[]>([]);
  readonly searchTerm = signal('');

  readonly filteredUsers = computed(() => {
    const q = this.searchTerm().toLowerCase();
    if (!q) return this.users();
    return this.users().filter(u =>
      u.name?.toLowerCase().includes(q) ||
      u.email?.toLowerCase().includes(q) ||
      u.role?.toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getAllUsers().subscribe({
      next:  u => { this.users.set(u); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  setSearch(v: string) { this.searchTerm.set(v); }

  toggleBlock(user: any): void {
    const action = user.isBlocked ? this.service.unblockUser(user.id) : this.service.blockUser(user.id);
    action.subscribe({ next: () => this.ngOnInit() });
  }

  roleLabel(role: string): string {
    const map: Record<string,string> = { admin: 'مدير', teacher: 'معلم', parent: 'ولي أمر', student: 'طالب', school: 'مدرسة' };
    return map[role] ?? role;
  }
}
