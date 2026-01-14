import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PasswordChangeField } from './password-change-field';

describe('PasswordChangeField', () => {
  let component: PasswordChangeField;
  let fixture: ComponentFixture<PasswordChangeField>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PasswordChangeField]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PasswordChangeField);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
