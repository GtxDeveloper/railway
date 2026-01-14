import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmailChangeField } from './email-change-field';

describe('EmailChangeField', () => {
  let component: EmailChangeField;
  let fixture: ComponentFixture<EmailChangeField>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmailChangeField]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmailChangeField);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
