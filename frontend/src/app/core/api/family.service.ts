import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChildDto } from './api.dtos';
import { ChildInfo } from './api.models';

@Injectable({ providedIn: 'root' })
export class FamilyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/me/children`;

  getChildren(): Observable<ChildInfo[]> {
    return this.http
      .get<ChildDto[]>(this.baseUrl)
      .pipe(map(children => children.map(({ child, classes }) => ({ ...child, classes }))));
  }

  linkChild(login: string): Observable<unknown> {
    return this.http.post(this.baseUrl, { childLogin: login });
  }

  unlinkChild(childId: string): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${childId}`);
  }
}
