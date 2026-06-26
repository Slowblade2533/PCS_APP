import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../shared/services/auth.service';

@Component({
  selector: 'app-dashboard-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './dashboard-sidebar.html',
})
export class DashboardSidebar {
  private authService = inject(AuthService);
  private router = inject(Router);

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout();
  }
}
