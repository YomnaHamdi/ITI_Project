using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DashboardService(
        AppDbContext db,
        ILogger<DashboardService> logger) : IDashboardService
    {
        public async Task<StudentDashboardDto?> GetStudentDashboardAsync(string childName)
        {
            var name = childName.Trim();
            var progress = await db.StudentProgress.Where(p => p.ChildName == name).ToListAsync();

            if (progress.Count == 0 && !await HasAnyActivityAsync(name))
                return null;

            var examHistory = await BuildExamHistoryAsync(name);
            var writingAttempts = await db.WritingAttempts.Where(w => w.ChildName == name).ToListAsync();
            var storiesCompleted  = progress.Count(p => p.StoryId.HasValue  && p.ExamCompleted);
            var lessonsCompleted  = progress.Count(p => p.LessonId.HasValue && p.ExamCompleted);
            var avgScore = progress.Any(p => p.ExamCompleted)
                ? progress.Where(p => p.ExamCompleted).Average(p => p.ScorePercentage) : 0;
            var accepted = writingAttempts.Count(w => w.IsAccepted);
            var stars    = CalculateStars(progress, writingAttempts);
            var level    = GetPerformanceLevel(avgScore);
            var recent   = await BuildRecentActivityAsync(name);

            return new StudentDashboardDto(
                name, stars, storiesCompleted,
                lessonsCompleted,
                progress.Count(p => p.ExamCompleted),
                Math.Round(avgScore, 1),
                writingAttempts.Count, accepted,
                writingAttempts.Count > 0
                    ? Math.Round((double)accepted / writingAttempts.Count * 100, 1) : 0,
                level, examHistory, recent);
        }

        public async Task<ParentDashboardDto?> GetParentDashboardAsync(string childName)
        {
            var student = await GetStudentDashboardAsync(childName);
            if (student is null) return null;
            return new ParentDashboardDto(
                student.ChildName, student.Stars, student.StoriesCompleted,
                student.LessonsCompleted, student.AverageExamScore,
                student.WritingAcceptanceRate, student.PerformanceLevel,
                student.ExamHistory, student.RecentActivity);
        }

        public async Task<TeacherDashboardDto> GetTeacherDashboardAsync()
        {
            var childNames = await GetKnownChildNamesAsync();
            logger.LogInformation("[Dashboard] Teacher view — {Count} students", childNames.Count);

            var studentSummaries = new List<StudentSummaryDto>();
            foreach (var name in childNames)
                studentSummaries.Add(await BuildStudentSummaryAsync(name));

            var allProgress = await db.StudentProgress.Where(p => p.ExamCompleted).ToListAsync();
            var allWriting  = await db.WritingAttempts.ToListAsync();
            double platformAvg = allProgress.Any() ? allProgress.Average(p => p.ScorePercentage) : 0;

            return new TeacherDashboardDto(
                childNames.Count, allProgress.Count, allProgress.Count, allProgress.Count,
                Math.Round(platformAvg, 1), allWriting.Count,
                studentSummaries.OrderByDescending(s => s.Stars).ToList(),
                await BuildTopStoriesAsync(), await BuildTopLessonsAsync());
        }

        public async Task<SchoolDashboardDto> GetSchoolDashboardAsync()
        {
            var childNames  = await GetKnownChildNamesAsync();
            var allProgress = await db.StudentProgress.Where(p => p.ExamCompleted).ToListAsync();
            var allWriting  = await db.WritingAttempts.ToListAsync();
            double platformAvg = allProgress.Any() ? allProgress.Average(p => p.ScorePercentage) : 0;
            var accepted = allWriting.Count(w => w.IsAccepted);

            var bands = new List<PerformanceBandDto>();
            if (allProgress.Any())
            {
                bands.Add(new("ممتاز (80٪+)",      allProgress.Count(p => p.ScorePercentage >= 80),             "#48bb78"));
                bands.Add(new("جيد (50-79٪)",       allProgress.Count(p => p.ScorePercentage >= 50 && p.ScorePercentage < 80), "#ed8936"));
                bands.Add(new("يحتاج تحسين (<50٪)", allProgress.Count(p => p.ScorePercentage < 50),              "#fc8181"));
            }

            return new SchoolDashboardDto(
                childNames.Count,
                await db.Stories.CountAsync(),
                await db.Lessons.CountAsync(),
                allProgress.Count,
                Math.Round(platformAvg, 1),
                allWriting.Count, accepted,
                allWriting.Count > 0 ? Math.Round((double)accepted / allWriting.Count * 100, 1) : 0,
                await BuildTopStoriesAsync(), await BuildTopLessonsAsync(), bands);
        }

        public async Task<List<string>> GetKnownChildNamesAsync()
        {
            var fromProgress = await db.StudentProgress.Select(p => p.ChildName).Distinct().ToListAsync();
            var fromWriting  = await db.WritingAttempts.Select(w => w.ChildName).Distinct().ToListAsync();
            return fromProgress.Union(fromWriting)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n).ToList();
        }

        private async Task<bool> HasAnyActivityAsync(string name)
        {
            return await db.WritingAttempts.AnyAsync(w => w.ChildName == name)
                || await db.StudentProgress.AnyAsync(p => p.ChildName == name);
        }

        private async Task<List<ExamHistoryDto>> BuildExamHistoryAsync(string name)
        {
            var items = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted)
                .Include(p => p.Story)
                .Include(p => p.Lesson)
                .OrderByDescending(p => p.LastUpdatedAt)
                .Take(10)
                .ToListAsync();

            return items.Select(p => new ExamHistoryDto(
                p.Story?.Title ?? p.Lesson?.Title ?? "امتحان",
                p.ScorePercentage, p.CorrectAnswers, p.TotalQuestions, p.LastUpdatedAt))
                .ToList();
        }

        private async Task<List<RecentActivityDto>> BuildRecentActivityAsync(string name)
        {
            var activities = new List<RecentActivityDto>();

            var progressItems = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted)
                .Include(p => p.Story)
                .Include(p => p.Lesson)
                .ToListAsync();

            foreach (var p in progressItems)
            {
                var title = p.Story?.Title ?? p.Lesson?.Title ?? "امتحان";
                activities.Add(new RecentActivityDto("exam", title, p.ScorePercentage, null, p.LastUpdatedAt));
            }

            var writings = await db.WritingAttempts
                .Where(w => w.ChildName == name)
                .Select(w => new RecentActivityDto(
                    "writing", w.ExpectedSentence, w.SimilarityScore, w.IsAccepted, w.AttemptedAt))
                .ToListAsync();

            activities.AddRange(writings);
            return activities.OrderByDescending(a => a.Date).Take(15).ToList();
        }

        private async Task<StudentSummaryDto> BuildStudentSummaryAsync(string name)
        {
            var progress = await db.StudentProgress.Where(p => p.ChildName == name).ToListAsync();
            var writing  = await db.WritingAttempts.Where(w => w.ChildName == name).ToListAsync();
            DateTime? lastActivity = progress.Any()
                ? (DateTime?)progress.Max(p => p.LastUpdatedAt)
                : writing.Any() ? (DateTime?)writing.Max(w => w.AttemptedAt) : null;
            double avgScore = progress.Any(p => p.ExamCompleted)
                ? progress.Where(p => p.ExamCompleted).Average(p => p.ScorePercentage) : 0;
            return new StudentSummaryDto(
                name, CalculateStars(progress, writing),
                progress.Count(p => p.ExamCompleted), 0,
                Math.Round(avgScore, 1),
                writing.Count(w => w.IsAccepted), writing.Count,
                GetPerformanceLevel(avgScore), lastActivity);
        }

        private async Task<List<TopContentDto>> BuildTopStoriesAsync()
        {
            return await db.StudentProgress
                .Where(p => p.ExamCompleted && p.StoryId.HasValue)
                .GroupBy(p => p.StoryId!.Value)
                .Select(g => new { StoryId = g.Key, Count = g.Count(), AvgScore = g.Average(p => p.ScorePercentage) })
                .OrderByDescending(x => x.Count).Take(5)
                .Join(db.Stories, x => x.StoryId, s => s.Id,
                    (x, s) => new TopContentDto(s.Id.ToString(), s.Title, "story", x.Count, Math.Round(x.AvgScore, 1)))
                .ToListAsync();
        }

        private async Task<List<TopContentDto>> BuildTopLessonsAsync()
        {
            var lessonAttempts = await db.WritingAttempts
                .Join(db.LessonPages, w => w.LessonPageId, p => p.Id, (w, p) => new { w.IsAccepted, p.LessonId })
                .GroupBy(x => x.LessonId)
                .Select(g => new { LessonId = g.Key, Count = g.Count(), AvgScore = g.Count(x => x.IsAccepted) * 100.0 / g.Count() })
                .OrderByDescending(x => x.Count).Take(5).ToListAsync();

            var lessonIds = lessonAttempts.Select(x => x.LessonId).ToList();
            var lessons   = await db.Lessons.Where(l => lessonIds.Contains(l.Id)).ToListAsync();

            return lessonAttempts.Join(lessons, x => x.LessonId, l => l.Id,
                (x, l) => new TopContentDto(l.Id.ToString(), l.Title, "lesson", x.Count, Math.Round(x.AvgScore, 1)))
                .ToList();
        }

        private static int CalculateStars(
            List<Domain.Entities.StudentProgress> progress,
            List<Domain.Entities.WritingAttempt> writing)
        {
            int stars = 0;
            foreach (var p in progress.Where(p => p.ExamCompleted))
            {
                if (p.ScorePercentage >= 90) stars += 3;
                else if (p.ScorePercentage >= 70) stars += 2;
                else if (p.ScorePercentage >= 50) stars += 1;
            }
            stars += writing.Count(w => w.IsAccepted);
            return stars;
        }

        private static string GetPerformanceLevel(double avg) =>
            avg >= 80 ? "ممتاز" : avg >= 50 ? "جيد" : "يحتاج تحسين";
    }
}
