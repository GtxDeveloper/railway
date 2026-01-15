import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  RefreshRequest,
  RegisterRequest, ResetPasswordRequest,
  VerifyRequest
} from '../models/auth.models';
import {Observable} from 'rxjs';
import {MessageResponse} from '../models/message.model';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);

  private apiUrl = environment.apiUrl;

  register(request : RegisterRequest) : Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}Auth/register`, request)
  }

  verify(request : VerifyRequest) : Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}Auth/verify-email`, request)
  }

  login(request : LoginRequest) : Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}Auth/login`, request)
  }

  refresh(request : RefreshRequest) : Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}Auth/refresh`, request)
  }

  logout() : Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}Auth/logout`, {})
  }

  forgotPassword(request: ForgotPasswordRequest) : Observable<MessageResponse> {
      return this.http.post<MessageResponse>(`${this.apiUrl}Auth/forgot-password`, request)
  }

  resetPassword(request : ResetPasswordRequest) : Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}Auth/reset-password`, request)
  }
}
