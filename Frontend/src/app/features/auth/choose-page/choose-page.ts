import { Component } from '@angular/core';
import {RouterLink} from '@angular/router';
import {TranslatePipe} from '@ngx-translate/core';

@Component({
  selector: 'app-choose-page',
  imports: [
    RouterLink,
    TranslatePipe
  ],
  standalone: true,
  templateUrl: './choose-page.html',
})
export class ChoosePage {

}
