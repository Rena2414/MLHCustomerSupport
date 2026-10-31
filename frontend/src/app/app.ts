import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UserRole } from './core/auth/auth.models';
import { AuthService } from './core/auth/auth.service';
import { PRODUCT_NAME } from './core/brand';
import { ChatStore } from './core/chat/chat.store';

interface NavLink {
  path: string;
  label: string;
}

const NAV_BY_ROLE: Record<UserRole, NavLink[]> = {
  Admin: [
    { path: '/admin/users', label: 'Users' },
    { path: '/admin/analytics', label: 'Teacher analytics' }
  ],
  Teacher: [{ path: '/teacher/classes', label: 'My classes' }],
  Parent: [{ path: '/parent/children', label: 'My children' }],
  Student: [
    { path: '/student/home', label: 'Home' },
    { path: '/student/chat', label: 'Help chat' }
  ]
};

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly chatStore = inject(ChatStore);

  readonly productName = PRODUCT_NAME;
  readonly user = this.auth.user;
  readonly homeRoute = this.auth.homeRoute;
  readonly navLinks = computed(() => {
    const user = this.user();
    return user ? NAV_BY_ROLE[user.role] : [];
  });

  logout(): void {
    this.chatStore.reset();
    this.auth.logout();
  }
}
