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
            IFormFile pdfFile,
            CancellationToken ct = default);
    }

    // ── Story Repository ───────────────────────────────────────────────────────────
    public interface IStoryRepository
    {
        Task<Story> SaveAsync(Story story);
        Task<Story?> GetByIdAsync(Guid id);
        Task<List<Story>> GetAllAsync();
        Task<bool> DeleteAsync(Guid id);
    }

    // ── Lesson Repository ──────────────────────────────────────────────────────────
    public interface ILessonRepository
    {
        Task<Lesson> SaveAsync(Lesson lesson);
        Task<Lesson?> GetByIdAsync(Guid id);
        Task<List<Lesson>> GetByLevelAsync(int level);
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
    }

    // ── Dashboard Service ──────────────────────────────────────────────────────────
    public interface IDashboardService
    {
        Task<StudentDashboardDto?> GetStudentDashboardAsync(string childName);
        Task<ParentDashboardDto?> GetParentDashboardAsync(string childName);
        Task<TeacherDashboardDto> GetTeacherDashboardAsync();
        Task<SchoolDashboardDto> GetSchoolDashboardAsync();
        Task<List<string>> GetKnownChildNamesAsync();
    }
}
