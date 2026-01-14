import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {MessageResponse} from '../models/message.model';
import {
  Balance, BusinessProfile, CreateWorkerPayload,
  LinkResponse,
  ProfileResponse,
  Summary,
  Transaction, UpdateWorkerPayload, UploadResponse, UserProfilePayload,
  WorkersResponse,
  Worker, PublicWorker
} from "../models/dashboard.models";
import {UserContext} from '../stores/dashboard-store';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private http = inject(HttpClient);

  private apiUrl = environment.apiUrl;

  profile(): Observable<ProfileResponse> {
    return this.http.get<ProfileResponse>(`${this.apiUrl}Account/profile`)
  }

  workers(): Observable<WorkersResponse> {
    return this.http.get<WorkersResponse>(`${this.apiUrl}Workers`)
  }

  onboard(id: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}Workers/${id}/onboard`, {})
  }

  qr(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}Workers/${id}/qr`, {
      responseType: 'blob' // <--- ЭТО САМОЕ ВАЖНОЕ
    });
  }

  pay(workerId: string, amount: number, currency: string = "EUR"): Observable<LinkResponse> {
    return this.http.post<LinkResponse>(`${this.apiUrl}Payments/checkout`, {workerId, amount, currency})
  }

  changePassword(oldPassword: string, newPassword: string): Observable<MessageResponse> {
    return  this.http.post<MessageResponse>(`${this.apiUrl}Account/change-password`, {oldPassword, newPassword})
  }

  onInitEmail(event: { newEmail: string, onSuccess: () => void }) {
    return  this.http.post(`${this.apiUrl}Account/change-email/init`, { newEmail: event.newEmail })
  }

  onConfirmEmail(event: { newEmail: string, code: string }) {
    return  this.http.post(`${this.apiUrl}Account/change-email/confirm`, {
      newEmail: event.newEmail,
      code: event.code
    })
  }

  changeProfile(payload: UserProfilePayload): Observable<any> {
    // Отправляем объект целиком.
    // Убедитесь, что имена полей в payload совпадают с DTO на C# бэкенде.
    return this.http.put(`${this.apiUrl}Account/profile`, payload);
  }

  changeJob(id: string ,newJob: string) {
    return this.http.post(`${this.apiUrl}Workers/${id}/job`, {newJob})
  }

  getLoginLink(id: string): Observable<LinkResponse> {
    return this.http.post<LinkResponse>(`${this.apiUrl}Workers/${id}/login-link`, {});
  }

  getSummary(): Observable<Summary> {
    return this.http.get<Summary>(`${this.apiUrl}Dashboard/summary`);
  }

  getBalance(id: string): Observable<Balance> {
    return this.http.get<Balance>(`${this.apiUrl}Dashboard/worker/${id}/balance`,);
  }

  uploadAvatar(file: File): Observable<{ url: string }> {
    const formData = new FormData();
    // 'file' — это имя параметра в вашем C# методе UploadAvatar(IFormFile file)
    formData.append('file', file);

    return this.http.post<{ url: string }>(`${this.apiUrl}Account/avatar`, formData);
  }

  getWorkerTransactions(workerId: string): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${this.apiUrl}Dashboard/worker/${workerId}/transactions`);
  }

  // 1. Получить профиль бизнеса
  getBusiness(): Observable<BusinessProfile> {
    return this.http.get<BusinessProfile>(`${this.apiUrl}Dashboard/business`);
  }

// 2. Загрузить логотип бизнеса
  uploadBusinessLogo(file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadResponse>(`${this.apiUrl}Dashboard/business/avatar`, formData);
  }

// 3. Загрузить аватар работника
  uploadWorkerAvatar(workerId: string, file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadResponse>(`${this.apiUrl}Dashboard/worker/${workerId}/avatar`, formData);
  }

  updateWorker(workerId: string, payload: UpdateWorkerPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}Dashboard/worker/${workerId}`, payload);
  }

  createWorker(payload: CreateWorkerPayload): Observable<any> {
    // Судя по скрину, эндпоинт находится по адресу /api/Workers
    return this.http.post<any>(`${this.apiUrl}Workers`, payload);
  }

  getMe() {
    // Эндпоинт, который мы создали в AccountController
    return this.http.get<UserContext>(`${this.apiUrl}Account/me`);
  }

  getWorkerById(workerId: string) {
    // Вызываем наш новый контроллер WorkersController
    return this.http.get<Worker>(`${this.apiUrl}Workers/${workerId}`);
  }

  generateInvite(workerId: string) {
    // POST запрос, тело пустое, так как workerId в URL
    return this.http.post<{ inviteUrl: string }>(`${this.apiUrl}Dashboard/worker/${workerId}/invite`, {});
  }

  getPayLink(workerId: string) : Observable<LinkResponse> {
    return this.http.get<LinkResponse>(`${this.apiUrl}Workers/${workerId}/pay-link`);
  }
  getWorkerSummary(workerId: string) {
    return this.http.get<Summary>(`${this.apiUrl}Dashboard/${workerId}/summary`);
  }

  getPublicWorker(workerId: string) {
    // Обратите внимание: /public в конце
    return this.http.get<PublicWorker>(`${this.apiUrl}Workers/${workerId}/public`);
  }

  deleteWorker(workerId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}Dashboard/worker/${workerId}`);
  }
}
