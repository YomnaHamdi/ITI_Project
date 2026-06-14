import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, DecimalPipe, AdminSidebarComponent],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css'
})
export class SubscriptionsComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading = signal(false);
  readonly data      = signal<any>(null);

  readonly plans = [
    { name: 'مجاني',    price: 0,   color: '#86EFAC', features: ['3 قصص/شهر', 'مستوى واحد', 'بدون تقارير'] },
    { name: 'عائلي',    price: 29,  color: '#F4788A', features: ['قصص غير محدودة', 'كل المستويات', 'تقارير متقدمة'] },
    { name: 'مدرسي',    price: 199, color: '#C4B5FD', features: ['عدد لا محدود من الطلاب', 'لوحة معلمين', 'تحليلات كاملة'] },
  ];

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getSubscriptionStats().subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }
}
