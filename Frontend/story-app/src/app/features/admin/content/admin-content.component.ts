import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-admin-content',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-content.component.html',
  styleUrl: './admin-content.component.css'
})
export class AdminContentComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading    = signal(false);
  readonly isUploading  = signal(false);
  readonly docs         = signal<any[]>([]);
  readonly uploadName   = signal('');
  readonly uploadDesc   = signal('');
  readonly uploadFile   = signal<File | null>(null);
  readonly uploadError  = signal<string | null>(null);
  readonly uploadSuccess = signal(false);

  ngOnInit(): void {
    this.loadDocs();
  }

  loadDocs(): void {
    this.isLoading.set(true);
    this.service.getKnowledgeDocuments().subscribe({
      next:  d => { this.docs.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) this.uploadFile.set(input.files[0]);
  }

  upload(): void {
    if (!this.uploadFile() || !this.uploadName()) {
      this.uploadError.set('يرجى تحديد ملف وإدخال اسم المستند.');
      return;
    }
    this.isUploading.set(true);
    this.uploadError.set(null);
    this.service.uploadKnowledgeDocument(this.uploadFile()!, this.uploadName(), this.uploadDesc()).subscribe({
      next: () => {
        this.isUploading.set(false);
        this.uploadSuccess.set(true);
        this.uploadName.set('');
        this.uploadDesc.set('');
        this.uploadFile.set(null);
        setTimeout(() => this.uploadSuccess.set(false), 3000);
        this.loadDocs();
      },
      error: () => {
        this.isUploading.set(false);
        this.uploadError.set('فشل الرفع. حاول مرة أخرى.');
      }
    });
  }

  deleteDoc(id: string): void {
    if (!confirm('هل تريد حذف هذا المستند؟')) return;
    this.service.deleteKnowledgeDocument(id).subscribe({
      next: () => this.loadDocs()
    });
  }

  setName(v: string)  { this.uploadName.set(v); }
  setDesc(v: string)  { this.uploadDesc.set(v); }
}
