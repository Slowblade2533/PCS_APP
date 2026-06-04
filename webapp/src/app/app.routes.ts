import { Routes } from '@angular/router';
import { DashboardLayout } from './pages/dashboard/dashboard-layout/dashboard-layout';
import { DashboardMainContent } from './pages/dashboard/dashboard-main-content/dashboard-main-content';
import { Login } from './pages/login/login';
import { ProductCreate } from './pages/products/product-create/product-create';
import { ProductsList } from './pages/products/products-list/products-list';
import { authGuard } from './shared/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: Login,
  },
  {
    path: '',
    component: DashboardLayout,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        component: DashboardMainContent,
      },
      {
        path: 'products',
        component: ProductsList,
      },
      {
        path: 'products/create',
        component: ProductCreate,
      },
      {
        path: 'products/:id',
        component: ProductCreate,
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
