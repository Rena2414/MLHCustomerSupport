import { Component, inject, signal } from '@angular/core';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeApiError } from '../../../core/api/api-error';
import { ClassStudent, TeacherClass } from '../../../core/api/api.models';
import { ClassesService } from '../../../core/api/classes.service';

@Component({
  selector: 'app-classes',
  imports: [FormsModule, ReactiveFormsModule],
  templateUrl: './classes.component.html',
  styleUrl: './classes.component.css'
})
export class ClassesComponent {
  private readonly classesService = inject(ClassesService);

  readonly classes = signal<TeacherClass[]>([]);
  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);
  readonly studentLogins = signal<Partial<Record<string, string>>>({});
  readonly classErrors = signal<Partial<Record<string, string>>>({});
  readonly busyClassIds = signal<ReadonlySet<string>>(new Set());
  readonly createForm = inject(NonNullableFormBuilder).group({
    name: ['', Validators.required],
    subject: ['', Validators.required]
  });

  constructor() {
    this.reload();
  }

  createClass(): void {
    if (this.createForm.invalid || this.creating()) {
      this.createForm.markAllAsTouched();
      return;
    }
    const { name, subject } = this.createForm.getRawValue();
    this.creating.set(true);
    this.createError.set(null);
    this.classesService.create(name.trim(), subject.trim()).subscribe({
      next: () => {
        this.creating.set(false);
        this.createForm.reset();
        this.reload();
      },
      error: error => {
        this.creating.set(false);
        this.createError.set(describeApiError(error, 'Could not create the class.'));
      }
    });
  }

  setLogin(classId: string, login: string): void {
    this.studentLogins.update(logins => ({ ...logins, [classId]: login }));
  }

  addStudent(schoolClass: TeacherClass): void {
    const login = (this.studentLogins()[schoolClass.id] ?? '').trim();
    if (!login || this.busyClassIds().has(schoolClass.id)) {
      return;
    }
    this.runForClass(schoolClass.id, this.classesService.addStudent(schoolClass.id, login), 'No student with this login.', () =>
      this.setLogin(schoolClass.id, '')
    );
  }

  removeStudent(schoolClass: TeacherClass, student: ClassStudent): void {
    if (this.busyClassIds().has(schoolClass.id)) {
      return;
    }
    this.runForClass(
      schoolClass.id,
      this.classesService.removeStudent(schoolClass.id, student.id),
      'Could not remove the student.'
    );
  }

  private runForClass(
    classId: string,
    request: ReturnType<ClassesService['addStudent']>,
    fallbackError: string,
    onSuccess?: () => void
  ): void {
    this.setBusy(classId, true);
    this.setClassError(classId, null);
    request.subscribe({
      next: () => {
        this.setBusy(classId, false);
        onSuccess?.();
        this.reload();
      },
      error: error => {
        this.setBusy(classId, false);
        this.setClassError(classId, describeApiError(error, fallbackError));
      }
    });
  }

  private reload(): void {
    this.classesService.getAll().subscribe({
      next: list => {
        this.classes.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private setBusy(classId: string, busy: boolean): void {
    this.busyClassIds.update(ids => {
      const next = new Set(ids);
      if (busy) {
        next.add(classId);
      } else {
        next.delete(classId);
      }
      return next;
    });
  }

  private setClassError(classId: string, message: string | null): void {
    this.classErrors.update(errors => {
      const next = { ...errors };
      if (message) {
        next[classId] = message;
      } else {
        delete next[classId];
      }
      return next;
    });
  }
}
