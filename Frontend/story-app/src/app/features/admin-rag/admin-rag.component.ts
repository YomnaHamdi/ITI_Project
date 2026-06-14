import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../services/story';
import {
  KnowledgeDocumentDto, RagSearchResult, GenerateLessonRequest
} from '../../models/story.models';

@Component({
  selector: 'app-admin-rag',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule],
  templateUrl: './admin-rag.component.html',
  styleUrl: './admin-rag.component.css',
})
export class AdminRagComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly documents   = signal<KnowledgeDocumentDto[]>([]);
  readonly searchResults = signal<RagSearchResult[]>([]);
  readonly isLoading   = signal(false);
  readonly message     = signal<string | null>(null);
  readonly isSuccess   = signal(false);

  // Ingest form
  selectedFile: File | null = null;
  ingestLetter  = '';
  ingestLevel?: number;
  ingestTags    = '';

  // Search
  searchQuery = '';

  // Lesson generation
  lessonTopic    = '';
  lessonLetter   = '';
  lessonLevel?: number;
  lessonChildName = '';

  ngOnInit(): void { this.loadDocuments(); }

  loadDocuments(): void {
    this.svc.getKnowledgeDocuments().subscribe({
      next: docs => this.documents.set(docs),
      error: () => this.showMessage('فشل تحميل المستندات', false)
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  ingestDocument(): void {
    if (!this.selectedFile) return;
    this.isLoading.set(true);
    this.svc.ingestDocument(
      this.selectedFile,
      this.ingestLetter || undefined,
      this.ingestLevel,
      this.ingestTags || undefined
    ).subscribe({
      next: res => {
        this.showMessage(`تم رفع "${res.fileName}" — ${res.chunkCount} قطعة نصية`, true);
        this.selectedFile = null;
        this.ingestLetter = '';
        this.ingestLevel  = undefined;
        this.ingestTags   = '';
        this.loadDocuments();
        this.isLoading.set(false);
      },
      error: () => { this.showMessage('فشل رفع الملف', false); this.isLoading.set(false); }
    });
  }

  deleteDocument(id: string, name: string): void {
    if (!confirm(`هل تريد حذف "${name}"؟`)) return;
    this.svc.deleteKnowledgeDocument(id).subscribe({
      next: () => { this.showMessage('تم الحذف', true); this.loadDocuments(); },
      error: () => this.showMessage('فشل الحذف', false)
    });
  }

  search(): void {
    if (!this.searchQuery.trim()) return;
    this.isLoading.set(true);
    this.svc.ragSearch(this.searchQuery).subscribe({
      next: res => { this.searchResults.set(res); this.isLoading.set(false); },
      error: () => { this.showMessage('فشل البحث', false); this.isLoading.set(false); }
    });
  }

  generateLesson(): void {
    if (!this.lessonTopic.trim()) return;
    this.isLoading.set(true);
    const req: GenerateLessonRequest = {
      topic:      this.lessonTopic,
      letter:     this.lessonLetter || undefined,
      level:      this.lessonLevel,
      childName:  this.lessonChildName || undefined,
    };
    this.svc.generateRagLesson(req).subscribe({
      next: lesson => {
        this.showMessage(`تم توليد الدرس: "${lesson.title}"`, true);
        this.isLoading.set(false);
      },
      error: () => { this.showMessage('فشل توليد الدرس', false); this.isLoading.set(false); }
    });
  }

  private showMessage(msg: string, success: boolean): void {
    this.message.set(msg);
    this.isSuccess.set(success);
    setTimeout(() => this.message.set(null), 4000);
  }
}
