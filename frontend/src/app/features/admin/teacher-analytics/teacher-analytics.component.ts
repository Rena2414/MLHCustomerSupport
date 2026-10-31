import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { describeApiError } from '../../../core/api/api-error';
import { AnalyticsService } from '../../../core/api/analytics.service';
import { Concern, TeacherAnalytics, TeacherAnalyticsDetails, TeacherSummary } from '../../../core/api/api.models';
import { HumanizePipe } from '../../shared/label.pipes';
import { TrendDay, buildTrend } from './trend';

const SEVERITY_ORDER = ['Low', 'Medium', 'High', 'Critical'];
const CHART_WIDTH = 600;
const CHART_HEIGHT = 140;
const BAR_GAP = 2;
const SPARK_WIDTH = 120;
const SPARK_HEIGHT = 28;

interface BarRow {
  label: string;
  count: number;
  percent: number;
  tone: string;
}

interface TrendBar {
  day: TrendDay;
  x: number;
  y: number;
  width: number;
  height: number;
}

@Component({
  selector: 'app-teacher-analytics',
  imports: [DatePipe, HumanizePipe],
  templateUrl: './teacher-analytics.component.html',
  styleUrl: './teacher-analytics.component.css'
})
export class TeacherAnalyticsComponent {
  private readonly analytics = inject(AnalyticsService);

  readonly chartWidth = CHART_WIDTH;
  readonly chartHeight = CHART_HEIGHT;
  readonly sparkWidth = SPARK_WIDTH;
  readonly sparkHeight = SPARK_HEIGHT;

  readonly teachers = signal<TeacherAnalytics[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly selectedId = signal<string | null>(null);
  readonly details = signal<TeacherAnalyticsDetails | null>(null);
  readonly detailsLoading = signal(false);
  readonly summary = signal<TeacherSummary | null>(null);
  readonly summaryLoading = signal(false);
  readonly summaryError = signal<string | null>(null);
  readonly reviewingIds = signal<ReadonlySet<string>>(new Set());
  readonly openOnly = signal(false);
  readonly hoveredDay = signal<number | null>(null);

  readonly sortedTeachers = computed(() =>
    [...this.teachers()].sort((a, b) => b.open - a.open || b.total - a.total || a.teacherName.localeCompare(b.teacherName))
  );
  readonly totals = computed(() => {
    const list = this.teachers();
    return {
      total: list.reduce((sum, t) => sum + t.total, 0),
      open: list.reduce((sum, t) => sum + t.open, 0),
      lastThirtyDays: list.reduce((sum, t) => sum + t.trend.reduce((inner, p) => inner + p.count, 0), 0)
    };
  });
  readonly sparklines = computed(() => {
    const result = new Map<string, string>();
    for (const teacher of this.teachers()) {
      result.set(teacher.teacherId, this.buildSparkline(buildTrend(teacher.trend)));
    }
    return result;
  });
  readonly categoryRows = computed(() => {
    const details = this.details();
    if (!details) {
      return [];
    }
    const rows = [...details.byCategory].sort((a, b) => b.count - a.count);
    return this.toBarRows(rows.map(row => ({ label: row.category, count: row.count, tone: 'series' })));
  });
  readonly severityRows = computed(() => {
    const details = this.details();
    if (!details) {
      return [];
    }
    const rank = (severity: string) => {
      const index = SEVERITY_ORDER.indexOf(severity);
      return index === -1 ? SEVERITY_ORDER.length : index;
    };
    const rows = [...details.bySeverity].sort((a, b) => rank(a.severity) - rank(b.severity));
    return this.toBarRows(rows.map(row => ({ label: row.severity, count: row.count, tone: `severity-${row.severity.toLowerCase()}` })));
  });
  readonly trendDays = computed(() => buildTrend(this.details()?.trend ?? []));
  readonly trendMax = computed(() => Math.max(1, ...this.trendDays().map(day => day.count)));
  readonly trendBars = computed<TrendBar[]>(() => {
    const days = this.trendDays();
    const max = this.trendMax();
    const slot = CHART_WIDTH / days.length;
    return days.map((day, index) => {
      const height = day.count === 0 ? 0 : Math.max(3, (day.count / max) * (CHART_HEIGHT - 8));
      return { day, x: index * slot + BAR_GAP / 2, y: CHART_HEIGHT - height, width: slot - BAR_GAP, height };
    });
  });
  readonly hoveredBar = computed(() => {
    const index = this.hoveredDay();
    return index === null ? null : this.trendBars()[index] ?? null;
  });
  readonly visibleConcerns = computed(() => {
    const concerns = [...(this.details()?.concerns ?? [])].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    return this.openOnly() ? concerns.filter(concern => concern.status !== 'Reviewed') : concerns;
  });

  constructor() {
    this.analytics.getTeachers().subscribe({
      next: list => {
        this.teachers.set(list);
        this.loading.set(false);
        const first = [...list].sort((a, b) => b.open - a.open || b.total - a.total)[0];
        if (first) {
          this.select(first.teacherId);
        }
      },
      error: error => {
        this.loading.set(false);
        this.loadError.set(describeApiError(error, 'Could not load analytics.'));
      }
    });
  }

  select(teacherId: string): void {
    if (this.selectedId() === teacherId && this.details()) {
      return;
    }
    this.selectedId.set(teacherId);
    this.details.set(null);
    this.summary.set(null);
    this.summaryError.set(null);
    this.hoveredDay.set(null);
    this.detailsLoading.set(true);
    this.analytics.getTeacher(teacherId).subscribe({
      next: details => {
        if (this.selectedId() === teacherId) {
          this.details.set(details);
          this.detailsLoading.set(false);
        }
      },
      error: () => {
        if (this.selectedId() === teacherId) {
          this.detailsLoading.set(false);
        }
      }
    });
  }

  generateSummary(): void {
    const teacherId = this.selectedId();
    if (!teacherId || this.summaryLoading()) {
      return;
    }
    this.summaryLoading.set(true);
    this.summaryError.set(null);
    this.analytics.generateSummary(teacherId).subscribe({
      next: summary => {
        if (this.selectedId() === teacherId) {
          this.summary.set(summary);
        }
        this.summaryLoading.set(false);
      },
      error: error => {
        this.summaryLoading.set(false);
        this.summaryError.set(describeApiError(error, 'The AI summary is not available right now.'));
      }
    });
  }

  markReviewed(concern: Concern): void {
    if (this.reviewingIds().has(concern.id)) {
      return;
    }
    this.reviewingIds.update(ids => new Set(ids).add(concern.id));
    this.analytics.markReviewed(concern.id).subscribe({
      next: () => {
        this.finishReviewing(concern.id);
        this.details.update(details =>
          details
            ? {
                ...details,
                open: Math.max(0, details.open - 1),
                concerns: details.concerns.map(item => (item.id === concern.id ? { ...item, status: 'Reviewed' } : item))
              }
            : details
        );
        this.teachers.update(list =>
          list.map(teacher =>
            teacher.teacherId === concern.teacherId ? { ...teacher, open: Math.max(0, teacher.open - 1) } : teacher
          )
        );
      },
      error: () => this.finishReviewing(concern.id)
    });
  }

  private finishReviewing(id: string): void {
    this.reviewingIds.update(ids => {
      const next = new Set(ids);
      next.delete(id);
      return next;
    });
  }

  private toBarRows(rows: { label: string; count: number; tone: string }[]): BarRow[] {
    const max = Math.max(1, ...rows.map(row => row.count));
    return rows.map(row => ({ ...row, percent: (row.count / max) * 100 }));
  }

  private buildSparkline(days: TrendDay[]): string {
    const max = Math.max(1, ...days.map(day => day.count));
    const step = SPARK_WIDTH / (days.length - 1);
    return days
      .map((day, index) => `${(index * step).toFixed(1)},${(SPARK_HEIGHT - 2 - (day.count / max) * (SPARK_HEIGHT - 4)).toFixed(1)}`)
      .join(' ');
  }
}
