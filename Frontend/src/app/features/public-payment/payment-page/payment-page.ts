import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {PaymentStore} from '../../../core/stores/payment-store';
import {TranslatePipe} from '@ngx-translate/core';


@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './payment-page.html',
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Quicksand:wght@500;700&display=swap');
    @import url('https://fonts.googleapis.com/css2?family=Raleway:wght@700&display=swap');

    .font-quicksand { font-family: 'Quicksand', sans-serif; }
    .raleway { font-family: 'Raleway', sans-serif; }

    /* Убираем стрелки в Chrome, Safari, Edge, Opera */
    .no-spin::-webkit-outer-spin-button,
    .no-spin::-webkit-inner-spin-button {
      -webkit-appearance: none;
      margin: 0;
    }

    /* Убираем стрелки в Firefox */
    .no-spin {
      -moz-appearance: textfield;
    }
  `]
})
export class PaymentPage implements OnInit {
  private route = inject(ActivatedRoute);

  // Используем PaymentStore вместо DashboardStore
  store = inject(PaymentStore);

  amount = signal<number | null>(null);
  note = signal<string>('');

  presets = [2, 5, 10, 20]; // Немного увеличил пресеты, стандартная практика

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      // Загружаем данные работника сразу при входе
      this.store.loadWorker(id);
    }
  }

  selectAmount(value: number) {
    this.amount.set(value);
  }

  pay() {
    const worker = this.store.worker();
    const amountVal = this.amount();
    const noteVal = this.note();

    if (!worker || !amountVal) {
      return;
    }

    // Вызываем метод оплаты из стора
    this.store.pay(worker.id, amountVal, noteVal);
  }
}
