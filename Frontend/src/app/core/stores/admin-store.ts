import { inject, Injectable, signal } from '@angular/core';
import { finalize, forkJoin } from 'rxjs';

import { AdminBusiness, AdminStats, AdminTransaction } from '../models/admin.models';
import {AdminService} from '../services/admin-service';

@Injectable({
  providedIn: 'root'
})
export class AdminStore {
  private api = inject(AdminService);

  // --- STATE (Сигналы) ---
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  readonly actionLoadingId = signal<string | null>(null);

  // Данные
  readonly stats = signal<AdminStats | null>(null);
  readonly transactions = signal<AdminTransaction[]>([]);
  readonly businesses = signal<AdminBusiness[]>([]);

  // --- ACTIONS ---

  // Загрузить всё разом (идеально для Init админ-панели)
  loadDashboardData() {
    this.isLoading.set(true);
    this.error.set(null);

    forkJoin({
      stats: this.api.getStats(),
      transactions: this.api.getTransactions(),
      businesses: this.api.getBusinesses()
    })
      .pipe(
        finalize(() => this.isLoading.set(false))
      )
      .subscribe({
        next: (response) => {
          this.stats.set(response.stats);
          this.transactions.set(response.transactions);
          this.businesses.set(response.businesses);
        },
        error: (err) => {
          console.error('Ошибка загрузки админки:', err);
          this.error.set('Nepodarilo sa načítať dáta pre Admin Dashboard.');
        }
      });
  }

  // 1. Скачать QR-код
  downloadQr(workerId: string, workerName: string) {
    this.actionLoadingId.set(workerId); // Включаем лоадер на кнопке

    this.api.getWorkerQr(workerId)
      .pipe(finalize(() => this.actionLoadingId.set(null)))
      .subscribe({
        next: (blob) => {
          // Магия скачивания файла в браузере
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          // Имя файла: qr-nikita-landyk.png
          link.download = `qr-${workerName.replace(/\s+/g, '-').toLowerCase()}.png`;
          link.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => {
          console.error(err);
          alert('Chyba pri sťahovaní QR kódu');
        }
      });
  }

  // 2. Скопировать ссылку
  copyLink(workerId: string) {
    this.actionLoadingId.set(workerId);

    this.api.getWorkerPayLink(workerId)
      .pipe(finalize(() => this.actionLoadingId.set(null)))
      .subscribe({
        next: (res) => {
          // Копируем в буфер обмена
          navigator.clipboard.writeText(res.url).then(() => {
            alert('Odkaz skopírovaný!'); // Или используй свой Toast
          });
        },
        error: (err) => {
          console.error(err);
          alert('Chyba pri získavaní odkazu');
        }
      });
  }
}
