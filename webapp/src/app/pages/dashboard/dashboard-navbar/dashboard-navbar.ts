import { Component, inject } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';

@Component({
  selector: 'app-dashboard-navbar',
  imports: [],
  templateUrl: './dashboard-navbar.html',
  styleUrl: './dashboard-navbar.css',
})
export class DashboardNavbar {
  private authService = inject(AuthService);

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout();
  }
}
