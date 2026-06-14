import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const studentGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isStudent()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const parentGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isParent()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const teacherGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && (auth.isTeacher() || auth.isAdmin())) return true;
  return router.createUrlTree(['/auth/login']);
};

export const schoolGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && (auth.isSchoolAdmin() || auth.isTeacher() || auth.isAdmin())) return true;
  return router.createUrlTree(['/auth/login']);
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isAdmin()) return true;
  return router.createUrlTree(['/auth/login']);
};
