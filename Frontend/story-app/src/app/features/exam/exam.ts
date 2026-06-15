import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { AppStateService } from '../../services/app-state-service';
import { StoryService } from '../../services/story';
import { SimpleLoadingComponent } from '../../shared/simple-loading/simple-loading';
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import {
  ExamResponse, ExamResult, SubmitAnswer,
  QuestionDto, QuizType
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

  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);
  readonly exam       = signal<ExamResponse | null>(null);
  readonly result     = signal<ExamResult | null>(null);
  readonly answers    = signal<Map<string, string>>(new Map());
  readonly activeQ    = signal(0);           // current question index
  readonly isPlaying  = signal(false);

  // Per-question feedback shown immediately after answering (before moving on)
  readonly currentFeedback = signal<{ isCorrect: boolean; correctAnswer: string; chosenAnswer: string } | null>(null);

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
      next: exam => { this.exam.set(exam); this.isLoading.set(false); },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  private loadLessonExam(lessonId: string): void {
    this.isLoading.set(true);
    this.storyService.generateLessonExam(lessonId).subscribe({
      next: exam => { this.exam.set(exam); this.isLoading.set(false); },
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
    ];
  }

  getAnswer(qId: string): string { return this.answers().get(qId) ?? ''; }

  // Feedback for the currently active question (before submission)
  qFeedback(): { isCorrect: boolean; correctAnswer: string; chosenAnswer: string } | null {
    return this.currentFeedback();
  }

  pickAnswer(qId: string, choice: string): void {
    if (this.currentFeedback()) return;  // already answered this Q
    this.answers.update(m => { const n = new Map(m); n.set(qId, choice); return n; });

    // Determine correct answer locally
    const q = this.exam()?.questions.find(x => x.questionId === qId);
    if (!q) return;
    const correct = q.correctAnswer ?? '';
    const isCorrect = choice === correct;

    // Speak feedback
    this.speakText(isCorrect ? 'شاطر' : 'حاول مجدداً');

    this.currentFeedback.set({ isCorrect, correctAnswer: correct, chosenAnswer: choice });
  }

  nextQ(): void {
    this.currentFeedback.set(null);
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
      error: (err: Error) => {
        // Fallback: calculate locally
        const qs  = exam.questions;
        const ans = this.answers();
        const correct = qs.filter(q => ans.get(q.questionId) === (q.correctAnswer ?? '')).length;
        const pct = Math.round(correct / qs.length * 100);
        this.result.set({
          examId:          exam.examId,
          correctAnswers:  correct,
          totalQuestions:  qs.length,
          scorePercentage: pct,
          feedback: qs.map(q => ({
            questionId:    q.questionId,
            isCorrect:     ans.get(q.questionId) === (q.correctAnswer ?? ''),
            correctAnswer: q.correctAnswer ?? '',
            chosenAnswer:  ans.get(q.questionId) ?? ''
          }))
        } as ExamResult);
        this.isLoading.set(false);
      }
    });
  }

  // ── Result helpers ────────────────────────────────────────────────────────────
  getFeedback(qId: string) {
    return this.result()?.feedback.find(f => f.questionId === qId);
  }

  getCorrectAnswerDisplay(q: QuestionDto, correctAnswer: string): string {
    if (q.type === QuizType.MCQ || q.type == null) {
      const map: Record<string, string | undefined> = {
        A: q.optionA, B: q.optionB, C: q.optionC, D: q.optionD
      };
      return map[correctAnswer] ?? correctAnswer;
    }
    try {
      const parsed = JSON.parse(correctAnswer);
      if (Array.isArray(parsed)) return (parsed as string[]).join(' ← ');
    } catch { /* ignore */ }
    return correctAnswer;
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

  // ── Navigation ────────────────────────────────────────────────────────────────
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
