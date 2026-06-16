import { Observable } from 'rxjs';

export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean | Observable<boolean> | Promise<boolean>;
}
