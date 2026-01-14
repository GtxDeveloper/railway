import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProfileEditField } from './profile-edit-field';

describe('ProfileEditField', () => {
  let component: ProfileEditField;
  let fixture: ComponentFixture<ProfileEditField>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfileEditField]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfileEditField);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
