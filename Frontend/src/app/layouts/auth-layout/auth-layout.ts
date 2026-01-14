import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {Footer} from '../footer/footer';
import {Header} from '../header/header';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [RouterOutlet, Footer, Header],
  template: `
    <app-header></app-header>
    <div class="min-h-[calc(100vh-92px)] bg-[#27a19b] flex flex-col justify-center items-center p-4">
      <div class="w-full">
        <router-outlet></router-outlet>
      </div>
    </div>
    <app-footer></app-footer>
  `
})
export class AuthLayoutComponent {
}
