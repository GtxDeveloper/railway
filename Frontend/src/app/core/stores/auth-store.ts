import { computed, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, tap, throwError, Observable, finalize } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { AuthService } from '../services/auth-service';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
  VerifyRequest,
  RefreshRequest, ForgotPasswordRequest, ResetPasswordRequest
} from '../models/auth.models';
import { MessageResponse } from '../models/message.model';

const KEYS = {
  ACCESS_TOKEN: 'tringelty_access_token',
  REFRESH_TOKEN: 'tringelty_refresh_token'
};

@Injectable({
  providedIn: 'root'
})
export class AuthStore implements OnDestroy {
  private api = inject(AuthService);
  private router = inject(Router);

  // --- STATE (SIGNALS) ---
  // Храним весь объект User, а не только роли
  private _user = signal<User | null>(null);

  // Computed Signals: автоматически обновляются при изменении _user
  public currentUser = this._user.asReadonly();
  public isAuthenticated = computed(() => !!this._user());
  public isAdmin = computed(() => this._user()?.role === 'Admin');

  // Listener для синхронизации вкладок
  private readonly storageEventListener = (event: StorageEvent) => {
    if (event.key === KEYS.ACCESS_TOKEN && event.newValue === null) {
      console.warn('AuthStore: Выход в другой вкладке detected.');
      this.clearSession(false); // false = без редиректа, мы уже может быть где угодно
    }
  };

  constructor() {
    // 1. При старте приложения восстанавливаем юзера из LocalStorage
    this.loadUserFromStorage();

    // 2. Слушаем события LocalStorage (синхронизация вкладок)
    window.addEventListener('storage', this.storageEventListener);
  }

  // --- PUBLIC API ---

  // Получить "сырой" токен (нужен для интерцептора)
  get accessToken(): string | null {
    return localStorage.getItem(KEYS.ACCESS_TOKEN);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(KEYS.REFRESH_TOKEN);
  }

  // 1. Регистрация (просто пробрасываем, стейт не меняем, так как нужно подтверждение)
  register(request: RegisterRequest): Observable<MessageResponse> {
    return this.api.register(request);
  }

  // 2. Подтверждение почты (тут мы получаем токены -> значит логинимся)
  verify(request: VerifyRequest): Observable<AuthResponse> {
    return this.api.verify(request).pipe(
      tap((response) => {
        this.setSession(response)
        this.router.navigate(['/dashboard']);
      }));
  }

  // 3. Логин
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.api.login(request).pipe(
      tap((response) => {
        this.setSession(response);
        this.router.navigate(['/dashboard']); // Редирект в кабинет
      })
    );
  }

  // 4. Рефреш токена
  refresh(): Observable<AuthResponse> {
    console.log("REFRESH")
    const access = this.accessToken;
    const refresh = this.refreshToken;

    if (!access || !refresh) {
      this.logout();
      return throwError(() => new Error('No tokens for refresh'));
    }

    const request: RefreshRequest = { accessToken: access, refreshToken: refresh };

    return this.api.refresh(request).pipe(
      tap((response) => this.setSession(response)),
      catchError((err) => {
        console.error('Refresh failed', err);
        this.logout(); // Если рефреш не прошел — полный выход
        return throwError(() => err);
      })
    );
  }

  // 5. Логаут
  logout(): void {
    // Сначала пытаемся сказать серверу "пока"
    this.api.logout().pipe(
      finalize(() => this.clearSession(true)) // В любом случае чистим стор
    ).subscribe();
  }

  // --- PRIVATE HELPERS ---

  private setSession(authData: AuthResponse): void {
    // 1. Сохраняем в Storage
    localStorage.setItem(KEYS.ACCESS_TOKEN, authData.token); // В AuthResponse поле называется token? Или accessToken? Проверь по модели.
    localStorage.setItem(KEYS.REFRESH_TOKEN, authData.refreshToken);

    // 2. Декодируем и обновляем Signal
    this.decodeAndSetUser(authData.token);
  }

  private clearSession(doRedirect: boolean): void {
    // 1. Чистим Storage
    localStorage.removeItem(KEYS.ACCESS_TOKEN);
    localStorage.removeItem(KEYS.REFRESH_TOKEN);

    // 2. Обнуляем Signal
    this._user.set(null);

    // 3. Редирект
    if (doRedirect) {
      this.router.navigate(['/login']);
    }
  }

  private loadUserFromStorage(): void {
    const token = localStorage.getItem(KEYS.ACCESS_TOKEN);
    if (token) {
      // Опционально: проверка isExpired перед установкой
      this.decodeAndSetUser(token);
    }
  }

  private decodeAndSetUser(token: string): void {
    try {
      const decoded: any = jwtDecode(token);

      // Маппим поля из JWT в нашу модель User
      // Важно: проверь, как поля называются в токене (sub vs nameid, role vs roles)
      const user: User = {
        id: decoded.sub || decoded.nameid,
        email: decoded.email,
        role: decoded.role || (decoded.roles && decoded.roles[0]), // Обработка массива ролей если нужно
        exp: decoded.exp
      };

      this._user.set(user);
    } catch (error) {
      console.error('Invalid token format', error);
      this.clearSession(true);
    }
  }

  // 6. Запрос сброса пароля (Forgot Password)
  // Обычно это просто отправка письма, поэтому состояние пользователя не меняем
  forgotPassword(request: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.api.forgotPassword(request);
  }

  // 7. Установка нового пароля (Reset Password)
  resetPassword(request: ResetPasswordRequest): Observable<MessageResponse> {
    return this.api.resetPassword(request).pipe(
      tap(() => {
        // После успешной смены пароля логично перенаправить пользователя на логин
        this.router.navigate(['/login']);
      })
    );
  }

  ngOnDestroy(): void {
    window.removeEventListener('storage', this.storageEventListener);
  }
}
