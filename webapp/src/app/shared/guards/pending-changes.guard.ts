import { CanDeactivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { HasUnsavedChanges } from './has-unsaved-changes.interface';
import { SweetAlertService } from '../services/sweet-alert.service';
import { isObservable, firstValueFrom } from 'rxjs';

export const pendingChangesGuard: CanDeactivateFn<HasUnsavedChanges> = async (component) => {
  const swal = inject(SweetAlertService);
  let hasChanges = false;

  if (typeof component.hasUnsavedChanges === 'function') {
    const result = component.hasUnsavedChanges();
    if (isObservable(result)) {
      hasChanges = await firstValueFrom(result);
    } else {
      hasChanges = await Promise.resolve(result);
    }
  }

  if (hasChanges) {
    const result = await swal.confirm(
      'คุณมีข้อมูลที่ยังไม่ได้บันทึก',
      'ตกลง (ออก)',
      'ยกเลิก (อยู่ต่อ)',
    );
    return result.isConfirmed;
  }
  return true;
};
