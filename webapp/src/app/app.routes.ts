import { Routes } from '@angular/router';
import { authGuard } from './shared/auth/auth.guard';
import { permissionGuard } from './shared/auth/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: '',
    loadComponent: () =>
      import('./pages/dashboard/dashboard-layout/dashboard-layout').then((m) => m.DashboardLayout),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/dashboard-main-content/dashboard-main-content').then(
            (m) => m.DashboardMainContent,
          ),
      },

      // ── Products ──────────────────────────────────────────────
      {
        path: 'products',
        loadComponent: () =>
          import('./pages/products/products-list/products-list').then((m) => m.ProductsList),
        canActivate: [permissionGuard],
        data: { permission: 'product:view' },
      },
      {
        path: 'products/create',
        loadComponent: () =>
          import('./pages/products/product-create/product-create').then((m) => m.ProductCreate),
      },
      {
        path: 'products/:id',
        loadComponent: () =>
          import('./pages/products/product-create/product-create').then((m) => m.ProductCreate),
      },

      // ── Stock ─────────────────────────────────────────────────
      {
        path: 'stock',
        loadComponent: () => import('./pages/stock/stock-list/stock-list').then((m) => m.StockList),
        canActivate: [permissionGuard],
        data: { permission: 'stock:view' },
      },
      {
        path: 'stock/transactions',
        loadComponent: () =>
          import('./pages/stock/stock-transaction/stock-transaction').then(
            (m) => m.StockTransactionList,
          ),
      },
      {
        path: 'stock/adjust',
        loadComponent: () =>
          import('./pages/stock/stock-transaction/stock-transaction').then(
            (m) => m.StockTransactionList,
          ),
      },

      // ── Users ─────────────────────────────────────────────────
      {
        path: 'users',
        loadComponent: () => import('./pages/users/users-list/users-list').then((m) => m.UsersList),
        canActivate: [permissionGuard],
        data: { permission: 'user:view' },
      },
      {
        path: 'users/create',
        loadComponent: () =>
          import('./pages/users/user-create/user-create').then((m) => m.UserCreate),
      },
      {
        path: 'users/:id',
        loadComponent: () =>
          import('./pages/users/user-create/user-create').then((m) => m.UserCreate),
      },
      // ── Settings ──────────────────────────────────────────────
      {
        path: 'settings/company-profile',
        loadComponent: () =>
          import('./pages/settings/company-profile/company-profile').then((m) => m.CompanyProfile),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
      },
      {
        path: 'settings/categories',
        loadComponent: () =>
          import('./pages/settings/categories/categories-list/categories-list').then((m) => m.CategoriesList),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
      },
      {
        path: 'settings/categories/create',
        loadComponent: () =>
          import('./pages/settings/categories/category-create/category-create').then((m) => m.CategoryCreate),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
      },
      {
        path: 'settings/categories/:id',
        loadComponent: () =>
          import('./pages/settings/categories/category-create/category-create').then((m) => m.CategoryCreate),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
  },
];
