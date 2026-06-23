import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvestmentDetail } from './investment-detail';

describe('InvestmentDetail', () => {
  let component: InvestmentDetail;
  let fixture: ComponentFixture<InvestmentDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvestmentDetail],
    }).compileComponents();

    fixture = TestBed.createComponent(InvestmentDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
