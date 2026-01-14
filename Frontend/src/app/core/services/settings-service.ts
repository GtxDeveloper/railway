import { Injectable, signal, effect, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type Locale = 'sk' | 'en';


@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  private translate = inject(TranslateService);

  // --- СИГНАЛЫ ---
  public locale = signal<Locale>(
    (localStorage.getItem('appLocale') as Locale | null) || 'sk' // 'ua' по умолчанию
  );

  constructor() {
    // Устанавливаем запасной язык
    this.translate.setDefaultLang('en');

    // --- ЭФФЕКТ ДЛЯ СМЕНЫ ЯЗЫКА ---
    effect(() => {
      const newLocale = this.locale();
      localStorage.setItem('appLocale', newLocale);
      //  Команда для смены языка во всем приложении
      this.translate.use(newLocale);
    });
  }

  // --- Публичные методы для изменения ---

  public setLocale(newLocale: Locale): void {
    this.locale.set(newLocale);
  }
}
