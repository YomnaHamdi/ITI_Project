using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    // ── AI Services ────────────────────────────────────────────────────────────────

    public interface IStoryGeneratorService
    {
        Task<AiStoryOutput> GenerateAsync(string childName, string character, string theme);
    }

    public interface IExamGeneratorService
    {
        Task<AiExamOutput> GenerateAsync(string storyText);
        Task<AiExamOutput> GenerateLessonAsync(string lessonText);
    }

    public interface IJudgeService
    {
        Task<JudgeResult> ValidateAsync(string storyTitle, List<string> sentences, List<string> imagePrompts);
    }

    public interface IImageGenerationService
    {
        Task<string> GenerateImageAsync(string prompt, string fileName);
    }

    public interface IOcrService
    {
        Task<string> ExtractArabicTextAsync(string imagePath);
    }

    public interface ITextSimilarityService
    {
        double Calculate(string expected, string actual);
    }

    public interface IPdfPageRenderer
    {
        Task<List<string>> RenderPagesAsync(string pdfPath, string outputDirectory, CancellationToken ct = default);
    }

    public interface IAiTextCleanupService
    {
        Task<string> CleanupArabicSentenceAsync(string ocrText);
    }

    public interface IPdfImportService
    {
        Task<LessonDetailResponse> ImportBookAsync(
            int level,
            string letter,
            string letterName,
            string title,
            IFormFile pdfFile,
            CancellationToken ct = default);
    }

    // ── Story Repository ───────────────────────────────────────────────────────────
    public interface IStoryRepository
    {
        Task<Story> SaveAsync(Story story);
        Task<Story?> GetByIdAsync(Guid id);
        Task<List<Story>> GetAllAsync();
        Task<List<Story>> GetByChildNameAsync(string childName);
        Task<bool> DeleteAsync(Guid id);
    }

    // ── Lesson Repository ──────────────────────────────────────────────────────────
    public interface ILessonRepository
    {
        Task<Lesson> SaveAsync(Lesson lesson);
        Task<Lesson?> GetByIdAsync(Guid id);
        Task<List<Lesson>> GetByLevelAsync(int level);
        Task<List<Lesson>> GetAllAsync(int? level = null);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> UpdatePageSentenceAsync(Guid pageId, string sentence);
        Task<Lesson> CreateManualAsync(Lesson lesson);
    }

    // ── Other Repositories ─────────────────────────────────────────────────────────
    public interface IExamRepository
    {
        Task<Exam> SaveAsync(Exam exam);
        Task<Exam?> GetByStoryIdAsync(Guid storyId);
        Task<Exam?> GetByLessonIdAsync(Guid lessonId);
        Task<Exam?> GetByIdAsync(Guid examId);
        Task SaveAnswersAsync(Guid examId, List<StudentAnswer> answers);
    }

    public interface IStudentProgressRepository
    {
        Task<StudentProgress> SaveAsync(StudentProgress progress);
        Task<StudentProgress?> GetAsync(Guid storyId, string childName);
        Task<StudentProgress?> GetByLessonAsync(Guid lessonId, string childName);
    }

    public interface IWritingAttemptRepository
    {
        Task<WritingAttempt> SaveAsync(WritingAttempt attempt);
    }

    // ── Auth Service ───────────────────────────────────────────────────────────────
    public interface IAuthService
    {
        Task<AuthResponse>        RegisterAsync(RegisterRequest request);
        Task<AuthResponse>        LoginAsync(LoginRequest request);
        Task<StudentAuthResponse> CreateStudentAsync(Guid creatorId, CreateStudentRequest request);
        Task<StudentAuthResponse> StudentLoginAsync(StudentLoginRequest request);
        Task<List<StudentProfileDto>> GetChildrenAsync(Guid parentId);
        Task<List<StudentProfileDto>> GetStudentsAsync(Guid teacherId);
        Task<(Guid id, string schoolCode)> CreateSchoolAdminAsync(string schoolName, string email, string password);
        Task<StudentAuthResponse> UpdateStudentLevelAsync(Guid studentId, int level);
    }

    // ── User Repository ────────────────────────────────────────────────────────────
    public interface IUserRepository
    {
        Task<User?>   FindByEmailAsync(string email);
        Task<User?>   FindByIdAsync(Guid id);
        Task<User>    SaveAsync(User user);
        Task<Parent>  SaveParentAsync(Parent parent);
        Task<Teacher> SaveTeacherAsync(Teacher teacher);
    }

    // ── Student Repository ─────────────────────────────────────────────────────────
    public interface IStudentRepository
    {
        Task<Student?>       FindByUsernameAsync(string username);
        Task<Student?>       FindByIdAsync(Guid id);
        Task<Student>        SaveAsync(Student student);
        Task<List<Student>>  GetByParentIdAsync(Guid parentId);
        Task<List<Student>>  GetByTeacherIdAsync(Guid teacherId);
        Task<bool>           UpdateLevelAsync(Guid id, int level);
    }

    // ── Level Word Config ──────────────────────────────────────────────────────────
    public interface ILevelWordConfigRepository
    {
        Task<int> GetWordCountAsync(int level);
        Task UpsertAsync(int level, int wordCount, string exampleSentence);
        Task<List<Domain.Entities.LevelWordConfig>> GetAllAsync();
    }

    // ── RAG Page Chunks ────────────────────────────────────────────────────────────
    public interface IRagPageChunkRepository
    {
        Task<Domain.Entities.RagPageChunk> SaveAsync(Domain.Entities.RagPageChunk chunk);
        Task<List<Domain.Entities.RagPageChunk>> GetAllAsync(int? level = null, string? letter = null);
        Task DeleteBySourceFileAsync(string sourceFile);
    }

    // ── Student Groups ─────────────────────────────────────────────────────────────
    public interface IStudentGroupRepository
    {
        Task<Domain.Entities.StudentGroup> SaveAsync(Domain.Entities.StudentGroup group);
        Task<Domain.Entities.StudentGroup?> GetByIdAsync(Guid id);
        Task<List<Domain.Entities.StudentGroup>> GetByTeacherIdAsync(Guid teacherId);
        Task<bool> AddMemberAsync(Guid groupId, Guid studentId);
        Task<bool> RemoveMemberAsync(Guid groupId, Guid studentId);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Domain.Entities.StudentGroup>> GetGroupsForStudentAsync(Guid studentId);
    }

    // ── Lesson Assignments ─────────────────────────────────────────────────────────
    public interface ILessonAssignmentRepository
    {
        Task<Domain.Entities.LessonAssignment> SaveAsync(Domain.Entities.LessonAssignment assignment);
        Task<List<Domain.Entities.LessonAssignment>> GetForStudentAsync(Guid studentId, List<Guid> groupIds);
        Task<List<Domain.Entities.LessonAssignment>> GetByTeacherAsync(Guid teacherId);
    }

    // ── Educational PDF Ingestion ──────────────────────────────────────────────────
    public interface IEducationalPdfIngestionService
    {
        Task<DTOs.IngestDocumentResponse> IngestAsync(
            Stream pdfStream,
            string fileName,
            int level,
            string letter,
            string letterName,
            CancellationToken ct = default);
    }

    // ── Dashboard Service ──────────────────────────────────────────────────────────
    public interface IDashboardService
    {
        Task<StudentDashboardDto?> GetStudentDashboardAsync(string childName);
        Task<ParentDashboardDto?> GetParentDashboardAsync(string childName);
        Task<TeacherDashboardDto> GetTeacherDashboardAsync(Guid teacherId);
        Task<SchoolDashboardDto> GetSchoolDashboardAsync();
        Task<List<string>> GetKnownChildNamesAsync();
        Task<List<LevelProgressDto>> GetLevelProgressAsync(string childName);
    }
}
