import { Component, Input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-profile-edit-field',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './profile-edit-field.html',
})
export class ProfileEditFieldComponent {
  @Input({ required: true }) control!: FormControl;
  @Input() label: string = '';
  @Input() type: 'text' | 'password' | 'email' = 'text';
  @Input() placeholder: string = '';

  // Состояние модального окна
  isOpen = signal(false);

  // Состояние видимости пароля (для модалки)
  isPasswordVisible = signal(false);

  // Для отмены изменений
  private backupValue: any = '';

  // Геттеры для валидации (как в твоем коде)
  get isInvalid() {
    return this.control.invalid && this.control.touched;
  }

  get isValid() {
    return this.control.valid && this.control.touched;
  }

  get errorMessage() {
    // Тут твоя логика ошибок, для примера:
    if (this.control.hasError('required')) return 'Pole je povinné';
    if (this.control.hasError('email')) return 'Nesprávny formát emailu';
    return '';
  }

  // --- Логика ---

  // Открываем редактор
  openEdit() {
    this.backupValue = this.control.value; // Запоминаем старое значение
    this.isOpen.set(true);
    this.isPasswordVisible.set(false); // Сбрасываем глазик
  }

  // Закрываем без сохранения
  cancel() {
    this.control.setValue(this.backupValue); // Возвращаем как было
    this.isOpen.set(false);
  }

  // Сохраняем
  save() {
    if (this.control.valid) {
      this.isOpen.set(false);
      // Тут можно эмитить событие наверх, если нужно сохранить на бэк
    } else {
      this.control.markAsTouched();
    }
  }

  togglePassword() {
    this.isPasswordVisible.update(v => !v);
  }

  // Тип инпута внутри модалки
  currentInputType() {
    if (this.type !== 'password') return this.type;
    return this.isPasswordVisible() ? 'text' : 'password';
  }
}
