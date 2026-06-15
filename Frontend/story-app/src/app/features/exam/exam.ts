import { Component, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { AppStateService } from '../../services/app-state-service';
import { StoryService } from '../../services/story';
import { SimpleLoadingComponent } from '../../shared/simple-loading/simple-loading';
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import {
  ExamResponse, ExamResult, SubmitAnswer,
  QuestionDto, QuizType, AnswerFeedback
} from '../../models/story.models';

@Component({
  selector: 'app-exam',
  standalone: true,
  imports: [SimpleLoadingComponent, ErrorToastComponent, DecimalPipe],
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
  readonly activeQ   = signal(0);
  readonly isPlaying = signal(false);
  // After picking an answer: show confirmation UI before moving to next Q
  readonly answered  = signal(false);

  isLessonExam = false;

  ngOnInit(): void {
    const story  = this.state.currentStory();
    const lesson = this.state.currentLesson();
    if (!story && !lesson) { this.router.navigate(['/']); return; }
    if (story)  { this.isLessonExam = false; this.loadExam(story.id); }
    else if (lesson) { this.isLessonExam = true; this.loadLessonExam(lesson.id); }
  }

  private loadExam(storyId: string): void {
    this.isLoading.set(true);
    this.storyService.generateExam(storyId).subscribe({
      next: e  => { this.exam.set(e); this.isLoading.set(false); },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  private loadLessonExam(lessonId: string): void {
    this.isLoading.set(true);
    this.storyService.generateLessonExam(lessonId).subscribe({
      next: e  => { this.exam.set(e); this.isLoading.set(false); },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  // ── MCQ helpers ──────────────────────────────────────────────────────────────
  mcqOptions(q: QuestionDto): { key: string; label: string }[] {
    return [
      { key: 'A', label: q.optionA ?? '' },
      { key: 'B', label: q.optionB ?? '' },
      { key: 'C', label: q.optionC ?? '' },
      { key: 'D', label: q.optionD ?? '' },
    ].filter(o => !!o.label);
  }

  getAnswer(qId: string): string { return this.answers().get(qId) ?? ''; }

  // After final result arrives: feedback per question
  getFeedback(qId: string): AnswerFeedback | undefined {
    return this.result()?.feedback.find(f => f.questionId === qId);
  }

  // Option class DURING answering (before result)
  optionClass(qId: string, key: string): string {
    if (this.result()) {
      // Post-result: colour from backend feedback
      const fb = this.getFeedback(qId);
      if (!fb) return 'opt-card';
      if (key === fb.correctAnswer) return 'opt-card correct';
      if (key === fb.chosenAnswer && !fb.isCorrect) return 'opt-card wrong';
      return 'opt-card';
    }
    // During answering: just highlight selected
    return this.getAnswer(qId) === key ? 'opt-card selected' : 'opt-card';
  }

  pickAnswer(qId: string, choice: string): void {
    if (this.answered()) return;
    this.answers.update(m => { const n = new Map(m); n.set(qId, choice); return n; });
    this.answered.set(true);
    this.speakText('ممتاز');
  }

  nextQ(): void {
    this.answered.set(false);
    const e = this.exam();
    if (!e) return;
    const next = this.activeQ() + 1;
    if (next < e.questions.length) {
      this.activeQ.set(next);
    } else {
      this.submitExam();
    }
  }

  // ── Submission ────────────────────────────────────────────────────────────────
  submitExam(): void {
    const exam = this.exam();
    if (!exam) return;
    const submitAnswers: SubmitAnswer[] = exam.questions.map(q => ({
      questionId:   q.questionId,
      chosenAnswer: this.answers().get(q.questionId) ?? ''
    }));
    this.isLoading.set(true);
    const childName = this.state.childName() || this.state.currentUser()?.name || 'طالب';
    this.storyService.submitExam({ examId: exam.examId, childName, answers: submitAnswers }).subscribe({
      next: res => {
        this.result.set(res);
        this.state.setExamResult(res);
        this.isLoading.set(false);
        // Show correct/wrong feedback via result screen
        const story  = this.state.currentStory();
        const lesson = this.state.currentLesson();
        if (story && !this.isLessonExam) {
          this.storyService.updateProgress({
            storyId: story.id, childName,
            currentPage: this.state.totalPages(),
            totalQuestions: res.totalQuestions, correctAnswers: res.correctAnswers,
            scorePercentage: res.scorePercentage, examCompleted: true
          }).subscribe();
        } else if (lesson && this.isLessonExam) {
          this.storyService.updateLessonProgress({
            lessonId: lesson.id, childName,
            totalQuestions: res.totalQuestions, correctAnswers: res.correctAnswers,
            scorePercentage: res.scorePercentage, examCompleted: true
          }).subscribe();
        }
      },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  // ── Result helpers ────────────────────────────────────────────────────────────
  getCorrectAnswerDisplay(q: QuestionDto, correctAnswer: string): string {
    const map: Record<string, string | undefined> = {
      A: q.optionA, B: q.optionB, C: q.optionC, D: q.optionD
    };
    return map[correctAnswer] ?? correctAnswer;
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

  // ── Audio ─────────────────────────────────────────────────────────────────────
  speakQ(text: string): void { this.speakText(text); }
  speakText(text: string): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'ar-SA'; u.rate = 0.85;
    u.onstart = () => this.isPlaying.set(true);
    u.onend   = () => this.isPlaying.set(false);
    window.speechSynthesis.speak(u);
  }

  goHome():  void { this.state.reset(); this.router.navigate(['/dashboard']); }
  goStory(): void {
    if (this.isLessonExam) { this.router.navigate(['/lessons-list']); return; }
    const story = this.state.currentStory();
    if (story) this.router.navigate(['/books', story.id, 'read']);
    else       this.router.navigate(['/dashboard']);
  }
}
