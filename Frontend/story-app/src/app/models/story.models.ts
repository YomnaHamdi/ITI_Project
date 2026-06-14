// ── Quiz Type ─────────────────────────────────────────────────────────────────

export type QuizType = 0 | 1 | 2 | 3;
export const QuizType = {
  MCQ:      0 as QuizType,
  Matching: 1 as QuizType,
  DragDrop: 2 as QuizType,
  Ordering: 3 as QuizType,
};

// ── Story (AI-generated) ───────────────────────────────────────────────────────

export interface GenerateStoryRequest {
  childName: string;
  character: string;
  theme:     string;
}

export interface StoryPage {
  pageId:     string;
  pageNumber: number;
  sentence:   string;
  imageUrl:   string;
  isUnlocked: boolean;
}

export interface StoryResponse {
  id:         string;
  title:      string;
  isApproved: boolean;
  pages:      StoryPage[];
}

// ── Lesson (PDF-imported or RAG-generated) ────────────────────────────────────

export interface LessonPage {
  pageId:      string;
  pageNumber:  number;
  sentence:    string;
  imageUrl:    string;
  isUnlocked:  boolean;
  isCoverPage: boolean;
}

export interface LessonSummary {
  id:            string;
  level:         number;
  letter:        string;
  letterName:    string;
  title:         string;
  coverImageUrl: string;
  pageCount:     number;
}

export interface LessonDetail {
  id:            string;
  level:         number;
  letter:        string;
  letterName:    string;
  title:         string;
  coverImageUrl: string;
  pages:         LessonPage[];
}

export interface ImportBookResponse {
  id:         string;
  title:      string;
  level:      number;
  letter:     string;
  letterName: string;
  pageCount:  number;
}

// ── Admin Book Management ─────────────────────────────────────────────────────

export interface AdminBooksPageDto {
  items:      LessonSummary[];
  totalCount: number;
  page:       number;
  pageSize:   number;
  totalPages: number;
}

export interface ManualPageDto {
  sentence: string;
}

export interface CreateManualBookRequest {
  title:      string;
  letterName: string;
  letter:     string;
  level:      number;
  pages:      ManualPageDto[];
}

// ── Exam ───────────────────────────────────────────────────────────────────────

export interface MatchPair {
  left:  string;
  right: string;
}

export interface QuestionDto {
  questionId:     string;
  questionNumber: number;
  type:           QuizType;
  text:           string;
  optionA?:       string;
  optionB?:       string;
  optionC?:       string;
  optionD?:       string;
  dataJson?:      string;
}

export interface ExamResponse {
  examId:    string;
  storyId:   string;
  questions: QuestionDto[];
}

export interface SubmitAnswer {
  questionId:   string;
  chosenAnswer: string;
}

export interface SubmitExamRequest {
  examId:    string;
  childName: string;
  answers:   SubmitAnswer[];
}

export interface AnswerFeedback {
  questionId:    string;
  type:          QuizType;
  chosenAnswer:  string;
  correctAnswer: string;
  isCorrect:     boolean;
}

export interface ExamResult {
  totalQuestions:  number;
  correctAnswers:  number;
  scorePercentage: number;
  feedback:        AnswerFeedback[];
}

// ── Writing ────────────────────────────────────────────────────────────────────

export interface WritingCorrectionResponse {
  extractedText:    string;
  expectedSentence: string;
  similarityScore:  number;
  isAccepted:       boolean;
  message:          string;
}

// ── Progress ───────────────────────────────────────────────────────────────────

export interface ProgressResponse {
  storyId:         string;
  childName:       string;
  currentPage:     number;
  totalQuestions:  number;
  correctAnswers:  number;
  scorePercentage: number;
  examCompleted:   boolean;
}

// ── RAG / Knowledge ───────────────────────────────────────────────────────────

export interface KnowledgeDocumentDto {
  id:           string;
  fileName:     string;
  documentType: string;
  letter?:      string;
  level?:       number;
  tags?:        string;
  chunkCount:   number;
  ingestedAt:   string;
}

export interface RagSearchResult {
  chunkText:  string;
  score:      number;
  sourceFile: string;
  letter?:    string;
  level?:     number;
}

export interface GenerateLessonRequest {
  topic:      string;
  letter?:    string;
  level?:     number;
  childName?: string;
}

export interface IngestDocumentResponse {
  documentId: string;
  fileName:   string;
  chunkCount: number;
  message:    string;
}

// ── Dashboards ────────────────────────────────────────────────────────────────

export interface TopContentDto {
  id:              string;
  title:           string;
  type:            string;
  completionCount: number;
  avgScore:        number;
}

export interface ExamHistoryDto {
  storyTitle:      string;
  score:           number;
  correctAnswers:  number;
  totalQuestions:  number;
  completedAt:     string;
}

export interface RecentActivityDto {
  childName:    string;
  activityType: string;
  title:        string;
  score?:       number;
  isAccepted?:  boolean;
  occurredAt:   string;
}

export interface PerformanceBandDto {
  band:  string;
  count: number;
  color: string;
}

export interface SkillBarDto {
  label: string;
  pct:   number;
}

export interface ClassroomStatsDto {
  name:        string;
  teacher:     string;
  students:    number;
  avgProgress: number;
}

export interface LevelDistributionDto {
  level: number;
  label: string;
  pct:   number;
  color: string;
}

export interface LevelProgressDto {
  level:            number;
  title:            string;
  subtitle:         string;
  icon:             string;
  tag:              string;
  locked:           boolean;
  stars:            number;
  totalStars:       number;
  lessonsCompleted: number;
  totalLessons:     number;
  avgScore:         number;
  unlockCondition:  string | null;
}

export interface StudentDashboardDto {
  childName:              string;
  stars:                  number;
  storiesRead:            number;
  lessonsCompleted:       number;
  examsCompleted:         number;
  avgScore:               number;
  writingAttempts:        number;
  writingAccepted:        number;
  writingAcceptanceRate:  number;
  performanceLevel:       string;
  currentStreak:          number;
  weeklyActivity:         number[];
  inProgressLessons:      LessonSummary[];
  topStories:             TopContentDto[];
  topLessons:             TopContentDto[];
  examHistory:            ExamHistoryDto[];
  recentActivity:         RecentActivityDto[];
}

export interface StudentSummaryDto {
  childName:        string;
  stars:            number;
  storiesRead:      number;
  lessonsCompleted: number;
  avgScore:         number;
  writingAccepted:  number;
  writingAttempts:  number;
  performanceLevel: string;
  lastActivity:     string | null;
}

export interface ParentDashboardDto {
  childName:              string;
  stars:                  number;
  storiesRead:            number;
  lessonsCompleted:       number;
  examsCompleted:         number;
  avgScore:               number;
  writingAccepted:        number;
  writingAcceptanceRate:  number;
  performanceLevel:       string;
  currentStreak:          number;
  weeklyActivity:         number[];
  inProgressLessons:      LessonSummary[];
  recentAssignments:      LessonAssignmentDto[];
  skillBars:              SkillBarDto[];
  topStories:             TopContentDto[];
  examHistory:            ExamHistoryDto[];
  recentActivity:         RecentActivityDto[];
}

export interface TeacherDashboardDto {
  totalStudents:     number;
  activeThisWeek:    number;
  avgClassScore:     number;
  topStories:        TopContentDto[];
  topLessons:        TopContentDto[];
  students:          StudentSummaryDto[];
  performanceBands:  PerformanceBandDto[];
}

export interface SchoolDashboardDto {
  totalStudents:     number;
  totalTeachers:     number;
  activeThisWeek:    number;
  avgSchoolScore:    number;
  totalStories:      number;
  totalLessons:      number;
  topContent:        TopContentDto[];
  recentActivities:  RecentActivityDto[];
  performanceBands:  PerformanceBandDto[];
  classrooms:        ClassroomStatsDto[];
  levelDistribution: LevelDistributionDto[];
}

// ── PDF Library ───────────────────────────────────────────────────────────────

export interface PdfDocumentDto {
  id:                 string;
  title:              string;
  letter:             string;
  level:              number;
  pageCount:          number;
  embeddedPageCount:  number;
  embeddingsGenerated: boolean;
  uploadedAt:         string;
}

export interface PdfPageDto {
  id:         string;
  pageNumber: number;
  sentence:   string;
  imageUrl:   string;
  isEmbedded: boolean;
}

export interface PdfDocumentDetailDto extends PdfDocumentDto {
  pages: PdfPageDto[];
}

export interface EmbedResultDto {
  embeddedCount: number;
  message:       string;
}

export interface PdfLibraryStatsDto {
  totalPdfs:     number;
  totalPages:    number;
  totalEmbedded: number;
  lastUpdated:   string | null;
}

// ── Educational PDF RAG (per-page chunks) ─────────────────────────────────────

export interface RagPageChunkDto {
  id:         string;
  sourceFile: string;
  pageNumber: number;
  sentence:   string;
  wordCount:  number;
  imageUrl:   string;
  level:      number;
  letter:     string;
  letterName: string;
}

export interface IngestEducationalPdfRequest {
  level:      number;
  letter:     string;
  letterName: string;
}

// ── Lesson Generation ─────────────────────────────────────────────────────────

export interface GenerateLessonV2Request {
  topic:           string;
  letter?:         string;
  level:           number;
  creatorId?:      string;
  creatorRole:     string;
  targetStudentId?: string;
  targetGroupId?:  string;
}

// ── Student Groups ────────────────────────────────────────────────────────────

export interface StudentGroupMemberDto {
  studentId:   string;
  studentName: string;
  addedAt:     string;
}

export interface StudentGroupDto {
  id:          string;
  name:        string;
  teacherId:   string;
  memberCount: number;
  createdAt:   string;
  members:     StudentGroupMemberDto[];
}

export interface CreateGroupRequest {
  name: string;
}

export interface AddGroupMemberRequest {
  studentId: string;
}

// ── Lesson Assignments ────────────────────────────────────────────────────────

export interface AssignLessonRequest {
  lessonId:        string;
  targetType:      'Student' | 'Group';
  targetStudentId?: string;
  targetGroupId?:  string;
}

export interface LessonAssignmentDto {
  id:               string;
  lessonId:         string;
  lessonTitle:      string;
  targetType:       string;
  targetStudentId?: string;
  targetStudentName?: string;
  targetGroupId?:   string;
  targetGroupName?: string;
  assignedAt:       string;
}
