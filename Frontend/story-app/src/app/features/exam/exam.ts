import { Component, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe, NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { AppStateService } from '../../services/app-state-service';
import { StoryService } from '../../services/story';
import { SimpleLoadingComponent } from '../../shared/simple-loading/simple-loading';
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import {
  ExamResponse, ExamResult, SubmitAnswer,
  QuestionDto, QuizType, MatchPair
} from '../../models/story.models';

@Component({
  selector: 'app-exam',
  standalone: true,
  imports: [SimpleLoadingComponent, ErrorToastComponent, DecimalPipe, NgClass],
  templateUrl: './exam.html',
  styleUrl: './exam.css',
})
export class Exam implements OnInit {
  private readonly storyService = inject(StoryService);
  private readonly state        = inject(AppStateService);
  private readonly router       = inject(Router);

  readonly QuizType = QuizType;

  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);
  readonly exam      = signal<ExamResponse | null>(null);
  readonly result    = signal<ExamResult | null>(null);
  readonly answers   = signal<Map<string, string>>(new Map());

  // Drag-drop state: questionId -> word currently being dragged
  dragWord = signal<string | null>(null);
  // Matching state: questionId -> { left: string, selectedRight: string }[]
  matchSelections = signal<Map<string, string>>(new Map());
  // Ordering state: questionId -> string[]
  orderItems = signal<Map<string, string[]>>(new Map());

  isLessonExam = false;

  ngOnInit(): void {
    const story  = this.state.currentStory();
    const lesson = this.state.currentLesson();
    if (!story && !lesson) { this.router.navigate(['/']); return; }

    if (story) {
      this.isLessonExam = false;
      this.loadExam(story.id);
    } else if (lesson) {
      this.isLessonExam = true;
      this.loadLessonExam(lesson.id);
    }
  }

  private loadExam(storyId: string): void {
    this.isLoading.set(true);
    this.storyService.generateExam(storyId).subscribe({
      next: exam => { this.exam.set(exam); this.initQuestionState(exam); this.isLoading.set(false); },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  private loadLessonExam(lessonId: string): void {
    this.isLoading.set(true);
    this.storyService.generateLessonExam(lessonId).subscribe({
      next: exam => { this.exam.set(exam); this.initQuestionState(exam); this.isLoading.set(false); },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  private initQuestionState(exam: ExamResponse): void {
    const orderMap = new Map<string, string[]>();
    for (const q of exam.questions) {
      if (q.type === QuizType.Ordering && q.dataJson) {
        try {
          const parsed = JSON.parse(q.dataJson) as { words?: string[] };
          orderMap.set(q.questionId, [...(parsed.words ?? [])]);
        } catch { orderMap.set(q.questionId, []); }
      }
    }
    this.orderItems.set(orderMap);
  }

  // ── MCQ ──────────────────────────────────────────────────────────────────────
  selectAnswer(qId: string, choice: string): void {
    if (this.result()) return;
    this.answers.update(m => { const n = new Map(m); n.set(qId, choice); return n; });
  }

  getAnswer(qId: string): string { return this.answers().get(qId) ?? ''; }

  // ── Matching ──────────────────────────────────────────────────────────────────
  getPairs(q: QuestionDto): MatchPair[] {
    if (!q.dataJson) return [];
    try {
      const parsed = JSON.parse(q.dataJson) as { pairs?: MatchPair[] };
      return parsed.pairs ?? [];
    } catch { return []; }
  }

  selectMatch(qId: string, pairIndex: number, right: string): void {
    if (this.result()) return;
    this.matchSelections.update(m => { const n = new Map(m); n.set(`${qId}:${pairIndex}`, right); return n; });
    this.syncMatchAnswer(qId);
  }

  private syncMatchAnswer(qId: string): void {
    const q = this.exam()?.questions.find(x => x.questionId === qId);
    if (!q) return;
    const pairs = this.getPairs(q);
    const arr   = pairs.map((p, i) => ({
      left:  p.left,
      right: this.matchSelections().get(`${qId}:${i}`) ?? ''
    }));
    if (arr.every(a => a.right)) {
      // Backend expects a flat string[] of right-side values (not objects)
      this.answers.update(m => { const n = new Map(m); n.set(qId, JSON.stringify(arr.map(a => a.right))); return n; });
    }
  }

  getMatchedRight(qId: string, pairIndex: number): string {
    return this.matchSelections().get(`${qId}:${pairIndex}`) ?? '';
  }

  // ── DragDrop ─────────────────────────────────────────────────────────────────
  getDragWords(q: QuestionDto): string[] {
    if (!q.dataJson) return [];
    try {
      const parsed = JSON.parse(q.dataJson) as { options?: string[] };
      return parsed.options ?? [];
    } catch { return []; }
  }

  getDragSentence(q: QuestionDto): string {
    if (!q.dataJson) return '';
    try {
      const parsed = JSON.parse(q.dataJson) as { sentence?: string };
      return parsed.sentence ?? '';
    } catch { return ''; }
  }

  onDragStart(word: string): void { this.dragWord.set(word); }

  onDrop(qId: string): void {
    const word = this.dragWord();
    if (!word || this.result()) return;
    this.answers.update(m => { const n = new Map(m); n.set(qId, word); return n; });
    this.dragWord.set(null);
  }

  onDragOver(event: DragEvent): void { event.preventDefault(); }

  // ── Ordering ─────────────────────────────────────────────────────────────────
  getOrderItems(qId: string): string[] {
    return this.orderItems().get(qId) ?? [];
  }

  moveItem(qId: string, fromIdx: number, toIdx: number): void {
    if (this.result()) return;
    this.orderItems.update(m => {
      const n    = new Map(m);
      const arr  = [...(n.get(qId) ?? [])];
      const item = arr.splice(fromIdx, 1)[0];
      arr.splice(toIdx, 0, item);
      n.set(qId, arr);
      this.answers.update(a => { const na = new Map(a); na.set(qId, JSON.stringify(arr)); return na; });
      return n;
    });
  }

  // ── Submission ────────────────────────────────────────────────────────────────
  allAnswered(): boolean {
    const exam = this.exam();
    return !!exam && exam.questions.every(q => this.answers().has(q.questionId));
  }

  submitExam(): void {
    const exam = this.exam();
    if (!exam) return;

    const submitAnswers: SubmitAnswer[] = exam.questions.map(q => ({
      questionId:   q.questionId,
      chosenAnswer: this.answers().get(q.questionId) ?? ''
    }));

    this.isLoading.set(true);
    const childName = this.state.childName() || this.state.currentUser()?.name || 'طالب';
    this.storyService.submitExam({
      examId:    exam.examId,
      childName,
      answers:   submitAnswers
    }).subscribe({
      next: res => {
        this.result.set(res);
        this.state.setExamResult(res);
        this.isLoading.set(false);
        const story  = this.state.currentStory();
        const lesson = this.state.currentLesson();
        if (story && !this.isLessonExam) {
          this.storyService.updateProgress({
            storyId:         story.id,
            childName,
            currentPage:     this.state.totalPages(),
            totalQuestions:  res.totalQuestions,
            correctAnswers:  res.correctAnswers,
            scorePercentage: res.scorePercentage,
            examCompleted:   true
          }).subscribe();
        } else if (lesson && this.isLessonExam) {
          this.storyService.updateLessonProgress({
            lessonId:        lesson.id,
            childName,
            totalQuestions:  res.totalQuestions,
            correctAnswers:  res.correctAnswers,
            scorePercentage: res.scorePercentage,
            examCompleted:   true
          }).subscribe();
        }
      },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  getFeedback(qId: string) {
    return this.result()?.feedback.find(f => f.questionId === qId);
  }

  getQuestionImage(q: QuestionDto): string | null {
    if (q.imageUrl) return q.imageUrl;
    if (!q.dataJson || q.dataJson === '{}') return null;
    try {
      const parsed = JSON.parse(q.dataJson) as { imageUrl?: string };
      return parsed.imageUrl ?? null;
    } catch { return null; }
  }

  getCorrectAnswerDisplay(q: QuestionDto, correctAnswer: string): string {
    // MCQ: resolve letter key → option text
    if (q.type === QuizType.MCQ) {
      const map: Record<string, string | undefined> = {
        A: q.optionA, B: q.optionB, C: q.optionC, D: q.optionD
      };
      return map[correctAnswer] ?? correctAnswer;
    }

    // Matching / Ordering: parse JSON → display readable Arabic
    if (q.type === QuizType.Matching || q.type === QuizType.Ordering) {
      try {
        const parsed = JSON.parse(correctAnswer);
        if (Array.isArray(parsed)) {
          if (parsed.length > 0 && typeof parsed[0] === 'object') {
            // [{left, right}] pairs
            return (parsed as { left: string; right: string }[])
              .map(p => `${p.right}`)
              .join('، ');
          }
          // string[]
          return (parsed as string[]).join(' ← ');
        }
      } catch { /* show as-is */ }
    }

    return correctAnswer;
  }

  optionClass(qId: string, opt: string): string {
    const sel = this.getAnswer(qId) === opt;
    const fb  = this.getFeedback(qId);
    if (!fb)                                             return sel ? 'opt-sel' : 'opt';
    if (opt === fb.correctAnswer)                        return 'opt-correct';
    if (sel && opt === fb.chosenAnswer && !fb.isCorrect) return 'opt-wrong';
    return 'opt';
  }

  questionStatusClass(qId: string): string {
    const fb = this.getFeedback(qId);
    if (!fb) return '';
    return fb.isCorrect ? 'q-correct' : 'q-wrong';
  }

  scoreEmoji(): string {
    const s = this.result()?.scorePercentage ?? 0;
    return s >= 80 ? '🌟' : s >= 60 ? '👍' : '💪';
  }

  starsCount(): number {
    const s = this.result()?.scorePercentage ?? 0;
    return s >= 80 ? 3 : s >= 50 ? 2 : 1;
  }

  readonly starsArr = [1, 2, 3];

  goHome():  void { this.state.reset(); this.router.navigate(['/dashboard']); }
  goStory(): void {
    if (this.isLessonExam) {
      this.router.navigate(['/lessons-list']);
    } else {
      const story = this.state.currentStory();
      if (story) this.router.navigate(['/books', story.id, 'read']);
      else       this.router.navigate(['/dashboard']);
    }
  }
}
