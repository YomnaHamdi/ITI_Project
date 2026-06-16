import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';

interface ClassroomReport {
  classroomId:   string;
  classroomName: string;
  level:         number;
  teacherName:   string;
  studentCount:  number;
  avgScore:      number;
  students:      { name: string; username: string; level: number; placementDone: boolean; avgScore: number }[];
  expanded:      boolean;
}

@Component({
  selector: 'app-school-reports',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-reports.component.html',
})
export class SchoolReportsComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading       = signal(false);
  readonly dashboardData   = signal<any>(null);
  readonly classroomReport = signal<ClassroomReport[]>([]);

  readonly totalStudents    = computed(() => this.dashboardData()?.totalStudents  ?? 0);
  readonly totalTeachers    = computed(() => this.dashboardData()?.totalTeachers  ?? 0);
  readonly activeThisWeek   = computed(() => this.dashboardData()?.activeThisWeek ?? 0);
  readonly avgSchoolScore   = computed(() => this.dashboardData()?.avgSchoolScore ?? 0);
  readonly performanceBands = computed(() => this.dashboardData()?.performanceBands ?? []);
  readonly maxBand = computed(() => {
    const bands = this.performanceBands();
    return bands.length ? Math.max(...bands.map((b: any) => b.count)) : 1;
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getSchoolDashboard().subscribe({
      next:  d => { this.dashboardData.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
    this.svc.getClassroomsReport().subscribe({
      next:  list => this.classroomReport.set(list.map((c: any) => ({ ...c, expanded: false }))),
      error: () => {}
    });
  }

  toggleExpand(id: string): void {
    this.classroomReport.update(list =>
      list.map(c => c.classroomId === id ? { ...c, expanded: !c.expanded } : c)
    );
  }

  bandColor(band: string): string {
    return band === 'ممتاز' ? '#22C55E' : band === 'جيد' ? '#F59E0B' : '#EF4444';
  }
  barWidth(count: number): number { return Math.round(count / this.maxBand() * 100); }
  levelColor(l: number): string   { return ['','#F4788A','#8B5CF6','#22C55E'][l] ?? '#F4788A'; }
  scoreColor(s: number): string   { return s >= 80 ? '#22C55E' : s >= 50 ? '#F59E0B' : '#EF4444'; }
}
