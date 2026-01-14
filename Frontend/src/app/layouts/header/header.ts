import {Component, inject, signal} from '@angular/core';
import {RouterLink} from "@angular/router";
import {AuthStore} from "../../core/stores/auth-store";
import {DashboardStore} from '../../core/stores/dashboard-store';
import {Locale, SettingsService} from '../../core/services/settings-service';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-header',
  imports: [
    RouterLink,
    TranslatePipe
  ],
  standalone: true,
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {

   store = inject(AuthStore);
   dashboardStore = inject(DashboardStore);
  settings = inject(SettingsService);

  // Сигнал для открытия/закрытия мобильного меню
  isMenuOpen = signal(false);

  // Метод переключения
  toggleMenu() {
    this.isMenuOpen.update(v => !v);
  }

  // Метод закрытия (нужен при клике на ссылку)
  closeMenu() {
    this.isMenuOpen.set(false);
  }

  switchLang(lang: Locale) {
    this.settings.setLocale(lang);
    // Опционально: закрыть меню на мобильных после выбора языка
    // this.closeMenu();
  }
}
