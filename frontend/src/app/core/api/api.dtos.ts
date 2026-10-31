import { AuditEntry, Concern, TeacherClass, UserListItem } from './api.models';

export interface CountDto {
  key: string;
  count: number;
}

export interface TeacherStatsDto {
  teacherId: string;
  teacherName: string;
  classCount: number;
  studentCount: number;
  totalConcerns: number;
  openConcerns: number;
  highOrCriticalConcerns: number;
  lastConcernAt: string | null;
  byCategory: CountDto[];
  bySeverity: CountDto[];
  last30Days: { date: string; count: number }[];
}

export interface AnalyticsOverviewDto {
  totalConcerns: number;
  openConcerns: number;
  highOrCriticalConcerns: number;
  byCategory: CountDto[];
  teachers: TeacherStatsDto[];
}

export interface TeacherAnalyticsDetailsDto {
  stats: TeacherStatsDto;
  concerns: Concern[];
}

export interface UserDetailsDto {
  user: UserListItem;
  parents: UserListItem[];
  children: UserListItem[];
  classes: TeacherClass[];
  concerns: Concern[];
  audit: AuditEntry[];
}

export interface ChildDto {
  child: UserListItem;
  classes: TeacherClass[];
}
