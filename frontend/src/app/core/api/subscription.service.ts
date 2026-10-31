import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/me/subscription`;

  subscribeSelf(): Observable<unknown> {
    return this.http.post(this.url, {});
  }

  subscribeChild(childId: string): Observable<unknown> {
    return this.http.post(this.url, { childId });
  }
}
