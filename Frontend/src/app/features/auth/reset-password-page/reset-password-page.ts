import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import {UiInputComponent} from '../../../shared/components/ui-input-component/ui-input-component';
import {AuthStore} from '../../../core/stores/auth-store';


@Component({
  selector: 'app-reset-password-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    UiInputComponent
  ],
  template: `
    <div class="flex items-center raleway justify-center bg-[#27a19b] px-4">
      <div class="max-w-md w-full bg-[#53b5b0] rounded-[2rem] shadow-sm border border-white p-8">

        <div class="text-center mb-8">
          <h2 class="text-2xl font-bold text-white mb-2">
            {{ 'RESET_PASSWORD.TITLE' | translate }}
          </h2>
          <p class="text-white text-sm">
            {{ 'RESET_PASSWORD.SUBTITLE' | translate }}
          </p>
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-5">

          <app-ui-input
            type="password"
            [control]="getControl('newPassword')"
            [placeholder]="'RESET_PASSWORD.NEW_PASSWORD_PLACEHOLDER' | translate"
          ></app-ui-input>

          @if (!isValidLink) {
            <div class="text-red-500 text-sm text-center">
              {{ 'RESET_PASSWORD.INVALID_LINK' | translate }}
            </div>
          }

          <button
            type="submit"
            [disabled]="form.invalid || isLoading() || !isValidLink"
            class="mx-auto w-full bg-[#ffc800] raleway font-bold text-2xl rounded-full px-15 py-4 hover:bg-white hover:text-[#ffc800] transition ease-in disabled:bg-gray-500 disabled:hover:text-black"
          >
            @if (isLoading()) {
              <svg class="animate-spin h-5 w-5 text-black" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            } @else {
              {{ 'RESET_PASSWORD.SUBMIT_BUTTON' | translate }}
            }
          </button>
        </form>

      </div>
    </div>
  `
})
export class ResetPasswordPageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private authStore = inject(AuthStore);

  isLoading = signal(false);
  isValidLink = true; // Флаг, есть ли данные в URL

  // Данные из URL
  private token: string = '';
  private email: string = '';

  form = new FormGroup({
    newPassword: new FormControl('', [Validators.required, Validators.minLength(8)])
  });

  ngOnInit() {
    // Получаем query params из URL
    this.route.queryParams.subscribe(params => {
      this.token = params['token'];
      this.email = params['email'];

      // Простая проверка, что параметры есть
      if (!this.token || !this.email) {
        this.isValidLink = false;
        console.error('Missing token or email in URL');
      }
    });
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  onSubmit() {
    if (this.form.invalid || !this.isValidLink) return;

    this.isLoading.set(true);
    const { newPassword } = this.form.getRawValue();

    // Вызываем метод стора
    this.authStore.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: newPassword!
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        // Стор сам делает редирект на /login, но можно добавить тостер "Успешно"
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
        // Обработка ошибок (например, токен истек)
      }
    });
  }
}
