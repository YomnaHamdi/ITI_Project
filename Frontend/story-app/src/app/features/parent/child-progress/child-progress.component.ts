import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { StudentDashboardDto } from '../../../models/story.models';

@Component({
  selector: 'app-child-progress',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './child-progress.component.html',
})
export class ChildProgressComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading   = signal(false);
  readonly childName   = signal('');
  readonly data        = signal<StudentDashboardDto | null>(null);
  readonly error       = signal<string | null>(null);

  ngOnInit(): void {
    const name = this.route.snapshot.paramMap.get('name') ?? '';
    this.childName.set(name);
    if (!name) return;
    this.isLoading.set(true);
    this.svc.getStudentDashboard(name).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات.'); }
    });
  }

  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 50 ? '#F59E0B' : '#EF4444'; }
  barPct(v: number, max: number): number { return max > 0 ? Math.round(v / max * 100) : 0; }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
