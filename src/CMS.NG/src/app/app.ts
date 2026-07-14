import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

interface NavChild {
  label: string;
  route: string;
  icon: string;
}

interface NavGroup {
  label: string;
  icon: string;
  children: NavChild[];
}

interface NavSection {
  title: string;
  groups: NavGroup[];
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly collapsed = signal(false);

  // Sidebar navigation (Ultima-style). Only 系統管理 Admin > 角色 AppRole is wired up.
  protected readonly navSections: NavSection[] = [
    {
      title: '選單 MENU',
      groups: [
        { label: '首頁管理 Home', icon: 'pi pi-home', children: [] },
        { label: '課程管理 Course', icon: 'pi pi-folder', children: [] },
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
          children: [
            { label: '角色 AppRole', route: '/app-roles', icon: 'pi pi-id-card' },
            { label: '使用者 AppUser', route: '/app-users', icon: 'pi pi-user' },
          ],
        },
      ],
    },
  ];

  protected readonly expanded = signal<Record<string, boolean>>({ '系統管理 Admin': true });

  toggleCollapse(): void {
    this.collapsed.update((v) => !v);
  }

  toggleGroup(label: string): void {
    this.expanded.update((state) => ({ ...state, [label]: !state[label] }));
  }

  isExpanded(label: string): boolean {
    return !!this.expanded()[label];
  }
}
