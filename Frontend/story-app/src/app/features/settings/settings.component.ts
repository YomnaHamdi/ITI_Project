import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { AppStateService } from '../../services/app-state-service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly user    = this.state.currentUser;
  readonly saved   = signal(false);
  readonly deleting = signal(false);

  form = { name: '', email: '', currentPassword: '', newPassword: '', confirmPassword: '' };
  readonly lang         = signal<'ar' | 'en'>('ar');
  readonly notifications = signal(true);
  readonly soundEffects  = signal(true);
  readonly ttsEnabled    = signal(true);

  ngOnInit(): void {
    const u = this.user();
    if (u) { this.form.name = u.name; }
    this.lang.set(this.state.lang());
  }

  saveProfile(): void {
    if (this.form.name.trim()) {
      const u = this.user();
      if (u) this.state.setUser({ ...u, name: this.form.name.trim() });
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), 3000);
    }
  }

  switchLang(l: 'ar' | 'en'): void {
    this.lang.set(l);
    this.state.setLang(l);
  }

  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
