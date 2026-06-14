using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        // Story side
        public DbSet<Story> Stories => Set<Story>();
        public DbSet<StoryPage> StoryPages => Set<StoryPage>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
        public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();

        // Lesson side
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<LessonPage> LessonPages => Set<LessonPage>();
        public DbSet<WritingAttempt> WritingAttempts => Set<WritingAttempt>();
        public DbSet<LessonAssignment> LessonAssignments => Set<LessonAssignment>();

        // RAG
        public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
        public DbSet<RagPageChunk> RagPageChunks => Set<RagPageChunk>();

        // Educational PDF library
        public DbSet<PdfDocument> PdfDocuments => Set<PdfDocument>();
        public DbSet<PdfPage> PdfPages => Set<PdfPage>();

        // Level config
        public DbSet<LevelWordConfig> LevelWordConfigs => Set<LevelWordConfig>();

        // Teacher groups
        public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
        public DbSet<StudentGroupMember> StudentGroupMembers => Set<StudentGroupMember>();

        // Placement test
        public DbSet<PlacementQuestion> PlacementQuestions => Set<PlacementQuestion>();

        // Auth / Identity
        public DbSet<User> Users => Set<User>();
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Student> Students => Set<Student>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // StudentGroupMember composite PK
            modelBuilder.Entity<StudentGroupMember>()
                .HasKey(m => new { m.GroupId, m.StudentId });

            // LevelWordConfig PK = Level (int) — manually provided, not auto-increment
            modelBuilder.Entity<LevelWordConfig>()
                .HasKey(c => c.Level);
            modelBuilder.Entity<LevelWordConfig>()
                .Property(c => c.Level)
                .ValueGeneratedNever();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
