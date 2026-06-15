import { Component, signal, computed, OnInit, OnDestroy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-placement-question',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './placement-question.component.html',
  styleUrl: './placement-question.component.css'
})
export class PlacementQuestionComponent implements OnInit, OnDestroy {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly questions    = signal<any[]>([]);
  readonly currentIdx   = signal(0);
  readonly answers      = signal<Record<string, string>>({});
  readonly selected     = signal<string | null>(null);
  readonly showFeedback = signal(false);
  readonly isPlaying    = signal(false);
  readonly isLoading    = signal(false);
  // NEW: track if correct answer is revealed after wrong pick
  readonly revealCorrect = signal(false);

  readonly currentQ  = computed(() => this.questions()[this.currentIdx()]);
  readonly total     = computed(() => this.questions().length);
  readonly progress  = computed(() =>
    this.total() > 0 ? Math.round(this.currentIdx() / this.total() * 100) : 0
  );
  readonly partLabel = computed(() => {
    const q = this.currentQ();
    return q ? `الجزء ${q.part ?? q.Part} من 3` : '';
  });

  // All questions are audio-only — no letter image shown
  readonly isAudioOnly = computed(() => true);

  readonly isCorrect = computed(() => {
    const q = this.currentQ();
    return q && this.selected() === q.correctAnswer;
  });

  private readonly mockQuestions: any[] = [
    { id:'q1',  part:1, imageContent:'أ',  questionText:'أي حرف هذا؟',                     audioText:'أي حرف هذا؟',           correctAnswer:'B', options:[{key:'A',emoji:'',label:'ب'},{key:'B',emoji:'',label:'أ'},{key:'C',emoji:'',label:'ج'},{key:'D',emoji:'',label:'د'}] },
    { id:'q2',  part:1, imageContent:'🦁', questionText:'ما الحيوان في الصورة؟',            audioText:'ما الحيوان في الصورة؟', correctAnswer:'B', options:[{key:'A',emoji:'🐘',label:'فيل'},{key:'B',emoji:'🦁',label:'أسد'},{key:'C',emoji:'🐸',label:'ضفدع'},{key:'D',emoji:'🐳',label:'حوت'}] },
    { id:'q3',  part:1, imageContent:'ب',  questionText:'أي حرف هذا؟',                     audioText:'أي حرف هذا؟',           correctAnswer:'C', options:[{key:'A',emoji:'',label:'ت'},{key:'B',emoji:'',label:'ث'},{key:'C',emoji:'',label:'ب'},{key:'D',emoji:'',label:'ن'}] },
    { id:'q4',  part:1, imageContent:'🍎', questionText:'ما لون التفاحة؟',                 audioText:'ما لون التفاحة؟',       correctAnswer:'C', options:[{key:'A',emoji:'',label:'أزرق'},{key:'B',emoji:'',label:'أخضر'},{key:'C',emoji:'',label:'أحمر'},{key:'D',emoji:'',label:'أصفر'}] },
    { id:'q5',  part:1, imageContent:'ج',  questionText:'أي حرف هذا؟',                     audioText:'أي حرف هذا؟',           correctAnswer:'C', options:[{key:'A',emoji:'',label:'ح'},{key:'B',emoji:'',label:'خ'},{key:'C',emoji:'',label:'ج'},{key:'D',emoji:'',label:'ع'}] },
    { id:'q6',  part:2, imageContent:'🐘', questionText:'ما أول حرف في كلمة فيل؟',          audioText:'ما أول حرف في كلمة فيل؟',  correctAnswer:'B', options:[{key:'A',emoji:'',label:'ق'},{key:'B',emoji:'',label:'ف'},{key:'C',emoji:'',label:'ب'},{key:'D',emoji:'',label:'م'}] },
    { id:'q7',  part:2, imageContent:'🌙', questionText:'ما أول حرف في كلمة قمر؟',          audioText:'ما أول حرف في كلمة قمر؟',  correctAnswer:'A', options:[{key:'A',emoji:'',label:'ق'},{key:'B',emoji:'',label:'ك'},{key:'C',emoji:'',label:'م'},{key:'D',emoji:'',label:'ر'}] },
    { id:'q8',  part:2, imageContent:'🏠', questionText:'ما أول حرف في كلمة بيت؟',          audioText:'ما أول حرف في كلمة بيت؟',  correctAnswer:'C', options:[{key:'A',emoji:'',label:'ت'},{key:'B',emoji:'',label:'ي'},{key:'C',emoji:'',label:'ب'},{key:'D',emoji:'',label:'هـ'}] },
    { id:'q9',  part:2, imageContent:'🐱', questionText:'ما أول حرف في كلمة قطة؟',          audioText:'ما أول حرف في كلمة قطة؟',  correctAnswer:'C', options:[{key:'A',emoji:'',label:'ط'},{key:'B',emoji:'',label:'ة'},{key:'C',emoji:'',label:'ق'},{key:'D',emoji:'',label:'ه'}] },
    { id:'q10', part:2, imageContent:'🌸', questionText:'ما أول حرف في كلمة زهرة؟',         audioText:'ما أول حرف في كلمة زهرة؟', correctAnswer:'B', options:[{key:'A',emoji:'',label:'هـ'},{key:'B',emoji:'',label:'ز'},{key:'C',emoji:'',label:'ر'},{key:'D',emoji:'',label:'ة'}] },
    { id:'q11', part:3, imageContent:'📚', questionText:'رتب الكلمات: يلعب / الأسد / في / الغابة', audioText:'رتب الكلمات الصحيحة', correctAnswer:'A', options:[{key:'A',emoji:'',label:'الأسد يلعب في الغابة'},{key:'B',emoji:'',label:'يلعب الغابة في الأسد'},{key:'C',emoji:'',label:'في الأسد الغابة يلعب'},{key:'D',emoji:'',label:'الغابة في الأسد يلعب'}] },
    { id:'q12', part:3, imageContent:'🦋', questionText:'أكمل الجملة: الفراشة ___',          audioText:'أكمل الجملة',           correctAnswer:'B', options:[{key:'A',emoji:'',label:'تسبح'},{key:'B',emoji:'',label:'تطير'},{key:'C',emoji:'',label:'تنام'},{key:'D',emoji:'',label:'تأكل الحجر'}] },
    { id:'q13', part:3, imageContent:'🌊', questionText:'ما معنى كلمة (بحر)؟',               audioText:'ما معنى كلمة بحر؟',     correctAnswer:'C', options:[{key:'A',emoji:'',label:'جبل'},{key:'B',emoji:'',label:'غابة'},{key:'C',emoji:'',label:'ماء كثير'},{key:'D',emoji:'',label:'نهر صغير'}] },
    { id:'q14', part:3, imageContent:'📖', questionText:'أين نقرأ القصص؟',                   audioText:'أين نقرأ القصص؟',       correctAnswer:'A', options:[{key:'A',emoji:'',label:'في الكتاب'},{key:'B',emoji:'',label:'في المطبخ'},{key:'C',emoji:'',label:'في الملعب'},{key:'D',emoji:'',label:'في الحديقة'}] },
    { id:'q15', part:3, imageContent:'🦁', questionText:'كم عدد حروف كلمة (أسد)؟',           audioText:'كم عدد حروف كلمة أسد؟', correctAnswer:'B', options:[{key:'A',emoji:'',label:'حرفان'},{key:'B',emoji:'',label:'ثلاثة حروف'},{key:'C',emoji:'',label:'أربعة حروف'},{key:'D',emoji:'',label:'خمسة حروف'}] },
  ];

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getPlacementQuestions().subscribe({
      next: qs => {
        this.questions.set(this.normalizeQuestions(qs));
        this.isLoading.set(false);
        this.speakQuestion();
      },
      error: () => {
        this.questions.set(this.mockQuestions);
        this.isLoading.set(false);
        this.speakQuestion();
      }
    });
  }

  ngOnDestroy(): void { window.speechSynthesis.cancel(); }

  private normalizeQuestions(qs: any[]): any[] {
    return qs.map(q => ({
      id:            q.id,
      part:          q.part ?? q.Part,
      imageContent:  q.imageContent ?? q.ImageContent ?? '',
      questionText:  q.questionText ?? q.QuestionText ?? '',
      audioText:     q.audioText    ?? q.AudioText    ?? '',
      correctAnswer: q.correctAnswer ?? q.CorrectAnswer ?? '',
      options:       (q.options ?? q.Options ?? []).map((o: any) => ({
        key:   o.key   ?? o.Key,
        emoji: o.emoji ?? o.Emoji ?? '',
        label: o.label ?? o.Label
      }))
    }));
  }

  selectOption(key: string): void {
    if (this.showFeedback()) return;
    this.selected.set(key);
    this.showFeedback.set(true);
    const q = this.currentQ();
    const correct = key === q.correctAnswer;

    if (correct) {
      // Correct: speak praise, advance after short delay
      this.speakText('شاطر');
      this.answers.update(a => ({ ...a, [q.id]: key }));
      setTimeout(() => this.advance(), 1400);
    } else {
      // Wrong: reveal correct answer in green, speak encouragement
      this.revealCorrect.set(true);
      this.speakText('حاول مجدداً');
      // After showing correct answer for 2s, advance
      setTimeout(() => {
        this.answers.update(a => ({ ...a, [q.id]: key }));
        this.revealCorrect.set(false);
        this.advance();
      }, 2000);
    }
  }

  private advance(): void {
    const idx = this.currentIdx();
    if (idx < this.total() - 1) {
      this.currentIdx.set(idx + 1);
      this.selected.set(null);
      this.showFeedback.set(false);
      this.revealCorrect.set(false);
      this.speakQuestion();
    } else {
      this.submitAll();
    }
  }

  private submitAll(): void {
    const qs  = this.questions();
    const ans = this.answers();
    const request = {
      answers: qs.map(q => ({ questionId: q.id, answer: ans[q.id] ?? '' }))
    };
    this.service.submitPlacement(request).subscribe({
      next: result => {
        const assignedLevel = result.assignedLevel ?? result.level ?? 1;
        sessionStorage.setItem('placement_result', JSON.stringify({
          score: result.totalScore,
          total: this.total(),
          level: assignedLevel,
          p1: result.part1Score,
          p2: result.part2Score,
          p3: result.part3Score
        }));
        if (this.state.isLoggedIn()) {
          this.service.updateStudentLevel(assignedLevel).subscribe({
            next: updated => this.state.updateStudentLevel(assignedLevel, updated.token),
            error: () => {}
          });
        }
        this.router.navigate(['/test/result']);
      },
      error: () => {
        const score = qs.filter(q => ans[q.id] === q.correctAnswer).length;
        const p1 = qs.filter(q => q.part === 1 && ans[q.id] === q.correctAnswer).length;
        const p2 = qs.filter(q => q.part === 2 && ans[q.id] === q.correctAnswer).length;
        const p3 = qs.filter(q => q.part === 3 && ans[q.id] === q.correctAnswer).length;
        const level = (p1 < 5 || p2 < 5) ? 1 : (p3 < 5 ? 2 : 3);
        sessionStorage.setItem('placement_result', JSON.stringify({ score, total: this.total(), level, p1, p2, p3 }));
        this.router.navigate(['/test/result']);
      }
    });
  }

  optionClass(key: string): string {
    if (!this.showFeedback()) return 'opt-btn';
    const correct = this.currentQ()?.correctAnswer;
    const sel = this.selected();
    if (key === correct) return 'opt-btn correct';
    if (key === sel && key !== correct) return 'opt-btn wrong';
    return 'opt-btn';
  }

  feedbackText(): string {
    return this.isCorrect() ? 'شاطر! 🌟' : 'حاول مجدداً 💙';
  }

  speakQuestion(): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const q = this.currentQ();
    if (!q) return;
    const text = q.audioText || q.questionText;
    this.speakText(text);
  }

  speakText(text: string): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang  = 'ar-SA';
    u.rate  = 0.85;
    u.onstart = () => this.isPlaying.set(true);
    u.onend   = () => this.isPlaying.set(false);
    window.speechSynthesis.speak(u);
  }
}
