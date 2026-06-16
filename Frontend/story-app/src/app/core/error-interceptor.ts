import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError, of } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 404 - return empty data instead of crashing
      if (error.status === 404) {
        console.warn('⚠️ API 404 Not Found:', req.url);

        // For dashboard endpoints, return default empty data
        if (req.url.includes('/dashboard/student/')) {
          return of({
            name: '',
            level: 1,
            lessonsCompleted: 0,
            totalLessons: 0,
            percentage: 0,
            currentStreak: 0,
            storiesRead: 0,
            stars: 0,
            examsCompleted: 0,
            avgScore: 0,
            weeklyActivity: [0,0,0,0,0,0,0],
            achievements: [],
            recentActivity: [],
            recentLessons: []
          } as any);
        }

        // For other 404s, return empty array or object
        if (req.method === 'GET') {
          return of([] as any);
        }
      }

      let message = 'حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.';

      if (error.error?.error) {
        message = error.error.error;
      } else if (error.status === 0) {
        message = 'تعذّر الاتصال بالخادم. تأكد من تشغيل الخادم.';
      } else if (error.status === 401) {
        message = 'انتهت صلاحية الجلسة. يرجى تسجيل الدخول مرة أخرى.';
      } else if (error.status === 422) {
        message = 'فشل إنشاء القصة. يرجى المحاولة مرة أخرى.';
      }

      return throwError(() => new Error(message));
    })
  );
};
