import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {AdminStore} from '../../core/stores/admin-store';
import {TranslatePipe} from '@ngx-translate/core';


@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './admin-page.html',
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Quicksand:wght@500;700&family=Raleway:wght@700;800&display=swap');

    .font-raleway { font-family: 'Raleway', sans-serif; }
    .font-quicksand { font-family: 'Quicksand', sans-serif; }
  `]
})
export class AdminPageComponent implements OnInit {
  store = inject(AdminStore);

  // Сигнал для текущей вкладки: 'dashboard' или 'transactions'
  activeTab = signal<'dashboard' | 'transactions'>('dashboard');

  ngOnInit() {
    this.store.loadDashboardData();
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();
  }
}
