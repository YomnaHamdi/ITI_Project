using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class StoryPage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StoryId { get; set; }
        public int PageNumber { get; set; }
        public string Sentence { get; set; } = string.Empty;
        public string ImagePrompt { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; } = true; // always true — AI stories have no writing gate

        public Story Story { get; set; } = null!;
    }
}
