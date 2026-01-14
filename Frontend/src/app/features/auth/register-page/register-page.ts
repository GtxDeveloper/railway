import {Component, ElementRef, inject, OnInit, QueryList, signal, ViewChildren} from '@angular/core';
import { CommonModule } from '@angular/common';
import {FormBuilder, ReactiveFormsModule, Validators, FormControl, FormsModule} from '@angular/forms';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {UiInputComponent} from '../../../shared/components/ui-input-component/ui-input-component';
import {AuthStore} from '../../../core/stores/auth-store';
import {RegisterRequest, VerifyRequest} from '../../../core/models/auth.models';
import {passwordMatchValidator} from '../../../core/helpers/password-match-validator';
import {TranslatePipe} from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http'; // Не забудь импортировать

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, UiInputComponent, FormsModule, TranslatePipe],
  templateUrl: './register-page.html'
})
export class RegisterPage implements OnInit {
  private fb = inject(FormBuilder);
  private authStore = inject(AuthStore);
  private route = inject(ActivatedRoute);
  private readonly STORAGE_KEY = 'pending_registration_email';
  // Signals для состояния UI
  isLoading = signal(false);
  isSuccess = signal(false);
  submittedEmail = signal('');
  error = signal<string | null>(null); // Сигнал ошибки
  role = signal<'owner' | 'worker'>('owner');

  form = this.fb.group({
    name: ['', [Validators.required]],
    surname: ['', [Validators.required]],
    brand: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    repeatPassword: ['', [Validators.required, Validators.minLength(6)]],
    phone: [''],
    city: ['']
  },{ validators: passwordMatchValidator });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const roleParam = params['role'];
      if (roleParam === 'worker') {
        this.role.set('worker');
      } else {
        this.role.set('owner');
      }
    });
    const savedEmail = sessionStorage.getItem(this.STORAGE_KEY);
    if (savedEmail) {
      this.submittedEmail.set(savedEmail);
      this.isSuccess.set(true); // Сразу переключаем на экран OTP
    }
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.error.set(null); // Сброс ошибки перед запросом

    const request = this.form.value as RegisterRequest;


    this.authStore.register(request).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.submittedEmail.set(request.email);

        sessionStorage.setItem(this.STORAGE_KEY, request.email);

        this.isSuccess.set(true);
        this.error.set(null); // Очищаем ошибки при переходе на экран OTP
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        console.error(err);

        // Обработка ошибок регистрации
        if (err.status === 409) {
          this.error.set('REGISTER.ERROR_EMAIL_EXISTS');
        } else {
          this.error.set('REGISTER.ERROR_GENERIC');
        }
      }
    });
  }

  // --- ЛОГИКА OTP ---

  digits: string[] = ['', '', '', '', '', ''];
  @ViewChildren('otpInput') inputs!: QueryList<ElementRef>;

  onInput(index: number, event: any) {
    const value = event.target.value;
    this.digits[index] = value.replace(/[^0-9]/g, '');

    if (this.digits[index].length === 1) {
      if (index < 5) {
        this.inputs.toArray()[index + 1].nativeElement.focus();
      } else {
        this.inputs.toArray()[index].nativeElement.blur();
        this.verifyCode();
      }
    }
  }

  onKeyDown(index: number, event: KeyboardEvent) {
    if (event.key === 'Backspace' && !this.digits[index] && index > 0) {
      this.inputs.toArray()[index - 1].nativeElement.focus();
    }
  }

  onPaste(event: ClipboardEvent) {
    event.preventDefault();
    const clipboardData = event.clipboardData?.getData('text') || '';
    const pastedData = clipboardData.replace(/\D/g, '').slice(0, 6).split('');

    pastedData.forEach((val, i) => {
      this.digits[i] = val;
    });

    const focusIndex = Math.min(pastedData.length, 5);
    this.inputs.toArray()[focusIndex].nativeElement.focus();

    if (pastedData.length === 6) {
      this.verifyCode();
    }
  }

  verifyCode() {
    const code = this.digits.join('');
    // Сбрасываем ошибку перед проверкой, но можно оставить, если хочешь, чтобы старая висела пока не введут верно
    this.error.set(null);

    const request : VerifyRequest = {
      email: this.submittedEmail(),
      code: code,
    }

    this.authStore.verify(request).subscribe({
      next: () => {
        sessionStorage.removeItem(this.STORAGE_KEY);
        // Успешный вход/верификация, стор сам сделает редирект
      },
      error: (err: HttpErrorResponse) => {
        console.error(err);
        // Сбрасываем цифры (опционально) или просто показываем ошибку
        this.error.set('REGISTER.ERROR_INVALID_CODE');
      }
    });
  }

  resetRegistration() {
    sessionStorage.removeItem(this.STORAGE_KEY);
    this.isSuccess.set(false);
    this.submittedEmail.set('');
    this.digits = ['', '', '', '', '', '']; // сброс цифр
  }
}
