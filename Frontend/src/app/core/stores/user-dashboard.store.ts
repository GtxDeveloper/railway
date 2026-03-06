import { Injectable, signal, computed, inject } from '@angular/core';
import { catchError, forkJoin, map, of, switchMap, tap, throwError } from 'rxjs';
import { DashboardService } from '../services/dashboard-service';
import {
    Balance,
    ProfileResponse,
    Summary,
    Transaction,
    UserProfilePayload,
    Worker,
} from '../models/dashboard.models';

export interface UserContext {
    userId: string;
    role: 'Owner' | 'Worker';
    businessId: string;
    workerId: string;
}

@Injectable({
    providedIn: 'root'
})
export class UserDashboardStore {
    private api = inject(DashboardService);

    // --- 1. STATE ---

    readonly userContext = signal<UserContext | null>(null);
    readonly currentWorker = signal<Worker | null>(null);
    readonly profile = signal<ProfileResponse | null>(null);

    // Finance
    readonly summary = signal<Summary>({ transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0 });
    readonly balance = signal<Balance>({ currency: 'eur', available: 0, pending: 0 });
    readonly transactions = signal<Transaction[]>([]);

    // Loaders
    readonly isLoading = signal<boolean>(false);
    readonly error = signal<string | null>(null);

    isRedirecting = signal(false);
    isOnboarding = signal(false);
    isQrLoading = signal(false);
    isBalanceLoading = signal(false);
    isTransactionsLoading = signal(false);
    isAvatarUploading = signal(false);
    isGettingPayLink = signal<string | null>(null);

    readonly qrCodeUrl = signal<string | null>(null);

    // --- 2. COMPUTED ---

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

                // Если у работника есть ID, готовим запрос на статистику
                const summaryRequest = context.workerId
                    ? this.api.getWorkerSummary(context.workerId)
                    : of({ transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0 }); // Пустая заглушка

                return forkJoin({
                    profile: this.api.profile(),
                    summary: summaryRequest,
                    currentWorker: meRequest
                });
            })
        ).subscribe({
            next: (data) => {
                this.profile.set(data.profile);
                this.currentWorker.set(data.currentWorker);
                if (data.summary) this.summary.set(data.summary);

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
        this.isGettingPayLink.set(workerId);

        return this.api.getPayLink(workerId).pipe(
            tap(() => {
                this.isGettingPayLink.set(null);
            }),
            catchError((err) => {
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

                // Обновляем профиль (User)
                this.profile.update(current => current ? { ...current, avatarUrl: newUrl } : current);
                this.isAvatarUploading.set(false);

                // Синхронизация: Обновляем аватарку Воркера
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

    private uploadWorkerAvatar(workerId: string, file: File) {
        this.api.uploadWorkerAvatar(workerId, file).subscribe({
            next: () => { },
            error: (err) => console.error('Ошибка фонового обновления аватара', err)
        });
    }

    changeProfile(payload: UserProfilePayload) {
        return this.api.changeProfile(payload).pipe(
            tap(() => {
                this.profile.update(current => current ? { ...current, ...payload } : current);
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

    changePassword(data: { oldPassword: string, newPassword: string }) {
        return this.api.changePassword(data.oldPassword, data.newPassword);
    }

    onInitEmail(event: { newEmail: string, onSuccess: () => void }) {
        return this.api.onInitEmail(event);
    }

    onConfirmEmail(event: { newEmail: string, code: string }) {
        return this.api.onConfirmEmail(event);
    }
}
