import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { describeApiError } from '../../../core/api/api-error';
import { ClassInfo } from '../../../core/api/api.models';
import { ClassesService } from '../../../core/api/classes.service';
import { SubscriptionService } from '../../../core/api/subscription.service';
import { AuthService } from '../../../core/auth/auth.service';
import { SubscriptionPipe } from '../../shared/label.pipes';

@Component({
  selector: 'app-student-home',
  imports: [RouterLink, SubscriptionPipe],
  templateUrl: './student-home.component.html',
  styleUrl: './student-home.component.css'
})
export class StudentHomeComponent {
  private readonly auth = inject(AuthService);
  private readonly classesService = inject(ClassesService);
  private readonly subscriptions = inject(SubscriptionService);

  readonly user = this.auth.user;
  readonly classes = signal<ClassInfo[]>([]);
  readonly loading = signal(true);
  readonly subscribing = signal(false);
  readonly subscribeError = signal<string | null>(null);
  readonly teachers = computed(() => {
    const byId = new Map<string, { id: string; name: string; subjects: string[] }>();
    for (const schoolClass of this.classes()) {
      const teacher = byId.get(schoolClass.teacherId) ?? { id: schoolClass.teacherId, name: schoolClass.teacherName, subjects: [] };
      if (!teacher.subjects.includes(schoolClass.subject)) {
        teacher.subjects.push(schoolClass.subject);
      }
      byId.set(schoolClass.teacherId, teacher);
    }
    return [...byId.values()];
  });

  constructor() {
    this.auth.refreshProfile().subscribe({ error: () => undefined });
    this.classesService.getMine().subscribe({
      next: list => {
        this.classes.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  subscribe(): void {
    if (this.subscribing()) {
      return;
    }
    this.subscribing.set(true);
    this.subscribeError.set(null);
    this.subscriptions.subscribeSelf().subscribe({
      next: () => {
        this.auth.refreshProfile().subscribe({
          next: () => this.subscribing.set(false),
          error: () => this.subscribing.set(false)
        });
      },
      error: error => {
        this.subscribing.set(false);
        this.subscribeError.set(describeApiError(error, 'Could not start the subscription.'));
      }
    });
  }
}
