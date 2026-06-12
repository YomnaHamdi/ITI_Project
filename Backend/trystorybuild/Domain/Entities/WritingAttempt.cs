using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class WritingAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LessonPageId { get; set; }        // was StoryPageId
        public string ChildName { get; set; } = string.Empty;
        public string UploadedImagePath { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string ExpectedSentence { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        public LessonPage LessonPage { get; set; } = null!; // was StoryPage
    }
}
