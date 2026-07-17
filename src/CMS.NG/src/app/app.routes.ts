import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'app-roles' },
  {
    path: 'login',
    loadComponent: () => import('@features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('@features/profile/profile').then((m) => m.Profile),
  },
  {
    path: 'featured-promo-items',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        '@features/featured-promo-items/featured-promo-item-board/featured-promo-item-board'
      ).then((m) => m.FeaturedPromoItemBoard),
  },
  {
    path: 'app-roles',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-roles/app-role-list/app-role-list').then((m) => m.AppRoleList),
  },
  {
    path: 'app-roles/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-roles/app-role-form/app-role-form').then((m) => m.AppRoleForm),
  },
  {
    path: 'app-roles/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-roles/app-role-form/app-role-form').then((m) => m.AppRoleForm),
  },
  {
    path: 'app-roles/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-roles/app-role-detail/app-role-detail').then((m) => m.AppRoleDetail),
  },
  {
    path: 'app-users',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-users/app-user-list/app-user-list').then((m) => m.AppUserList),
  },
  {
    path: 'app-users/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-users/app-user-form/app-user-form').then((m) => m.AppUserForm),
  },
  {
    path: 'app-users/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-users/app-user-form/app-user-form').then((m) => m.AppUserForm),
  },
  {
    path: 'app-users/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/app-users/app-user-detail/app-user-detail').then((m) => m.AppUserDetail),
  },
  {
    path: 'publish-statuses',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/publish-statuses/publish-status-list/publish-status-list').then(
        (m) => m.PublishStatusList,
      ),
  },
  {
    path: 'publish-statuses/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/publish-statuses/publish-status-form/publish-status-form').then(
        (m) => m.PublishStatusForm,
      ),
  },
  {
    path: 'publish-statuses/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/publish-statuses/publish-status-form/publish-status-form').then(
        (m) => m.PublishStatusForm,
      ),
  },
  {
    path: 'publish-statuses/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/publish-statuses/publish-status-detail/publish-status-detail').then(
        (m) => m.PublishStatusDetail,
      ),
  },
  {
    path: 'partners',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/partners/partner-list/partner-list').then((m) => m.PartnerList),
  },
  {
    path: 'partners/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/partners/partner-form/partner-form').then((m) => m.PartnerForm),
  },
  {
    path: 'partners/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/partners/partner-form/partner-form').then((m) => m.PartnerForm),
  },
  {
    path: 'partners/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/partners/partner-detail/partner-detail').then((m) => m.PartnerDetail),
  },
  {
    path: 'course-groups',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/course-groups/course-group-list/course-group-list').then(
        (m) => m.CourseGroupList,
      ),
  },
  {
    path: 'course-groups/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/course-groups/course-group-form/course-group-form').then(
        (m) => m.CourseGroupForm,
      ),
  },
  {
    path: 'course-groups/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/course-groups/course-group-form/course-group-form').then(
        (m) => m.CourseGroupForm,
      ),
  },
  {
    path: 'course-groups/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/course-groups/course-group-detail/course-group-detail').then(
        (m) => m.CourseGroupDetail,
      ),
  },
  {
    path: 'courses',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/courses/course-list/course-list').then((m) => m.CourseList),
  },
  {
    path: 'courses/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/courses/course-form/course-form').then((m) => m.CourseForm),
  },
  {
    path: 'courses/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/courses/course-form/course-form').then((m) => m.CourseForm),
  },
  {
    path: 'courses/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@features/courses/course-detail/course-detail').then((m) => m.CourseDetail),
  },
  { path: '**', redirectTo: 'app-roles' },
];
