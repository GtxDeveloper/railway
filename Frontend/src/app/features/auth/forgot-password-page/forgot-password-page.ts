import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import {UiInputComponent} from '../../../shared/components/ui-input-component/ui-input-component';
import {AuthStore} from '../../../core/stores/auth-store';




@Component({
  selector: 'app-forgot-password-page',
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
            {{ 'FORGOT_PASSWORD.TITLE' | translate }}
          </h2>
          <p class="text-white text-sm">
            {{ 'FORGOT_PASSWORD.SUBTITLE' | translate }}
          </p>
        </div>

        @if (isSuccess()) {
          <div class="bg-green-50 text-green-700 p-4 rounded-2xl text-center mb-6 border border-green-100">
            {{ 'FORGOT_PASSWORD.SUCCESS_MESSAGE' | translate }}
          </div>
          <button
            routerLink="/login"
            class="mx-auto w-full bg-[#ffc800] raleway font-bold text-2xl rounded-full px-15 py-4 hover:bg-white hover:text-[#ffc800] transition ease-in disabled:bg-gray-500 disabled:hover:text-black">
            {{ 'FORGOT_PASSWORD.BACK_TO_LOGIN' | translate }}
          </button>
        } @else {
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-5">

            <app-ui-input
              type="email"
              [control]="getControl('email')"
              [placeholder]="'LOGIN.EMAIL' | translate"
            ></app-ui-input>

            <button
              type="submit"
              [disabled]="form.invalid || isLoading()"
              class="mx-auto w-full bg-[#ffc800] raleway font-bold text-2xl rounded-full px-15 py-4 hover:bg-white hover:text-[#ffc800] transition ease-in disabled:bg-gray-500 disabled:hover:text-black mt-4"
            >
              @if (isLoading()) {
                <svg class="animate-spin mx-auto h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              } @else {
                {{ 'FORGOT_PASSWORD.SUBMIT_BUTTON' | translate }}
              }
            </button>

            <div class="text-center mt-2">
              <a routerLink="/login" class="text-sm font-semibold text-[#ffc800] hover:text-white hover:underline transition-colors">
                {{ 'FORGOT_PASSWORD.BACK_TO_LOGIN' | translate }}
              </a>
            </div>
          </form>
        }
      </div>
    </div>
  `
})
export class ForgotPasswordPageComponent {
  private authStore = inject(AuthStore);

  isLoading = signal(false);
  isSuccess = signal(false);

  form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email])
  });

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.isLoading.set(true);
    const { email } = this.form.getRawValue();

    this.authStore.forgotPassword({ email: email! }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.isSuccess.set(true);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
        // Тут можно добавить вывод тостера с ошибкой
      }
    });
  }
}
