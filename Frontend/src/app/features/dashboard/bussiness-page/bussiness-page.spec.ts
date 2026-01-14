import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BussinessPage } from './bussiness-page';

describe('BussinessPage', () => {
  let component: BussinessPage;
  let fixture: ComponentFixture<BussinessPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BussinessPage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BussinessPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
