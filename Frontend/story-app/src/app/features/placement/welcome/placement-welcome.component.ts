import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-placement-welcome',
  standalone: true,
  imports: [NavbarComponent],
  templateUrl: './placement-welcome.component.html',
  styleUrl: './placement-welcome.component.css'
})
export class PlacementWelcomeComponent {
  private readonly router = inject(Router);
  startTest(): void { this.router.navigate(['/test/question']); }
}
