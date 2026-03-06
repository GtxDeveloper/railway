import { Injectable, signal, computed, inject } from '@angular/core';
import { catchError, forkJoin, map, of, switchMap, tap, throwError } from 'rxjs';
import { DashboardService } from '../services/dashboard-service';
import {
    BusinessProfile,
    CreateWorkerPayload,
    Summary,
    UpdateWorkerPayload,
    Worker
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
export class BusinessDashboardStore {
    private api = inject(DashboardService);

    // --- 1. STATE ---

    readonly userContext = signal<UserContext | null>(null);
    readonly workers = signal<Worker[]>([]);
    readonly business = signal<BusinessProfile | null>(null);
    readonly businessSummary = signal<Summary>({ transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0 });

    readonly isInviting = signal<string | null>(null);
    readonly isWorkerQrLoading = signal<string | null>(null);
    readonly isLoading = signal<boolean>(false);
    readonly error = signal<string | null>(null);
    readonly isBusinessLogoUploading = signal(false);
    readonly isWorkerAvatarUploading = signal<string | null>(null);

    // --- 2. COMPUTED ---

    readonly isOwner = computed(() => this.userContext()?.role === 'Owner');

    // --- 3. METHODS ---

    loadAll() {
        this.isLoading.set(true);
        this.error.set(null);

        // 1. Узнаем Context (Кто я?)
        this.api.getMe().pipe(
            tap((context) => this.userContext.set(context)),
            switchMap((context) => {

                // === ЛОГИКА ДЛЯ ВЛАДЕЛЬЦА ===
                if (context.role === 'Owner') {
                    return forkJoin({
                        workers: this.api.workers(),
                        businessSummary: this.api.getSummary(),
                        business: this.api.getBusiness(),
                    });
                }

                // Если не владелец - возвращаем пустые данные (в идеале, не должно вызываться)
                return of({
                    workers: [],
                    businessSummary: { transactionsCount: 0, todayEarnings: 0, monthEarnings: 0, totalEarnings: 0 },
                    business: null,
                });
            })
        ).subscribe({
            next: (data) => {
                this.workers.set(data.workers || []);
                if (data.business) this.business.set(data.business);
                if (data.businessSummary) this.businessSummary.set(data.businessSummary);

                this.isLoading.set(false);
            },
            error: (err) => {
                console.error(err);
                this.error.set('Chyba pri načítaní údajov podniku');
                this.isLoading.set(false);
            }
        });
    }

    deleteWorker(workerId: string) {
        this.isLoading.set(true);

        this.api.deleteWorker(workerId).subscribe({
            next: () => {
                this.workers.update(workers => workers.filter(w => w.id !== workerId));
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error(err);
                this.isLoading.set(false);
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

    getWorkerQr(workerId: string) {
        this.isWorkerQrLoading.set(workerId);

        return this.api.qr(workerId).pipe(
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
}
