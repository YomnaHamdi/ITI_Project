using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class StudentAnswer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuestionId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public string ChosenAnswer { get; set; } = string.Empty;  // "A" | "B" | "C" | "D"
        public bool IsCorrect { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        public Question Question { get; set; } = null!;
    }

}
