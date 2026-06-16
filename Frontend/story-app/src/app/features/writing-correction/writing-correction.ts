import {
  Component,
  inject,
  signal,
  OnInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  Inject,
  PLATFORM_ID
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AppStateService } from '../../services/app-state-service';
import { StoryService } from '../../services/story';
import { LoadingComponent } from '../../shared/loading/loading';
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import { WritingCorrectionResponse } from '../../models/story.models';

@Component({
  selector: 'app-writing-correction',
  standalone: true,
  imports: [
    LoadingComponent,
    ErrorToastComponent
  ],
  templateUrl: './writing-correction.html',
  styleUrl: './writing-correction.css',
})
export class WritingCorrection implements OnInit, OnDestroy {

  @ViewChild('drawingCanvas')
  canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly storyService = inject(StoryService);
  private readonly state = inject(AppStateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<WritingCorrectionResponse | null>(null);

  readonly pageNum = signal(1);
  readonly sentence = signal('');

  readonly hasDrawing = signal(false);
  readonly writingMode = signal(false);

  private ctx!: CanvasRenderingContext2D;
  private isDrawing = false;

  constructor(
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    const story = this.state.currentStory();
    if (!story) {
      this.router.navigate(['/']);
      return;
    }
    const p = Number(this.route.snapshot.queryParams['page'] ?? 1);
    this.pageNum.set(p);
    const page = story.pages.find(pg => pg.pageNumber === p);
    if (page) {
      this.sentence.set(page.sentence);
    }
  }

  ngOnDestroy(): void {
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = '';
      document.documentElement.style.overflow = '';
    }
  }

  startWriting(): void {
    this.writingMode.set(true);
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = 'hidden';
      document.documentElement.style.overflow = 'hidden';
    }
    setTimeout(() => {
      this.setupCanvas();
    }, 100);
  }

  finishWriting(): void {
    this.writingMode.set(false);
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = '';
      document.documentElement.style.overflow = '';
    }
    this.isDrawing = false;
  }

  private setupCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    this.ctx = ctx;
    this.ctx.strokeStyle = '#1a1a2e';
    this.ctx.lineWidth = 3;
    this.ctx.lineCap = 'round';
    this.ctx.lineJoin = 'round';
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);

    canvas.onpointerdown = (e: PointerEvent) => {
      canvas.setPointerCapture(e.pointerId);
      const { x, y } = this.getPos(e, canvas);
      this.isDrawing = true;
      this.ctx.beginPath();
      this.ctx.moveTo(x, y);
    };

    canvas.onpointermove = (e: PointerEvent) => {
      if (!this.isDrawing) return;
      const { x, y } = this.getPos(e, canvas);
      this.ctx.lineTo(x, y);
      this.ctx.stroke();
      this.hasDrawing.set(true);
    };

    canvas.onpointerup = () => {
      this.isDrawing = false;
      this.ctx.beginPath();
    };

    canvas.onpointercancel = () => {
      this.isDrawing = false;
      this.ctx.beginPath();
    };
  }

  private getPos(e: PointerEvent, canvas: HTMLCanvasElement): { x: number; y: number } {
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    return {
      x: (e.clientX - rect.left) * scaleX,
      y: (e.clientY - rect.top) * scaleY
    };
  }

  clearCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas || !this.ctx) return;
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);
    this.hasDrawing.set(false);
    this.result.set(null);
    this.isDrawing = false;
  }

  get currentPage() {
    return this.state.currentStory()?.pages.find(p => p.pageNumber === this.pageNum()) ?? null;
  }

  submit(): void {
    if (!this.hasDrawing()) {
      this.error.set('يرجى كتابة الجملة في المربع أولاً.');
      return;
    }
    const story = this.state.currentStory();
    const page = this.currentPage;
    if (!story || !page) return;
    if (!this.state.childName()) {
      this.error.set('اسم الطفل مطلوب.');
      return;
    }

    this.canvasRef.nativeElement.toBlob(blob => {
      if (!blob) {
        this.error.set('فشل تصدير الرسم. حاول مرة أخرى.');
        return;
      }
      this.isLoading.set(true);
      this.storyService.submitLessonWriting(
        story.id,
        page.pageId,
        this.state.childName(),
        blob,
        `writing_${page.pageId}.png`
      ).subscribe({
        next: res => {
          this.result.set(res);
          this.isLoading.set(false);
          if (res.isAccepted) {
            const savedPage = this.state.currentPage();
            const s = this.state.currentStory();
            if (s) {
              this.state.currentStory.set({
                ...s,
                pages: s.pages.map(pg =>
                  pg.pageNumber === this.pageNum() + 1
                    ? { ...pg, isUnlocked: true }
                    : pg
                )
              });
              this.state.currentPage.set(savedPage);
            }
          }
        },
        error: (err: Error) => {
          this.error.set(err.message);
          this.isLoading.set(false);
        }
      });
    }, 'image/png');
  }

  goBack(): void {
    const story = this.state.currentStory();
    if (story) this.router.navigate(['/books', story.id, 'read']);
    else this.router.navigate(['/dashboard']);
  }

  tryAgain(): void {
    this.clearCanvas();
    this.result.set(null);
  }
}
