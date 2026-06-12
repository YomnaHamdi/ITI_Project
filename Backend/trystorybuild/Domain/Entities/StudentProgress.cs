using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class StudentProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? StoryId { get; set; }
        public Guid? LessonId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double ScorePercentage { get; set; }
        public bool ExamCompleted { get; set; } = false;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public Story? Story { get; set; }
        public Lesson? Lesson { get; set; }
    }
}
