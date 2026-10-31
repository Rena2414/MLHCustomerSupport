import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClassInfo, TeacherClass } from './api.models';

@Injectable({ providedIn: 'root' })
export class ClassesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/classes`;

  getAll(): Observable<TeacherClass[]> {
    return this.http.get<TeacherClass[]>(this.baseUrl);
  }

  create(name: string, subject: string): Observable<TeacherClass> {
    return this.http.post<TeacherClass>(this.baseUrl, { name, subject });
  }

  addStudent(classId: string, login: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/${classId}/students`, { studentLogin: login });
  }

  removeStudent(classId: string, studentId: string): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${classId}/students/${studentId}`);
  }

  getMine(): Observable<ClassInfo[]> {
    return this.http.get<ClassInfo[]>(`${environment.apiUrl}/me/classes`);
  }
}
