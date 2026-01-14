import { inject, Injectable, signal } from '@angular/core';

import { finalize } from 'rxjs';
import {DashboardService} from '../services/dashboard-service';
import {PublicWorker} from '../models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class PaymentStore {
  private api = inject(DashboardService);

  // --- STATE ---
  readonly isLoading = signal(false);
  readonly isPaying = signal(false); // Лоадер для кнопки оплаты
  readonly error = signal<string | null>(null);

  readonly worker = signal<PublicWorker | null>(null);

  // --- ACTIONS ---

  // 1. Загрузка профиля
  loadWorker(workerId: string) {
    this.isLoading.set(true);
    this.error.set(null);

    this.api.getPublicWorker(workerId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (data) => this.worker.set(data),
        error: (err) => {
          console.error(err);
          this.error.set('Pracovník sa nenašiel');
        }
      });
  }

  // 2. Оплата (редирект на Stripe)
  pay(workerId: string, amount: number, note: string) {
    this.isPaying.set(true);

    // Предположим, в сервисе есть метод createCheckoutSession
    // который возвращает { url: string }
    this.api.pay(workerId, amount)
      .pipe(finalize(() => this.isPaying.set(false)))
      .subscribe({
        next: (res) => {
          // Редирект на Stripe
          window.location.href = res.url;
        },
        error: (err) => {
          console.error(err);
          alert('Chyba pri vytváraní platby');
        }
      });
  }
}
