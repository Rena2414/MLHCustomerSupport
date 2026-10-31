export type UserRole = 'Admin' | 'Teacher' | 'Student' | 'Parent';

export type RegisterRole = 'teacher' | 'student' | 'parent';

export type SubscriptionType = 0 | 1;

export interface AuthUser {
  id: string;
  login: string;
  name: string;
  age: number;
  role: UserRole;
  subscriptionType: SubscriptionType;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}

export interface LoginRequest {
  login: string;
  password: string;
}

export interface RegisterRequest {
  login: string;
  password: string;
  name: string;
  age: number;
  role: RegisterRole;
}

export const HOME_ROUTE_BY_ROLE: Record<UserRole, string> = {
  Admin: '/admin/users',
  Teacher: '/teacher/classes',
  Parent: '/parent/children',
  Student: '/student/home'
};
