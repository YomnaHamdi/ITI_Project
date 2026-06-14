import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-ai-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './ai-settings.component.html',
  styleUrl: './ai-settings.component.css'
})
export class AiSettingsComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading = signal(false);
  readonly isSaving  = signal(false);
  readonly saved     = signal(false);
  readonly error     = signal<string | null>(null);

  readonly settings = signal({
    model: 'gpt-4o',
    temperature: 0.7,
    maxTokens: 2000,
    systemPrompt: 'أنت مساعد تعليمي متخصص في تعليم الأطفال العرب اللغة العربية.',
    ragEnabled: true,
    ragTopK: 5,
    storyMaxLength: 600,
    lessonMaxLength: 400,
  });

  readonly modelOptions = ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'claude-sonnet-4-6'];

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getAiSettings().subscribe({
      next:  s => { this.settings.set({ ...this.settings(), ...s }); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  patch(key: string, value: any): void {
    this.settings.update(s => ({ ...s, [key]: value }));
  }

  save(): void {
    this.isSaving.set(true);
    this.error.set(null);
    this.service.saveAiSettings(this.settings()).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: () => {
        this.isSaving.set(false);
        this.error.set('فشل الحفظ. حاول مرة أخرى.');
      }
    });
  }
}
