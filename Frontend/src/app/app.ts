import {Component, inject, signal} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {Locale, SettingsService} from './core/services/settings-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  standalone: true,
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('tringelty-frontend');
  public settings = inject(SettingsService);


  onLocaleChange(event: Event): void {
    const locale = (event.target as HTMLSelectElement).value as Locale;
    this.settings.setLocale(locale);
  }
}
