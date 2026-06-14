import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

export interface AuthResponse {
  token:     string;
  userId:    string;
  name:      string;
  role:      string;
  expiresAt: string;
}

export interface StudentAuthResponse {
  token:         string;
  studentId:     string;
  name:          string;
  level:         number;
  placementDone: boolean;
  expiresAt:     string;
}

export interface StudentSummary {
  id:            string;
  name:          string;
  age:           number;
  username:      string;
  level:         number;
  placementDone: boolean;
  avatarUrl:     string | null;
}

export interface CreateStudentRequest {
  name:      string;
  age:       number;
  username:  string;
  imagePin1: number;
  imagePin2: number | null;
  level:     number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http   = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly api    = environment.apiUrl;

  private readonly TOKEN_KEY = 'lughati_token';
  private readonly USER_KEY  = 'lughati_user';

  // Signals
  private readonly _token = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(this.TOKEN_KEY) : null
  );
  private readonly _user = signal<AuthResponse | StudentAuthResponse | null>(
    this.loadUserFromStorage()
  );

  readonly isLoggedIn = computed(() => this._token() !== null);
  readonly currentUser = this._user.asReadonly();
  readonly token = this._token.asReadonly();

  readonly userRole = computed(() => {
    const u = this._user();
    if (!u) return '';
    return 'role' in u ? u.role : 'student';
  });

  readonly isStudent     = computed(() => this.userRole() === 'student');
  readonly isParent      = computed(() => this.userRole() === 'parent');
  readonly isTeacher     = computed(() => this.userRole() === 'teacher');
  readonly isSchoolAdmin = computed(() => this.userRole() === 'schooladmin');
  readonly isAdmin       = computed(() =>
    this.userRole() === 'systemadmin' || this.userRole() === 'admin');

  // ── Adult register ─────────────────────────────────────────────────────────
  register(body: { fullName: string; email: string; password: string; role: string; schoolCode?: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/api/auth/register`, body).pipe(
      tap(res => this.persistSession(res))
    );
  }

  // ── Adult login ────────────────────────────────────────────────────────────
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/api/auth/login`, { email, password }).pipe(
      tap(res => this.persistSession(res))
    );
  }

  // ── Student login (username + image PIN) ───────────────────────────────────
  studentLogin(username: string, imagePin1: number, imagePin2: number | null): Observable<StudentAuthResponse> {
    return this.http.post<StudentAuthResponse>(`${this.api}/api/auth/students/login`, {
      username, imagePin1, imagePin2
    }).pipe(
      tap(res => this.persistStudentSession(res))
    );
  }

  // ── Create student (called by logged-in parent/teacher) ────────────────────
  createStudent(req: CreateStudentRequest): Observable<StudentAuthResponse> {
    return this.http.post<StudentAuthResponse>(`${this.api}/api/auth/students`, req);
  }

  // ── List children / students ───────────────────────────────────────────────
  getMyStudents(): Observable<StudentSummary[]> {
    return this.http.get<StudentSummary[]>(`${this.api}/api/auth/students`);
  }

  // ── Logout ─────────────────────────────────────────────────────────────────
  logout(): void {
    this._token.set(null);
    this._user.set(null);
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
      localStorage.removeItem('lughati_child');
    }
    this.router.navigate(['/auth/login']);
  }

  // ── Internal helpers ───────────────────────────────────────────────────────
  private persistSession(res: AuthResponse): void {
    this._token.set(res.token);
    this._user.set(res);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.TOKEN_KEY, res.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(res));
    }
  }

  private persistStudentSession(res: StudentAuthResponse): void {
    this._token.set(res.token);
    this._user.set(res);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.TOKEN_KEY, res.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify({ ...res, role: 'student' }));
    }
  }

  private loadUserFromStorage(): AuthResponse | StudentAuthResponse | null {
    try {
      if (typeof localStorage === 'undefined') return null;
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }
}
