import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

export function escapeHtml(text: string): string {
  if (!text) return '';
  return String(text)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

@Injectable({
  providedIn: 'root',
})
export class SweetAlertService {
  confirm(title: string, confirmText: string = 'Yes', cancelText: string = 'No'): Promise<any> {
    return Swal.fire({
      icon: 'question',
      title: title,
      showCancelButton: true,
      confirmButtonText: confirmText,
      cancelButtonText: cancelText,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      reverseButtons: true,
    });
  }

  error(message: string): Promise<any> {
    return Swal.fire({
      toast: true,
      position: 'center',
      icon: 'error',
      title: 'ข้อผิดพลาด',
      text: message,
      showConfirmButton: true,
      confirmButtonText: 'ตกลง',
      confirmButtonColor: '#e32400',
    });
  }

  htmlInfo(title: string, htmlContent: string, width: string = '600px'): Promise<any> {
    return Swal.fire({
      icon: 'info',
      title: title,
      html: htmlContent,
      showConfirmButton: true,
      confirmButtonText: 'ปิด',
      confirmButtonColor: '#3085d6',
      width: width,
    });
  }

  success(message: string): Promise<any> {
    return Swal.fire({
      icon: 'success',
      title: 'สำเร็จ',
      text: message,
      showConfirmButton: false,
      timer: 1000,
      timerProgressBar: true,
      allowOutsideClick: false,
      allowEscapeKey: false,
    });
  }

  warning(message: string): Promise<any> {
    return Swal.fire({
      toast: true,
      position: 'center',
      icon: 'warning',
      title: 'แจ้งเตือน',
      text: message,
      showConfirmButton: true,
      confirmButtonText: 'รับทราบ',
      confirmButtonColor: '#f8b400',
    });
  }
}
