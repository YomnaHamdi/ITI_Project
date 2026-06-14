namespace Domain.Entities
{
    public class LevelWordConfig
    {
        public int Level { get; set; }
        public int WordCount { get; set; }
        public string ExampleSentence { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
