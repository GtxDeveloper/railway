import {Component, inject, OnDestroy, OnInit, signal} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-onboarding-success',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="flex flex-col items-center justify-center  p-4">
      <div class="bg-white p-8 rounded-2xl shadow-xl max-w-md w-full text-center">

        <div class="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-green-100 mb-6">
          <svg class="h-10 w-10 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
          </svg>
        </div>

        <h2 class="text-3xl raleway font-bold text-gray-900 mb-4 raleway">{{ 'ONBOARD_SUCCESS.TITLE' | translate }}</h2>
        <p class="text-gray-600 raleway mb-8 text-lg">
          {{ 'ONBOARD_SUCCESS.MESSAGE' | translate }}
        </p>

        <button
          (click)="navigateNow()"
          class="w-full bg-[#ffc800] text-black font-bold py-4 rounded-full hover:bg-white border-3 border-[#ffc800] transition-colors ease-in hover:text-[#ffc800] raleway text-xl mb-4">
          {{ 'ONBOARD_SUCCESS.BUTTON' | translate }}
        </button>

        <p class="text-gray-400 raleway text-sm">
          {{ 'ONBOARD_SUCCESS.REDIRECT_MSG' | translate:{ seconds: timeLeft() } }}
        </p>
      </div>
    </div>
  `
})
export class OnboardingSuccessComponent implements OnInit, OnDestroy {
  private router = inject(Router);

  timeLeft = signal<number>(10);
  private intervalId: any;

  ngOnInit() {
    // Запускаем таймер обратного отсчета
    this.intervalId = setInterval(() => {
      this.timeLeft.set(this.timeLeft() - 1);

      if (this.timeLeft() <= 0) {
        this.navigateNow();
      }
    }, 1000);
  }

  navigateNow() {
    // Очищаем таймер, чтобы он не сработал после перехода
    this.clearTimer();
    this.router.navigate(['/dashboard']);
  }

  ngOnDestroy() {
    // Обязательно очищаем таймер, если пользователь ушел со страницы сам
    this.clearTimer();
  }

  private clearTimer() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }
}
