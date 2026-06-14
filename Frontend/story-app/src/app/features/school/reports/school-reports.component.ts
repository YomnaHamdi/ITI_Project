import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { SchoolDashboardDto } from '../../../models/story.models';

@Component({
  selector: 'app-school-reports',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-reports.component.html',
})
export class SchoolReportsComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading = signal(false);
  readonly data      = signal<SchoolDashboardDto | null>(null);

  readonly maxBand = computed(() => {
    const bands = this.data()?.performanceBands ?? [];
    return bands.length > 0 ? Math.max(...bands.map(b => b.count)) : 1;
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getSchoolDashboard().subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  bandColor(band: string): string {
    const m: Record<string,string> = { ممتاز: '#22C55E', جيد: '#F59E0B', ضعيف: '#EF4444' };
    return m[band] ?? '#F4788A';
  }

  barWidth(count: number): number {
    return Math.round(count / this.maxBand() * 100);
  }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
}
