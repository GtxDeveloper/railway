import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminBusiness, AdminStats, AdminTransaction } from '../models/admin.models';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  // Предполагаем, что apiUrl = 'https://api.tringelty.com/api/'
  private apiUrl = `${environment.apiUrl}Admin`;

  getStats(): Observable<AdminStats> {
    return this.http.get<AdminStats>(`${this.apiUrl}/stats`);
  }

  getTransactions(): Observable<AdminTransaction[]> {
    return this.http.get<AdminTransaction[]>(`${this.apiUrl}/transactions`);
  }

  getBusinesses(): Observable<AdminBusiness[]> {
    return this.http.get<AdminBusiness[]>(`${this.apiUrl}/businesses`);
  }

  getWorkerPayLink(workerId: string): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`${this.apiUrl}/workers/${workerId}/link`);
  }

  // 2. Получение QR-кода (BLOB - Бинарные данные)
  getWorkerQr(workerId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/workers/${workerId}/qr`, {
      responseType: 'blob' // <--- КРИТИЧЕСКИ ВАЖНО
    });
  }
}
