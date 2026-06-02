import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DashboardNavbar } from '../dashboard-navbar/dashboard-navbar';
import { DashboardSidebar } from '../dashboard-sidebar/dashboard-sidebar';

@Component({
  selector: 'app-dashboard-layout',
  imports: [DashboardNavbar, DashboardSidebar, RouterOutlet],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.css',
})
export class DashboardLayout {}
