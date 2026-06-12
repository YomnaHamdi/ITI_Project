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

        // RAG
        public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

        // Educational PDF library
        public DbSet<PdfDocument> PdfDocuments => Set<PdfDocument>();
        public DbSet<PdfPage>     PdfPages     => Set<PdfPage>();

        // Placement test
        public DbSet<PlacementQuestion> PlacementQuestions => Set<PlacementQuestion>();

        // Auth / Identity
        public DbSet<User>    Users    => Set<User>();
        public DbSet<Parent>  Parents  => Set<Parent>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Student> Students => Set<Student>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
