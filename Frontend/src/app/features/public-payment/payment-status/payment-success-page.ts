import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="flex flex-col items-center justify-center p-4">
      <div class="bg-white p-8 rounded-2xl shadow-xl max-w-md w-full text-center">

        <div class="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-green-100 mb-6">
          <svg class="h-10 w-10 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
          </svg>
        </div>

        <h2 class="text-3xl raleway font-bold text-gray-900 mb-4">{{ 'PAYMENT_SUCCESS.TITLE' | translate }}</h2>
        <p class="text-gray-600 raleway mb-8 text-lg">
          {{ 'PAYMENT_SUCCESS.MESSAGE' | translate }}
        </p>
      </div>
    </div>
  `
})
export class PaymentSuccessComponent {

}
