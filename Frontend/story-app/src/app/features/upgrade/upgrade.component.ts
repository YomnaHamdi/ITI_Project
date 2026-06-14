import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-upgrade',
  standalone: true,
  imports: [CommonModule, RouterLink, NavbarComponent],
  templateUrl: './upgrade.component.html',
  styleUrl: './upgrade.component.css'
})
export class UpgradeComponent {
  readonly selectedPlan = signal<'family' | 'school' | null>(null);
  readonly billingCycle = signal<'monthly' | 'yearly'>('monthly');

  readonly plans = [
    {
      id: 'family' as const,
      name: 'العائلي',
      icon: '👨‍👩‍👧',
      monthlyPrice: 29,
      yearlyPrice: 19,
      color: '#F4788A',
      popular: true,
      features: [
        'قصص غير محدودة',
        'كل المستويات الثلاثة',
        'حتى 4 أطفال',
        'تقارير أولياء الأمور',
        'مولّد القصص بالذكاء الاصطناعي',
        'دعم عبر البريد الإلكتروني',
      ]
    },
    {
      id: 'school' as const,
      name: 'المدرسي',
      icon: '🏫',
      monthlyPrice: 199,
      yearlyPrice: 149,
      color: '#7C3AED',
      popular: false,
      features: [
        'عدد غير محدود من الطلاب',
        'لوحة تحكم المعلمين',
        'مولّد الدروس بالذكاء الاصطناعي',
        'تحليلات متقدمة',
        'تقارير الفصل الدراسي',
        'دعم مخصص على مدار الساعة',
      ]
    }
  ];

  price(plan: typeof this.plans[0]): number {
    return this.billingCycle() === 'yearly' ? plan.yearlyPrice : plan.monthlyPrice;
  }

  savings(plan: typeof this.plans[0]): number {
    return Math.round((plan.monthlyPrice - plan.yearlyPrice) * 12);
  }
}
