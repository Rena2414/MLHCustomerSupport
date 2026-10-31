import { HttpErrorResponse } from '@angular/common/http';

export function describeApiError(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;
    if (typeof body === 'string' && body.trim()) {
      return body;
    }
    if (body && typeof body === 'object') {
      const candidate = body.message ?? body.detail ?? body.title;
      if (typeof candidate === 'string' && candidate.trim()) {
        return candidate;
      }
    }
    if (error.status === 0) {
      return 'The server is not reachable. Please try again later.';
    }
  }
  return fallback;
}
