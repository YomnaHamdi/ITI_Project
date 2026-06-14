import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [NavbarComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  private readonly router = inject(Router);

  readonly levels = [
    {
      number: 1,
      title: 'الحروف والأصوات',
      subtitle: 'تعلّم 28 حرفاً عربياً',
      icon: '📖',
      locked: false,
    },
    {
      number: 2,
      title: 'الكلمات والمفردات',
      subtitle: 'تعلّم أكثر من 200 كلمة',
      icon: '📝',
      locked: true,
    },
    {
      number: 3,
      title: 'الجمل والقصص',
      subtitle: 'اقرأ جملاً وقصصاً كاملة',
      icon: '📚',
      locked: true,
    },
  ];

  readonly features = [
    { icon: '🔊', title: 'قصص صوتية',     subtitle: 'استمع وتعلم بنطق صحيح' },
    { icon: '✏️', title: 'تدريب الكتابة',  subtitle: 'اتبع وادرس الحروف العربية' },
    { icon: '🤖', title: 'ذكاء اصطناعي',   subtitle: 'قصص مخصصة لمستواك' },
    { icon: '📊', title: 'تتبع التقدم',    subtitle: 'راقب رحلتك التعليمية' },
  ];

  readonly stats = [
    { value: '+10K', label: 'طالب' },
    { value: '+150', label: 'قصة' },
    { value: '28',   label: 'حرف' },
  ];

  readonly pricingPlans = [
    {
      icon: '🌱', name: 'مجاني', price: 0, popular: false,
      features: ['3 قصص/شهر', 'مستوى واحد', 'بدون تقارير'],
    },
    {
      icon: '👨‍👧', name: 'عائلي', price: 29, popular: true,
      features: ['قصص غير محدودة', 'كل المستويات', 'تقارير متقدمة'],
    },
    {
      icon: '🏫', name: 'مدرسي', price: 199, popular: false,
      features: ['عدد لا محدود من الطلاب', 'لوحة معلمين', 'تحليلات كاملة'],
    },
  ];

  startNow():     void { this.router.navigate(['/auth/register']); }
  viewLevels():   void { this.router.navigate(['/levels']); }
  takeFreeTest(): void { this.router.navigate(['/test']); }
  goUpgrade():    void { this.router.navigate(['/upgrade']); }
}
