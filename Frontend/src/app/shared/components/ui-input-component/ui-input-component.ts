import { CommonModule } from '@angular/common';
import { Component, Input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-ui-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="flex flex-col gap-1">
      <div class="relative">
        <input
          [type]="currentType()"
          [formControl]="control"
          [placeholder]="placeholder"
          class="w-full bg-white px-6 py-4 raleway  rounded-full font-medium text-2xl border-3 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors"
          [ngClass]="{
            'border-red-500 focus:ring-red-200': isInvalid,
            'border-[#ffc800]': isValid,
            'border-white': !isInvalid && !isValid,
            'pr-10': type === 'password',
            'pr-4': type !== 'password'
          }"
        />

        @if (type === 'password') {
          <button
            type="button"
            (click)="togglePassword()"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700 focus:outline-none"
            tabindex="-1"
          >
            @if (isPasswordVisible()) {
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-8 h-8">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
              </svg>
            }
            @else {
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-8 h-8">
                <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
            }
          </button>
        }
      </div>

      @if (isInvalid) {
        <span class="font-bold text-xs ms-7 raleway text-red-500 min-h-[16px]">
          {{ errorMessage }}
        </span>
      }
    </div>
  `,
})
export class UiInputComponent {
  @Input({ required: true }) control!: FormControl;
  @Input() placeholder = 'Placeholder';
  @Input() type: 'text' | 'password' | 'email' | 'tel' = 'text';

  isPasswordVisible = signal(false);

  togglePassword() {
    this.isPasswordVisible.update(v => !v);
  }

  currentType() {
    if (this.type !== 'password') return this.type;
    return this.isPasswordVisible() ? 'text' : 'password';
  }

  // Состояние ошибки: невалидно + пользователь трогал поле
  get isInvalid(): boolean {
    return this.control.invalid && (this.control.dirty || this.control.touched);
  }

  // Состояние успеха: валидно + пользователь трогал поле (чтобы не светилось желтым сразу при загрузке)
  get isValid(): boolean {
    return this.control.valid && (this.control.dirty || this.control.touched);
  }

  get errorMessage(): string {
    if (this.control.hasError('required')) return 'Povinné pole';
    if (this.control.hasError('email')) return 'Neplatný formát emailu';
    if (this.control.hasError('minlength')) {
      const min = this.control.errors?.['minlength'].requiredLength;
      return `Minimálne ${min} znakov`;
    }
    return '';
  }
}
