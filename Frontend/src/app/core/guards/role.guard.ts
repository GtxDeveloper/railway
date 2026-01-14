import { inject } from '@angular/core';
import {
  Router,
  CanActivateFn,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree
} from '@angular/router';
import { AuthStore } from '../stores/auth-store'; // Проверь путь

/**
 * Универсальная логика проверки
 */
const checkAccess = (route: ActivatedRouteSnapshot): boolean | UrlTree => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  // 1. Проверяем аутентификацию (через Computed Signal)
  if (!auth.isAuthenticated()) {
    // Если не вошел — на логин
    return router.createUrlTree(['/login']);
  }

  // 2. Получаем требуемые роли из роутинга
  // Пример в роутах: data: { roles: ['Admin'] }
  const requiredRoles = route.data['roles'] as string[] | undefined;

  // Если роли не заданы — пускаем любого авторизованного
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  // 3. Получаем текущего юзера из Сигнала
  const user = auth.currentUser();

  console.log(user)

  // Внимание: В нашей модели user.role - это строка (например "Admin").
  // Проверяем, есть ли эта строка в списке разрешенных.
  if (user && user.role && requiredRoles.includes(user.role)) {
    return true;
  }

  // 4. Если роль не подошла — редирект (например, на главную dashboard)
  // Можно сделать отдельную страницу /forbidden, но часто просто кидают домой
  return router.createUrlTree(['/login']);
};

// --- EXPORTS ---

export const authGuard: CanActivateFn = (route, state) => {
  return checkAccess(route);
};

// Можно использовать ту же логику для детей
export const authChildGuard: CanActivateFn = (route, state) => {
  return checkAccess(route);
};
