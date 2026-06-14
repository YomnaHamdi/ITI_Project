import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { StoryService } from '../../services/story';
import {
  PdfDocumentDto, PdfDocumentDetailDto,
  PdfPageDto, PdfLibraryStatsDto
} from '../../models/story.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-admin-pdf-library',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './admin-pdf-library.component.html',
  styleUrl:    './admin-pdf-library.component.css',
})
export class AdminPdfLibraryComponent implements OnInit {
  private readonly svc = inject(StoryService);
  readonly api = environment.apiUrl;

  // ── State ──────────────────────────────────────────────────────────────────
  readonly docs       = signal<PdfDocumentDto[]>([]);
  readonly stats      = signal<PdfLibraryStatsDto | null>(null);
  readonly isLoading  = signal(false);
  readonly message    = signal<string | null>(null);
  readonly isSuccess  = signal(false);

  // ── Upload form ────────────────────────────────────────────────────────────
  selectedFile: File | null = null;
  uploadLetter = '';
  uploadLevel  = 1;

  // ── Detail panel ──────────────────────────────────────────────────────────
  readonly detailDoc    = signal<PdfDocumentDetailDto | null>(null);
  readonly detailLoading = signal(false);

  // ── Per-doc embedding state ────────────────────────────────────────────────
  readonly embeddingId = signal<string | null>(null);

  readonly arabicLetters = [
    'أ','ب','ت','ث','ج','ح','خ',
    'د','ذ','ر','ز','س','ش','ص',
    'ض','ط','ظ','ع','غ','ف','ق',
    'ك','ل','م','ن','ه','و','ي'
  ];

  ngOnInit(): void {
    this.reload();
  }

  // ── File selection ─────────────────────────────────────────────────────────
  onFileSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  // ── Upload ─────────────────────────────────────────────────────────────────
  upload(): void {
    if (!this.selectedFile)    { this.showMsg('يرجى اختيار ملف PDF.', false); return; }
    if (!this.uploadLetter)    { this.showMsg('يرجى تحديد الحرف.', false); return; }
    if (!this.uploadLevel)     { this.showMsg('يرجى تحديد المستوى.', false); return; }

    this.isLoading.set(true);
    this.svc.uploadPdfDocument(this.selectedFile, this.uploadLetter, this.uploadLevel).subscribe({
      next: doc => {
        this.showMsg(`تم رفع "${doc.title}" — ${doc.pageCount} صفحة`, true);
        this.selectedFile = null;
        this.uploadLetter = '';
        this.isLoading.set(false);
        this.reload();
      },
      error: err => {
        this.showMsg(err?.error?.error ?? 'فشل رفع الملف.', false);
        this.isLoading.set(false);
      }
    });
  }

  // ── Generate embeddings ────────────────────────────────────────────────────
  embed(id: string): void {
    this.embeddingId.set(id);
    this.svc.generatePdfEmbeddings(id).subscribe({
      next: res => {
        this.showMsg(res.message, true);
        this.embeddingId.set(null);
        this.reload();
        if (this.detailDoc()?.id === id) this.loadDetail(id);
      },
      error: err => {
        this.showMsg(err?.error?.error ?? 'فشل التضمين.', false);
        this.embeddingId.set(null);
      }
    });
  }

  // ── View pages ─────────────────────────────────────────────────────────────
  loadDetail(id: string): void {
    this.detailLoading.set(true);
    this.svc.getPdfDocument(id).subscribe({
      next: d  => { this.detailDoc.set(d); this.detailLoading.set(false); },
      error: () => { this.detailLoading.set(false); }
    });
  }

  closeDetail(): void { this.detailDoc.set(null); }

  // ── Delete ─────────────────────────────────────────────────────────────────
  deleteDoc(id: string, title: string): void {
    if (!confirm(`هل تريد حذف "${title}" وجميع صوره وتضميناته؟`)) return;
    this.svc.deletePdfDocument(id).subscribe({
      next: () => {
        this.showMsg('تم الحذف.', true);
        if (this.detailDoc()?.id === id) this.detailDoc.set(null);
        this.reload();
      },
      error: () => this.showMsg('فشل الحذف.', false)
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  imageUrl(path: string): string {
    return path ? `${this.api}${path}` : '';
  }

  embedProgress(doc: PdfDocumentDto): number {
    return doc.pageCount > 0
      ? Math.round(doc.embeddedPageCount / doc.pageCount * 100)
      : 0;
  }

  private reload(): void {
    this.svc.getPdfDocuments().subscribe({
      next: docs => this.docs.set(docs),
      error: ()  => {}
    });
    this.svc.getPdfLibraryStats().subscribe({
      next: s => this.stats.set(s),
      error: () => {}
    });
  }

  private showMsg(msg: string, ok: boolean): void {
    this.message.set(msg);
    this.isSuccess.set(ok);
    setTimeout(() => this.message.set(null), 4000);
  }
}
