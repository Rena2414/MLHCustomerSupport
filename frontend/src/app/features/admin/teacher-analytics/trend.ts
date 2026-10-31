import { TrendPoint } from '../../../core/api/api.models';

export const TREND_DAYS = 30;

export interface TrendDay {
  date: Date;
  key: string;
  count: number;
}

export function toDayKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function buildTrend(points: TrendPoint[], today = new Date()): TrendDay[] {
  const counts = new Map<string, number>();
  for (const point of points) {
    const key = point.date.slice(0, 10);
    counts.set(key, (counts.get(key) ?? 0) + point.count);
  }
  const days: TrendDay[] = [];
  for (let offset = TREND_DAYS - 1; offset >= 0; offset--) {
    const date = new Date(today.getFullYear(), today.getMonth(), today.getDate() - offset);
    const key = toDayKey(date);
    days.push({ date, key, count: counts.get(key) ?? 0 });
  }
  return days;
}
