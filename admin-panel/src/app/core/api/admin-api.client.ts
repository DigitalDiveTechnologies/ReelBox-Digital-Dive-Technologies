import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { publicRuntimeConfig } from '../config/public-runtime.config';

export type QueryParams = Record<
  string,
  string | number | boolean | null | undefined
>;

@Injectable({ providedIn: 'root' })
export class AdminApiClient {
  private readonly http = inject(HttpClient);

  get<T>(path: string, query?: QueryParams): Observable<T> {
    return this.request<T>('GET', path, undefined, query);
  }

  post<T>(path: string, body?: unknown): Observable<T> {
    return this.request<T>('POST', path, body);
  }

  patch<T>(path: string, body?: unknown): Observable<T> {
    return this.request<T>('PATCH', path, body);
  }

  put<T>(path: string, body?: unknown): Observable<T> {
    return this.request<T>('PUT', path, body);
  }

  delete<T>(path: string): Observable<T> {
    return this.request<T>('DELETE', path);
  }

  /** Binary/text download (e.g. CSV export). */
  getBlob(path: string, query?: QueryParams): Observable<Blob> {
    if (!this.canCallApi()) {
      return throwError(
        () =>
          new Error(
            'Admin API base URL is not configured. Set publicRuntimeConfig.apiBaseUrl.',
          ),
      );
    }
    return this.http
      .request('GET', this.url(path), {
        params: this.toParams(query),
        responseType: 'blob',
      })
      .pipe(catchError((error: unknown) => throwError(() => this.toError(error))));
  }

  canCallApi(): boolean {
    return publicRuntimeConfig.apiBaseUrl.trim().length > 0;
  }

  private request<T>(
    method: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE',
    path: string,
    body?: unknown,
    query?: QueryParams,
  ): Observable<T> {
    if (!this.canCallApi()) {
      return throwError(
        () =>
          new Error(
            'Admin API base URL is not configured. Set publicRuntimeConfig.apiBaseUrl.',
          ),
      );
    }

    return this.http
      .request<T>(method, this.url(path), {
        body,
        params: this.toParams(query),
      })
      .pipe(catchError((error: unknown) => throwError(() => this.toError(error))));
  }

  private url(path: string): string {
    const base = publicRuntimeConfig.apiBaseUrl.replace(/\/$/, '');
    return `${base}${path.startsWith('/') ? path : `/${path}`}`;
  }

  private toParams(query?: QueryParams): HttpParams | undefined {
    if (!query) return undefined;
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value === null || value === undefined || value === '') continue;
      params = params.set(key, String(value));
    }
    return params;
  }

  private toError(error: unknown): Error {
    if (error instanceof Error && !(error instanceof HttpErrorResponse)) {
      return error;
    }
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return new Error('Cannot reach Admin API. Check apiBaseUrl and CORS.');
      }
      if (error.status === 401) {
        return new Error('Session expired. Sign in again.');
      }
      if (error.status === 403) {
        return new Error('You do not have permission for this action.');
      }
      const problem = error.error as
        | { title?: string; detail?: string; message?: string }
        | string
        | null;
      if (typeof problem === 'string' && problem.trim()) {
        return new Error(problem);
      }
      if (problem && typeof problem === 'object') {
        return new Error(
          problem.detail ?? problem.title ?? problem.message ?? error.message,
        );
      }
      return new Error(error.message || 'Request failed.');
    }
    return new Error('Request failed.');
  }
}
