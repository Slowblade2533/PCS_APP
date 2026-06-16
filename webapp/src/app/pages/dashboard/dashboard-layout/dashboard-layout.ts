import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DashboardSidebar } from '../dashboard-sidebar/dashboard-sidebar';

@Component({
  selector: 'app-dashboard-layout',
  imports: [DashboardSidebar, RouterOutlet],
  templateUrl: './dashboard-layout.html',
})
export class DashboardLayout {}
