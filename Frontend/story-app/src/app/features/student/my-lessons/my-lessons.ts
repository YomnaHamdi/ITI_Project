import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { LessonSummary } from '../../../models/story.models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-my-lessons',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-lessons.html',
  styleUrl: './my-lessons.css'
})
export class MyLessonsComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);
  readonly api = environment.apiUrl;

  lessons = signal<LessonSummary[]>([]);
  loading = signal(true);
  error   = signal('');

  ngOnInit(): void {
    const user = this.state.currentUser();
    if (!user?.id) { this.error.set('يرجى تسجيل الدخول.'); this.loading.set(false); return; }
    this.svc.getMyLessons(user.id).subscribe({
      next:  l => { this.lessons.set(l); this.loading.set(false); },
      error: () => { this.error.set('فشل تحميل الدروس.'); this.loading.set(false); }
    });
  }

  open(id: string): void { this.router.navigate(['/lessons', id]); }
}
