using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DashboardService(
        AppDbContext db,
        ILogger<DashboardService> logger) : IDashboardService
    {
        // ── Student ───────────────────────────────────────────────────────────
        public async Task<StudentDashboardDto?> GetStudentDashboardAsync(string childName)
        {
            var name     = childName.Trim();
            var progress = await db.StudentProgress.Where(p => p.ChildName == name).ToListAsync();

            if (progress.Count == 0 && !await HasAnyActivityAsync(name))
                return null;

            var writing = await db.WritingAttempts.Where(w => w.ChildName == name).ToListAsync();

            int storiesRead      = progress.Count(p => p.StoryId.HasValue  && p.ExamCompleted);
            int lessonsCompleted = progress.Count(p => p.LessonId.HasValue && p.ExamCompleted);
            int examsCompleted   = progress.Count(p => p.ExamCompleted);
            double avgScore      = progress.Any(p => p.ExamCompleted)
                ? progress.Where(p => p.ExamCompleted).Average(p => p.ScorePercentage) : 0;
            int writingAccepted  = writing.Count(w => w.IsAccepted);
            int stars            = CalculateStars(progress, writing);

            return new StudentDashboardDto(
                name, stars,
                storiesRead, lessonsCompleted, examsCompleted,
                Math.Round(avgScore, 1),
                writing.Count, writingAccepted,
                writing.Count > 0 ? Math.Round((double)writingAccepted / writing.Count * 100, 1) : 0,
                GetPerformanceLevel(avgScore),
                await CalculateStreakAsync(name),
                await BuildWeeklyActivityAsync(name),
                await GetInProgressLessonsAsync(name),
                await GetStudentTopContentAsync(name, storyOnly: true),
                await GetStudentTopContentAsync(name, storyOnly: false),
                await BuildExamHistoryAsync(name),
                await BuildRecentActivityAsync(name));
        }

        // ── Parent ────────────────────────────────────────────────────────────
        public async Task<ParentDashboardDto?> GetParentDashboardAsync(string childName)
        {
            var name     = childName.Trim();
            var progress = await db.StudentProgress.Where(p => p.ChildName == name).ToListAsync();

            if (progress.Count == 0 && !await HasAnyActivityAsync(name))
                return null;

            var writing      = await db.WritingAttempts.Where(w => w.ChildName == name).ToListAsync();
            int storiesRead  = progress.Count(p => p.StoryId.HasValue  && p.ExamCompleted);
            int lessonsComp  = progress.Count(p => p.LessonId.HasValue && p.ExamCompleted);
            int examsComp    = progress.Count(p => p.ExamCompleted);
            double avgScore  = progress.Any(p => p.ExamCompleted)
                ? progress.Where(p => p.ExamCompleted).Average(p => p.ScorePercentage) : 0;
            int writingAcc   = writing.Count(w => w.IsAccepted);

            return new ParentDashboardDto(
                name,
                CalculateStars(progress, writing),
                storiesRead, lessonsComp, examsComp,
                Math.Round(avgScore, 1),
                writingAcc,
                writing.Count > 0 ? Math.Round((double)writingAcc / writing.Count * 100, 1) : 0,
                GetPerformanceLevel(avgScore),
                await CalculateStreakAsync(name),
                await BuildWeeklyActivityAsync(name),
                await GetInProgressLessonsAsync(name),
                new List<LessonAssignmentDto>(),  // assignments require student Guid — left for future
                BuildSkillBars(progress, writing),
                await GetStudentTopContentAsync(name, storyOnly: true),
                await BuildExamHistoryAsync(name),
                await BuildRecentActivityAsync(name));
        }

        // ── Teacher ───────────────────────────────────────────────────────────
        public async Task<TeacherDashboardDto> GetTeacherDashboardAsync(Guid teacherId)
        {
            var childEntries = await db.Students
                .Where(s => s.TeacherId == teacherId)
                .Select(s => new { s.Name, s.Level })
                .ToListAsync();
            var childNames   = childEntries.Select(e => e.Name).ToList();
            var allProgress  = await db.StudentProgress.Where(p => p.ExamCompleted).ToListAsync();
            var cutoff       = DateTime.UtcNow.AddDays(-7);
            int activeWeek   = await db.StudentProgress
                .Where(p => p.LastUpdatedAt >= cutoff)
                .Select(p => p.ChildName).Distinct().CountAsync();
            double avgScore  = allProgress.Any() ? allProgress.Average(p => p.ScorePercentage) : 0;

            logger.LogInformation("[Dashboard] Teacher — {Count} students, avg {Avg}%", childNames.Count, Math.Round(avgScore,1));

            var students = new List<StudentSummaryDto>();
            foreach (var entry in childEntries)
                students.Add(await BuildStudentSummaryAsync(entry.Name, entry.Level));

            return new TeacherDashboardDto(
                childNames.Count,
                activeWeek,
                Math.Round(avgScore, 1),
                await BuildTopStoriesAsync(),
                await BuildTopLessonsAsync(),
                students.OrderByDescending(s => s.Stars).ToList(),
                BuildPerformanceBands(allProgress));
        }

        // ── School ────────────────────────────────────────────────────────────
        public async Task<SchoolDashboardDto> GetSchoolDashboardAsync()
        {
            var allProgress   = await db.StudentProgress.Where(p => p.ExamCompleted).ToListAsync();
            var cutoff        = DateTime.UtcNow.AddDays(-7);
            int activeWeek    = await db.StudentProgress
                .Where(p => p.LastUpdatedAt >= cutoff)
                .Select(p => p.ChildName).Distinct().CountAsync();
            double avgScore   = allProgress.Any() ? allProgress.Average(p => p.ScorePercentage) : 0;
            int totalTeachers = await db.Users.CountAsync(u => u.Role == UserRole.Teacher && u.IsActive);
            int totalStudents = await db.Students.CountAsync();

            var topStories    = await BuildTopStoriesAsync();
            var topLessons    = await BuildTopLessonsAsync();
            var topContent    = topStories.Concat(topLessons)
                .OrderByDescending(t => t.CompletionCount).Take(5).ToList();

            var recentProgress = await db.StudentProgress
                .Where(p => p.ExamCompleted)
                .Include(p => p.Story).Include(p => p.Lesson)
                .OrderByDescending(p => p.LastUpdatedAt)
                .Take(15).ToListAsync();

            var recentActivities = recentProgress.Select(p => new RecentActivityDto(
                "exam", p.ChildName,
                p.Story?.Title ?? p.Lesson?.Title ?? "امتحان",
                p.ScorePercentage, null, p.LastUpdatedAt)).ToList();

            return new SchoolDashboardDto(
                totalStudents, totalTeachers, activeWeek,
                Math.Round(avgScore, 1),
                await db.Stories.CountAsync(),
                await db.Lessons.CountAsync(),
                topContent,
                recentActivities,
                BuildPerformanceBands(allProgress),
                await GetClassroomsAsync(),
                await GetLevelDistributionAsync());
        }

        // ── Level Progress ────────────────────────────────────────────────────
        public async Task<List<LevelProgressDto>> GetLevelProgressAsync(string childName)
        {
            var name = childName.Trim();

            var doneProgress = await db.StudentProgress
                .Where(p => p.ChildName == name && p.LessonId.HasValue && p.ExamCompleted)
                .Include(p => p.Lesson)
                .ToListAsync();

            var levelCounts = await db.Lessons
                .Where(l => !l.IsGenerated)
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Total = g.Count() })
                .ToListAsync();

            var defs = new[]
            {
                new { Level=1, Title="الحروف والأصوات",   Subtitle="أتقن كل 28 حرفاً عربياً", Icon="📖", Tag="حروف" },
                new { Level=2, Title="الكلمات والمفردات", Subtitle="تعلم أكثر من 200 كلمة",   Icon="📝", Tag="كلمات" },
                new { Level=3, Title="الجمل والقصص",      Subtitle="اقرأ واكتب جملاً وقصصاً", Icon="📚", Tag="جمل"  },
            };

            var result       = new List<LevelProgressDto>();
            int prevComplete = 0;

            foreach (var d in defs)
            {
                var lp        = doneProgress.Where(p => p.Lesson?.Level == d.Level).ToList();
                int completed = lp.Count;
                int total     = levelCounts.FirstOrDefault(x => x.Level == d.Level)?.Total ?? 0;
                double avg    = lp.Any() ? lp.Average(p => p.ScorePercentage) : 0;
                int stars     = lp.Sum(p => p.ScorePercentage >= 90 ? 3 : p.ScorePercentage >= 70 ? 2 : p.ScorePercentage >= 50 ? 1 : 0);
                bool locked   = d.Level > 1 && prevComplete == 0;

                result.Add(new LevelProgressDto(
                    d.Level, d.Title, d.Subtitle, d.Icon,
                    locked ? "مغلق" : d.Tag,
                    locked, stars, total * 3,
                    completed, total,
                    Math.Round(avg, 1),
                    locked ? $"أكمل المستوى {d.Level - 1} لفتحه" : null));

                prevComplete = completed;
            }
            return result;
        }

        // ── Known names ───────────────────────────────────────────────────────
        public async Task<List<string>> GetKnownChildNamesAsync()
        {
            var a = await db.StudentProgress.Select(p => p.ChildName).Distinct().ToListAsync();
            var b = await db.WritingAttempts.Select(w => w.ChildName).Distinct().ToListAsync();
            return a.Union(b).Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n).ToList();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private async Task<bool> HasAnyActivityAsync(string name) =>
            await db.WritingAttempts.AnyAsync(w => w.ChildName == name)
            || await db.StudentProgress.AnyAsync(p => p.ChildName == name);

        private async Task<int[]> BuildWeeklyActivityAsync(string name)
        {
            var since = DateTime.UtcNow.AddDays(-6);
            var eDates = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted && p.LastUpdatedAt >= since)
                .Select(p => p.LastUpdatedAt).ToListAsync();
            var wDates = await db.WritingAttempts
                .Where(w => w.ChildName == name && w.AttemptedAt >= since)
                .Select(w => w.AttemptedAt).ToListAsync();

            var act = new int[7];
            foreach (var dt in eDates.Concat(wDates))
                act[((int)dt.DayOfWeek + 6) % 7]++;
            return act;
        }

        private async Task<int> CalculateStreakAsync(string name)
        {
            var eDates = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted)
                .Select(p => p.LastUpdatedAt.Date).ToListAsync();
            var wDates = await db.WritingAttempts
                .Where(w => w.ChildName == name)
                .Select(w => w.AttemptedAt.Date).ToListAsync();

            var days = eDates.Concat(wDates).Distinct()
                .OrderByDescending(d => d).ToList();
            if (days.Count == 0) return 0;

            var today = DateTime.UtcNow.Date;
            if (days[0] < today.AddDays(-1)) return 0;

            int streak = 0; var exp = today;
            foreach (var day in days)
            {
                if (day >= exp.AddDays(-1) && day <= exp)
                { streak++; exp = day.AddDays(-1); }
                else break;
            }
            return streak;
        }

        private async Task<List<LessonSummaryDto>> GetInProgressLessonsAsync(string name)
        {
            var startedIds = await db.StudentProgress
                .Where(p => p.ChildName == name && p.LessonId.HasValue && !p.ExamCompleted)
                .Select(p => p.LessonId!.Value).ToListAsync();

            if (startedIds.Count > 0)
            {
                var lessons = await db.Lessons.Include(l => l.Pages)
                    .Where(l => startedIds.Contains(l.Id)).Take(5).ToListAsync();
                return lessons.Select(ToSummaryDto).ToList();
            }

            var doneIds = await db.StudentProgress
                .Where(p => p.ChildName == name && p.LessonId.HasValue && p.ExamCompleted)
                .Select(p => p.LessonId!.Value).ToListAsync();

            var recs = await db.Lessons.Include(l => l.Pages)
                .Where(l => l.Level == 1 && !l.IsGenerated && !doneIds.Contains(l.Id))
                .Take(5).ToListAsync();
            return recs.Select(ToSummaryDto).ToList();
        }

        private async Task<List<TopContentDto>> GetStudentTopContentAsync(string name, bool storyOnly)
        {
            var items = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted
                    && (storyOnly ? p.StoryId.HasValue : p.LessonId.HasValue))
                .Include(p => p.Story).Include(p => p.Lesson)
                .OrderByDescending(p => p.ScorePercentage)
                .Take(5).ToListAsync();

            return items.Select(p => new TopContentDto(
                (storyOnly ? p.StoryId : p.LessonId)!.Value.ToString(),
                (storyOnly ? p.Story?.Title : p.Lesson?.Title) ?? "—",
                storyOnly ? "story" : "lesson",
                1, Math.Round(p.ScorePercentage, 1))).ToList();
        }

        private async Task<List<ExamHistoryDto>> BuildExamHistoryAsync(string name)
        {
            var items = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted)
                .Include(p => p.Story).Include(p => p.Lesson)
                .OrderByDescending(p => p.LastUpdatedAt)
                .Take(10).ToListAsync();

            return items.Select(p => new ExamHistoryDto(
                p.Story?.Title ?? p.Lesson?.Title ?? "امتحان",
                p.ScorePercentage, p.CorrectAnswers, p.TotalQuestions,
                p.LastUpdatedAt)).ToList();
        }

        private async Task<List<RecentActivityDto>> BuildRecentActivityAsync(string name)
        {
            var list = new List<RecentActivityDto>();

            var prog = await db.StudentProgress
                .Where(p => p.ChildName == name && p.ExamCompleted)
                .Include(p => p.Story).Include(p => p.Lesson).ToListAsync();

            list.AddRange(prog.Select(p => new RecentActivityDto(
                "exam", name,
                p.Story?.Title ?? p.Lesson?.Title ?? "امتحان",
                p.ScorePercentage, null, p.LastUpdatedAt)));

            var writings = await db.WritingAttempts.Where(w => w.ChildName == name).ToListAsync();
            list.AddRange(writings.Select(w => new RecentActivityDto(
                "writing", name, w.ExpectedSentence, w.SimilarityScore, w.IsAccepted, w.AttemptedAt)));

            return list.OrderByDescending(a => a.OccurredAt).Take(15).ToList();
        }

        private async Task<StudentSummaryDto> BuildStudentSummaryAsync(string name, int level = 1)
        {
            var progress = await db.StudentProgress.Where(p => p.ChildName == name).ToListAsync();
            var writing  = await db.WritingAttempts.Where(w => w.ChildName == name).ToListAsync();
            double avg   = progress.Any(p => p.ExamCompleted)
                ? progress.Where(p => p.ExamCompleted).Average(p => p.ScorePercentage) : 0;
            DateTime? last = progress.Any()
                ? (DateTime?)progress.Max(p => p.LastUpdatedAt)
                : writing.Any() ? (DateTime?)writing.Max(w => w.AttemptedAt) : null;

            return new StudentSummaryDto(
                name, CalculateStars(progress, writing),
                progress.Count(p => p.StoryId.HasValue  && p.ExamCompleted),
                progress.Count(p => p.LessonId.HasValue && p.ExamCompleted),
                Math.Round(avg, 1),
                writing.Count(w => w.IsAccepted), writing.Count,
                GetPerformanceLevel(avg), last, level);
        }

        private async Task<List<TopContentDto>> BuildTopStoriesAsync() =>
            await db.StudentProgress
                .Where(p => p.ExamCompleted && p.StoryId.HasValue)
                .GroupBy(p => p.StoryId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count(), Avg = g.Average(p => p.ScorePercentage) })
                .OrderByDescending(x => x.Count).Take(5)
                .Join(db.Stories, x => x.Id, s => s.Id,
                    (x, s) => new TopContentDto(s.Id.ToString(), s.Title, "story", x.Count, Math.Round(x.Avg, 1)))
                .ToListAsync();

        private async Task<List<TopContentDto>> BuildTopLessonsAsync()
        {
            var atts = await db.WritingAttempts
                .Join(db.LessonPages, w => w.LessonPageId, p => p.Id,
                    (w, p) => new { w.IsAccepted, p.LessonId })
                .GroupBy(x => x.LessonId)
                .Select(g => new { LessonId = g.Key, Count = g.Count(), Avg = g.Count(x => x.IsAccepted) * 100.0 / g.Count() })
                .OrderByDescending(x => x.Count).Take(5).ToListAsync();

            var ids     = atts.Select(x => x.LessonId).ToList();
            var lessons = await db.Lessons.Where(l => ids.Contains(l.Id)).ToListAsync();

            return atts.Join(lessons, x => x.LessonId, l => l.Id,
                (x, l) => new TopContentDto(l.Id.ToString(), l.Title, "lesson", x.Count, Math.Round(x.Avg, 1)))
                .ToList();
        }

        private async Task<List<ClassroomStatsDto>> GetClassroomsAsync()
        {
            var tIds = await db.Students
                .Where(s => s.TeacherId.HasValue)
                .Select(s => s.TeacherId!.Value).Distinct().ToListAsync();

            if (tIds.Count == 0) return new List<ClassroomStatsDto>();

            var teachers = await db.Users.Where(u => tIds.Contains(u.Id) && u.IsActive).ToListAsync();
            var result   = new List<ClassroomStatsDto>();

            foreach (var t in teachers.Take(8))
            {
                var usernames = await db.Students
                    .Where(s => s.TeacherId == t.Id)
                    .Select(s => s.Username).ToListAsync();
                if (usernames.Count == 0) continue;

                var prog = await db.StudentProgress
                    .Where(p => usernames.Contains(p.ChildName) && p.ExamCompleted).ToListAsync();
                double avg = prog.Any() ? prog.Average(p => p.ScorePercentage) : 0;

                result.Add(new ClassroomStatsDto($"فصل {t.Name}", t.Name, usernames.Count, Math.Round(avg, 1)));
            }
            return result;
        }

        private async Task<List<LevelDistributionDto>> GetLevelDistributionAsync()
        {
            var counts = await db.Students
                .GroupBy(s => s.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() }).ToListAsync();

            int total  = counts.Sum(x => x.Count);
            var colors = new[] { "#F4788A", "#C4B5FD", "#86EFAC" };
            var labels = new[] { "المستوى 1", "المستوى 2", "المستوى 3" };
            var result = new List<LevelDistributionDto>();

            for (int i = 1; i <= 3; i++)
            {
                int cnt  = counts.FirstOrDefault(x => x.Level == i)?.Count ?? 0;
                double p = total > 0 ? Math.Round((double)cnt / total * 100, 1) : 0;
                result.Add(new LevelDistributionDto(i, labels[i-1], p, colors[i-1]));
            }
            return result;
        }

        private static List<PerformanceBandDto> BuildPerformanceBands(
            List<Domain.Entities.StudentProgress> p)
        {
            if (!p.Any()) return new();
            return new()
            {
                new("ممتاز (80٪+)",       p.Count(x => x.ScorePercentage >= 80),             "#48bb78"),
                new("جيد (50-79٪)",        p.Count(x => x.ScorePercentage >= 50 && x.ScorePercentage < 80), "#ed8936"),
                new("يحتاج تحسين (<50٪)", p.Count(x => x.ScorePercentage < 50),              "#fc8181"),
            };
        }

        private static List<SkillBarDto> BuildSkillBars(
            List<Domain.Entities.StudentProgress> p,
            List<Domain.Entities.WritingAttempt>  w)
        {
            double examAvg   = p.Any(x => x.ExamCompleted) ? p.Where(x => x.ExamCompleted).Average(x => x.ScorePercentage) : 0;
            double storyAvg  = p.Any(x => x.ExamCompleted && x.StoryId.HasValue)  ? p.Where(x => x.ExamCompleted && x.StoryId.HasValue).Average(x => x.ScorePercentage)  : examAvg;
            double lessonAvg = p.Any(x => x.ExamCompleted && x.LessonId.HasValue) ? p.Where(x => x.ExamCompleted && x.LessonId.HasValue).Average(x => x.ScorePercentage) : examAvg;
            double writePct  = w.Any() ? (double)w.Count(x => x.IsAccepted) / w.Count * 100 : 0;
            return new()
            {
                new("التعرف على الحروف", (int)Math.Round(lessonAvg)),
                new("طلاقة القراءة",     (int)Math.Round(storyAvg)),
                new("تدريب الكتابة",     (int)Math.Round(writePct)),
                new("المفردات",           (int)Math.Round(examAvg)),
            };
        }

        private static LessonSummaryDto ToSummaryDto(Domain.Entities.Lesson l) => new(
            l.Id, l.Level, l.Letter, l.LetterName, l.Title,
            l.Pages.FirstOrDefault(p => p.IsCoverPage)?.ImagePath
                ?? l.Pages.FirstOrDefault()?.ImagePath ?? "",
            l.Pages.Count);

        private static int CalculateStars(
            List<Domain.Entities.StudentProgress> p,
            List<Domain.Entities.WritingAttempt>  w)
        {
            int stars = p.Where(x => x.ExamCompleted)
                .Sum(x => x.ScorePercentage >= 90 ? 3 : x.ScorePercentage >= 70 ? 2 : x.ScorePercentage >= 50 ? 1 : 0);
            return stars + w.Count(x => x.IsAccepted);
        }

        private static string GetPerformanceLevel(double avg) =>
            avg >= 80 ? "ممتاز" : avg >= 50 ? "جيد" : "يحتاج تحسين";
    }
}
