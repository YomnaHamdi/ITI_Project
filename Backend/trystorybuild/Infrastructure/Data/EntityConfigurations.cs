using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data
{
    public class StoryConfiguration : IEntityTypeConfiguration<Story>
    {
        public void Configure(EntityTypeBuilder<Story> b)
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Title).HasMaxLength(200).IsRequired();
            b.Property(s => s.ChildName).HasMaxLength(100);
            b.Property(s => s.Character).HasMaxLength(100);
            b.Property(s => s.Theme).HasMaxLength(100);

            b.HasMany(s => s.Pages)
             .WithOne(p => p.Story)
             .HasForeignKey(p => p.StoryId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(s => s.Exams)
             .WithOne(e => e.Story)
             .HasForeignKey(e => e.StoryId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.ClientSetNull);

            b.HasMany(s => s.Progress)
             .WithOne(p => p.Story)
             .HasForeignKey(p => p.StoryId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StoryPageConfiguration : IEntityTypeConfiguration<StoryPage>
    {
        public void Configure(EntityTypeBuilder<StoryPage> b)
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Sentence).HasMaxLength(500).IsRequired();
            b.Property(p => p.ImagePrompt).HasMaxLength(1000);
            b.Property(p => p.ImagePath).HasMaxLength(500).IsRequired();
        }
    }

    public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
    {
        public void Configure(EntityTypeBuilder<Lesson> b)
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Title).HasMaxLength(200).IsRequired();
            b.Property(l => l.Letter).HasMaxLength(10);
            b.Property(l => l.LetterName).HasMaxLength(100);
            b.Property(l => l.CoverImagePath).HasMaxLength(500);
            b.HasIndex(l => new { l.Level, l.Letter });

            b.HasMany(l => l.Pages)
             .WithOne(p => p.Lesson)
             .HasForeignKey(p => p.LessonId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LessonPageConfiguration : IEntityTypeConfiguration<LessonPage>
    {
        public void Configure(EntityTypeBuilder<LessonPage> b)
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Sentence).HasMaxLength(500).IsRequired();
            b.Property(p => p.ImagePath).HasMaxLength(500).IsRequired();

            b.HasMany(p => p.WritingAttempts)
             .WithOne(w => w.LessonPage)
             .HasForeignKey(w => w.LessonPageId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ExamConfiguration : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> b)
        {
            b.HasKey(e => e.Id);
            b.HasMany(e => e.Questions)
             .WithOne(q => q.Exam)
             .HasForeignKey(q => q.ExamId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> b)
        {
            b.HasKey(q => q.Id);
            b.Property(q => q.Text).HasMaxLength(500).IsRequired();

            // MCQ — nullable because Matching/DragDrop/Ordering don't use them
            b.Property(q => q.OptionA).HasMaxLength(200);
            b.Property(q => q.OptionB).HasMaxLength(200);
            b.Property(q => q.OptionC).HasMaxLength(200);
            b.Property(q => q.OptionD).HasMaxLength(200);

            // CorrectAnswer holds plain "A" for MCQ, JSON array for others
            b.Property(q => q.CorrectAnswer).HasMaxLength(2000).IsRequired();

            // JSON payload for non-MCQ types
            b.Property(q => q.DataJson).HasColumnType("nvarchar(max)");

            // Type stored as int
            b.Property(q => q.Type).HasConversion<int>();

            b.HasMany(q => q.Answers)
             .WithOne(a => a.Question)
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
    {
        public void Configure(EntityTypeBuilder<StudentAnswer> b)
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.ChildName).HasMaxLength(100).IsRequired();
            // Was HasMaxLength(1) — now holds JSON arrays for Matching/Ordering
            b.Property(a => a.ChosenAnswer).HasMaxLength(2000).IsRequired();
        }
    }

    public class WritingAttemptConfiguration : IEntityTypeConfiguration<WritingAttempt>
    {
        public void Configure(EntityTypeBuilder<WritingAttempt> b)
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.ChildName).HasMaxLength(100).IsRequired();
            b.Property(w => w.ExtractedText).HasMaxLength(1000);
            b.Property(w => w.ExpectedSentence).HasMaxLength(500).IsRequired();
            b.Property(w => w.UploadedImagePath).HasMaxLength(500);
        }
    }

    public class StudentProgressConfiguration : IEntityTypeConfiguration<StudentProgress>
    {
        public void Configure(EntityTypeBuilder<StudentProgress> b)
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.ChildName).HasMaxLength(100).IsRequired();
            b.HasIndex(p => new { p.StoryId, p.ChildName }).IsUnique();
        }
    }

    public class PdfDocumentConfiguration : IEntityTypeConfiguration<PdfDocument>
    {
        public void Configure(EntityTypeBuilder<PdfDocument> b)
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Title).HasMaxLength(300).IsRequired();
            b.Property(d => d.FilePath).HasMaxLength(1000).IsRequired();
            b.Property(d => d.Letter).HasMaxLength(10).IsRequired();
            b.HasIndex(d => new { d.Level, d.Letter });

            b.HasMany(d => d.Pages)
             .WithOne(p => p.Document)
             .HasForeignKey(p => p.PdfDocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PdfPageConfiguration : IEntityTypeConfiguration<PdfPage>
    {
        public void Configure(EntityTypeBuilder<PdfPage> b)
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Sentence).HasMaxLength(500).IsRequired();
            b.Property(p => p.ImageUrl).HasMaxLength(500).IsRequired();
            b.Property(p => p.ChromaId).HasMaxLength(200);
        }
    }

    public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
    {
        public void Configure(EntityTypeBuilder<KnowledgeDocument> b)
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            b.Property(d => d.DocumentType).HasMaxLength(20).IsRequired();
            b.Property(d => d.Letter).HasMaxLength(10);
            b.Property(d => d.Tags).HasMaxLength(500);
            b.Property(d => d.FilePath).HasMaxLength(1000);
        }
    }

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Name).HasMaxLength(100).IsRequired();
            b.Property(u => u.Email).HasMaxLength(200).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            b.Property(u => u.Role).HasConversion<int>();

            b.HasOne(u => u.Parent)
             .WithOne(p => p.User)
             .HasForeignKey<Parent>(p => p.Id)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(u => u.Teacher)
             .WithOne(t => t.User)
             .HasForeignKey<Teacher>(t => t.Id)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ParentConfiguration : IEntityTypeConfiguration<Parent>
    {
        public void Configure(EntityTypeBuilder<Parent> b)
        {
            b.HasKey(p => p.Id);
            b.HasMany(p => p.Children)
             .WithOne(s => s.Parent)
             .HasForeignKey(s => s.ParentId)
             .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }

    public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
    {
        public void Configure(EntityTypeBuilder<Teacher> b)
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.SchoolCode).HasMaxLength(50);
            b.HasMany(t => t.Students)
             .WithOne(s => s.Teacher)
             .HasForeignKey(s => s.TeacherId)
             .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }

    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> b)
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(100).IsRequired();
            b.Property(s => s.Username).HasMaxLength(50).IsRequired();
            b.HasIndex(s => s.Username).IsUnique();
            b.Property(s => s.AvatarUrl).HasMaxLength(500);
            b.Property(s => s.PasswordHash).HasMaxLength(500);
            b.Property(s => s.LoginMethod).HasConversion<int>();
        }
    }

    public class PlacementQuestionConfiguration : IEntityTypeConfiguration<PlacementQuestion>
    {
        public void Configure(EntityTypeBuilder<PlacementQuestion> b)
        {
            b.HasKey(q => q.Id);
            b.Property(q => q.QuestionText).HasMaxLength(500).IsRequired();
            b.Property(q => q.ImageContent).HasMaxLength(500);
            b.Property(q => q.OptionsJson).HasColumnType("nvarchar(max)");
            b.Property(q => q.CorrectAnswer).HasMaxLength(5).IsRequired();
            b.Property(q => q.AudioText).HasMaxLength(500);
        }
    }
}
