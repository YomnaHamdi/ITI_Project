import { Routes } from '@angular/router';
import { authGuard, studentGuard, parentGuard, teacherGuard, schoolGuard, adminGuard } from './core/auth.guard';

export const routes: Routes = [
  // ── Public ──────────────────────────────────────────────────────────────────
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'auth/create-student',
    loadComponent: () =>
      import('./features/auth/create-student/create-student.component').then(m => m.CreateStudentComponent),
    canActivate: [authGuard]
  },

  // ── Placement Test ───────────────────────────────────────────────────────────
  {
    path: 'test',
    loadComponent: () =>
      import('./features/placement/welcome/placement-welcome.component')
        .then(m => m.PlacementWelcomeComponent)
  },
  {
    path: 'test/question',
    loadComponent: () =>
      import('./features/placement/question/placement-question.component')
        .then(m => m.PlacementQuestionComponent)
  },
  {
    path: 'test/result',
    loadComponent: () =>
      import('./features/placement/result/placement-result.component')
        .then(m => m.PlacementResultComponent)
  },

  // ── Student ──────────────────────────────────────────────────────────────────
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboards/student-dashboard/student-dashboard.component')
        .then(m => m.StudentDashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'levels',
    loadComponent: () =>
      import('./features/levels/levels.component').then(m => m.LevelsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'levels/:id/books',
    loadComponent: () =>
      import('./features/books/books.component').then(m => m.BooksComponent),
    canActivate: [authGuard]
  },
  {
    path: 'books/:id/read',
    loadComponent: () =>
      import('./features/story-reader/story-reader').then(m => m.StoryReaderComponent),
    canActivate: [authGuard]
  },
  {
    path: 'writing-practice',
    loadComponent: () =>
      import('./features/writing-practice/writing-practice').then(m => m.WritingPracticeComponent),
    canActivate: [authGuard]
  },

  // ── Student sub-pages ────────────────────────────────────────────────────────
  {
    path: 'progress',
    loadComponent: () =>
      import('./features/student/progress/progress.component').then(m => m.ProgressComponent),
    canActivate: [authGuard]
  },
  {
    path: 'my-stories',
    loadComponent: () =>
      import('./features/student/my-stories/my-stories.component').then(m => m.MyStoriesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'achievements',
    loadComponent: () =>
      import('./features/student/achievements/achievements.component').then(m => m.AchievementsComponent),
    canActivate: [authGuard]
  },

  // ── Student lesson views ─────────────────────────────────────────────────────
  {
    path: 'generate-lesson',
    loadComponent: () =>
      import('./features/student/generate-lesson/generate-lesson')
        .then(m => m.GenerateLessonComponent),
    canActivate: [authGuard]
  },
  {
    path: 'my-lessons',
    loadComponent: () =>
      import('./features/student/my-lessons/my-lessons')
        .then(m => m.MyLessonsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'assigned-lessons',
    loadComponent: () =>
      import('./features/student/assigned-lessons/assigned-lessons')
        .then(m => m.AssignedLessonsComponent),
    canActivate: [authGuard]
  },

  // ── Quiz Result ──────────────────────────────────────────────────────────────
  {
    path: 'books/:id/quiz-result',
    loadComponent: () =>
      import('./features/quiz-result/quiz-result.component').then(m => m.QuizResultComponent),
    canActivate: [authGuard]
  },

  // ── Story Generator ──────────────────────────────────────────────────────────
  {
    path: 'generate-story',
    loadComponent: () =>
      import('./features/story-generator/story-generator').then(m => m.StoryGeneratorComponent),
    canActivate: [authGuard]
  },

  // ── AI Story Wizard ──────────────────────────────────────────────────────────
  {
    path: 'ai-story',
    loadComponent: () =>
      import('./features/ai-story-wizard/ai-story-wizard.component')
        .then(m => m.AiStoryWizardComponent)
  },

  // ── Lessons ──────────────────────────────────────────────────────────────────
  {
    path: 'lessons/:id',
    loadComponent: () =>
      import('./features/lesson-reader/lesson-reader').then(m => m.LessonReaderComponent)
  },

  // ── Parent ───────────────────────────────────────────────────────────────────
  {
    path: 'parent/dashboard',
    loadComponent: () =>
      import('./features/dashboards/parent-dashboard/parent-dashboard.component')
        .then(m => m.ParentDashboardComponent),
    canActivate: [parentGuard]
  },
  {
    path: 'parent/children',
    loadComponent: () =>
      import('./features/parent/children/parent-children.component').then(m => m.ParentChildrenComponent),
    canActivate: [parentGuard]
  },
  {
    path: 'parent/notifications',
    loadComponent: () =>
      import('./features/parent/notifications/parent-notifications.component').then(m => m.ParentNotificationsComponent),
    canActivate: [parentGuard]
  },
  {
    path: 'parent/child/:name/progress',
    loadComponent: () =>
      import('./features/parent/child-progress/child-progress.component').then(m => m.ChildProgressComponent),
    canActivate: [parentGuard]
  },

  // ── Teacher ──────────────────────────────────────────────────────────────────
  {
    path: 'teacher/students',
    loadComponent: () =>
      import('./features/dashboards/teacher-dashboard/teacher-dashboard.component')
        .then(m => m.TeacherDashboardComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/ai-generator',
    loadComponent: () =>
      import('./features/teacher/ai-generator/ai-generator.component')
        .then(m => m.AiGeneratorComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/lessons/create',
    loadComponent: () =>
      import('./features/teacher/lesson-create/lesson-create.component')
        .then(m => m.LessonCreateComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/reports',
    loadComponent: () =>
      import('./features/teacher/reports/teacher-reports.component')
        .then(m => m.TeacherReportsComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/lessons',
    loadComponent: () =>
      import('./features/teacher/lessons/teacher-lessons.component').then(m => m.TeacherLessonsComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/students/:name',
    loadComponent: () =>
      import('./features/teacher/student-detail/student-detail.component').then(m => m.StudentDetailComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/groups',
    loadComponent: () =>
      import('./features/teacher/groups/teacher-groups')
        .then(m => m.TeacherGroupsComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/classes',
    loadComponent: () =>
      import('./features/teacher/teacher-classes/teacher-classes.component')
        .then(m => m.TeacherClassesComponent),
    canActivate: [teacherGuard]
  },

  // ── School ───────────────────────────────────────────────────────────────────
  {
    path: 'school/dashboard',
    loadComponent: () =>
      import('./features/dashboards/school-dashboard/school-dashboard.component')
        .then(m => m.SchoolDashboardComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/teachers',
    loadComponent: () =>
      import('./features/school/teachers/school-teachers.component').then(m => m.SchoolTeachersComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/classrooms',
    loadComponent: () =>
      import('./features/school/classrooms/school-classrooms.component').then(m => m.SchoolClassroomsComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/reports',
    loadComponent: () =>
      import('./features/school/reports/school-reports.component').then(m => m.SchoolReportsComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/subscription',
    loadComponent: () =>
      import('./features/school/subscription/school-subscription.component').then(m => m.SchoolSubscriptionComponent),
    canActivate: [schoolGuard]
  },

  // ── Exam ──────────────────────────────────────────────────────────────────────
  {
    path: 'exam',
    loadComponent: () =>
      import('./features/exam/exam').then(m => m.Exam),
    canActivate: [authGuard]
  },

  // ── Lessons list (letter-books) ────────────────────────────────────────────
  {
    path: 'lessons-list',
    loadComponent: () =>
      import('./features/lessons-list/lessons-list')
        .then(m => m.LessonsListComponent),
    canActivate: [authGuard]
  },

  // ── Admin ─────────────────────────────────────────────────────────────────────
  {
    path: 'admin/rag',
    loadComponent: () =>
      import('./features/admin-rag/admin-rag.component')
        .then(m => m.AdminRagComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/pdf-library',
    loadComponent: () =>
      import('./features/admin-pdf-library/admin-pdf-library.component')
        .then(m => m.AdminPdfLibraryComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/content',
    loadComponent: () =>
      import('./features/admin/content/admin-content.component')
        .then(m => m.AdminContentComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/books',
    loadComponent: () =>
      import('./features/admin-import/admin-import')
        .then(m => m.AdminImportComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/ai-settings',
    loadComponent: () =>
      import('./features/admin/ai-settings/ai-settings.component')
        .then(m => m.AiSettingsComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/subscriptions',
    loadComponent: () =>
      import('./features/admin/subscriptions/subscriptions.component')
        .then(m => m.SubscriptionsComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/users',
    loadComponent: () =>
      import('./features/admin/users/admin-users.component')
        .then(m => m.AdminUsersComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/schools',
    loadComponent: () =>
      import('./features/admin/schools/admin-schools.component')
        .then(m => m.AdminSchoolsComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/stories',
    loadComponent: () =>
      import('./features/admin/stories/admin-stories.component')
        .then(m => m.AdminStoriesComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/rag-chunks',
    loadComponent: () =>
      import('./features/admin-rag-chunks/admin-rag-chunks')
        .then(m => m.AdminRagChunksComponent),
    canActivate: [adminGuard]
  },

  // ── Utility ───────────────────────────────────────────────────────────────────
  {
    path: 'settings',
    loadComponent: () =>
      import('./features/settings/settings.component').then(m => m.SettingsComponent)
  },
  {
    path: 'upgrade',
    loadComponent: () =>
      import('./features/upgrade/upgrade.component').then(m => m.UpgradeComponent)
  },

  // ── 404 (must be last) ────────────────────────────────────────────────────────
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then(m => m.NotFoundComponent)
  },
];
