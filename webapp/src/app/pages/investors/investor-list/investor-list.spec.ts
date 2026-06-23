import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvestorList } from './investor-list';

describe('InvestorList', () => {
  let component: InvestorList;
  let fixture: ComponentFixture<InvestorList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvestorList],
    }).compileComponents();

    fixture = TestBed.createComponent(InvestorList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
