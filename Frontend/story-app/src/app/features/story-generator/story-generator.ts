import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../services/story';
import { LoadingComponent } from '../../shared/loading/loading';         // story-only animated loader
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import { AppStateService } from '../../services/app-state-service';

@Component({
  selector: 'app-story-generator',
  standalone: true,
  imports: [FormsModule, CommonModule, LoadingComponent, ErrorToastComponent],
  templateUrl: './story-generator.html'
})
export class StoryGeneratorComponent {
  private readonly storyService = inject(StoryService);
  private readonly state        = inject(AppStateService);
  private readonly router       = inject(Router);

  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);

  form = { childName: '', character: '', theme: '' };

  readonly themes = [
    { value: 'الصداقة',   label: 'الصداقة 🤝'  },
    { value: 'الشجاعة',   label: 'الشجاعة 🦁'  },
    { value: 'المغامرة',  label: 'المغامرة 🚀'  },
    { value: 'الطبيعة',   label: 'الطبيعة 🌿'   },
    { value: 'الحيوانات', label: 'الحيوانات 🐾' },
    { value: 'السحر',     label: 'السحر ✨'      },
  ];

  selectTheme(v: string): void { this.form.theme = v; }

  goToLessons(): void { this.router.navigate(['/lessons']); }
  goToAdmin(): void { this.router.navigate(['/admin/import']); }
  goToAdminRag(): void { this.router.navigate(['/admin/rag']); }
  goTo(path: string): void { this.router.navigate([path]); }

  submit(): void {
    if (!this.form.childName.trim() || !this.form.character.trim() || !this.form.theme) {
      this.error.set('يرجى ملء جميع الحقول واختيار موضوع.');
      return;
    }
    this.isLoading.set(true);
    this.error.set(null);

    this.storyService.generateStory(this.form).subscribe({
      next: story => {
        this.state.setStory(story);
        this.state.setChildName(this.form.childName);
        this.router.navigate(['/books', story.id, 'read']);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.isLoading.set(false);
      }
    });
  }
}