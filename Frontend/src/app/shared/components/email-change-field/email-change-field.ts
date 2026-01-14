import { Component, ElementRef, EventEmitter, Input, Output, QueryList, ViewChildren, inject, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-email-change-field',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslatePipe],
  templateUrl: './email-change-field.html',
})
export class EmailChangeField {
  // Текущая почта для отображения в кнопке
  @Input() currentEmail: string = '';

  // События для родителя (API вызовы)
  // 1. Инициализация (отправляем новую почту)
  @Output() initChange = new EventEmitter<{ newEmail: string, onSuccess: () => void }>();

  // 2. Подтверждение (отправляем почту и код)
  @Output() confirmChange = new EventEmitter<{ newEmail: string, code: string }>();

  // Состояние
  isOpen = signal(false);
  step = signal<'email' | 'otp'>('email'); // Текущий шаг
  isLoading = signal(false);

  // Контрол для новой почты
  emailControl = new FormControl('', [Validators.required, Validators.email]);

  // --- ЛОГИКА OTP (из твоего примера) ---
  digits: string[] = ['', '', '', '', '', ''];
  @ViewChildren('otpInput') inputs!: QueryList<ElementRef>;

  // --- МЕТОДЫ УПРАВЛЕНИЯ МОДАЛКОЙ ---

  openEdit() {
    this.emailControl.reset();
    this.digits = ['', '', '', '', '', ''];
    this.step.set('email'); // Всегда начинаем с ввода почты
    this.isOpen.set(true);
  }

  cancel() {
    this.isOpen.set(false);
    this.digits = ['', '', '', '', '', ''];
  }

  // --- ШАГ 1: ОТПРАВКА ПОЧТЫ ---

  submitEmail() {
    if (this.emailControl.valid) {
      this.isLoading.set(true);
      const newEmail = this.emailControl.value!;

      // Эмитим событие. Родитель должен вызвать onSuccess, если API вернул 200 OK
      this.initChange.emit({
        newEmail: newEmail,
        onSuccess: () => {
          this.isLoading.set(false);
          this.step.set('otp'); // Переключаем на OTP
          // Небольшой таймаут, чтобы инпуты успели отрендериться перед фокусом
          setTimeout(() => this.inputs?.first?.nativeElement.focus(), 100);
        }
      });
    } else {
      this.emailControl.markAsTouched();
    }
  }

  // --- ШАГ 2: ЛОГИКА OTP ---

  onInput(index: number, event: any) {
    const value = event.target.value;
    this.digits[index] = value.replace(/[^0-9]/g, '');

    if (this.digits[index].length === 1) {
      if (index < 5) {
        this.inputs.toArray()[index + 1].nativeElement.focus();
      } else {
        this.inputs.toArray()[index].nativeElement.blur();
        this.verifyCode(); // Авто-отправка при заполнении
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
    // Проверка на undefined, т.к. viewChildren могут быть не готовы
    if(this.inputs && this.inputs.toArray()[focusIndex]) {
      this.inputs.toArray()[focusIndex].nativeElement.focus();
    }

    if (pastedData.length === 6) {
      this.verifyCode();
    }
  }

  verifyCode() {
    const code = this.digits.join('');
    if (code.length < 6) return;

    console.log('Отправляем код:', code);

    this.confirmChange.emit({
      newEmail: this.emailControl.value!,
      code: code
    });

    // Закрытие модалки можно сделать здесь или пусть родитель управляет через ViewChild
    // this.isOpen.set(false);
  }
}
