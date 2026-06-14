import { Component, input, OnInit, OnDestroy, signal } from '@angular/core';

interface AgentStep {
  label: string;
  done: boolean;
  active: boolean;
}

@Component({
  selector: 'app-loading',
  standalone: true,
  template: `
    <div class="overlay" role="status">
      <div class="card">
        <div class="stars">⭐ 🌙 ✨</div>
        <div class="ring"></div>
        <p class="title">{{ message() }}</p>
        <div class="steps">
          @for (s of steps(); track s.label) {
            <div class="step" [class.active]="s.active" [class.done]="s.done">
              <span class="icon">{{ s.done ? '✅' : s.active ? '⏳' : '⬜' }}</span>
              <span>{{ s.label }}</span>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Baloo+Bhaijaan+2:wght@700;800&display=swap');
    .overlay{position:fixed;inset:0;background:rgba(15,20,50,.9);display:flex;align-items:center;justify-content:center;z-index:9999;backdrop-filter:blur(6px)}
    .card{background:#fff;border-radius:24px;padding:2rem 2.5rem;text-align:center;max-width:340px;width:90%;font-family:'Baloo Bhaijaan 2',sans-serif;box-shadow:0 20px 60px rgba(0,0,0,.4)}
    .stars{font-size:2rem;margin-bottom:.6rem;animation:bounce 1.5s ease-in-out infinite}
    @keyframes bounce{0%,100%{transform:translateY(0)}50%{transform:translateY(-10px)}}
    .ring{width:52px;height:52px;border:5px solid #e0e7ff;border-top-color:#667eea;border-radius:50%;animation:spin .8s linear infinite;margin:0 auto .8rem}
    @keyframes spin{to{transform:rotate(360deg)}}
    .title{font-size:1.1rem;font-weight:800;color:#333;margin:0 0 1rem}
    .steps{display:flex;flex-direction:column;gap:.4rem;direction:rtl;text-align:right}
    .step{display:flex;align-items:center;gap:.5rem;padding:.45rem .75rem;border-radius:10px;background:#f8faff;border:1.5px solid #e0e7ff;font-size:.9rem;font-weight:700;color:#555;transition:all .3s}
    .step.active{background:#eef2ff;border-color:#667eea}
    .step.done{background:#f0fff4;border-color:#68d391}
    .icon{font-size:1rem;flex-shrink:0}
  `]
})
export class LoadingComponent implements OnInit, OnDestroy {
  readonly message = input<string>('جارٍ إنشاء القصة...');

  readonly steps = signal<AgentStep[]>([
    { label: 'كتابة القصة العربية 📝',         done: false, active: false },
    { label: 'التحقق من المحتوى 🛡️',           done: false, active: false },
    { label: 'رسم صورة الصفحة الأولى 🎨',      done: false, active: false },
    { label: 'رسم صورة الصفحة الثانية 🎨',     done: false, active: false },
    { label: 'رسم صورة الصفحة الثالثة 🎨',     done: false, active: false },
    { label: 'حفظ القصة في قاعدة البيانات 💾',  done: false, active: false },
  ]);

  private readonly durations = [8000, 3000, 25000, 25000, 25000, 2000];
  private timer?: ReturnType<typeof setTimeout>;

  ngOnInit():     void { this.activate(0); }
  ngOnDestroy():  void { clearTimeout(this.timer as unknown as number); }

  private activate(i: number): void {
    if (i >= this.steps().length) return;
    this.steps.update(list =>
      list.map((s, j) => ({ ...s, active: j === i, done: j < i })));
    this.timer = setTimeout(() => this.activate(i + 1), this.durations[i] ?? 5000);
  }
}