import {
  Component, signal, ViewChild, ElementRef,
  AfterViewInit, OnDestroy, inject, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { WritingCorrectionResponse } from '../../models/story.models';

type Tool = 'pen' | 'eraser';

@Component({
  selector: 'app-writing-practice',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './writing-practice.html',
  styleUrl:    './writing-practice.css'
})
export class WritingPracticeComponent implements AfterViewInit, OnDestroy {
  @ViewChild('drawingCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly expectedText = signal('');
  readonly tool         = signal<Tool>('pen');
  readonly isLoading    = signal(false);
  readonly hasDrawing   = signal(false);
  readonly result       = signal<WritingCorrectionResponse | null>(null);
  readonly error        = signal<string | null>(null);

  private ctx!: CanvasRenderingContext2D;
  private isDrawing = false;

  constructor() {
    // Redraw watermark when expected text changes
    effect(() => {
      const text = this.expectedText();
      if (this.ctx) this.drawBackground(text);
    });
  }

  ngAfterViewInit(): void {
    document.body.style.overflow = 'hidden';
    document.documentElement.style.overflow = 'hidden';
    this.setupCanvas();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
    document.documentElement.style.overflow = '';
  }

  // ── Canvas setup ─────────────────────────────────────────────────────────────
  private setupCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    this.ctx = ctx;
    this.ctx.lineCap  = 'round';
    this.ctx.lineJoin = 'round';
    this.drawBackground(this.expectedText());
    this.bindPointerEvents(canvas);
  }

  private drawBackground(text: string): void {
    const canvas = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, canvas.width, canvas.height);
    this.ctx.fillStyle = '#ffffff';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);

    if (text.trim()) {
      // Draw faint watermark guide text
      this.ctx.save();
      this.ctx.fillStyle   = 'rgba(200,200,220,0.35)';
      this.ctx.font        = 'bold 56px "Cairo", "Baloo Bhaijaan 2", sans-serif';
      this.ctx.textAlign   = 'center';
      this.ctx.textBaseline = 'middle';
      this.ctx.fillText(text.trim(), canvas.width / 2, canvas.height / 2);
      this.ctx.restore();
    }

    this.applyToolStyle();
  }

  private applyToolStyle(): void {
    const isPen = this.tool() === 'pen';
    this.ctx.globalCompositeOperation = 'source-over';
    this.ctx.strokeStyle = isPen ? '#1a1a2e' : '#ffffff';
    this.ctx.lineWidth   = isPen ? 3 : 24;
  }

  private bindPointerEvents(canvas: HTMLCanvasElement): void {
    canvas.style.touchAction = 'none';

    canvas.onpointerdown = (e: PointerEvent) => {
      canvas.setPointerCapture(e.pointerId);
      this.applyToolStyle();
      const { x, y } = this.pos(e, canvas);
      this.isDrawing = true;
      this.ctx.beginPath();
      this.ctx.moveTo(x, y);
    };

    canvas.onpointermove = (e: PointerEvent) => {
      if (!this.isDrawing) return;
      const { x, y } = this.pos(e, canvas);
      // Stylus pressure support
      if (this.tool() === 'pen' && e.pressure > 0) {
        this.ctx.lineWidth = Math.max(2, e.pressure * 10);
      }
      this.ctx.lineTo(x, y);
      this.ctx.stroke();
      this.hasDrawing.set(true);
    };

    canvas.onpointerup = canvas.onpointercancel = () => {
      this.isDrawing = false;
      this.ctx.beginPath();
    };
  }

  private pos(e: PointerEvent, canvas: HTMLCanvasElement) {
    const r = canvas.getBoundingClientRect();
    return {
      x: (e.clientX - r.left) * (canvas.width  / r.width),
      y: (e.clientY - r.top)  * (canvas.height / r.height)
    };
  }

  // ── Tool switching ────────────────────────────────────────────────────────────
  setTool(t: Tool): void {
    this.tool.set(t);
    this.applyToolStyle();
  }

  // ── Clear ─────────────────────────────────────────────────────────────────────
  clearCanvas(): void {
    this.hasDrawing.set(false);
    this.result.set(null);
    this.error.set(null);
    this.drawBackground(this.expectedText());
  }

  // ── Submit ────────────────────────────────────────────────────────────────────
  submit(): void {
    const expected = this.expectedText().trim();
    if (!expected) { this.error.set('يرجى كتابة الجملة المطلوبة أولاً.'); return; }
    if (!this.hasDrawing()) { this.error.set('يرجى كتابة الجملة على اللوحة.'); return; }

    // Composite canvas onto pure white (strips any transparency)
    const canvas = this.canvasRef.nativeElement;
    const off    = document.createElement('canvas');
    off.width  = canvas.width;
    off.height = canvas.height;
    const offCtx = off.getContext('2d')!;
    offCtx.fillStyle = '#ffffff';
    offCtx.fillRect(0, 0, off.width, off.height);
    offCtx.drawImage(canvas, 0, 0);

    const base64 = off.toDataURL('image/png').split(',')[1];

    this.isLoading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.svc.evaluateCanvasWriting(base64, expected).subscribe({
      next: res => {
        this.result.set(res);
        this.isLoading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message ?? 'حدث خطأ أثناء التحليل.');
        this.isLoading.set(false);
      }
    });
  }

  tryAgain(): void { this.clearCanvas(); }

  goBack(): void {
    this.router.navigate([this.state.userRole() === 'student' ? '/dashboard' : '/']);
  }
}
