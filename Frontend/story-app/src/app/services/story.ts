import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map } from 'rxjs';
import {
  GenerateStoryRequest, StoryResponse,
  ExamResponse, SubmitExamRequest, ExamResult,
  WritingCorrectionResponse, ProgressResponse,
  LessonSummary, LessonDetail, ImportBookResponse,
  KnowledgeDocumentDto, RagSearchResult, GenerateLessonRequest,
  IngestDocumentResponse,
  StudentDashboardDto, ParentDashboardDto, TeacherDashboardDto, SchoolDashboardDto, LevelProgressDto,
  PdfDocumentDto, PdfDocumentDetailDto, EmbedResultDto, PdfLibraryStatsDto,
  AdminBooksPageDto, CreateManualBookRequest,
  RagPageChunkDto, GenerateLessonV2Request,
  StudentGroupDto, AssignLessonRequest, LessonAssignmentDto
} from '../models/story.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class StoryService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  // ── Story ──────────────────────────────────────────────────────────────────
  generateStory(req: GenerateStoryRequest): Observable<StoryResponse> {
    return this.http.post<StoryResponse>(`${this.api}/api/story/generate`, req);
  }

  getStory(id: string): Observable<StoryResponse> {
    return this.http.get<any>(`${this.api}/api/story/${id}`).pipe(
      map(s => ({
        ...s,
        pages: (s.pages ?? []).map((p: any) => ({
          ...p,
          pageId:   p.pageId   ?? p.id,
          imageUrl: p.imageUrl ?? p.imagePath ?? ''
        }))
      }))
    );
  }

  getAllStories(): Observable<StoryResponse[]> {
    return this.http.get<StoryResponse[]>(`${this.api}/api/story`);
  }

  deleteStory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/story/${id}`);
  }

  // ── Exam ───────────────────────────────────────────────────────────────────
  generateExam(storyId: string): Observable<ExamResponse> {
    return this.http.post<ExamResponse>(`${this.api}/api/exam/generate/${storyId}`, {});
  }

  generateLessonExam(lessonId: string): Observable<ExamResponse> {
    return this.http.post<ExamResponse>(`${this.api}/api/exam/generate/lesson/${lessonId}`, {});
  }

  getOrGenerateExam(storyId: string): Observable<ExamResponse> {
    return this.http.get<ExamResponse>(`${this.api}/api/exam/story/${storyId}`).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 404)
          return this.http.post<ExamResponse>(`${this.api}/api/exam/generate/${storyId}`, {});
        throw err;
      })
    );
  }

  submitExam(req: SubmitExamRequest): Observable<ExamResult> {
    return this.http.post<ExamResult>(`${this.api}/api/exam/submit`, req);
  }

  // ── Writing ────────────────────────────────────────────────────────────────
  submitLessonWriting(
    lessonId:     string,
    lessonPageId: string,
    childName:    string,
    imageBlob:    Blob,
    fileName:     string = 'drawing.png'
  ): Observable<WritingCorrectionResponse> {
    const form = new FormData();
    form.append('lessonId',     lessonId);
    form.append('lessonPageId', lessonPageId);
    form.append('childName',    childName);
    form.append('image',        imageBlob, fileName);
    return this.http.post<WritingCorrectionResponse>(
      `${this.api}/api/writing/evaluate`, form);
  }

  evaluateCanvasWriting(imageBase64: string, expectedText: string): Observable<WritingCorrectionResponse> {
    return this.http.post<WritingCorrectionResponse>(`${this.api}/api/writing/canvas`, {
      imageBase64,
      expectedText
    });
  }

  // ── Lessons ────────────────────────────────────────────────────────────────
  getLessonsByLevel(level: number): Observable<LessonSummary[]> {
    return this.http.get<LessonSummary[]>(`${this.api}/api/lessons?level=${level}`);
  }

  getLesson(id: string): Observable<LessonDetail> {
    return this.http.get<any>(`${this.api}/api/lessons/${id}`).pipe(
      map(l => ({
        ...l,
        pages: (l.pages ?? []).map((p: any) => ({
          ...p,
          pageId:   p.pageId   ?? p.id,
          imageUrl: p.imageUrl ?? p.imagePath ?? ''
        }))
      }))
    );
  }

  // ── Admin ──────────────────────────────────────────────────────────────────
  importBook(
    level: number, letter: string, letterName: string, pdfFile: File
  ): Observable<ImportBookResponse> {
    const form = new FormData();
    form.append('level',      String(level));
    form.append('letter',     letter);
    form.append('letterName', letterName);
    form.append('pdfFile',    pdfFile);
    return this.http.post<ImportBookResponse>(`${this.api}/api/admin/import-book`, form);
  }

  importBookV2(
    level: number, letter: string, letterName: string, title: string, pdfFile: File
  ): Observable<ImportBookResponse> {
    const form = new FormData();
    form.append('level',      String(level));
    form.append('letter',     letter);
    form.append('letterName', letterName);
    form.append('title',      title);
    form.append('pdfFile',    pdfFile);
    return this.http.post<ImportBookResponse>(`${this.api}/api/admin/import-book`, form);
  }

  getAllBooksAdmin(level?: number, page = 1, pageSize = 9): Observable<AdminBooksPageDto> {
    let url = `${this.api}/api/admin/books?page=${page}&pageSize=${pageSize}`;
    if (level != null) url += `&level=${level}`;
    return this.http.get<AdminBooksPageDto>(url);
  }

  getBookDetailAdmin(id: string): Observable<LessonDetail> {
    return this.http.get<any>(`${this.api}/api/admin/books/${id}`).pipe(
      map(l => ({
        ...l,
        pages: (l.pages ?? []).map((p: any) => ({
          ...p,
          pageId:   p.pageId   ?? p.id,
          imageUrl: p.imageUrl ?? p.imagePath ?? ''
        }))
      }))
    );
  }

  deleteBook(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/admin/books/${id}`);
  }

  updateBookPageSentence(bookId: string, pageId: string, sentence: string): Observable<void> {
    return this.http.patch<void>(
      `${this.api}/api/admin/books/${bookId}/pages/${pageId}/sentence`,
      { sentence }
    );
  }

  createManualBook(req: CreateManualBookRequest): Observable<ImportBookResponse> {
    return this.http.post<ImportBookResponse>(`${this.api}/api/admin/books/manual`, req);
  }

  // ── Progress ───────────────────────────────────────────────────────────────
  getProgress(storyId: string, childName: string): Observable<ProgressResponse> {
    return this.http.get<ProgressResponse>(`${this.api}/api/progress/${storyId}/${childName}`);
  }

  updateProgress(progress: ProgressResponse): Observable<ProgressResponse> {
    return this.http.put<ProgressResponse>(`${this.api}/api/progress`, progress);
  }

  // ── RAG ────────────────────────────────────────────────────────────────────
  ingestDocument(
    file: File, letter?: string, level?: number, tags?: string
  ): Observable<IngestDocumentResponse> {
    const form = new FormData();
    form.append('file', file);
    if (letter) form.append('letter', letter);
    if (level  != null) form.append('level', String(level));
    if (tags)  form.append('tags', tags);
    return this.http.post<IngestDocumentResponse>(`${this.api}/api/rag/ingest`, form);
  }

  getKnowledgeDocuments(): Observable<KnowledgeDocumentDto[]> {
    return this.http.get<KnowledgeDocumentDto[]>(`${this.api}/api/rag/documents`);
  }

  deleteKnowledgeDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/rag/documents/${id}`);
  }

  ragSearch(query: string): Observable<RagSearchResult[]> {
    return this.http.post<RagSearchResult[]>(`${this.api}/api/rag/search`, JSON.stringify(query), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  generateRagLesson(req: GenerateLessonRequest): Observable<LessonDetail> {
    return this.http.post<LessonDetail>(`${this.api}/api/rag/generate-lesson`, req);
  }

  // ── Dashboards ────────────────────────────────────────────────────────────
  getStudentDashboard(childName: string): Observable<StudentDashboardDto> {
    return this.http.get<StudentDashboardDto>(`${this.api}/api/dashboard/student/${childName}`);
  }

  getParentDashboard(childName: string): Observable<ParentDashboardDto> {
    return this.http.get<ParentDashboardDto>(`${this.api}/api/dashboard/parent/${childName}`);
  }

  getTeacherDashboard(): Observable<TeacherDashboardDto> {
    return this.http.get<TeacherDashboardDto>(`${this.api}/api/dashboard/teacher`);
  }

  getSchoolDashboard(): Observable<SchoolDashboardDto> {
    return this.http.get<SchoolDashboardDto>(`${this.api}/api/dashboard/school`);
  }

  getKnownStudentNames(): Observable<string[]> {
    return this.http.get<string[]>(`${this.api}/api/dashboard/students`);
  }

  getLevelProgress(childName: string): Observable<LevelProgressDto[]> {
    return this.http.get<LevelProgressDto[]>(`${this.api}/api/dashboard/levels/progress/${encodeURIComponent(childName)}`);
  }

  // ── Knowledge document upload ─────────────────────────────────────────────
  uploadKnowledgeDocument(file: File, name: string, description: string): Observable<any> {
    const form = new FormData();
    form.append('file',        file);
    form.append('name',        name);
    form.append('description', description);
    return this.http.post<any>(`${this.api}/api/rag/documents`, form);
  }

  // ── Admin ─────────────────────────────────────────────────────────────────
  getAiSettings(): Observable<any> {
    return this.http.get<any>(`${this.api}/api/admin/ai-settings`);
  }

  saveAiSettings(settings: any): Observable<any> {
    return this.http.put<any>(`${this.api}/api/admin/ai-settings`, settings);
  }

  getSubscriptionStats(): Observable<any> {
    return this.http.get<any>(`${this.api}/api/admin/subscriptions/stats`);
  }

  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/admin/users`);
  }

  blockUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/api/admin/users/${id}/block`, {});
  }

  unblockUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/api/admin/users/${id}/unblock`, {});
  }

  createSchool(body: { schoolName: string; adminEmail: string; adminPassword: string }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/admin/schools`, body);
  }

  // ── Placement ─────────────────────────────────────────────────────────────
  getPlacementQuestions(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/placement/questions`);
  }

  submitPlacement(request: { answers: { questionId: string; answer: string }[] }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/placement/submit`, request);
  }

  // ── PDF Library ───────────────────────────────────────────────────────────
  uploadPdfDocument(file: File, letter: string, level: number): Observable<PdfDocumentDto> {
    const fd = new FormData();
    fd.append('file', file);
    fd.append('letter', letter);
    fd.append('level', level.toString());
    return this.http.post<PdfDocumentDto>(`${this.api}/api/pdf-library/upload`, fd);
  }

  generatePdfEmbeddings(id: string): Observable<EmbedResultDto> {
    return this.http.post<EmbedResultDto>(`${this.api}/api/pdf-library/${id}/embed`, {});
  }

  getPdfDocuments(): Observable<PdfDocumentDto[]> {
    return this.http.get<PdfDocumentDto[]>(`${this.api}/api/pdf-library`);
  }

  getPdfDocument(id: string): Observable<PdfDocumentDetailDto> {
    return this.http.get<PdfDocumentDetailDto>(`${this.api}/api/pdf-library/${id}`);
  }

  deletePdfDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/pdf-library/${id}`);
  }

  getPdfLibraryStats(): Observable<PdfLibraryStatsDto> {
    return this.http.get<PdfLibraryStatsDto>(`${this.api}/api/pdf-library/stats`);
  }

  // ── Educational PDF RAG ingestion ─────────────────────────────────────────
  ingestEducationalPdf(
    file: File, level: number, letter: string, letterName: string
  ): Observable<IngestDocumentResponse> {
    const form = new FormData();
    form.append('file',       file);
    form.append('level',      String(level));
    form.append('letter',     letter);
    form.append('letterName', letterName);
    return this.http.post<IngestDocumentResponse>(`${this.api}/api/rag/ingest-educational`, form);
  }

  getRagPageChunks(level?: number, letter?: string): Observable<RagPageChunkDto[]> {
    let url = `${this.api}/api/rag/page-chunks`;
    const params: string[] = [];
    if (level  != null) params.push(`level=${level}`);
    if (letter)         params.push(`letter=${encodeURIComponent(letter)}`);
    if (params.length)  url += '?' + params.join('&');
    return this.http.get<RagPageChunkDto[]>(url);
  }

  // ── Lesson generation (teacher/student prompt) ────────────────────────────
  generateLesson(req: GenerateLessonV2Request): Observable<LessonDetail> {
    return this.http.post<LessonDetail>(`${this.api}/api/lessons/generate`, req);
  }

  getMyLessons(creatorId: string): Observable<LessonSummary[]> {
    return this.http.get<LessonSummary[]>(`${this.api}/api/lessons/my/${creatorId}`);
  }

  // ── Student Groups ────────────────────────────────────────────────────────
  getTeacherGroups(teacherId: string): Observable<StudentGroupDto[]> {
    return this.http.get<StudentGroupDto[]>(`${this.api}/api/groups/teacher/${teacherId}`);
  }

  createGroup(teacherId: string, name: string): Observable<StudentGroupDto> {
    return this.http.post<StudentGroupDto>(
      `${this.api}/api/groups/teacher/${teacherId}`, { name }
    );
  }

  addGroupMember(groupId: string, studentId: string): Observable<void> {
    return this.http.post<void>(`${this.api}/api/groups/${groupId}/members`, { studentId });
  }

  removeGroupMember(groupId: string, studentId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/groups/${groupId}/members/${studentId}`);
  }

  deleteGroup(groupId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/groups/${groupId}`);
  }

  // ── Lesson Assignments ────────────────────────────────────────────────────
  assignLesson(req: AssignLessonRequest): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(`${this.api}/api/groups/assign`, req);
  }

  getAssignedLessons(studentId: string): Observable<LessonAssignmentDto[]> {
    return this.http.get<LessonAssignmentDto[]>(`${this.api}/api/groups/assigned/student/${studentId}`);
  }

  getTeacherAssignments(teacherId: string): Observable<LessonAssignmentDto[]> {
    return this.http.get<LessonAssignmentDto[]>(`${this.api}/api/groups/assignments/teacher/${teacherId}`);
  }
}
