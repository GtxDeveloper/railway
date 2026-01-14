import { Component, ElementRef, inject, OnInit, QueryList, signal, ViewChildren } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormControl, FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UiInputComponent } from '../../../shared/components/ui-input-component/ui-input-component'; // Путь может отличаться
import { AuthStore } from '../../../core/stores/auth-store';
import { RegisterRequest, VerifyRequest } from '../../../core/models/auth.models';
import { passwordMatchValidator } from '../../../core/helpers/password-match-validator';
import {TranslatePipe} from "@ngx-translate/core";

@Component({
  selector: 'app-invite-page',
  standalone: true,
    imports: [CommonModule, ReactiveFormsModule, RouterLink, UiInputComponent, FormsModule, TranslatePipe],
  templateUrl: './invite-page.html'
})
export class InvitePage implements OnInit {
  private fb = inject(FormBuilder);
  private authStore = inject(AuthStore);
  private route = inject(ActivatedRoute);
  private readonly STORAGE_KEY = 'pending_registration_email';
  // Signals
  isLoading = signal(false);
  isSuccess = signal(false);
  submittedEmail = signal('');
  inviteToken = signal<string | null>(null);

  // Упрощенная форма (без brand, phone, city)
  form = this.fb.group({
    name: ['', [Validators.required]],
    surname: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    repeatPassword: ['', [Validators.required, Validators.minLength(6)]],
  }, { validators: passwordMatchValidator });

  ngOnInit() {
    // 1. Достаем токен из URL (/invite/:token)
    const token = this.route.snapshot.paramMap.get('token');
    if (token) {
      this.inviteToken.set(token);
    } else {
      // Обработка ошибки, если токена нет
      console.error('Invite token is missing');
    }
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

    if (!this.inviteToken()) {
      alert('Chýba token pozvánky');
      return;
    }

    this.isLoading.set(true);
    const formValue = this.form.value;

    // 2. Формируем Payload специально для воркера
    const request: RegisterRequest = {
      name: formValue.name!,
      surname: formValue.surname!,
      email: formValue.email!,
      password: formValue.password!,
      // Важно: Brand null, так как это работник
      brand: '',
      // Добавляем токен
      inviteToken: this.inviteToken()!,
      phone: '',
      city: ''
    };



    this.authStore.register(request).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.submittedEmail.set(request.email);

        sessionStorage.setItem(this.STORAGE_KEY, request.email);

        this.isSuccess.set(true);
      },
      error: (err) => {
        this.isLoading.set(false);
        console.error(err);
        alert('Chyba pri registrácii.');
      }
    });
  }

  // --- ЛОГИКА OTP (Остается такой же) ---
  digits: string[] = ['', '', '', '', '', ''];
  @ViewChildren('otpInput') inputs!: QueryList<ElementRef>;

  onInput(index: number, event: any) {
    const value = event.target.value;
    this.digits[index] = value.replace(/[^0-9]/g, '');
    if (this.digits[index].length === 1) {
      if (index < 5) this.inputs.toArray()[index + 1].nativeElement.focus();
      else {
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
    pastedData.forEach((val, i) => this.digits[i] = val);
    const focusIndex = Math.min(pastedData.length, 5);
    this.inputs.toArray()[focusIndex].nativeElement.focus();
    if (pastedData.length === 6) this.verifyCode();
  }

  verifyCode() {
    const code = this.digits.join('');
    const request: VerifyRequest = {
      email: this.submittedEmail(),
      code: code,
    };
    this.authStore.verify(request).subscribe({
      next: () => {
        sessionStorage.removeItem(this.STORAGE_KEY);
      }, // Успех -> редирект происходит внутри стора или тут можно сделать router.navigate(['/personal'])
      error: (err) => {
        console.error(err);
        alert('Nesprávny kód');
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
