import { Component, inject } from '@angular/core';
import {TranslatePipe} from '@ngx-translate/core';
import {SettingsService} from '../../core/services/settings-service';

@Component({
  selector: 'app-footer',
  imports: [
    TranslatePipe
  ],
  standalone: true,
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class Footer {
  private settings = inject(SettingsService);

  // Ссылка на условия зависит от текущего языка
  get termsUrl(): string {
    return this.settings.locale() === 'en'
      ? 'https://tringelty.com/en/business-conditions-en/'
      : 'https://tringelty.com/bussiness-conditions/';
  }
}
