export interface Result<T> {
  isSuccess: boolean;
  data?: T;
  value?: T;
  error?: string;
  errorMessage?: string;
}
