import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { AdminShellComponent } from './layouts/admin-shell/admin-shell.component';
import { AuthShellComponent } from './layouts/auth-shell/auth-shell.component';

export const routes: Routes = [
  {
    path: 'auth',
    component: AuthShellComponent,
    canActivate: [guestGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login-page.component').then(
            (m) => m.LoginPageComponent,
          ),
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./features/auth/forgot-password/forgot-password-page.component').then(
            (m) => m.ForgotPasswordPageComponent,
          ),
      },
    ],
  },
  {
    path: '403',
    loadComponent: () =>
      import('./features/errors/forbidden-page.component').then(
        (m) => m.ForbiddenPageComponent,
      ),
  },
  {
    path: '404',
    loadComponent: () =>
      import('./features/errors/not-found-page.component').then(
        (m) => m.NotFoundPageComponent,
      ),
  },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    canActivateChild: [roleGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component').then(
            (m) => m.DashboardPageComponent,
          ),
        data: { title: 'Dashboard' },
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/users/users-page.component').then(
            (m) => m.UsersPageComponent,
          ),
        data: { title: 'Users' },
      },
      {
        path: 'media',
        loadComponent: () =>
          import('./features/media/media-page.component').then(
            (m) => m.MediaPageComponent,
          ),
        data: { title: 'Media' },
      },
      {
        path: 'jobs',
        loadComponent: () =>
          import('./features/jobs/jobs-page.component').then(
            (m) => m.JobsPageComponent,
          ),
        data: { title: 'Download Jobs' },
      },
      {
        path: 'platforms',
        loadComponent: () =>
          import('./features/platforms/platforms-page.component').then(
            (m) => m.PlatformsPageComponent,
          ),
        data: { title: 'Platforms' },
      },
      {
        path: 'providers',
        loadComponent: () =>
          import('./features/providers/providers-page.component').then(
            (m) => m.ProvidersPageComponent,
          ),
        data: { title: 'Providers' },
      },
      {
        path: 'storage',
        loadComponent: () =>
          import('./features/storage/storage-page.component').then(
            (m) => m.StoragePageComponent,
          ),
        data: { title: 'Storage' },
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports-page.component').then(
            (m) => m.ReportsPageComponent,
          ),
        data: { title: 'Reports' },
      },
      {
        path: 'system-health',
        loadComponent: () =>
          import('./features/health/health-page.component').then(
            (m) => m.HealthPageComponent,
          ),
        data: { title: 'System Health' },
      },
      {
        path: 'logs',
        loadComponent: () =>
          import('./features/logs/logs-page.component').then(
            (m) => m.LogsPageComponent,
          ),
        data: { title: 'Logs' },
      },
      {
        path: 'admin-users',
        loadComponent: () =>
          import('./features/admins/admins-page.component').then(
            (m) => m.AdminsPageComponent,
          ),
        data: { title: 'Admin Users' },
      },
      {
        path: 'roles',
        loadComponent: () =>
          import('./features/roles/roles-page.component').then(
            (m) => m.RolesPageComponent,
          ),
        data: { title: 'Roles' },
      },
      {
        path: 'audit-logs',
        loadComponent: () =>
          import('./features/audit/audit-page.component').then(
            (m) => m.AuditPageComponent,
          ),
        data: { title: 'Audit Logs' },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/settings/settings-page.component').then(
            (m) => m.SettingsPageComponent,
          ),
        data: { title: 'Settings' },
      },
    ],
  },
  { path: '**', redirectTo: '404' },
];
