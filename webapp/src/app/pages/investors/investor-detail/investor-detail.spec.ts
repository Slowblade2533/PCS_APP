import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvestorDetail } from './investor-detail';

describe('InvestorDetail', () => {
  let component: InvestorDetail;
  let fixture: ComponentFixture<InvestorDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvestorDetail],
    }).compileComponents();

    fixture = TestBed.createComponent(InvestorDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
