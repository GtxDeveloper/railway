import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const repeatPassword = control.get('repeatPassword');

  // Если поля существуют и значения разные
  if (password && repeatPassword && password.value !== repeatPassword.value) {
    // Ставим ошибку на поле repeatPassword, чтобы оно подсветилось красным
    repeatPassword.setErrors({ mismatch: true });
    return { mismatch: true };
  }

  // Если валидатор запускается, но ошибка была именно mismatch, убираем её
  if (repeatPassword?.hasError('mismatch')) {
    repeatPassword.setErrors(null);
  }

  return null;
};
