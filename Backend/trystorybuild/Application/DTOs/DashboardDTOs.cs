namespace Application.DTOs
{
    // ── Shared ────────────────────────────────────────────────────────────────
    public record RecentActivityDto(
        string Type,        // story | lesson | exam | writing
        string Title,
        double? Score,
        bool? IsAccepted,
        DateTime Date);

    // ── Student ───────────────────────────────────────────────────────────────
    public record StudentDashboardDto(
        string ChildName,
        int Stars,
        int StoriesCompleted,
        int LessonsCompleted,
        int ExamsTaken,
        double AverageExamScore,
        int WritingAttempts,
        int WritingAccepted,
        double WritingAcceptanceRate,
        string PerformanceLevel,
        List<ExamHistoryDto> ExamHistory,
        List<RecentActivityDto> RecentActivity);

    public record ExamHistoryDto(
        string StoryTitle,
        double Score,
        int CorrectAnswers,
        int TotalQuestions,
        DateTime Date);

    // ── Parent ────────────────────────────────────────────────────────────────
    public record ParentDashboardDto(
        string ChildName,
        int Stars,
        int StoriesCompleted,
        int LessonsCompleted,
        double AverageExamScore,
        double WritingAcceptanceRate,
        string PerformanceLevel,
        List<ExamHistoryDto> ExamHistory,
        List<RecentActivityDto> RecentActivity);

    // ── Teacher ───────────────────────────────────────────────────────────────
    public record TeacherDashboardDto(
        int TotalActiveStudents,
        int TotalStoriesRead,
        int TotalLessonsCompleted,
        int TotalExamsTaken,
        double PlatformAverageScore,
        int TotalWritingAttempts,
        List<StudentSummaryDto> Students,
        List<TopContentDto> TopStories,
        List<TopContentDto> TopLessons);

    public record StudentSummaryDto(
        string ChildName,
        int Stars,
        int StoriesCompleted,
        int LessonsCompleted,
        double AverageExamScore,
        int WritingAccepted,
        int WritingAttempts,
        string PerformanceLevel,
        DateTime? LastActivity);

    public record TopContentDto(
        string Id,
        string Title,
        string Type,
        int CompletionCount,
        double AverageScore);

    // ── School ────────────────────────────────────────────────────────────────
    public record SchoolDashboardDto(
        int TotalActiveStudents,
        int TotalStories,
        int TotalLessons,
        int TotalExamsTaken,
        double PlatformAverageScore,
        int TotalWritingAttempts,
        int TotalWritingAccepted,
        double WritingAcceptanceRate,
        List<TopContentDto> TopStories,
        List<TopContentDto> TopLessons,
        List<PerformanceBandDto> PerformanceBands);

    public record PerformanceBandDto(
        string Label,
        int Count,
        string Color);
}
