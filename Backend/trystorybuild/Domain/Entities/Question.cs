namespace Domain.Entities
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ExamId { get; set; }
        public int QuestionNumber { get; set; }
        public QuizType Type { get; set; } = QuizType.MCQ;
        public string Text { get; set; } = string.Empty;

        // MCQ only — nullable for other types
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;

        // Matching / DragDrop / Ordering — serialised JSON
        public string DataJson { get; set; } = "{}";

        public Exam Exam { get; set; } = null!;
        public List<StudentAnswer> Answers { get; set; } = new();
    }
}
