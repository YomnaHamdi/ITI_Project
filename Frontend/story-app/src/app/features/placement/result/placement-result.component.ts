import { Component, signal, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

interface PlacementResult {
  score: number; total: number; level: number;
  p1: number; p2: number; p3: number;
}

@Component({
  selector: 'app-placement-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './placement-result.component.html',
  styleUrl: './placement-result.component.css'
})
export class PlacementResultComponent implements OnInit {
  private readonly router = inject(Router);

  readonly result   = signal<PlacementResult | null>(null);
  readonly confetti = signal<{ x: number; y: number; color: string; delay: number }[]>([]);

  readonly levelInfo = [
    { level: 1, name: 'الحروف والأصوات',   icon: '📖', color: '#F4788A', desc: 'ستتعلم الحروف العربية والأصوات الأساسية' },
    { level: 2, name: 'الكلمات والمفردات', icon: '📝', color: '#7C3AED', desc: 'ستتعلم مئات الكلمات العربية المفيدة' },
    { level: 3, name: 'الجمل والقصص',      icon: '📚', color: '#16A34A', desc: 'ستقرأ جملاً وقصصاً عربية كاملة' },
  ];

  readonly parts = [1, 2, 3];

  ngOnInit(): void {
    const raw = sessionStorage.getItem('placement_result');
    this.result.set(raw ? JSON.parse(raw) : { score: 10, total: 15, level: 1, p1: 4, p2: 3, p3: 3 });
    this.generateConfetti();
  }

  private generateConfetti(): void {
    const colors = ['#F4788A', '#C4B5FD', '#86EFAC', '#FDE68A', '#F9A8B4'];
    this.confetti.set(
      Array.from({ length: 30 }, (_, i) => ({
        x: Math.random() * 100,
        y: Math.random() * 100,
        color: colors[i % colors.length],
        delay: Math.random() * 1.5
      }))
    );
  }

  get levelData() {
    return this.levelInfo.find(l => l.level === (this.result()?.level ?? 1))!;
  }

  partScore(p: number): number {
    const r = this.result();
    if (!r) return 0;
    return p === 1 ? r.p1 : p === 2 ? r.p2 : r.p3;
  }

  startLearning(): void { this.router.navigate(['/levels']); }
}
