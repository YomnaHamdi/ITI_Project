import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { LevelProgressDto } from '../../models/story.models';

@Component({
  selector: 'app-levels',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './levels.component.html',
  styleUrl: './levels.component.css'
})
export class LevelsComponent implements OnInit {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly isLoading = signal(false);
  readonly levels    = signal<LevelProgressDto[]>([]);

  ngOnInit(): void {
    const childName = this.state.childName();
    if (!childName) return;
    this.isLoading.set(true);
    this.service.getLevelProgress(childName).subscribe({
      next:  data => { this.levels.set(data); this.isLoading.set(false); },
      error: ()   => this.isLoading.set(false)
    });
  }

  openLevel(level: LevelProgressDto): void {
    if (!level.locked) this.router.navigate(['/levels', level.level, 'books']);
  }

  progressPct(level: LevelProgressDto): number {
    return level.totalLessons > 0
      ? Math.round(level.lessonsCompleted / level.totalLessons * 100) : 0;
  }

  starsEarned(level: LevelProgressDto): number {
    return level.totalStars > 0
      ? Math.round(level.stars / level.totalStars * 5) : 0;
  }

  readonly starDots = [1, 2, 3, 4, 5];
}
