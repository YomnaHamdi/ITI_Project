import { Component, signal, OnInit, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-quiz-result',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './quiz-result.component.html',
  styleUrl: './quiz-result.component.css'
})
export class QuizResultComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  readonly correct  = signal(0);
  readonly total    = signal(2);
  readonly storyId  = signal('');
  readonly stars    = signal(0);
  readonly points   = signal(0);
  readonly confetti = signal<{ x: number; y: number; color: string; delay: number; size: number }[]>([]);

  readonly starsArr = [1, 2, 3];

  ngOnInit(): void {
    this.storyId.set(this.route.snapshot.paramMap.get('id') ?? '');
    const raw = sessionStorage.getItem('quiz_result');
    if (raw) {
      const r = JSON.parse(raw);
      this.correct.set(r.correct);
      this.total.set(r.total);
    }
    const ratio = this.correct() / Math.max(this.total(), 1);
    this.stars.set(ratio >= 0.8 ? 3 : ratio >= 0.5 ? 2 : 1);
    this.points.set(this.stars() * 10);
    this.generateConfetti();
  }

  private generateConfetti(): void {
    const colors = ['#F4788A', '#C4B5FD', '#86EFAC', '#FDE68A', '#93C5FD'];
    this.confetti.set(
      Array.from({ length: 40 }, (_, i) => ({
        x:     Math.random() * 100,
        y:     Math.random() * 80,
        color: colors[i % colors.length],
        delay: Math.random() * 2,
        size:  Math.random() * 8 + 6
      }))
    );
  }

  tryAgain(): void { this.router.navigate(['/books', this.storyId(), 'read']); }
  nextBook():  void { this.router.navigate(['/levels']); }
  share():     void { alert('تمت مشاركة الوسام! 🎉'); }
}
