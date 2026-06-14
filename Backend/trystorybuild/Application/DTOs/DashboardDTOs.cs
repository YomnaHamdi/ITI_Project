namespace Application.DTOs
{
    // ── Shared ─────────────────────────────────────────────────────────────────

    public record RecentActivityDto(
        string ActivityType,     // maps to frontend activityType
        string ChildName,
        string Title,
        double? Score,
        bool? IsAccepted,
        DateTime OccurredAt);   // maps to frontend occurredAt

    public record ExamHistoryDto(
        string StoryTitle,
        double Score,
        int CorrectAnswers,
        int TotalQuestions,
        DateTime CompletedAt);  // maps to frontend completedAt

    public record TopContentDto(
        string Id,
        string Title,
        string Type,
        int CompletionCount,
        double AvgScore);       // maps to frontend avgScore

    public record PerformanceBandDto(
        string Band,            // maps to frontend band (was Label)
        int Count,
        string Color);

    public record SkillBarDto(string Label, int Pct);

    // ── Student Dashboard ─────────────────────────────────────────────────────

    public record StudentDashboardDto(
        string ChildName,
        int Stars,
        int StoriesRead,              // frontend: storiesRead
        int LessonsCompleted,
        int ExamsCompleted,           // frontend: examsCompleted
        double AvgScore,              // frontend: avgScore
        int WritingAttempts,
        int WritingAccepted,
        double WritingAcceptanceRate,
        string PerformanceLevel,
        int CurrentStreak,
        int[] WeeklyActivity,         // [Mon..Sun] activity counts
        List<LessonSummaryDto> InProgressLessons,
        List<TopContentDto> TopStories,
        List<TopContentDto> TopLessons,
        List<ExamHistoryDto> ExamHistory,
        List<RecentActivityDto> RecentActivity);

    // ── Teacher-view student card ─────────────────────────────────────────────

    public record StudentSummaryDto(
        string ChildName,
        int Stars,
        int StoriesRead,
        int LessonsCompleted,
        double AvgScore,
        int WritingAccepted,
        int WritingAttempts,
        string PerformanceLevel,
        DateTime? LastActivity);

    // ── Parent Dashboard ──────────────────────────────────────────────────────

    public record ParentDashboardDto(
        string ChildName,
        int Stars,
        int StoriesRead,
        int LessonsCompleted,
        int ExamsCompleted,
        double AvgScore,
        int WritingAccepted,
        double WritingAcceptanceRate,
        string PerformanceLevel,
        int CurrentStreak,
        int[] WeeklyActivity,
        List<LessonSummaryDto> InProgressLessons,
        List<LessonAssignmentDto> RecentAssignments,
        List<SkillBarDto> SkillBars,
        List<TopContentDto> TopStories,
        List<ExamHistoryDto> ExamHistory,
        List<RecentActivityDto> RecentActivity);

    // ── Teacher Dashboard ─────────────────────────────────────────────────────

    public record TeacherDashboardDto(
        int TotalStudents,
        int ActiveThisWeek,
        double AvgClassScore,
        List<TopContentDto> TopStories,
        List<TopContentDto> TopLessons,
        List<StudentSummaryDto> Students,
        List<PerformanceBandDto> PerformanceBands);

    // ── School Dashboard ──────────────────────────────────────────────────────

    public record SchoolDashboardDto(
        int TotalStudents,
        int TotalTeachers,
        int ActiveThisWeek,
        double AvgSchoolScore,
        int TotalStories,
        int TotalLessons,
        List<TopContentDto> TopContent,
        List<RecentActivityDto> RecentActivities,
        List<PerformanceBandDto> PerformanceBands,
        List<ClassroomStatsDto> Classrooms,
        List<LevelDistributionDto> LevelDistribution);

    public record ClassroomStatsDto(
        string Name,
        string Teacher,
        int Students,
        double AvgProgress);

    public record LevelDistributionDto(
        int Level,
        string Label,
        double Pct,
        string Color);

    // ── Level Progress (for /levels page) ────────────────────────────────────

    public record LevelProgressDto(
        int Level,
        string Title,
        string Subtitle,
        string Icon,
        string Tag,
        bool Locked,
        int Stars,
        int TotalStars,
        int LessonsCompleted,
        int TotalLessons,
        double AvgScore,
        string? UnlockCondition);
}
