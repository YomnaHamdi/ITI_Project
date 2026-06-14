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

  readonly isLoading    = signal(false);
  readonly children     = signal<StudentSummary[]>([]);
  readonly error        = signal<string | null>(null);
  readonly editingId    = signal<string | null>(null);
  readonly savingId     = signal<string | null>(null);
  readonly saveError    = signal<string | null>(null);

  readonly levels = [
    { value: 1, label: 'المستوى الأول', color: '#F4788A' },
    { value: 2, label: 'المستوى الثاني', color: '#8B5CF6' },
    { value: 3, label: 'المستوى الثالث', color: '#22C55E' },
  ];

  ngOnInit(): void {
    this.isLoading.set(true);
    this.auth.getMyStudents().subscribe({
      next:  c => { this.children.set(c); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('تعذّر تحميل قائمة الأطفال.'); }
    });
  }

  toggleEdit(id: string): void {
    this.editingId.set(this.editingId() === id ? null : id);
    this.saveError.set(null);
  }

  setLevel(child: StudentSummary, level: number): void {
    if (child.level === level) { this.editingId.set(null); return; }
    this.savingId.set(child.id);
    this.saveError.set(null);
    this.auth.updateChildLevel(child.id, level).subscribe({
      next: () => {
        this.children.update(list => list.map(c => c.id === child.id ? { ...c, level } : c));
        this.savingId.set(null);
        this.editingId.set(null);
      },
      error: () => {
        this.savingId.set(null);
        this.saveError.set('تعذّر تحديث المستوى. حاول مجدداً.');
      }
    });
  }

  levelLabel(l: number): string { return `المستوى ${l}`; }
  levelColor(l: number): string { return ['','#F4788A','#8B5CF6','#22C55E'][l] ?? '#F4788A'; }
}
