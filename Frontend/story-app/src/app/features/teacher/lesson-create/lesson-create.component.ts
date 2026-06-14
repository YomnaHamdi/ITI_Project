import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { StudentGroupDto } from '../../../models/story.models';

interface ContentBlock {
  id:      string;
  type:    'text' | 'image' | 'audio' | 'activity';
  content: string;
  label:   string;
}

@Component({
  selector: 'app-lesson-create',
  standalone: true,
  imports: [CommonModule, FormsModule, TeacherSidebarComponent],
  templateUrl: './lesson-create.component.html',
  styleUrl: './lesson-create.component.css'
})
export class LessonCreateComponent implements OnInit {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly isPublishing   = signal(false);
  readonly publishError   = signal<string | null>(null);
  readonly publishSuccess = signal<string | null>(null);

  form = { title: '', letter: '', level: 1, tags: '' };

  readonly levels = [
    { value: 1, label: 'المستوى 1 — الحروف' },
    { value: 2, label: 'المستوى 2 — الكلمات' },
    { value: 3, label: 'المستوى 3 — الجمل'  },
  ];

  readonly blocks        = signal<ContentBlock[]>([]);
  readonly isDraggingPdf = signal(false);
  selectedPdf: File | null = null;
  private nextId = 1;

  // AI generation targeting
  groups          = signal<StudentGroupDto[]>([]);
  targetType: 'none' | 'student' | 'group' = 'none';
  targetStudentId = '';
  targetGroupId   = '';

  ngOnInit(): void {
    const user = this.state.currentUser();
    if (user?.id) {
      this.service.getTeacherGroups(user.id).subscribe({
        next: g => this.groups.set(g),
        error: () => {}
      });
    }
  }

  addBlock(type: ContentBlock['type']): void {
    const labels: Record<ContentBlock['type'], string> = {
      text: 'نص تعليمي', image: 'صورة', audio: 'ملف صوتي', activity: 'نشاط'
    };
    this.blocks.update(bs => [
      ...bs, { id: `b${this.nextId++}`, type, content: '', label: labels[type] }
    ]);
  }

  removeBlock(id: string): void { this.blocks.update(bs => bs.filter(b => b.id !== id)); }

  moveUp(idx: number): void {
    if (idx === 0) return;
    this.blocks.update(bs => {
      const a = [...bs]; [a[idx - 1], a[idx]] = [a[idx], a[idx - 1]]; return a;
    });
  }

  moveDown(idx: number): void {
    if (idx === this.blocks().length - 1) return;
    this.blocks.update(bs => {
      const a = [...bs]; [a[idx], a[idx + 1]] = [a[idx + 1], a[idx]]; return a;
    });
  }

  onPdfDragOver(e: DragEvent): void { e.preventDefault(); this.isDraggingPdf.set(true); }
  onPdfDragLeave(): void { this.isDraggingPdf.set(false); }
  onPdfDrop(e: DragEvent): void {
    e.preventDefault(); this.isDraggingPdf.set(false);
    const file = e.dataTransfer?.files[0];
    if (file?.type === 'application/pdf') this.selectedPdf = file;
  }
  onPdfInput(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.selectedPdf = file;
  }

  publishFromPdf(): void {
    if (!this.selectedPdf) { this.publishError.set('يرجى اختيار ملف PDF.'); return; }
    this.isPublishing.set(true);
    this.publishError.set(null);
    this.service.importBook(
      this.form.level,
      this.form.letter || '',
      this.form.title  || this.selectedPdf.name,
      this.selectedPdf
    ).subscribe({
      next: result => {
        this.isPublishing.set(false);
        this.publishSuccess.set(`✅ تم نشر الكتاب: ${result.title}`);
        setTimeout(() => this.router.navigate(['/lessons', result.id]), 1500);
      },
      error: (err: any) => {
        this.publishError.set(err?.message ?? 'فشل الاستيراد. حاول مرة أخرى.');
        this.isPublishing.set(false);
      }
    });
  }

  publishFromAi(): void {
    if (!this.form.title.trim()) { this.publishError.set('يرجى إدخال عنوان الدرس.'); return; }
    this.isPublishing.set(true);
    this.publishError.set(null);

    const user = this.state.currentUser();
    this.service.generateLesson({
      topic:           this.form.title.trim(),
      level:           this.form.level,
      letter:          this.form.letter.trim() || undefined,
      creatorId:       user?.id,
      creatorRole:     'Teacher',
      targetStudentId: this.targetType === 'student' ? this.targetStudentId || undefined : undefined,
      targetGroupId:   this.targetType === 'group'   ? this.targetGroupId   || undefined : undefined,
    }).subscribe({
      next: lesson => {
        this.isPublishing.set(false);
        this.publishSuccess.set(`✅ تم توليد الدرس: ${(lesson as any).title ?? (lesson as any).letterName}`);
        setTimeout(() => this.router.navigate(['/lessons', (lesson as any).id]), 1500);
      },
      error: (err: any) => {
        this.publishError.set(err?.message ?? 'فشل التوليد. حاول مرة أخرى.');
        this.isPublishing.set(false);
      }
    });
  }
}
