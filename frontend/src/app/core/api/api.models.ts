import { SubscriptionType, UserRole } from '../auth/auth.models';

export interface UserSummary {
  id: string;
  login: string;
  name: string;
  age: number;
  role: UserRole;
  subscriptionType: SubscriptionType;
}

export interface UserListItem extends UserSummary {
  createdAt: string;
}

export interface ClassStudent {
  id: string;
  login: string;
  name: string;
  age: number;
}

export interface ClassInfo {
  id: string;
  name: string;
  subject: string;
  teacherId: string;
  teacherName: string;
}

export interface TeacherClass extends ClassInfo {
  createdAt: string;
  students: ClassStudent[];
}

export type ConcernStatus = 'New' | 'Reviewed';

export interface Concern {
  id: string;
  studentId: string;
  studentName: string;
  teacherId: string;
  teacherName: string;
  text: string;
  category: string;
  severity: string;
  summary: string;
  status: ConcernStatus;
  createdAt: string;
}

export interface AuditEntry {
  id: string;
  actor: string;
  action: string;
  details: string;
  createdAt: string;
}

export interface UserDetails extends UserListItem {
  children: UserSummary[];
  parents: UserSummary[];
  classes: TeacherClass[];
  concerns: Concern[];
  audit: AuditEntry[];
}

export interface ChildInfo extends UserSummary {
  classes: ClassInfo[];
}

export interface CategoryCount {
  category: string;
  count: number;
}

export interface SeverityCount {
  severity: string;
  count: number;
}

export interface TrendPoint {
  date: string;
  count: number;
}

export interface TeacherAnalytics {
  teacherId: string;
  teacherName: string;
  total: number;
  open: number;
  byCategory: CategoryCount[];
  bySeverity: SeverityCount[];
  trend: TrendPoint[];
}

export interface TeacherAnalyticsDetails extends TeacherAnalytics {
  concerns: Concern[];
}

export interface TeacherSummary {
  summary: string;
  recommendedActions: string[];
  generatedAt: string;
}
