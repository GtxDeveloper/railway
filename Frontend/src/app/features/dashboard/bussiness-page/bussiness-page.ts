import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import {DashboardStore} from '../../../core/stores/dashboard-store';
import {UpdateWorkerPayload, UserProfilePayload} from '../../../core/models/dashboard.models';
import {TranslatePipe} from '@ngx-translate/core';


@Component({
  selector: 'app-business-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './bussiness-page.html',
  styles: [`
    .no-scrollbar::-webkit-scrollbar { display: none; }
    .no-scrollbar { -ms-overflow-style: none; scrollbar-width: none; }
  `]
})
export class BusinessDashboardComponent implements OnInit {

  // Внедряем Стор
  readonly store = inject(DashboardStore);

  isEditModalOpen = signal(false);
  isAddModalOpen = signal(false);
  isAddSaving = signal(false);
  isEditSaving = signal(false);
  editingWorkerId = signal<string | null>(null);
  deletingWorkerId = signal<string | null>(null);
  // Локальные сигналы для формы (чтобы редактировать, не ломая стор сразу)
  formBrandName = signal('');
  formCity = signal('');
  formWebsite = signal(''); // Убедитесь, что в DTO профиля есть это поле, или используйте другое
  editFirstName = signal('');
  editLastName = signal('');
  editJob = signal('');
  // Состояние сохранения
  isSaving = signal(false);
  loadingQrForWorkerId = signal<string | null>(null);
  isQrModalOpen = signal(false);
  qrModalData = signal<{ name: string; qrUrl: string; payLink: string } | null>(null);

  // Поля формы добавления
  addFirstName = signal('');
  addLastName = signal('');
  addJob = signal('');

  constructor() {
    // ЭФФЕКТ: Синхронизируем данные из Стора в Инпуты при загрузке
    effect(() => {
      const profile = this.store.profile();
      if (profile) {
        // untracked не обязателен, если мы просто сетим, но хорошая практика внутри эффекта
        this.formBrandName.set(profile.brandName || '');
        this.formCity.set(profile.city || '');
        // this.formWebsite.set(profile.website || ''); // Если поле есть в DTO
      }
    });
  }

  ngOnInit() {
    // Загружаем все данные (Профиль, Воркеры, Статистика)
    this.store.loadAll();
  }

  openQrModal(worker: any) {
    this.loadingQrForWorkerId.set(worker.id);

    // Запрашиваем и QR, и Ссылку параллельно
    forkJoin({
      qrUrl: this.store.getWorkerQr(worker.id),
      payLinkObj: this.store.getPayLink(worker.id)
    }).subscribe({
      next: ({ qrUrl, payLinkObj }) => {

        this.qrModalData.set({
          name: worker.name,
          qrUrl: qrUrl,
          payLink: payLinkObj.url
        });

        this.loadingQrForWorkerId.set(null);
        this.isQrModalOpen.set(true);
      },
      error: (err) => {
        console.error(err);
        this.loadingQrForWorkerId.set(null);
        alert('Nepodarilo sa načítať QR kód');
      }
    });
  }

  downloadQr(data: { name: string; qrUrl: string }) {
    const link = document.createElement('a');
    link.href = data.qrUrl;

    // Формируем имя файла: "qr_code_meno_priezvisko.png"
    const filename = data.name.replace(/\s+/g, '_').toLowerCase();
    link.download = `qr_code_${filename}.png`;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  closeQrModal() {
    this.isQrModalOpen.set(false);
    // Очищаем данные (опционально можно делать revokeObjectURL для qrUrl, если нужно освободить память)
    this.qrModalData.set(null);
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      alert('Odkaz skopírovaný!');
    });
  }

  // ОТКРЫТЬ МОДАЛКУ (Метод, который вызывается кнопкой "Pridať nového")
  addWorker() {
    // Сбрасываем форму перед открытием
    this.addFirstName.set('');
    this.addLastName.set('');
    this.addJob.set('');

    this.isAddModalOpen.set(true);
  }

  closeAddModal() {
    this.isAddModalOpen.set(false);
  }

  // СОХРАНИТЬ НОВОГО
  saveNewWorker() {
    if (!this.addFirstName() || !this.addJob()) {
      alert('Meno a pozícia sú povinné'); // Валидация
      return;
    }

    this.isAddSaving.set(true);

    const payload = {
      firstName: this.addFirstName(),
      lastName: this.addLastName(),
      job: this.addJob()
    };

    this.store.addWorker(payload).subscribe({
      next: () => {
        this.isAddSaving.set(false);
        this.closeAddModal();
        // Уведомление об успехе
      },
      error: (err) => {
        this.isAddSaving.set(false);
        console.error(err);
        alert('Chyba pri vytváraní pracovníka');
      }
    });
  }


  openEditModal(workerId: string) {
    const worker = this.store.workers().find(w => w.id === workerId);
    if (!worker) return;

    this.editingWorkerId.set(workerId);

    // Пытаемся разбить имя на Имя и Фамилию
    // Если у вас в worker.name хранится "Иван Иванов"
    const nameParts = worker.name.split(' ');
    const fName = nameParts[0] || '';
    const lName = nameParts.slice(1).join(' ') || '';

    this.editFirstName.set(fName);
    this.editLastName.set(lName);
    this.editJob.set(worker.job);

    this.isEditModalOpen.set(true);
  }

  // ЗАКРЫТЬ МОДАЛКУ
  closeEditModal() {
    this.isEditModalOpen.set(false);
    this.editingWorkerId.set(null);
  }

  // СОХРАНИТЬ ИЗМЕНЕНИЯ
  saveWorker() {
    const id = this.editingWorkerId();
    if (!id) return;

    if (!this.editFirstName() || !this.editJob()) {
      alert('Meno a pozícia sú povinné');
      return;
    }

    this.isEditSaving.set(true);

    const payload: UpdateWorkerPayload = {
      firstName: this.editFirstName(),
      lastName: this.editLastName(),
      job: this.editJob()
    };

    this.store.updateWorker(id, payload).subscribe({
      next: () => {
        this.isEditSaving.set(false);
        this.closeEditModal();
        // Можно добавить тост/уведомление "Успешно"
      },
      error: (err) => {
        this.isEditSaving.set(false);
        console.error(err);
        alert('Chyba pri ukladaní');
      }
    });
  }

  // --- ЛОГИКА СОХРАНЕНИЯ ---
  saveBusiness() {
    // Валидация
    if (!this.formBrandName() || !this.formCity()) {
      alert('Názov prevádzky a mesto sú povinné údaje.');
      return;
    }

    this.isSaving.set(true);

    // 1. Берем текущий профиль, чтобы не потерять Имя и Фамилию
    const currentProfile = this.store.profile();

    if (!currentProfile) return; // Защита

    // 2. Собираем Payload
    const payload: UserProfilePayload = {
      firstName: currentProfile.firstName,   // Оставляем как было
      lastName: currentProfile.lastName,     // Оставляем как было
      phoneNumber: currentProfile.phoneNumber, // Оставляем как было

      // Обновляем то, что редактировали в форме
      city: this.formCity(),
      brandName: this.formBrandName()
    };

    // 3. Отправляем
    const tasks = [
      this.store.changeProfile(payload)
    ];

    forkJoin(tasks).subscribe({
      next: () => {
        this.isSaving.set(false);
        alert('Údaje boli úspešne uložené');
        // this.store.loadAll(); // Можно не вызывать, так как мы обновили стейт в tap()
      },
      error: (err) => {
        this.isSaving.set(false);
        console.error(err);
        alert('Chyba pri ukladaní');
      }
    });
  }

  // 1. Выбор логотипа бизнеса
  onLogoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      const file = input.files[0];

      // Валидация размера (2MB)
      if (file.size > 2 * 1024 * 1024) {
        alert('Súbor je príliš veľký (max 2MB)');
        return;
      }

      this.store.uploadBusinessLogo(file);
      input.value = ''; // Сброс, чтобы можно было загрузить тот же файл
    }
  }

  // 2. Выбор аватара работника
  onWorkerAvatarSelected(event: Event, workerId: string) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      const file = input.files[0];

      if (file.size > 2 * 1024 * 1024) {
        alert('Súbor je príliš veľký (max 2MB)');
        return;
      }

      this.store.uploadWorkerAvatar(workerId, file);
      input.value = '';
    }
  }

  // Хелпер для клика по скрытому инпуту работника
  triggerWorkerInput(workerId: string) {
    const input = document.getElementById('file-input-' + workerId) as HTMLInputElement;
    if (input) input.click();
  }

  // --- РАБОТА С РАБОТНИКАМИ ---

  inviteWorker(workerId: string) {
    this.store.generateInvite(workerId).subscribe({
      next: (res) => {
        // Копируем в буфер обмена
        navigator.clipboard.writeText(res.inviteUrl).then(() => {
          alert(`Odkaz skopírovaný:\n${res.inviteUrl}`);
        });
      },
      error: (err) => {
        console.error(err);
        alert('Chyba pri generovaní odkazu');
      }
    });
  }

  // Метод для открытия модалки (вызывается при клике на иконку корзины)
  confirmDelete(workerId: string) {
    this.deletingWorkerId.set(workerId);
  }

  // Метод отмены
  closeDeleteModal() {
    this.deletingWorkerId.set(null);
  }

  // Метод подтверждения удаления
  deleteWorker() {
    const id = this.deletingWorkerId();
    if (id) {
      this.store.deleteWorker(id);
      this.closeDeleteModal();
    }
  }
}
