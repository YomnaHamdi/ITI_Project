import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AuthService, StudentSummary } from '../../../services/auth.service';

@Component({
  selector: 'app-parent-children',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './parent-children.component.html',
})
export class ParentChildrenComponent implements OnInit {
  private readonly auth = inject(AuthService);

  readonly isLoading = signal(false);
  readonly children  = signal<StudentSummary[]>([]);
  readonly error     = signal<string | null>(null);

  ngOnInit(): void {
    this.isLoading.set(true);
    this.auth.getMyStudents().subscribe({
      next:  c => { this.children.set(c); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('تعذّر تحميل قائمة الأطفال.'); }
    });
  }

  levelLabel(l: number): string { return `المستوى ${l}`; }
  levelColor(l: number): string { return ['','#F4788A','#8B5CF6','#22C55E'][l] ?? '#F4788A'; }
}
