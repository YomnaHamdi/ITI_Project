using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
   
        public class StoryRepository(AppDbContext db) : IStoryRepository
        {
            public async Task<Story> SaveAsync(Story story)
            {
                var tracked = db.ChangeTracker.Entries<Story>()
                    .FirstOrDefault(e => e.Entity.Id == story.Id);

                if (tracked is null)
                {
                    var existing = await db.Stories
                        .Include(s => s.Pages)
                        .FirstOrDefaultAsync(s => s.Id == story.Id);

                    if (existing is null)
                        db.Stories.Add(story);
                    else
                        db.Entry(existing).CurrentValues.SetValues(story);
                }

                await db.SaveChangesAsync();
                return story;
            }

            public async Task<Story?> GetByIdAsync(Guid id) =>
                await db.Stories
                    .Include(s => s.Pages)
                    .Include(s => s.Exams).ThenInclude(e => e.Questions).ThenInclude(q => q.Answers)
                    .Include(s => s.Progress)
                    .FirstOrDefaultAsync(s => s.Id == id);

            public async Task<List<Story>> GetAllAsync() =>
                await db.Stories
                    .Include(s => s.Pages)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

            public async Task<bool> DeleteAsync(Guid id)
            {
                var story = await db.Stories.FindAsync(id);
                if (story is null) return false;
                db.Stories.Remove(story);
                await db.SaveChangesAsync();
                return true;
            }
        }

        // ── Lesson Repository (PDF-imported lessons) ───────────────────────────────────
        public class LessonRepository(AppDbContext db) : ILessonRepository
        {
            public async Task<Lesson> SaveAsync(Lesson lesson)
            {
                var tracked = db.ChangeTracker.Entries<Lesson>()
                    .FirstOrDefault(e => e.Entity.Id == lesson.Id);

                if (tracked is not null)
                {
                    // Already tracked — sync unlock status only
                    foreach (var page in lesson.Pages)
                    {
                        var trackedPage = db.ChangeTracker.Entries<LessonPage>()
                            .FirstOrDefault(e => e.Entity.Id == page.Id);
                        if (trackedPage is not null)
                            trackedPage.Entity.IsUnlocked = page.IsUnlocked;
                    }
                }
                else
                {
                    var existing = await db.Lessons
                        .Include(l => l.Pages)
                        .FirstOrDefaultAsync(l => l.Id == lesson.Id);

                    if (existing is null)
                    {
                        db.Lessons.Add(lesson);
                    }
                    else
                    {
                        foreach (var page in lesson.Pages)
                        {
                            var ep = existing.Pages.FirstOrDefault(p => p.Id == page.Id);
                            if (ep is not null) ep.IsUnlocked = page.IsUnlocked;
                        }
                    }
                }

                await db.SaveChangesAsync();
                return lesson;
            }

            public async Task<Lesson?> GetByIdAsync(Guid id) =>
                await db.Lessons
                    .Include(l => l.Pages).ThenInclude(p => p.WritingAttempts)
                    .FirstOrDefaultAsync(l => l.Id == id);

            public async Task<List<Lesson>> GetByLevelAsync(int level) =>
                await db.Lessons
                    .Include(l => l.Pages)
                    .Where(l => l.Level == level)
                    .OrderBy(l => l.Letter)
                    .ToListAsync();

            public async Task<List<Lesson>> GetAllAsync(int? level = null)
            {
                var query = db.Lessons.Include(l => l.Pages).AsQueryable();
                if (level.HasValue)
                    query = query.Where(l => l.Level == level.Value);
                return await query.OrderBy(l => l.Level).ThenBy(l => l.Letter).ToListAsync();
            }

            public async Task<bool> DeleteAsync(Guid id)
            {
                var lesson = await db.Lessons
                    .Include(l => l.Pages).ThenInclude(p => p.WritingAttempts)
                    .FirstOrDefaultAsync(l => l.Id == id);
                if (lesson is null) return false;
                db.Lessons.Remove(lesson);
                await db.SaveChangesAsync();
                return true;
            }

            public async Task<bool> UpdatePageSentenceAsync(Guid pageId, string sentence)
            {
                var page = await db.Set<LessonPage>().FirstOrDefaultAsync(p => p.Id == pageId);
                if (page is null) return false;
                page.Sentence = sentence;
                await db.SaveChangesAsync();
                return true;
            }

            public async Task<Lesson> CreateManualAsync(Lesson lesson)
            {
                db.Lessons.Add(lesson);
                await db.SaveChangesAsync();
                return lesson;
            }
        }

        // ── Exam Repository ────────────────────────────────────────────────────────────
        public class ExamRepository(AppDbContext db) : IExamRepository
        {
            public async Task<Exam> SaveAsync(Exam exam)
            {
                var trackedExam = db.ChangeTracker.Entries<Exam>()
                    .FirstOrDefault(e => e.Entity.Id == exam.Id);

                if (trackedExam is null)
                {
                    var existing = await db.Exams.Include(e => e.Questions)
                        .FirstOrDefaultAsync(e => e.Id == exam.Id);

                    if (existing is null) db.Exams.Add(exam);
                }

                await db.SaveChangesAsync();
                return exam;
            }

            public async Task SaveAnswersAsync(Guid examId, List<StudentAnswer> answers)
            {
                foreach (var answer in answers)
                {
                    answer.Id = answer.Id == Guid.Empty ? Guid.NewGuid() : answer.Id;
                    var entry = db.Entry(answer);
                    if (entry.State == EntityState.Detached)
                        db.StudentAnswers.Add(answer);
                }
                await db.SaveChangesAsync();
            }

            public async Task<Exam?> GetByStoryIdAsync(Guid storyId) =>
                await db.Exams
                    .Include(e => e.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(e => e.StoryId == storyId);

            public async Task<Exam?> GetByLessonIdAsync(Guid lessonId) =>
                await db.Exams
                    .Include(e => e.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(e => e.LessonId == lessonId);

            public async Task<Exam?> GetByIdAsync(Guid examId) =>
                await db.Exams
                    .Include(e => e.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(e => e.Id == examId);
        }

        // ── Other Repositories ─────────────────────────────────────────────────────────
        public class StudentProgressRepository(AppDbContext db) : IStudentProgressRepository
        {
            public async Task<StudentProgress> SaveAsync(StudentProgress progress)
            {
                StudentProgress? existing = null;

                if (progress.StoryId.HasValue)
                    existing = await db.StudentProgress
                        .FirstOrDefaultAsync(p => p.StoryId == progress.StoryId && p.ChildName == progress.ChildName);
                else if (progress.LessonId.HasValue)
                    existing = await db.StudentProgress
                        .FirstOrDefaultAsync(p => p.LessonId == progress.LessonId && p.ChildName == progress.ChildName);

                if (existing is null)
                    db.StudentProgress.Add(progress);
                else
                {
                    existing.CurrentPage     = progress.CurrentPage;
                    existing.CorrectAnswers  = progress.CorrectAnswers;
                    existing.TotalQuestions  = progress.TotalQuestions;
                    existing.ScorePercentage = progress.ScorePercentage;
                    existing.ExamCompleted   = progress.ExamCompleted;
                    existing.LastUpdatedAt   = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                return progress;
            }

            public async Task<StudentProgress?> GetAsync(Guid storyId, string childName) =>
                await db.StudentProgress
                    .FirstOrDefaultAsync(p => p.StoryId == storyId && p.ChildName == childName);

            public async Task<StudentProgress?> GetByLessonAsync(Guid lessonId, string childName) =>
                await db.StudentProgress
                    .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.ChildName == childName);
        }

        public class WritingAttemptRepository(AppDbContext db) : IWritingAttemptRepository
        {
            public async Task<WritingAttempt> SaveAsync(WritingAttempt attempt)
            {
                db.WritingAttempts.Add(attempt);
                await db.SaveChangesAsync();
                return attempt;
            }
        }

        // ── LevelWordConfig Repository ─────────────────────────────────────────────────
        public class LevelWordConfigRepository(AppDbContext db) : ILevelWordConfigRepository
        {
            public async Task<int> GetWordCountAsync(int level)
            {
                var cfg = await db.LevelWordConfigs.FindAsync(level);
                return cfg?.WordCount ?? (level == 1 ? 2 : level == 2 ? 3 : 5);
            }

            public async Task UpsertAsync(int level, int wordCount, string exampleSentence)
            {
                var existing = await db.LevelWordConfigs.FindAsync(level);
                if (existing is null)
                    db.LevelWordConfigs.Add(new LevelWordConfig { Level = level, WordCount = wordCount, ExampleSentence = exampleSentence });
                else
                {
                    existing.WordCount = wordCount;
                    existing.ExampleSentence = exampleSentence;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
            }

            public async Task<List<LevelWordConfig>> GetAllAsync() =>
                await db.LevelWordConfigs.OrderBy(c => c.Level).ToListAsync();
        }

        // ── RagPageChunk Repository ────────────────────────────────────────────────────
        public class RagPageChunkRepository(AppDbContext db) : IRagPageChunkRepository
        {
            public async Task<RagPageChunk> SaveAsync(RagPageChunk chunk)
            {
                db.RagPageChunks.Add(chunk);
                await db.SaveChangesAsync();
                return chunk;
            }

            public async Task<List<RagPageChunk>> GetAllAsync(int? level = null, string? letter = null)
            {
                var query = db.RagPageChunks.AsQueryable();
                if (level.HasValue) query = query.Where(c => c.Level == level.Value);
                if (!string.IsNullOrWhiteSpace(letter)) query = query.Where(c => c.Letter == letter);
                return await query.OrderBy(c => c.Level).ThenBy(c => c.Letter).ThenBy(c => c.PageNumber).ToListAsync();
            }

            public async Task DeleteBySourceFileAsync(string sourceFile)
            {
                var chunks = await db.RagPageChunks.Where(c => c.SourceFile == sourceFile).ToListAsync();
                db.RagPageChunks.RemoveRange(chunks);
                await db.SaveChangesAsync();
            }
        }

        // ── StudentGroup Repository ────────────────────────────────────────────────────
        public class StudentGroupRepository(AppDbContext db) : IStudentGroupRepository
        {
            public async Task<StudentGroup> SaveAsync(StudentGroup group)
            {
                db.StudentGroups.Add(group);
                await db.SaveChangesAsync();
                return group;
            }

            public async Task<StudentGroup?> GetByIdAsync(Guid id) =>
                await db.StudentGroups
                    .Include(g => g.Members).ThenInclude(m => m.Student)
                    .FirstOrDefaultAsync(g => g.Id == id);

            public async Task<List<StudentGroup>> GetByTeacherIdAsync(Guid teacherId) =>
                await db.StudentGroups
                    .Include(g => g.Members).ThenInclude(m => m.Student)
                    .Where(g => g.TeacherId == teacherId)
                    .OrderBy(g => g.Name)
                    .ToListAsync();

            public async Task<bool> AddMemberAsync(Guid groupId, Guid studentId)
            {
                var exists = await db.StudentGroupMembers.AnyAsync(m => m.GroupId == groupId && m.StudentId == studentId);
                if (exists) return false;
                db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = groupId, StudentId = studentId });
                await db.SaveChangesAsync();
                return true;
            }

            public async Task<bool> RemoveMemberAsync(Guid groupId, Guid studentId)
            {
                var member = await db.StudentGroupMembers.FindAsync(groupId, studentId);
                if (member is null) return false;
                db.StudentGroupMembers.Remove(member);
                await db.SaveChangesAsync();
                return true;
            }

            public async Task<bool> DeleteAsync(Guid id)
            {
                var group = await db.StudentGroups.FindAsync(id);
                if (group is null) return false;
                db.StudentGroups.Remove(group);
                await db.SaveChangesAsync();
                return true;
            }

            public async Task<List<StudentGroup>> GetGroupsForStudentAsync(Guid studentId) =>
                await db.StudentGroups
                    .Include(g => g.Members)
                    .Where(g => g.Members.Any(m => m.StudentId == studentId))
                    .ToListAsync();
        }

        // ── LessonAssignment Repository ────────────────────────────────────────────────
        public class LessonAssignmentRepository(AppDbContext db) : ILessonAssignmentRepository
        {
            public async Task<LessonAssignment> SaveAsync(LessonAssignment assignment)
            {
                db.LessonAssignments.Add(assignment);
                await db.SaveChangesAsync();
                return assignment;
            }

            public async Task<List<LessonAssignment>> GetForStudentAsync(Guid studentId, List<Guid> groupIds)
            {
                return await db.LessonAssignments
                    .Include(a => a.Lesson)
                    .Where(a =>
                        (a.TargetType == "Student" && a.TargetStudentId == studentId) ||
                        (a.TargetType == "Group" && a.TargetGroupId != null && groupIds.Contains(a.TargetGroupId.Value)))
                    .OrderByDescending(a => a.AssignedAt)
                    .ToListAsync();
            }

            public async Task<List<LessonAssignment>> GetByTeacherAsync(Guid teacherId) =>
                await db.LessonAssignments
                    .Include(a => a.Lesson)
                    .Where(a => a.TeacherId == teacherId)
                    .OrderByDescending(a => a.AssignedAt)
                    .ToListAsync();
        }

    }

