import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import {BehaviorSubject, catchError, filter, Observable, switchMap, take, tap, throwError} from 'rxjs';
import { AuthStore } from '../stores/auth-store';

// Глобальные переменные для состояния рефреша
let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn) => {

  // 1. Игнорируем Google callback (как у вас было)
  if (req.url.includes('/oauth2/callback/google')) {
    return next(req);
  }

  const authStore = inject(AuthStore);

  const token = authStore.accessToken;

  // 2. Добавляем токен, если он есть
  let authReq = req;
  if (token) {
    authReq = addToken(req, token);
  }
  return next(authReq).pipe(
    catchError((error) => {
      // 3. Ловим 401 (Unauthorized)
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        // ВАЖНО: Не перехватываем ошибки самого логина или рефреша, чтобы не было вечного цикла
        !req.url.includes('Auth/login') &&
        !req.url.includes('Auth/refresh') &&
        !req.url.includes('Auth/logout') &&
        !req.url.includes('Auth/register') &&
        !req.url.includes('Auth/verify-email')
      ) {
        return handle401Error(authReq, next, authStore);
      }

      return throwError(() => error);
    })
  );
};

// Логика обработки 401
const handle401Error = (
  request: HttpRequest<any>,
  next: HttpHandlerFn,
  store: AuthStore
): Observable<any> => {

  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return store.refresh().pipe(
      switchMap((tokens) => {
        isRefreshing = false;
        refreshTokenSubject.next(tokens.token);

        // Повторяем упавший запрос с НОВЫМ токеном
        return next(addToken(request, tokens.token));
      }),
      catchError((err) => {
        isRefreshing = false;
        // store.logout() у тебя уже есть внутри refresh(), дублировать не надо
        return throwError(() => err);
      })
    );
  }

  // Если рефреш уже идет — ждем
  return refreshTokenSubject.pipe(
    filter(token => token !== null),
    take(1),
    switchMap(token => next(addToken(request, token!)))
  );
};

// Хелпер для клонирования запроса
const addToken = (request: HttpRequest<any>, token: string) => {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
};
