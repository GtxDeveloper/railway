import { inject, Injectable, signal } from '@angular/core';

import { finalize } from 'rxjs';
import {DashboardService} from '../services/dashboard-service';
import {PlatformFeeConfig, PublicWorker} from '../models/dashboard.models';

// Фолбэк-тариф на случай падения сети (чтобы оплата не сломалась)
const FALLBACK_FEE_CONFIG: PlatformFeeConfig = {
  thresholdAmount: 5,
  lowTier: { percent: 5, fixedCents: 5 },
  highTier: { percent: 1.5, fixedCents: 25 }
};

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
  feeConfig = signal<PlatformFeeConfig | null>(null);
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
  pay(workerId: string, amount: number, note: string, coverFee: boolean) {
    this.isPaying.set(true);

    // Предположим, в сервисе есть метод createCheckoutSession
    // который возвращает { url: string }
    this.api.pay(workerId, amount, coverFee)
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

  loadPlatformFee() {
    this.api.getPlatformFee().subscribe({
      next: (res) => {
        this.feeConfig.set(res);
      },
      error: (err) => {
        console.error('Не удалось загрузить тариф комиссии', err);
        this.feeConfig.set(FALLBACK_FEE_CONFIG);
      }
    });
  }
}
