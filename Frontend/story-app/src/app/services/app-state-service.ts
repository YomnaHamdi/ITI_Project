import { Injectable, signal, computed } from '@angular/core';
import { StoryResponse, StoryPage, ExamResult, LessonDetail, LessonPage } from '../models/story.models';

export interface CurrentUser {
  id:          string;
  name:        string;
  role:        'student' | 'parent' | 'teacher' | 'school' | 'admin';
  level?:      number;
  avatar?:     string;
  schoolCode?: string;
}

@Injectable({ providedIn: 'root' })
export class AppStateService {

  // ── Auth ────────────────────────────────────────────────────────────────────
  private readonly _user = signal<CurrentUser | null>(this.loadUserFromStorage());
  readonly currentUser     = this._user.asReadonly();
  readonly isLoggedIn      = computed(() => this._user() !== null);
  readonly userRole        = computed(() => this._user()?.role ?? '');
  readonly currentUserName = computed(() => this._user()?.name ?? '');

  setUser(user: CurrentUser): void {
    this._user.set(user);
    if (typeof localStorage !== 'undefined') localStorage.setItem('lughati_user', JSON.stringify(user));
  }

  logout(): void {
    this._user.set(null);
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('lughati_user');
      localStorage.removeItem('lughati_token');
      localStorage.removeItem('lughati_child');
    }
    this._childName.set('');
    this.currentStory.set(null);
    this.currentLesson.set(null);
    this.currentExamResult.set(null);
    this.currentPage.set(1);
  }

  updateStudentLevel(level: number, newToken?: string): void {
    const user = this._user();
    if (!user) return;
    const updated = { ...user, level };
    this._user.set(updated);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('lughati_user', JSON.stringify(updated));
      if (newToken) localStorage.setItem('lughati_token', newToken);
    }
  }

  private loadUserFromStorage(): CurrentUser | null {
    try {
      if (typeof localStorage === 'undefined') return null;
      const raw = localStorage.getItem('lughati_user');
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }

  // ── Language ────────────────────────────────────────────────────────────────
  private readonly _lang = signal<'ar' | 'en'>(
    ((typeof localStorage !== 'undefined' ? localStorage.getItem('lughati_lang') : null) as 'ar' | 'en') ?? 'ar'
  );
  readonly lang  = this._lang.asReadonly();
  readonly isRtl = computed(() => this._lang() === 'ar');

  setLang(lang: 'ar' | 'en'): void {
    this._lang.set(lang);
    if (typeof localStorage !== 'undefined') localStorage.setItem('lughati_lang', lang);
    if (typeof document !== 'undefined') {
      document.documentElement.dir  = lang === 'ar' ? 'rtl' : 'ltr';
      document.documentElement.lang = lang;
    }
  }

  // ── Child name ───────────────────────────────────────────────────────────────
  private readonly _childName = signal<string>(
    (typeof localStorage !== 'undefined' ? localStorage.getItem('lughati_child') : null) ?? ''
  );
  readonly childName = this._childName.asReadonly();

  setChildName(name: string): void {
    this._childName.set(name.trim());
    if (typeof localStorage !== 'undefined') localStorage.setItem('lughati_child', name.trim());
  }

  // ── Story / Lesson state ────────────────────────────────────────────────────
  readonly currentStory      = signal<StoryResponse | null>(null);
  readonly currentLesson     = signal<LessonDetail | null>(null);
  readonly currentExamResult = signal<ExamResult | null>(null);
  readonly currentPage       = signal<number>(1);
  readonly lessonStarted     = signal<boolean>(false);

  readonly activePage = computed((): StoryPage | LessonPage | null => {
    const lesson = this.currentLesson();
    if (lesson)
      return lesson.pages.find(p => p.pageNumber === this.currentPage()) ?? null;
    const story = this.currentStory();
    if (!story) return null;
    return story.pages.find(p => p.pageNumber === this.currentPage()) ?? null;
  });

  readonly totalPages = computed(() => {
    const lesson = this.currentLesson();
    if (lesson) return lesson.pages.length;
    return this.currentStory()?.pages.length ?? 0;
  });

  readonly isLastPage     = computed(() => this.currentPage() >= this.totalPages());
  readonly progressPercent = computed(() => {
    const total = this.totalPages();
    return total > 0 ? Math.round((this.currentPage() / total) * 100) : 0;
  });

  setStory(story: StoryResponse): void {
    this.currentLesson.set(null);
    this.currentStory.set(story);
    this.currentPage.set(1);
    this.lessonStarted.set(false);
  }

  setLesson(lesson: LessonDetail): void {
    this.currentStory.set(null);
    this.currentLesson.set(lesson);
    this.currentPage.set(1);
    this.lessonStarted.set(false);
  }

  updateLessonPages(pages: LessonPage[]): void {
    const lesson = this.currentLesson();
    if (!lesson) return;
    this.currentLesson.set({ ...lesson, pages });
  }

  goToPage(page: number): void {
    const total = this.totalPages();
    if (page >= 1 && page <= total) this.currentPage.set(page);
  }

  nextPage(): void {
    if (!this.isLastPage()) this.currentPage.update(p => p + 1);
  }

  startLesson(): void { this.lessonStarted.set(true); this.currentPage.set(2); }

  setExamResult(result: ExamResult): void { this.currentExamResult.set(result); }

  reset(): void {
    this.currentStory.set(null);
    this.currentLesson.set(null);
    this.currentExamResult.set(null);
    this.currentPage.set(1);
    this.lessonStarted.set(false);
  }

  // ── Lesson progress (persisted per lesson) ──────────────────────────────────
  private readonly LESSON_PROG_KEY = 'lughati_lesson_progress';

  private loadLessonProgressMap(): Record<string, { currentPage: number; totalPages: number; completed: boolean }> {
    try {
      if (typeof localStorage === 'undefined') return {};
      const raw = localStorage.getItem(this.LESSON_PROG_KEY);
      return raw ? JSON.parse(raw) : {};
    } catch { return {}; }
  }

  lessonProgress(lessonId: string): { currentPage: number; totalPages: number; completed: boolean } | null {
    return this.loadLessonProgressMap()[lessonId] ?? null;
  }

  saveLessonProgress(lessonId: string, currentPage: number, totalPages: number, completed: boolean): void {
    const map = this.loadLessonProgressMap();
    map[lessonId] = { currentPage, totalPages, completed };
    if (typeof localStorage !== 'undefined')
      localStorage.setItem(this.LESSON_PROG_KEY, JSON.stringify(map));
  }
}
