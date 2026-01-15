import { Routes } from '@angular/router';
import {AuthLayoutComponent} from './layouts/auth-layout/auth-layout';
import {RegisterPage} from './features/auth/register-page/register-page';
import {ChoosePage} from './features/auth/choose-page/choose-page';
import {LoginPage} from './features/auth/login-page/login-page';
import {MainLayout} from './layouts/main-layout/main-layout';
import {MainPage} from './features/dashboard/main-page/main-page';
import {authGuard} from './core/guards/role.guard';
import {PaymentPage} from './features/public-payment/payment-page/payment-page';
import {OnboardingSuccessComponent} from './features/dashboard/onboarding/onboarding-success-page';
import {PaymentSuccessComponent} from './features/public-payment/payment-status/payment-success-page';
import {PaymentCancelComponent} from './features/public-payment/payment-status/payment-cancel-page';
import {BusinessDashboardComponent} from './features/dashboard/bussiness-page/bussiness-page';
import {InvitePage} from './features/auth/invite-page/invite-page';
import {AdminPageComponent} from './features/admin/admin-page';
import {guestGuard} from './core/guards/guest.guard';
import {NotFoundPage} from './features/not-found/not-found-page';
import {ForgotPasswordPageComponent} from './features/auth/forgot-password-page/forgot-password-page';
import {ResetPasswordPageComponent} from './features/auth/reset-password-page/reset-password-page';


export const routes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      {
        path: '',
        redirectTo: '/dashboard',
        pathMatch: 'full'
      },
      {
        path: 'register',
        component: RegisterPage,
        canActivate: [guestGuard]
      },
      {
        path: 'invite/:token',
        component: InvitePage,
        canActivate: [guestGuard]
      },
      {
        path: 'login',
        component: LoginPage,
        canActivate: [guestGuard]
      },
      {
        path: 'choose',
        component: ChoosePage,
        canActivate: [guestGuard]
      },
      {
        path: 'forgot-password',
        component: ForgotPasswordPageComponent,
        canActivate: [guestGuard]
      },
      {
        path: 'reset-password',
        component: ResetPasswordPageComponent,
        canActivate: [guestGuard]
      },
      {
        path: 'pay/:id',
        component: PaymentPage
      },

    ]
  },
  {
    path: 'dashboard',
    component: MainLayout,

    // 1. Подключаем гард
    canActivate: [authGuard],

    // 2. Указываем требуемую роль (например, 'User' или 'owner' — как у вас в БД)
    data: { roles: ['User', 'Admin'] },

    children: [
      {
        path: '',
        component: MainPage,
      },
      {
        path: 'business',
        component: BusinessDashboardComponent,
      },
      {
        path: 'admin',
        component: AdminPageComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] },
      }
    ]
  },
  {
    path: 'onboarding',
    component: MainLayout,
    children: [
      {
        path: 'success',
        component: OnboardingSuccessComponent
      }
    ]
  },
  {
    path: 'payment',
    component: MainLayout,
    children: [
      {
        path: 'success',
        component: PaymentSuccessComponent
      },
      {
        path: 'cancel',
        component: PaymentCancelComponent
      },
    ]
  },
  {
    path: '**',
    component: NotFoundPage
  }
];
