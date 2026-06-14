import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * MainLayoutComponent — thin shell around all routed pages.
 * Currently just passes through the router outlet.
 * Add a shared nav bar or footer here when needed.
 */
@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <main>
      <router-outlet />
    </main>
  `,
  styles: [`
    main { display: contents; }
  `]
})
export class MainLayoutComponent {}
