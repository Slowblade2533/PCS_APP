import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../../shared/services/auth.service';

@Component({
  selector: 'app-dashboard-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './dashboard-sidebar.html',
  styleUrl: './dashboard-sidebar.css',
})
export class DashboardSidebar {
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  constructor() {
    // Workaround for RouterLinkActive in Zoneless Angular 18
    this.router.events.subscribe(() => {
      this.cdr.markForCheck();
    });
  }

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout();
  }
}
