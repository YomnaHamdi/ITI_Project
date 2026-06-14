import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.';

      if (error.error?.error) {
        message = error.error.error;
      } else if (error.status === 0) {
        message = 'تعذّر الاتصال بالخادم. تأكد من تشغيل الخادم.';
      } else if (error.status === 422) {
        message = 'فشل إنشاء القصة. يرجى المحاولة مرة أخرى.';
      }

      return throwError(() => new Error(message));
    })
  );
};