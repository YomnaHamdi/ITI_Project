using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Exam
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? StoryId  { get; set; }
        public Guid? LessonId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Story? Story { get; set; }
        public List<Question> Questions { get; set; } = new();
    }
}
