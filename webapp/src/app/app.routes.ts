import { Routes } from '@angular/router';
import { authGuard } from './shared/auth/auth.guard';
import { permissionGuard } from './shared/auth/permission.guard';
import { pendingChangesGuard } from './shared/guards/pending-changes.guard';

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
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'products/:id',
        loadComponent: () =>
          import('./pages/products/product-create/product-create').then((m) => m.ProductCreate),
        canDeactivate: [pendingChangesGuard],
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
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'users/:id',
        loadComponent: () =>
          import('./pages/users/user-create/user-create').then((m) => m.UserCreate),
        canDeactivate: [pendingChangesGuard],
      },

      // ── Procurement ───────────────────────────────────────────
      {
        path: 'procurement/vcb-orders',
        loadComponent: () =>
          import('./pages/procurement/vcb-orders-list/vcb-orders-list').then(
            (m) => m.VcbOrdersListComponent,
          ),
      },
      {
        path: 'procurement/vcb-orders/create',
        loadComponent: () =>
          import('./pages/procurement/vcb-orders-create/vcb-orders-create').then(
            (m) => m.VcbOrdersCreateComponent,
          ),
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'procurement/vcb-orders/:id',
        loadComponent: () =>
          import('./pages/procurement/vcb-orders-create/vcb-orders-create').then(
            (m) => m.VcbOrdersCreateComponent,
          ),
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'procurement/vcb-shipments',
        loadComponent: () =>
          import('./pages/procurement/vcb-shipments-list/vcb-shipments-list').then(
            (m) => m.VcbShipmentsListComponent,
          ),
      },
      {
        path: 'procurement/vcb-shipments/create',
        loadComponent: () =>
          import('./pages/procurement/vcb-shipments-create/vcb-shipments-create').then(
            (m) => m.VcbShipmentsCreateComponent,
          ),
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'procurement/vcb-deliveries',
        loadComponent: () =>
          import('./pages/procurement/vcb-deliveries-list/vcb-deliveries-list').then(
            (m) => m.VcbDeliveriesList,
          ),
      },
      {
        path: 'procurement/vcb-deliveries/create',
        loadComponent: () =>
          import('./pages/procurement/vcb-deliveries-create/vcb-deliveries-create').then(
            (m) => m.VcbDeliveriesCreateComponent,
          ),
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'procurement/vcb-deliveries/:id',
        loadComponent: () =>
          import('./pages/procurement/vcb-deliveries-create/vcb-deliveries-create').then(
            (m) => m.VcbDeliveriesCreateComponent,
          ),
        canDeactivate: [pendingChangesGuard],
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
          import('./pages/settings/categories/categories-list/categories-list').then(
            (m) => m.CategoriesList,
          ),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
      },
      {
        path: 'settings/categories/create',
        loadComponent: () =>
          import('./pages/settings/categories/category-create/category-create').then(
            (m) => m.CategoryCreate,
          ),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
        canDeactivate: [pendingChangesGuard],
      },
      {
        path: 'settings/categories/:id',
        loadComponent: () =>
          import('./pages/settings/categories/category-create/category-create').then(
            (m) => m.CategoryCreate,
          ),
        canActivate: [permissionGuard],
        data: { permission: 'system:settings' },
        canDeactivate: [pendingChangesGuard],
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
  },
];
