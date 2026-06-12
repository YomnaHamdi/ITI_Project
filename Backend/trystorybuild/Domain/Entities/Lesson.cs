using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Level { get; set; }
        public string Letter { get; set; } = string.Empty;
        public string LetterName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<LessonPage> Pages { get; set; } = new();
    }
}
