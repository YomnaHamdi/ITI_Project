import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';

@Component({
  selector: 'app-lesson-list',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './lesson-list.html',
  styleUrl: './lesson-list.css'
})
export class LessonListComponent implements OnInit {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly lessons      = signal<any[]>([]);
  readonly isLoading    = signal(false);
  readonly studentLevel = signal(1);

  readonly categories = [
    { key: 'letters',    icon: 'أ',  label: 'الحروف',   sublabel: '28 حرفاً',   color: '#FFE4E8', progress: 60 },
    { key: 'words',      icon: '📝', label: 'الكلمات',  sublabel: '200+ كلمة', color: '#EDE9FE', progress: 30 },
    { key: 'sentences',  icon: '📖', label: 'الجمل',    sublabel: 'جمل كاملة', color: '#D1FAE5', progress: 15 },
    { key: 'activities', icon: '🎮', label: 'الأنشطة',  sublabel: 'تفاعلية',   color: '#FFF7ED', progress: 45 },
  ];

  ngOnInit(): void {
    const level = this.state.currentUser()?.level ?? 1;
    this.studentLevel.set(level);
    this.isLoading.set(true);
    this.service.getLessonsByLevel(level).subscribe({
      next:  l => { this.lessons.set(l); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  changeLevel(level: number): void {
    this.studentLevel.set(level);
    this.isLoading.set(true);
    this.service.getLessonsByLevel(level).subscribe({
      next:  l => { this.lessons.set(l); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  openLesson(id: string): void { this.router.navigate(['/lessons', id]); }
}
