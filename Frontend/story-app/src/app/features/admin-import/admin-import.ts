import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { StoryService } from '../../services/story';
import { AdminSidebarComponent } from '../admin/shared/admin-sidebar.component';
import {
  LessonSummary, LessonDetail, LessonPage,
  ImportBookResponse, CreateManualBookRequest
} from '../../models/story.models';
import { environment } from '../../../environments/environment';

type Tab = 'list' | 'import' | 'manual' | 'edit';

@Component({
  selector: 'app-admin-import',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-import.html',
  styleUrl: './admin-import.css'
})
export class AdminImportComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly router = inject(Router);
  readonly api = environment.apiUrl;

  // ── Tab state ─────────────────────────────────────────────────────────────
  readonly tab = signal<Tab>('list');

  // ── Books list ────────────────────────────────────────────────────────────
  readonly books      = signal<LessonSummary[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly page       = signal(1);
  readonly pageSize   = 9;
  readonly levelFilter = signal<number | null>(null);
  readonly listLoading = signal(false);
  readonly listError   = signal<string | null>(null);

  readonly levelFilters = [
    { label: 'الكل', value: null },
    { label: '١', value: 1 },
    { label: '٢', value: 2 },
    { label: '٣', value: 3 },
    { label: '٤', value: 4 },
  ];

  readonly pages = computed(() =>
    Array.from({ length: this.totalPages() }, (_, i) => i + 1)
  );

  // ── Import form ───────────────────────────────────────────────────────────
  importForm = {
    title:      '',
    level:      1,
    letter:     '',
    letterName: '',
    pdfFile:    null as File | null
  };
  readonly importLoading = signal(false);
  readonly importSuccess = signal<ImportBookResponse | null>(null);
  readonly importError   = signal<string | null>(null);

  // ── Manual form ───────────────────────────────────────────────────────────
  manualForm = {
    title:      '',
    level:      1,
    letter:     '',
    letterName: '',
    pages:      [{ sentence: '' }, { sentence: '' }] as { sentence: string }[]
  };
  readonly manualLoading = signal(false);
  readonly manualSuccess = signal<ImportBookResponse | null>(null);
  readonly manualError   = signal<string | null>(null);

  // ── Edit view ─────────────────────────────────────────────────────────────
  readonly editBook    = signal<LessonDetail | null>(null);
  readonly editLoading = signal(false);
  readonly editError   = signal<string | null>(null);
  readonly editingPageId = signal<string | null>(null);
  editSentence = '';

  readonly levels = [1, 2, 3, 4];

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadBooks();
  }

  // ── Books list actions ────────────────────────────────────────────────────
  loadBooks(): void {
    this.listLoading.set(true);
    this.listError.set(null);
    const lf = this.levelFilter();
    this.svc.getAllBooksAdmin(lf ?? undefined, this.page(), this.pageSize).subscribe({
      next: res => {
        this.books.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        this.listLoading.set(false);
      },
      error: () => {
        this.listError.set('تعذّر تحميل الكتب.');
        this.listLoading.set(false);
      }
    });
  }

  setLevelFilter(v: number | null): void {
    this.levelFilter.set(v);
    this.page.set(1);
    this.loadBooks();
  }

  goPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadBooks();
  }

  openEdit(book: LessonSummary): void {
    this.editLoading.set(true);
    this.editError.set(null);
    this.svc.getBookDetailAdmin(book.id).subscribe({
      next: detail => {
        this.editBook.set(detail);
        this.editLoading.set(false);
        this.tab.set('edit');
      },
      error: () => {
        this.editError.set('تعذّر تحميل تفاصيل الكتاب.');
        this.editLoading.set(false);
      }
    });
  }

  deleteBook(book: LessonSummary): void {
    if (!confirm(`هل تريد حذف كتاب "${book.title}"؟`)) return;
    this.svc.deleteBook(book.id).subscribe({
      next: () => this.loadBooks(),
      error: () => alert('فشل حذف الكتاب.')
    });
  }

  startEditSentence(page: LessonPage): void {
    this.editingPageId.set(page.pageId);
    this.editSentence = page.sentence;
  }

  cancelEditSentence(): void {
    this.editingPageId.set(null);
    this.editSentence = '';
  }

  savePageSentence(page: LessonPage): void {
    const book = this.editBook();
    if (!book) return;
    this.svc.updateBookPageSentence(book.id, page.pageId, this.editSentence).subscribe({
      next: () => {
        this.editingPageId.set(null);
        this.svc.getBookDetailAdmin(book.id).subscribe({
          next: d => this.editBook.set(d)
        });
      },
      error: () => alert('فشل تحديث الجملة.')
    });
  }

  backToList(): void {
    this.editBook.set(null);
    this.tab.set('list');
    this.loadBooks();
  }

  // ── Import form actions ───────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.importForm.pdfFile = input.files?.[0] ?? null;
    this.importSuccess.set(null);
    this.importError.set(null);
  }

  submitImport(): void {
    if (!this.importForm.title.trim()) { this.importError.set('يرجى إدخال اسم الكتاب.'); return; }
    if (!this.importForm.letter.trim()) { this.importError.set('يرجى إدخال الحرف.'); return; }
    if (!this.importForm.letterName.trim()) { this.importError.set('يرجى إدخال اسم الحرف.'); return; }
    if (!this.importForm.pdfFile) { this.importError.set('يرجى اختيار ملف PDF.'); return; }

    this.importLoading.set(true);
    this.importError.set(null);
    this.importSuccess.set(null);

    this.svc.importBookV2(
      this.importForm.level,
      this.importForm.letter.trim(),
      this.importForm.letterName.trim(),
      this.importForm.title.trim(),
      this.importForm.pdfFile
    ).subscribe({
      next: res => {
        this.importSuccess.set(res);
        this.importLoading.set(false);
        this.importForm = { title: '', level: 1, letter: '', letterName: '', pdfFile: null };
        this.loadBooks();
      },
      error: (e: any) => {
        this.importError.set(e?.error?.error ?? 'فشل الاستيراد. حاول مرة أخرى.');
        this.importLoading.set(false);
      }
    });
  }

  // ── Manual form actions ───────────────────────────────────────────────────
  addManualPage(): void {
    if (this.manualForm.pages.length >= 3) return;
    this.manualForm.pages.push({ sentence: '' });
  }

  removeManualPage(i: number): void {
    if (this.manualForm.pages.length <= 1) return;
    this.manualForm.pages.splice(i, 1);
  }

  submitManual(): void {
    if (!this.manualForm.title.trim()) { this.manualError.set('يرجى إدخال عنوان الكتاب.'); return; }
    if (!this.manualForm.letter.trim()) { this.manualError.set('يرجى إدخال الحرف.'); return; }
    if (!this.manualForm.letterName.trim()) { this.manualError.set('يرجى إدخال اسم الحرف.'); return; }
    if (this.manualForm.pages.some(p => !p.sentence.trim())) {
      this.manualError.set('يرجى إدخال جملة لكل صفحة.');
      return;
    }

    const req: CreateManualBookRequest = {
      title:      this.manualForm.title.trim(),
      letter:     this.manualForm.letter.trim(),
      letterName: this.manualForm.letterName.trim(),
      level:      this.manualForm.level,
      pages:      this.manualForm.pages.map(p => ({ sentence: p.sentence.trim() }))
    };

    this.manualLoading.set(true);
    this.manualError.set(null);
    this.manualSuccess.set(null);

    this.svc.createManualBook(req).subscribe({
      next: res => {
        this.manualSuccess.set(res);
        this.manualLoading.set(false);
        this.manualForm = { title: '', level: 1, letter: '', letterName: '', pages: [{ sentence: '' }] };
        this.loadBooks();
      },
      error: (e: any) => {
        this.manualError.set(e?.error?.error ?? 'فشل إنشاء الكتاب. حاول مرة أخرى.');
        this.manualLoading.set(false);
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  coverUrl(book: LessonSummary): string {
    const url = book.coverImageUrl;
    if (!url) return '';
    if (url.startsWith('http')) return url;
    return `${this.api}${url}`;
  }

  pageImageUrl(page: LessonPage): string {
    const url = page.imageUrl;
    if (!url) return '';
    if (url.startsWith('http')) return url;
    return `${this.api}${url}`;
  }

  levelLabel(level: number): string {
    return ['', '١', '٢', '٣', '٤'][level] ?? String(level);
  }
}
