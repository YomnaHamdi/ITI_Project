import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { StoryService } from '../../../services/story';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';

type StyleKey = 'cartoon' | 'fantasy' | 'realistic';

@Component({
  selector: 'app-ai-generator',
  standalone: true,
  imports: [CommonModule, FormsModule, TeacherSidebarComponent],
  templateUrl: './ai-generator.component.html',
  styleUrl: './ai-generator.component.css'
})
export class AiGeneratorComponent {
  private readonly service = inject(StoryService);
  private readonly router  = inject(Router);

  readonly isGenerating    = signal(false);
  readonly error           = signal<string | null>(null);
  readonly generatedLesson = signal<any>(null);

  form = { topic: '', level: 1, style: 'cartoon' as StyleKey, letter: '' };

  readonly styles: { key: StyleKey; label: string; emoji: string }[] = [
    { key: 'cartoon',   label: 'كرتون',  emoji: '🎨' },
    { key: 'fantasy',   label: 'خيالي',  emoji: '✨' },
    { key: 'realistic', label: 'واقعي',  emoji: '📷' },
  ];

  readonly levels = [
    { value: 1, label: 'المستوى 1 — الحروف' },
    { value: 2, label: 'المستوى 2 — الكلمات' },
    { value: 3, label: 'المستوى 3 — الجمل' },
  ];

  readonly suggestedTopics = [
    'الحيوانات بالعربية', 'عائلتي', 'الألوان والأشكال', 'الأرقام 1-10',
  ];

  generate(): void {
    if (!this.form.topic.trim()) { this.error.set('يرجى إدخال موضوع الدرس.'); return; }
    this.isGenerating.set(true);
    this.error.set(null);
    this.generatedLesson.set(null);
    this.service.generateRagLesson({
      topic:  this.form.topic.trim(),
      level:  this.form.level,
      letter: this.form.letter.trim() || undefined
    }).subscribe({
      next:  lesson => { this.generatedLesson.set(lesson); this.isGenerating.set(false); },
      error: (err: any) => {
        const msg = err?.error?.error
          ?? err?.error?.message
          ?? err?.message
          ?? 'حدث خطأ أثناء التوليد. تأكد من صحة الموضوع وأن المحتوى متاح في قاعدة المعرفة.';
        this.error.set(msg);
        this.isGenerating.set(false);
      }
    });
  }

  approveLesson(): void {
    const l = this.generatedLesson();
    if (l) this.router.navigate(['/lessons', l.id]);
  }

  regenerate(): void { this.generatedLesson.set(null); this.generate(); }
}
