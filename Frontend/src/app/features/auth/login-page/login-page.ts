import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UiInputComponent } from '../../../shared/components/ui-input-component/ui-input-component';
import { AuthStore } from '../../../core/stores/auth-store';
import { RegisterRequest } from '../../../core/models/auth.models';
import { TranslatePipe } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    UiInputComponent,
    TranslatePipe,
  ],
  templateUrl: './login-page.html',
})
export class LoginPage {
  loginForm: FormGroup;

  // Сигналы
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null); // <-- Для текста ошибки

  private authStore = inject(AuthStore);

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  getControl(controlName: string) {
    return this.loginForm.get(controlName) as any;
  }

  onSubmit() {
    if (this.loginForm.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null); // Сбрасываем ошибку перед новым запросом

    const request = this.loginForm.value as RegisterRequest;

    this.authStore.login(request).subscribe({
      next: () => {
        // Успех (обычно стор сам редиректит, но на всякий случай выключаем лоадер)
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        console.error('Login error', err);

        // Обработка кодов ошибок
        if (err.status === 401 || err.status === 403) {
          // Неверный логин/пароль
          this.errorMessage.set('LOGIN.ERROR_CREDENTIALS');
        } else {
          // Другая ошибка (сервер упал и т.д.)
          this.errorMessage.set('LOGIN.ERROR_GENERIC');
        }
      }
    });
  }
}
