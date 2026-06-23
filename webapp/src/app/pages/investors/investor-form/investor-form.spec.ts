import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvestorForm } from './investor-form';

describe('InvestorForm', () => {
  let component: InvestorForm;
  let fixture: ComponentFixture<InvestorForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvestorForm],
    }).compileComponents();

    fixture = TestBed.createComponent(InvestorForm);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
