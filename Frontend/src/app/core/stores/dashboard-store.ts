import { Injectable, signal, computed, inject } from '@angular/core';
import {catchError, forkJoin, map, of, switchMap, tap, throwError} from 'rxjs';
import { DashboardService } from '../services/dashboard-service';
import {
  Balance,
  BusinessProfile,
  CreateWorkerPayload,
  ProfileResponse,
  Summary,
  Transaction,
  UpdateWorkerPayload,
  UserProfilePayload,
  Worker
} from '../models/dashboard.models';

// Интерфейс ответа /api/Account/me
export interface UserContext {
  userId: string;
  role: 'Owner' | 'Worker';
  businessId: string;
  workerId: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardStore {
  private api = inject(DashboardService);

  // --- 1. STATE ---

  // НОВЫЙ СИГНАЛ: Контекст текущего пользователя (Кто я?)
  readonly userContext = signal<UserContext | null>(null);
  readonly currentWorker = signal<Worker | null>(null);
  readonly profile = signal<ProfileResponse | null>(null);
  readonly workers = signal<Worker[]>([]);
  readonly business = signal<BusinessProfile | null>(null);
  readonly isInviting = signal<string | null>(null);
  readonly isGettingPayLink = signal<string | null>(null);
  readonly isWorkerQrLoading = signal<string | null>(null);
  // Финансы
  readonly summary = signal<Summary>({transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0});
  readonly businessSummary = signal<Summary>({transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0});
  readonly balance = signal<Balance>({currency: 'eur', available: 0, pending: 0});
  readonly transactions = signal<Transaction[]>([]);

  // Состояния загрузки
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  isRedirecting = signal(false);
  isOnboarding = signal(false);
  isQrLoading = signal(false);
  isPaying = signal(false);
  isBalanceLoading = signal(false);
  isTransactionsLoading = signal(false);

  isAvatarUploading = signal(false);
  isBusinessLogoUploading = signal(false);
  isWorkerAvatarUploading = signal<string | null>(null);

  readonly qrCodeUrl = signal<string | null>(null);

  // --- 2. COMPUTED ---


  // Хелперы
  readonly isOwner = computed(() => this.userContext()?.role === 'Owner');
  readonly isOnboarded = computed(() => this.currentWorker()?.isOnboarded ?? false);
  readonly isLinked = computed(() => this.currentWorker()?.isLinked ?? false);
  readonly stripeAccountId = computed(() => this.currentWorker()?.stripeAccountId ?? null);


  // --- 3. METHODS ---

  loadAll() {
    this.isLoading.set(true);
    this.error.set(null);

    // 1. Узнаем Context (Кто я?)
    this.api.getMe().pipe(
      tap((context) => this.userContext.set(context)),
      switchMap((context) => {

        // Подготавливаем запрос на получение "Меня как работника"
        const meRequest = context.workerId
          ? this.api.getWorkerById(context.workerId)
          : of(null);

        // === ЛОГИКА ДЛЯ ВЛАДЕЛЬЦА ===
        if (context.role === 'Owner') {
          return forkJoin({
            profile: this.api.profile(),
            workers: this.api.workers(),
            summary: context.workerId
              ? this.api.getWorkerSummary(context.workerId)
              : of({ transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0 }),
            businessSummary : this.api.getSummary(),
            business: this.api.getBusiness(),
            currentWorker: meRequest
          });
        }

        // === ЛОГИКА ДЛЯ РАБОТНИКА ===
        else {
          // Если у работника есть ID, готовим запрос на статистику
          const summaryRequest = context.workerId
            ? this.api.getWorkerSummary(context.workerId)
            : of({ transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0 }); // Пустая заглушка

          return forkJoin({
            profile: this.api.profile(),
            workers: of([]),

            // ТЕПЕРЬ ГРУЗИМ РЕАЛЬНУЮ СТАТИСТИКУ
            summary: summaryRequest,

            business: of(null),
            businessSummary: of(null),
            currentWorker: meRequest
          });
        }
      })
    ).subscribe({
      next: (data) => {
        this.profile.set(data.profile);
        this.workers.set(data.workers || []);

        // Записываем данные текущего работника
        this.currentWorker.set(data.currentWorker);

        if (data.summary) this.summary.set(data.summary);
        if (data.business) this.business.set(data.business);
        if (data.businessSummary) this.businessSummary.set(data.businessSummary);

        this.isLoading.set(false);

        // === ПРОВЕРКА ONBOARDING ===
        // Запрашиваем баланс и QR, только если пользователь прошел онбординг
        const myWorkerId = this.userContext()?.workerId;
        const isUserOnboarded = data.currentWorker?.isOnboarded ?? false;

        if (myWorkerId && isUserOnboarded) {
          this.loadBalance(myWorkerId);
          this.loadTransactions(myWorkerId);
          this.getQr();
        }
      },
      error: (err) => {
        console.error(err);
        this.error.set('Chyba pri načítaní údajov');
        this.isLoading.set(false);
      }
    });
  }

  // --- МЕТОДЫ, ИСПОЛЬЗУЮЩИЕ workerId ИЗ КОНТЕКСТА ---

  getQr() {
    const workerId = this.userContext()?.workerId;
    if (!workerId) return;

    this.isQrLoading.set(true);

    const oldUrl = this.qrCodeUrl();
    if (oldUrl) URL.revokeObjectURL(oldUrl);

    this.api.qr(workerId).subscribe({
      next: (blob: Blob) => {
        const objectUrl = URL.createObjectURL(blob);
        this.qrCodeUrl.set(objectUrl);
        this.isQrLoading.set(false);
      },
      error: (err) => {
        console.error('Ошибка QR:', err);
        this.isQrLoading.set(false);
      }
    });
  }

  getLoginLink() {
    const workerId = this.userContext()?.workerId;
    if (!workerId) return;

    this.isRedirecting.set(true);

    this.api.getLoginLink(workerId).subscribe({
      next: (res: any) => {
        if (res.url) window.location.href = res.url;
        else this.isRedirecting.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isRedirecting.set(false);
        alert('Chyba: Nepodarilo sa získať odkaz');
      }
    });
  }

  startOnboarding() {
    const workerId = this.userContext()?.workerId;
    if (!workerId) return;

    this.isOnboarding.set(true);

    this.api.onboard(workerId).subscribe({
      next: (res: any) => {
        if (res.url) window.location.href = res.url;
        else this.isOnboarding.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isOnboarding.set(false);
        alert('Chyba: Nepodarilo sa získať odkaz');
      }
    });
  }

  // --- ОБНОВЛЕНИЕ ПРОФИЛЯ С СИНХРОНИЗАЦИЕЙ ---

  getPayLink(workerId: string) {
    // Включаем лоадер для конкретного ID
    this.isGettingPayLink.set(workerId);

    return this.api.getPayLink(workerId).pipe(
      tap(() => {
        // Выключаем лоадер при успехе
        this.isGettingPayLink.set(null);
      }),
      catchError((err) => {
        // Выключаем лоадер при ошибке и пробрасываем её дальше
        this.isGettingPayLink.set(null);
        return throwError(() => err);
      })
    );
  }

  uploadAvatar(file: File) {
    this.isAvatarUploading.set(true);

    this.api.uploadAvatar(file).subscribe({
      next: (res) => {
        const newUrl = `${res.url}?t=${Date.now()}`;

        // 1. Обновляем профиль (User)
        this.profile.update(current => current ? { ...current, avatarUrl: newUrl } : current);

        this.isAvatarUploading.set(false);

        // 2. СИНХРОНИЗАЦИЯ: Обновляем аватарку Воркера
        // Берем ID из контекста!
        const myWorkerId = this.userContext()?.workerId;
        if (myWorkerId) {
          this.uploadWorkerAvatar(myWorkerId, file);
        }
      },
      error: (err) => {
        console.error(err);
        this.isAvatarUploading.set(false);
      }
    });
  }

  changeProfile(payload: UserProfilePayload) {
    return this.api.changeProfile(payload).pipe(
      tap(() => {
        // 1. Обновляем профиль
        this.profile.update(current => current ? { ...current, ...payload } : current);

        // 2. СИНХРОНИЗАЦИЯ: Обновляем данные Воркера
        const currentWorker = this.currentWorker(); // Берем вычисленного воркера

        if (currentWorker) {
          const workerPayload = {
            firstName: payload.firstName,
            lastName: payload.lastName,
            job: currentWorker.job // Оставляем старую должность
          };
          // Обновляем себя же в списке
          this.updateWorker(currentWorker.id, workerPayload).subscribe();
        }
      })
    );
  }

  // --- ОСТАЛЬНЫЕ МЕТОДЫ (Без изменений логики, только стиль) ---

  deleteWorker(workerId: string) {
    this.isLoading.set(true);

    this.api.deleteWorker(workerId).subscribe({
      next: () => {
        // Обновляем список локально (фильтруем удаленного)
        this.workers.update(workers => workers.filter(w => w.id !== workerId));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
        // Тут можно добавить тост с ошибкой
      }
    });
  }

  generateInvite(workerId: string) {
    this.isInviting.set(workerId);

    return this.api.generateInvite(workerId).pipe(
      tap(() => {
        this.isInviting.set(null);
      }),
      catchError((err) => {
        this.isInviting.set(null);
        throw err;
      })
    );
  }

  loadTransactions(workerId: string) {
    this.isTransactionsLoading.set(true);
    this.api.getWorkerTransactions(workerId).subscribe({
      next: (data) => {
        this.transactions.set(data);
        this.isTransactionsLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isTransactionsLoading.set(false);
      }
    });
  }

  loadBalance(workerId: string) {
    this.isBalanceLoading.set(true);
    this.api.getBalance(workerId).subscribe({
      next: (balanceData) => {
        this.balance.set(balanceData);
        this.isBalanceLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isBalanceLoading.set(false);
      }
    });
  }

  uploadBusinessLogo(file: File) {
    this.isBusinessLogoUploading.set(true);
    this.api.uploadBusinessLogo(file).subscribe({
      next: (res) => {
        const newUrl = `${res.url}?t=${Date.now()}`;
        this.business.update(current => current ? { ...current, logoUrl: newUrl } : current);
        this.isBusinessLogoUploading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isBusinessLogoUploading.set(false);
        alert('Chyba pri nahrávaní loga');
      }
    });
  }

  uploadWorkerAvatar(workerId: string, file: File) {
    this.isWorkerAvatarUploading.set(workerId);
    this.api.uploadWorkerAvatar(workerId, file).subscribe({
      next: (res) => {
        const newUrl = `${res.url}?t=${Date.now()}`;
        this.workers.update(list => list.map(w => w.id === workerId ? { ...w, avatarUrl: newUrl } : w));
        this.isWorkerAvatarUploading.set(null);
      },
      error: (err) => {
        console.error(err);
        this.isWorkerAvatarUploading.set(null);
        alert('Chyba pri nahrávaní avatara');
      }
    });
  }

  updateWorker(workerId: string, payload: UpdateWorkerPayload) {
    return this.api.updateWorker(workerId, payload).pipe(
      tap(() => {
        // 1. Обновляем список (если там есть этот воркер)
        this.workers.update(list => list.map(w => {
          if (w.id === workerId) {
            return {
              ...w,
              firstName: payload.firstName,
              lastName: payload.lastName,
              name: `${payload.firstName} ${payload.lastName}`.trim(),
              job: payload.job
            };
          }
          return w;
        }));

        // 2. Обновляем currentWorker (если редактировали самого себя)
        const me = this.currentWorker();
        if (me && me.id === workerId) {
          this.currentWorker.set({
            ...me,
            name: `${payload.firstName} ${payload.lastName}`.trim(),
            job: payload.job
          });
        }
      })
    );
  }

  addWorker(payload: { firstName: string, lastName: string, job: string }) {
    const apiPayload: CreateWorkerPayload = {
      name: `${payload.firstName} ${payload.lastName}`.trim(),
      job: payload.job
    };
    return this.api.createWorker(apiPayload).pipe(
      tap((newWorker) => {
        this.workers.update(list => [...list, newWorker]);
      })
    );
  }

  changeJob(workerId: string, newJob: string) {
    // Тут важный момент:
    // Если мы Owner и меняем кого-то из списка - мы ищем в this.workers()
    // Если мы Worker и меняем себя (теоретически) - мы можем взять из this.currentWorker()

    let worker = this.workers().find(w => w.id === workerId);

    // Если в списке не нашли (например, мы Worker и список пуст), берем из currentWorker
    if (!worker && this.currentWorker()?.id === workerId) {
      worker = this.currentWorker()!;
    }

    if (!worker) return of(null);

    const nameParts = worker.name.split(' ');
    const fName = nameParts[0] || '';
    const lName = nameParts.slice(1).join(' ') || '';

    const payload: UpdateWorkerPayload = {
      firstName: fName,
      lastName: lName,
      job: newJob
    };

    return this.updateWorker(workerId, payload);
  }

  // Метод принимает ID и возвращает ссылку на картинку (blob url)
  getWorkerQr(workerId: string) {
    this.isWorkerQrLoading.set(workerId);

    return this.api.qr(workerId).pipe(
      // Превращаем Blob сразу в URL-строку
      map((blob) => URL.createObjectURL(blob)),
      tap(() => {
        this.isWorkerQrLoading.set(null);
      }),
      catchError((err) => {
        console.error('Ошибка при загрузке QR:', err);
        this.isWorkerQrLoading.set(null);
        return throwError(() => err);
      })
    );
  }

  pay(workerId: string, amount: number) {
    this.isPaying.set(true);
    this.api.pay(workerId, amount).subscribe({
      next: (res: any) => {
        if (res.url) window.location.href = res.url;
        else this.isPaying.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isPaying.set(false);
        alert('Chyba pri vytváraní platby');
      }
    });
  }

  changePassword(data: {oldPassword: string, newPassword: string}) {
    return this.api.changePassword(data.oldPassword, data.newPassword);
  }

  onInitEmail(event: { newEmail: string, onSuccess: () => void }) {
    return this.api.onInitEmail(event);
  }

  onConfirmEmail(event: { newEmail: string, code: string }) {
    return this.api.onConfirmEmail(event);
  }
}
