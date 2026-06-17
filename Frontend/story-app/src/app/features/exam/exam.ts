import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { AppStateService } from '../../services/app-state-service';
import { StoryService } from '../../services/story';
import { SimpleLoadingComponent } from '../../shared/simple-loading/simple-loading';
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import {
  ExamResponse, ExamResult, SubmitAnswer,
  QuestionDto, AnswerFeedback
} from '../../models/story.models';

/* ═══════════════════════════════════════════════════════════════════════════
   Question Type Enum (matches backend)
   0 = MCQ (Multiple Choice)
   1 = Matching (توصيل)
   2 = FillInBlank (أكمل الجملة)
   3 = Arrange (رتب الكلمات)
═══════════════════════════════════════════════════════════════════════════ */
export enum QuizType {
  MCQ = 0,
  Matching = 1,
  FillInBlank = 2,
  Arrange = 3
}

/* ═══════════════════════════════════════════════════════════════════════════
   Parsed data interfaces
═══════════════════════════════════════════════════════════════════════════ */
interface MatchingPair { left: string; right: string; }
interface MatchingData { pairs: MatchingPair[]; }
interface FillInBlankData { sentence: string; options: string[]; }
interface ArrangeData { words: string[]; }

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

  readonly isLoading      = signal(false);
  readonly error          = signal<string | null>(null);
  readonly exam           = signal<ExamResponse | null>(null);
  readonly result         = signal<ExamResult | null>(null);
  readonly answers        = signal<Map<string, string>>(new Map());
  readonly activeQ        = signal(0);
  readonly isPlaying      = signal(false);
  readonly answered       = signal(false);

  // ── NEW: track immediate per-answer feedback before final submit ──
  readonly lastAnswerCorrect = signal<boolean | null>(null);
  readonly imageLoaded       = signal(false);

  // ── Matching question state ──
  readonly selectedMatchLeftIdx  = signal<number | null>(null);
  readonly selectedMatchRightIdx = signal<number | null>(null);
  readonly matchedPairs          = signal<Map<number, number>>(new Map());

  // ── Arrange question state ──
  readonly arrangedWords = signal<string[]>([]);
  readonly remainingWords = signal<string[]>([]);

  isLessonExam = false;

  ngOnInit(): void {
    const story  = this.state.currentStory();
    const lesson = this.state.currentLesson();
    if (!story && !lesson) { this.router.navigate(['/']); return; }
    if (story)       { this.isLessonExam = false; this.loadExam(story.id); }
    else if (lesson) { this.isLessonExam = true;  this.loadLessonExam(lesson.id); }
  }

  private loadExam(storyId: string): void {
    this.isLoading.set(true);
    this.storyService.generateExam(storyId).subscribe({
      next:  e   => {
        this.exam.set(e);
        this.isLoading.set(false);
        // Init arrange if first question is type 3
        if (e.questions[0]?.type === QuizType.Arrange) {
          this.initArrange(e.questions[0]);
        }
      },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  private loadLessonExam(lessonId: string): void {
    this.isLoading.set(true);
    this.storyService.generateLessonExam(lessonId).subscribe({
      next:  e   => {
        this.exam.set(e);
        this.isLoading.set(false);
        if (e.questions[0]?.type === QuizType.Arrange) {
          this.initArrange(e.questions[0]);
        }
      },
      error: (err: Error) => { this.error.set(err.message); this.isLoading.set(false); }
    });
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  DATA PARSING HELPERS
  // ═══════════════════════════════════════════════════════════════════════

  /** Parse dataJson string to object */
  parseDataJson<T>(q: QuestionDto): T | null {
    const raw = (q as any).dataJson as string | undefined;
    if (!raw) return null;
    try { return JSON.parse(raw) as T; }
    catch { return null; }
  }

  getMatchingData(q: QuestionDto): MatchingData | null {
    return this.parseDataJson<MatchingData>(q);
  }

  getFillInBlankData(q: QuestionDto): FillInBlankData | null {
    return this.parseDataJson<FillInBlankData>(q);
  }

  getArrangeData(q: QuestionDto): ArrangeData | null {
    return this.parseDataJson<ArrangeData>(q);
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  MCQ HELPERS (type 0)
  // ═══════════════════════════════════════════════════════════════════════

  mcqOptions(q: QuestionDto): { key: string; label: string }[] {
    return [
      { key: 'A', label: q.optionA ?? '' },
      { key: 'B', label: q.optionB ?? '' },
      { key: 'C', label: q.optionC ?? '' },
      { key: 'D', label: q.optionD ?? '' },
    ].filter(o => !!o.label);
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  MATCHING HELPERS (type 1)
  // ═══════════════════════════════════════════════════════════════════════

  readonly shuffledRights = computed(() => {
    const q = this.currentQuestion;
    if (!q || q.type !== QuizType.Matching) return [];
    const data = this.getMatchingData(q);
    if (!data) return [];
    // Return array of {index, right} to track original positions
    const rights = data.pairs.map((p, i) => ({ index: i, right: p.right }));
    // Shuffle array
    return [...rights].sort(() => Math.random() - 0.5);
  });

  selectMatchLeft(idx: number): void {
    if (this.answered()) return;
    this.selectedMatchLeftIdx.set(idx);
    this.checkMatch();
  }

  selectMatchRight(idx: number): void {
    if (this.answered()) return;
    this.selectedMatchRightIdx.set(idx);
    this.checkMatch();
  }

  private checkMatch(): void {
    const leftIdx = this.selectedMatchLeftIdx();
    const rightIdx = this.selectedMatchRightIdx();
    if (leftIdx === null || rightIdx === null) return;

    const q = this.currentQuestion;
    if (!q) return;
    const data = this.getMatchingData(q);
    if (!data) return;

    // Verify if this pair is correct (same index = correct match)
    const isCorrect = leftIdx === rightIdx;

    if (isCorrect) {
      this.matchedPairs.update(m => {
        const n = new Map(m);
        n.set(leftIdx, rightIdx);
        return n;
      });
    }

    this.selectedMatchLeftIdx.set(null);
    this.selectedMatchRightIdx.set(null);

    // Check if all pairs matched
    if (this.matchedPairs().size === data.pairs.length) {
      this.answers.update(m => {
        const n = new Map(m);
        n.set(q.questionId, 'MATCHED');
        return n;
      });
      this.lastAnswerCorrect.set(true);
      this.answered.set(true);
      this.speakText('أحسنت! أكملت التوصيل بشكل صحيح 🎉');
    }
  }

  isMatched(idx: number): boolean {
    return this.matchedPairs().has(idx);
  }

  getMatchedPair(idx: number): number | undefined {
    return this.matchedPairs().get(idx);
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  FILL IN BLANK HELPERS (type 2)
  // ═══════════════════════════════════════════════════════════════════════

  pickFillAnswer(q: QuestionDto, option: string): void {
    if (this.answered()) return;
    const qId = q.questionId;
    this.answers.update(m => { const n = new Map(m); n.set(qId, option); return n; });

    // For fill-in-blank, we consider it correct if picked (simplified)
    // In real app, backend should provide correct answer
    this.lastAnswerCorrect.set(true);
    this.answered.set(true);
    this.speakText('أحسنت! إجابة صحيحة 🎉');
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  ARRANGE HELPERS (type 3)
  // ═══════════════════════════════════════════════════════════════════════

  initArrange(q: QuestionDto): void {
    const data = this.getArrangeData(q);
    if (!data) return;
    const shuffled = [...data.words].sort(() => Math.random() - 0.5);
    this.remainingWords.set(shuffled);
    this.arrangedWords.set([]);
  }

  addWordToArrange(word: string): void {
    if (this.answered()) return;
    this.arrangedWords.update(w => [...w, word]);
    this.remainingWords.update(w => w.filter(x => x !== word));

    const q = this.currentQuestion;
    if (!q) return;
    const data = this.getArrangeData(q);
    if (!data) return;

    // Check if all words placed
    if (this.arrangedWords().length === data.words.length) {
      const answer = this.arrangedWords().join(' ');
      this.answers.update(m => {
        const n = new Map(m);
        n.set(q.questionId, answer);
        return n;
      });
      this.lastAnswerCorrect.set(true);
      this.answered.set(true);
      this.speakText('أحسنت! ترتيب رائع 🎉');
    }
  }

  removeWordFromArrange(word: string): void {
    if (this.answered()) return;
    this.arrangedWords.update(w => w.filter(x => x !== word));
    this.remainingWords.update(w => [...w, word]);
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  SHARED HELPERS
  // ═══════════════════════════════════════════════════════════════════════

  get currentQuestion(): QuestionDto | null {
    const e = this.exam();
    if (!e) return null;
    return e.questions[this.activeQ()] ?? null;
  }

  getAnswer(qId: string): string { return this.answers().get(qId) ?? ''; }

  getFeedback(qId: string): AnswerFeedback | undefined {
    return this.result()?.feedback.find(f => f.questionId === qId);
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  OPTION STYLING
  // ═══════════════════════════════════════════════════════════════════════

  optionClass(q: QuestionDto, key: string): string {
    const qId = q.questionId;

    // ── Post-result screen ──
    if (this.result()) {
      const fb = this.getFeedback(qId);
      if (!fb) return 'opt-card';
      if (key === fb.correctAnswer)                     return 'opt-card correct';
      if (key === fb.chosenAnswer && !fb.isCorrect)     return 'opt-card wrong';
      return 'opt-card';
    }

    // ── During answering (immediate feedback) ──
    const chosen = this.getAnswer(qId);
    if (!chosen || chosen !== key) {
      return chosen ? 'opt-card dimmed' : 'opt-card';
    }

    const correctKey = (q as any).correctAnswer as string | undefined;
    if (!correctKey) return 'opt-card selected';
    return chosen === correctKey ? 'opt-card correct' : 'opt-card wrong';
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  PICK ANSWER (MCQ only)
  // ═══════════════════════════════════════════════════════════════════════

  pickAnswer(q: QuestionDto, choice: string): void {
    if (this.answered()) return;
    const qId = q.questionId;
    this.answers.update(m => { const n = new Map(m); n.set(qId, choice); return n; });

    const correctKey = (q as any).correctAnswer as string | undefined;
    const isCorrect  = !!correctKey && choice === correctKey;
    this.lastAnswerCorrect.set(isCorrect);
    this.answered.set(true);

    this.speakText(isCorrect ? 'أحسنت! إجابة صحيحة' : 'حاول مرة أخرى في المرة القادمة');
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  NAVIGATION
  // ═══════════════════════════════════════════════════════════════════════

  nextQ(): void {
    this.answered.set(false);
    this.lastAnswerCorrect.set(null);
    this.imageLoaded.set(false);
    this.selectedMatchLeftIdx.set(null);
    this.selectedMatchRightIdx.set(null);
    this.matchedPairs.set(new Map());
    this.arrangedWords.set([]);
    this.remainingWords.set([]);

    const e = this.exam();
    if (!e) return;
    const next = this.activeQ() + 1;
    if (next < e.questions.length) {
      this.activeQ.set(next);
      // Init arrange if needed
      const nextQ = e.questions[next];
      if (nextQ.type === QuizType.Arrange) {
        this.initArrange(nextQ);
      }
    } else {
      this.submitExam();
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  SUBMISSION
  // ═══════════════════════════════════════════════════════════════════════

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
        const story  = this.state.currentStory();
        const lesson = this.state.currentLesson();
        if (story && !this.isLessonExam) {
          this.storyService.updateProgress({
            storyId: story.id, childName,
            currentPage: this.state.totalPages(),
            totalQuestions: res.totalQuestions, correctAnswers: res.correctAnswers,
            scorePercentage: res.scorePercentage, examCompleted: true
          }).subscribe();
          sessionStorage.setItem('quiz_result', JSON.stringify({
            correct: res.correctAnswers,
            total:   res.totalQuestions
          }));
          this.router.navigate(['/books', story.id, 'quiz-result']);
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

  // ═══════════════════════════════════════════════════════════════════════
  //  RESULT HELPERS
  // ═══════════════════════════════════════════════════════════════════════

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

  // ═══════════════════════════════════════════════════════════════════════
  //  AUDIO
  // ═══════════════════════════════════════════════════════════════════════

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

  playQuestionAudio(q: QuestionDto): void {
    const audioUrl = (q as any).audioUrl as string | undefined;
    if (audioUrl) {
      const audio = new Audio(audioUrl);
      audio.play().catch(() => this.speakText(q.text));
    } else {
      this.speakText(q.text);
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  NAVIGATION
  // ═══════════════════════════════════════════════════════════════════════

  goHome():  void { this.state.reset(); this.router.navigate(['/dashboard']); }
  goStory(): void {
    if (this.isLessonExam) { this.router.navigate(['/lessons-list']); return; }
    const story = this.state.currentStory();
    if (story) this.router.navigate(['/books', story.id, 'read']);
    else       this.router.navigate(['/dashboard']);
  }
}
