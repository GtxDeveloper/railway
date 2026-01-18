import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgClass } from '@angular/common';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-password-change-field',
  standalone: true,
  imports: [ReactiveFormsModule, NgClass, TranslatePipe],
  templateUrl: './password-change-field.html',
})
export class PasswordChangeField {
  private fb = inject(FormBuilder);
  private readonly passwordPattern = /^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$/;
  // Событие, которое полетит в родителя для запроса на бэк
  @Output() save = new EventEmitter<{oldPassword: string, newPassword: string}>();

  // Локальная форма
  form = this.fb.group({
    oldPassword: ['', Validators.required],
    newPassword: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(this.passwordPattern) // <-- Добавил валидатор
    ]]
  });

  // Состояние модалки
  isOpen = signal(false);

  // Видимость паролей (отдельно для старого и нового)
  showOldPassword = signal(false);
  showNewPassword = signal(false);

  // --- Геттеры для валидации в HTML ---

  get oldControl() { return this.form.get('oldPassword')!; }
  get newControl() { return this.form.get('newPassword')!; }

  isInvalid(controlName: 'oldPassword' | 'newPassword') {
    const control = this.form.get(controlName);
    return control ? control.invalid && control.touched : false;
  }

  isValid(controlName: 'oldPassword' | 'newPassword') {
    const control = this.form.get(controlName);
    return control ? control.valid && control.touched : false;
  }

  getErrorMessage(controlName: 'oldPassword' | 'newPassword') {
    const control = this.form.get(controlName);
    if (!control) return '';

    if (control.hasError('required')) return 'Pole je povinné';

    // Обновил текст для длины (динамически берет число 8)
    if (control.hasError('minlength')) {
      const min = control.errors?.['minlength'].requiredLength;
      return `Minimálne ${min} znakov`;
    }

    // Добавил сообщение для сложности пароля
    if (control.hasError('pattern')) {
      return 'Heslo musí obsahovať veľké písmeno, číslo a znak';
    }

    return '';
  }

  // --- Логика ---

  openEdit() {
    this.form.reset(); // Очищаем форму при открытии
    this.showOldPassword.set(false);
    this.showNewPassword.set(false);
    this.isOpen.set(true);
  }

  cancel() {
    this.isOpen.set(false);
    this.form.reset();
  }

  submit() {
    if (this.form.valid) {
      const { oldPassword, newPassword } = this.form.getRawValue();

      // Отправляем данные наверх
      this.save.emit({
        oldPassword: oldPassword!,
        newPassword: newPassword!
      });

      this.isOpen.set(false);
    } else {
      this.form.markAllAsTouched();
    }
  }

  // Переключалки глазиков
  toggleOld() { this.showOldPassword.update(v => !v); }
  toggleNew() { this.showNewPassword.update(v => !v); }
}
