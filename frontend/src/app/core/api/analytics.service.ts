import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AnalyticsOverviewDto, TeacherAnalyticsDetailsDto, TeacherStatsDto } from './api.dtos';
import { Concern, TeacherAnalytics, TeacherAnalyticsDetails, TeacherSummary } from './api.models';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/analytics/teachers`;

  getTeachers(): Observable<TeacherAnalytics[]> {
    return this.http.get<AnalyticsOverviewDto>(this.baseUrl).pipe(map(overview => overview.teachers.map(toTeacherAnalytics)));
  }

  getTeacher(id: string): Observable<TeacherAnalyticsDetails> {
    return this.http
      .get<TeacherAnalyticsDetailsDto>(`${this.baseUrl}/${id}`)
      .pipe(map(details => ({ ...toTeacherAnalytics(details.stats), concerns: details.concerns })));
  }

  generateSummary(id: string): Observable<TeacherSummary> {
    return this.http.post<TeacherSummary>(`${this.baseUrl}/${id}/summary`, {});
  }

  markReviewed(concernId: string): Observable<Concern> {
    return this.http.patch<Concern>(`${environment.apiUrl}/concerns/${concernId}`, { status: 'Reviewed' });
  }
}

function toTeacherAnalytics(stats: TeacherStatsDto): TeacherAnalytics {
  return {
    teacherId: stats.teacherId,
    teacherName: stats.teacherName,
    total: stats.totalConcerns,
    open: stats.openConcerns,
    byCategory: stats.byCategory.map(c => ({ category: c.key, count: c.count })),
    bySeverity: stats.bySeverity.map(c => ({ severity: c.key, count: c.count })),
    trend: stats.last30Days
  };
}
