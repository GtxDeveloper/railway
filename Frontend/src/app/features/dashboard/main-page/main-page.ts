import {Component, computed, effect, inject, OnInit, signal, ViewChild} from '@angular/core';
import {DashboardStore} from '../../../core/stores/dashboard-store';
import {ProfileEditFieldComponent} from '../../../shared/components/profile-edit-field/profile-edit-field';
import {FormControl, Validators} from '@angular/forms';
import {PasswordChangeField} from '../../../shared/components/password-change-field/password-change-field';
import {EmailChangeField} from '../../../shared/components/email-change-field/email-change-field';
import {forkJoin} from 'rxjs';
import {DatePipe, DecimalPipe} from '@angular/common';
import {UserProfilePayload} from '../../../core/models/dashboard.models';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-main-page',
  imports: [
    ProfileEditFieldComponent,
    PasswordChangeField,
    EmailChangeField,
    DecimalPipe,
    DatePipe,
    TranslatePipe
  ],
  standalone: true,
  templateUrl: './main-page.html',
  styleUrl: './main-page.css',
})
export class MainPage implements OnInit {

  @ViewChild(EmailChangeField) emailField!: EmailChangeField;

  readonly store = inject(DashboardStore);
  showToast = signal(false);
  toastMessage = signal(''); // Текст сообщения
  toastType = signal<'success' | 'error'>('success'); // Тип сообщения
  private toastTimeout: any;
  isSaving = signal<boolean>(false);
  isBalanceOpen = signal(false);

  // 1. Используем опцию { nonNullable: true }, чтобы избежать null и ! в будущем
  firstNameControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required]
  });

  lastNameControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required]
  });

  phoneControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required]
  });

  roleControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required]
  });

  constructor() {
    // 2. Автоматическое заполнение полей при появлении данных в сторе
    effect(() => {
      const profile = this.store.profile();
      const worker = this.store.currentWorker();

      // Если профиль загрузился, обновляем форму
      if (profile) {
        // patchValue обновляет только указанные поля, игнорируя отсутствующие
        this.firstNameControl.setValue(profile.firstName);
        this.lastNameControl.setValue(profile.lastName);
        this.phoneControl.setValue(profile.phoneNumber);
      }

      // Если данные о работе загрузились
      if (worker) {
        this.roleControl.setValue(worker.job);
      }
      if (this.store.isOnboarded() && !this.store.qrCodeUrl() && !this.store.isQrLoading()) {
        this.store.getQr();
      }
    });
  }

  getLoginLink() {
    this.store.getLoginLink();
  }

  toggleBalanceDetails() {
    this.isBalanceOpen.update(v => !v);
  }

  onboard() {
    this.store.startOnboarding();
  }

  ngOnInit(): void {
    this.store.loadAll();
  }

  downloadQrImage() {
    const url = this.store.qrCodeUrl();
    if (!url) return;

    // Создаем временную ссылку
    const link = document.createElement('a');
    link.href = url;

    // Имя файла при скачивании
    link.download = 'moj-qr-kod.png'; // Или .svg, зависит от формата бэка

    // Эмулируем клик
    document.body.appendChild(link);
    link.click();

    // Чистим мусор
    document.body.removeChild(link);
  }

  saveProfile() {
    if (this.firstNameControl.invalid || this.lastNameControl.invalid || this.roleControl.invalid) {
      this.triggerToast('Vyplňte, prosím, požadované polia', 'error');
      return;
    }

    this.isSaving.set(true);

    const currentProfile = this.store.profile();
    if (!currentProfile) return;

    // 1. Собираем Payload
    const payload: UserProfilePayload = {
      // Сохраняем бизнес-данные, если они были
      city: currentProfile.city,
      brandName: currentProfile.brandName,

      // Обновляем личные данные из формы
      firstName: this.firstNameControl.value,
      lastName: this.lastNameControl.value,
      phoneNumber: this.phoneControl.value,
      job: this.roleControl.value
    };

    const job = this.roleControl.value;

    // 2. Задачи
    const tasks = [
      this.store.changeProfile(payload)
    ];

    // 3. Выполнение
    forkJoin(tasks).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.triggerToast('Profil uložený', 'success');
        this.store.currentWorker()!.job = job;
      },
      error: (err) => {
        this.isSaving.set(false);
        console.error(err);
        this.triggerToast('Chyba pri ukladaní', 'error');
      }
    });
  }

  onChangePassword(data: { oldPassword: string, newPassword: string }) {
    console.log('Отправка на бэк:', data);
    this.store.changePassword(data).subscribe({
      next: () => this.triggerToast('Heslo bolo úspešne zmenené', 'success'),
      error: (err) => this.triggerToast('Chyba: ' + err.message, 'error')
    });
  }

  onInitEmail(event: { newEmail: string, onSuccess: () => void }) {
    this.store.onInitEmail(event)
      .subscribe({
        next: () => event.onSuccess(),
        error: (err) => this.triggerToast('Chyba: ' + err.message, 'error')
      });
  }

  onConfirmEmail(event: { newEmail: string, code: string }) {
    this.store.onConfirmEmail(event).subscribe({
      next: () => {
        this.triggerToast('Email úspešne zmenený!', 'success');
        this.store.loadAll();
        this.emailField.cancel();
      },
      error: (err) => this.triggerToast('Nesprávny kód!', 'error')
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;

    if (input.files && input.files.length > 0) {
      const file = input.files[0];

      // Простая валидация (например, макс 5 МБ)
      if (file.size > 5 * 1024 * 1024) {
        this.triggerToast('Súbor je príliš veľký (max 5MB)', 'error');
        return;
      }

      // Вызываем метод Store
      this.store.uploadAvatar(file);

      // Сбрасываем value, чтобы событие change сработало,
      // даже если юзер выберет тот же файл второй раз подряд (например, после ошибки)
      input.value = '';
    }
  }

  readonly paymentLink = computed(() => {
    const workerId = this.store.currentWorker()?.id;
    if (!workerId) return '';
    // Формируем ссылку: текущий сайт + /pay/ + ID
    return `${window.location.origin}/pay/${workerId}`;
  });

  copyLink() {
    const link = this.paymentLink();
    if (!link) return;

    navigator.clipboard.writeText(link).then(() => {
      // Если у тебя есть translate service, можешь использовать this.translate.instant('KEY')
      // Или просто хардкод для теста:
      this.triggerToast('Odkaz skopírovaný', 'success');
    }).catch(err => {
      console.error('Failed to copy: ', err);
      this.triggerToast('Nepodarilo sa skopírovať odkaz', 'error');
    });
  }

  private triggerToast(message: string, type: 'success' | 'error' = 'success') {
    this.toastMessage.set(message);
    this.toastType.set(type);
    this.showToast.set(true);

    if (this.toastTimeout) clearTimeout(this.toastTimeout);

    this.toastTimeout = setTimeout(() => {
      this.showToast.set(false);
    }, 3000);
  }
}
