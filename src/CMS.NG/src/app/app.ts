import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { ToastModule } from 'primeng/toast';
import { MenuItem } from 'primeng/api';
import { AuthService } from '@core/services/auth.service';

interface NavChild {
  label: string;
  route: string;
  icon: string;
}

interface NavGroup {
  label: string;
  icon: string;
  children: NavChild[];
  /** When true, the group is shown only to users whose roles include "Admin". */
  requiresAdmin?: boolean;
}

interface NavSection {
  title: string;
  groups: NavGroup[];
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MenuModule, ToastModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly collapsed = signal(false);

  // Popup menu anchored to the signed-in user chip in the topbar.
  protected readonly userMenuItems: MenuItem[] = [
    { label: '個人資料 My Profile', icon: 'pi pi-user-edit', routerLink: '/profile' },
    { separator: true },
    { label: '登出 Logout', icon: 'pi pi-sign-out', command: () => this.logout() },
  ];

  // Sidebar navigation (Ultima-style). Only 系統管理 Admin > 角色 AppRole is wired up.
  private readonly navSections: NavSection[] = [
    {
      title: '選單 MENU',
      groups: [
        {
          label: '首頁 Home',
          icon: 'pi pi-home',
          children: [
            { label: '上稿作業 FeaturedPromoItem', route: '/featured-promo-items', icon: 'pi pi-calendar' },
          ],
        },
        {
          label: '課程管理 Course',
          icon: 'pi pi-folder',
          children: [
            { label: '課程 Course', route: '/courses', icon: 'pi pi-book' },
            { label: '合作廠商 Partner', route: '/partners', icon: 'pi pi-building' },
            { label: '課程群組 CourseGroup', route: '/course-groups', icon: 'pi pi-sitemap' },
          ],
        },
        { label: '說明會 Seminar', icon: 'pi pi-comments', children: [] },
        { label: '活動管理 Promotion', icon: 'pi pi-megaphone', children: [] },
        { label: '線上報名 Forms', icon: 'pi pi-pencil', children: [] },
        { label: '網站資訊 WebInfo', icon: 'pi pi-globe', children: [] },
        { label: '考試中心 TestingCenter', icon: 'pi pi-verified', children: [] },
      ],
    },
    {
      title: '系統 SYSTEM',
      groups: [
        {
          label: '系統管理 Admin',
          icon: 'pi pi-shield',
          requiresAdmin: true,
          children: [
            { label: '角色 AppRole', route: '/app-roles', icon: 'pi pi-id-card' },
            { label: '使用者 AppUser', route: '/app-users', icon: 'pi pi-user' },
            { label: '發布狀態 PublishStatus', route: '/publish-statuses', icon: 'pi pi-flag' },
          ],
        },
      ],
    },
  ];

  // Admin-only groups are hidden unless the token's roles include "Admin"; empty sections drop out.
  protected readonly visibleSections = computed<NavSection[]>(() => {
    const isAdmin = this.auth.hasRole('Admin');
    return this.navSections
      .map((section) => ({
        ...section,
        groups: section.groups.filter((group) => !group.requiresAdmin || isAdmin),
      }))
      .filter((section) => section.groups.length > 0);
  });

  protected readonly expanded = signal<Record<string, boolean>>({ '首頁 Home': true, '系統管理 Admin': true });

  toggleCollapse(): void {
    this.collapsed.update((v) => !v);
  }

  toggleGroup(label: string): void {
    this.expanded.update((state) => ({ ...state, [label]: !state[label] }));
  }

  isExpanded(label: string): boolean {
    return !!this.expanded()[label];
  }

  logout(): void {
    this.auth.logoutAndRedirect();
  }
}
