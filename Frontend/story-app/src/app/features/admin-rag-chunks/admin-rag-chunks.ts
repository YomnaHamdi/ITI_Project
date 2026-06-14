import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../services/story';
import { AdminSidebarComponent } from '../admin/shared/admin-sidebar.component';
import { RagPageChunkDto } from '../../models/story.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-admin-rag-chunks',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-rag-chunks.html',
  styleUrl: './admin-rag-chunks.css'
})
export class AdminRagChunksComponent implements OnInit {
  private readonly svc = inject(StoryService);
  readonly api = environment.apiUrl;

  chunks    = signal<RagPageChunkDto[]>([]);
  loading   = signal(false);
  error     = signal('');
  filterLevel  = signal<number | undefined>(undefined);
  filterLetter = signal('');

  // educational PDF upload
  uploadLoading = signal(false);
  uploadMessage = signal('');
  uploadError   = signal('');
  uploadFile: File | null = null;
  uploadLevel    = 1;
  uploadLetter   = '';
  uploadLetterName = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.svc.getRagPageChunks(this.filterLevel(), this.filterLetter() || undefined)
      .subscribe({
        next:  c => { this.chunks.set(c); this.loading.set(false); },
        error: () => { this.error.set('فشل تحميل البيانات.'); this.loading.set(false); }
      });
  }

  applyFilter(): void { this.load(); }

  clearFilter(): void {
    this.filterLevel.set(undefined);
    this.filterLetter.set('');
    this.load();
  }

  onFileChange(evt: Event): void {
    const input = evt.target as HTMLInputElement;
    this.uploadFile = input.files?.[0] ?? null;
  }

  submitUpload(): void {
    if (!this.uploadFile || !this.uploadLetter.trim() || !this.uploadLetterName.trim()) {
      this.uploadError.set('يرجى ملء جميع الحقول واختيار ملف PDF.');
      return;
    }
    this.uploadLoading.set(true);
    this.uploadMessage.set('');
    this.uploadError.set('');
    this.svc.ingestEducationalPdf(
      this.uploadFile, this.uploadLevel,
      this.uploadLetter.trim(), this.uploadLetterName.trim()
    ).subscribe({
      next: r => {
        this.uploadMessage.set(r.message);
        this.uploadLoading.set(false);
        this.uploadFile = null;
        this.load();
      },
      error: (err: any) => {
        const detail = err?.error?.detail ?? err?.error?.error ?? err?.message ?? '';
        this.uploadError.set('فشل رفع الكتاب: ' + detail);
        this.uploadLoading.set(false);
      }
    });
  }
}
