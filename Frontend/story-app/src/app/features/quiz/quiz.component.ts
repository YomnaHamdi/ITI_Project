import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../services/story';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';

interface QuizOption { key: string; emoji: string; label: string; }
interface QuizQuestion { id: string; text: string; options: QuizOption[]; correct: string; }

@Component({
  selector: 'app-quiz',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './quiz.component.html',
  styleUrl: './quiz.component.css'
})
export class QuizComponent implements OnInit {
  private readonly router  = inject(Router);
  private readonly route   = inject(ActivatedRoute);
  private readonly service = inject(StoryService);

  readonly isLoading    = signal(false);
  readonly questions    = signal<QuizQuestion[]>([]);
  readonly currentIdx   = signal(0);
  readonly selected     = signal<string | null>(null);
  readonly correctCount = signal(0);
  readonly storyId      = signal('');

  readonly currentQ = computed(() => this.questions()[this.currentIdx()]);
  readonly total    = computed(() => this.questions().length);
  readonly progress = computed(() => Math.round(this.currentIdx() / Math.max(this.total(), 1) * 100));
  readonly isLast   = computed(() => this.currentIdx() === this.total() - 1);

  private readonly mockQuestions: QuizQuestion[] = [
    {
      id: 'q1', text: 'أي حيوان ظهر في القصة؟',
      options: [
        { key:'A', emoji:'🐘', label:'فيل' },
        { key:'B', emoji:'🦁', label:'أسد' },
        { key:'C', emoji:'🐸', label:'ضفدع' },
        { key:'D', emoji:'🐳', label:'حوت' },
      ],
      correct: 'B'
    },
    {
      id: 'q2', text: 'أين تجري أحداث القصة؟',
      options: [
        { key:'A', emoji:'🌲', label:'الغابة' },
        { key:'B', emoji:'🌊', label:'البحر' },
        { key:'C', emoji:'🏙️', label:'المدينة' },
        { key:'D', emoji:'🏔️', label:'الجبل' },
      ],
      correct: 'A'
    }
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.storyId.set(id);
    this.isLoading.set(true);
    this.service.generateExam(id).subscribe({
      next: exam => {
        if (exam?.questions?.length) {
          const mapped: QuizQuestion[] = exam.questions.map(q => ({
            id: q.questionId,
            text: q.text,
            options: ([
              { key: 'A', emoji: '🇦', label: q.optionA ?? '' },
              { key: 'B', emoji: '🇧', label: q.optionB ?? '' },
              { key: 'C', emoji: '🇨', label: q.optionC ?? '' },
              { key: 'D', emoji: '🇩', label: q.optionD ?? '' },
            ] as QuizOption[]).filter(o => o.label),
            correct: ''
          }));
          this.questions.set(mapped);
        } else {
          this.questions.set(this.mockQuestions);
        }
        this.isLoading.set(false);
      },
      error: () => { this.questions.set(this.mockQuestions); this.isLoading.set(false); }
    });
  }

  selectAnswer(key: string): void {
    if (this.selected()) return;
    this.selected.set(key);
    if (key === this.currentQ().correct) this.correctCount.update(n => n + 1);
    setTimeout(() => this.advance(), 1400);
  }

  private advance(): void {
    if (this.isLast()) {
      sessionStorage.setItem('quiz_result', JSON.stringify({
        correct: this.correctCount(),
        total:   this.total(),
        storyId: this.storyId()
      }));
      this.router.navigate(['/books', this.storyId(), 'result']);
    } else {
      this.currentIdx.update(i => i + 1);
      this.selected.set(null);
    }
  }

  optionClass(key: string): string {
    const sel = this.selected();
    if (!sel) return 'opt';
    if (key === this.currentQ().correct) return 'opt correct';
    if (key === sel)                     return 'opt wrong';
    return 'opt';
  }
}
