import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../stores/auth-store'; // Проверь путь к стору

export const guestGuard: CanActivateFn = (route, state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  // Проверяем, залогинен ли юзер.
  // Если у тебя в сторе есть сигнал isAuthenticated(), используй его.
  // Если нет, можно проверить наличие токена: !!authStore.token()
  if (authStore.isAuthenticated()) {
    // Если залогинен — кидаем на дашборд
    router.navigate(['/dashboard']);
    return false;
  }

  // Если не залогинен — пускаем на страницу входа/регистрации
  return true;
};
