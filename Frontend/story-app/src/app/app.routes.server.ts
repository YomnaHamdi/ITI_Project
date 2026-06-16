import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'lessons/:id',                  renderMode: RenderMode.Server },
  { path: 'levels/:id/books',             renderMode: RenderMode.Server },
  { path: 'books/:id/read',               renderMode: RenderMode.Server },
  { path: 'books/:id/quiz-result',        renderMode: RenderMode.Client },
  { path: 'teacher/students/:name',       renderMode: RenderMode.Server },
  { path: 'parent/child/:name/progress',  renderMode: RenderMode.Server },
  { path: 'writing-practice',             renderMode: RenderMode.Client },
  { path: 'test/question',                renderMode: RenderMode.Client },
  { path: 'test/result',                  renderMode: RenderMode.Client },
  { path: 'auth/create-student',          renderMode: RenderMode.Client },
  { path: '**',                           renderMode: RenderMode.Prerender }
];
