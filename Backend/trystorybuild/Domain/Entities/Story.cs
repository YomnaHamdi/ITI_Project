using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Story
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string ChildName { get; set; } = string.Empty;
        public string Character { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<StoryPage> Pages { get; set; } = new();
        public List<Exam> Exams { get; set; } = new();
        public List<StudentProgress> Progress { get; set; } = new();
    }
}
