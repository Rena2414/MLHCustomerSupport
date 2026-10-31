import { Routes } from '@angular/router';
import { guestGuard, redirectByRole, roleGuard } from './core/auth/role.guard';
import { TeacherAnalyticsComponent } from './features/admin/teacher-analytics/teacher-analytics.component';
import { UserDetailComponent } from './features/admin/user-detail/user-detail.component';
import { UsersListComponent } from './features/admin/users-list/users-list.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { ChatPageComponent } from './features/chat/chat-page/chat-page.component';
import { ChildrenComponent } from './features/parent/children/children.component';
import { StudentHomeComponent } from './features/student/home/student-home.component';
import { ClassesComponent } from './features/teacher/classes/classes.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: redirectByRole },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
  {
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'users' },
      { path: 'users', component: UsersListComponent },
      { path: 'users/:id', component: UserDetailComponent },
      { path: 'analytics', component: TeacherAnalyticsComponent }
    ]
  },
  {
    path: 'teacher',
    canActivate: [roleGuard('Teacher')],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'classes' },
      { path: 'classes', component: ClassesComponent }
    ]
  },
  {
    path: 'parent',
    canActivate: [roleGuard('Parent')],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'children' },
      { path: 'children', component: ChildrenComponent }
    ]
  },
  {
    path: 'student',
    canActivate: [roleGuard('Student')],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'home' },
      { path: 'home', component: StudentHomeComponent },
      { path: 'chat', component: ChatPageComponent }
    ]
  },
  { path: '**', redirectTo: redirectByRole }
];
