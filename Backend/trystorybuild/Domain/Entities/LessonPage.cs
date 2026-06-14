namespace Domain.Entities
{
    public class LessonPage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LessonId { get; set; }
        public int PageNumber { get; set; }
        public string Sentence { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string ImagePrompt { get; set; } = string.Empty;
        public bool IsCoverPage { get; set; } = false;
        public bool IsUnlocked { get; set; } = false;

        public Lesson Lesson { get; set; } = null!;
        public List<WritingAttempt> WritingAttempts { get; set; } = new();
    }
}
