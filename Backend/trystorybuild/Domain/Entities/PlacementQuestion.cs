namespace Domain.Entities
{
    public class PlacementQuestion
    {
        public Guid   Id             { get; set; } = Guid.NewGuid();
        public int    Part           { get; set; } // 1, 2, 3
        public int    Order          { get; set; } // within part
        public string QuestionText   { get; set; } = string.Empty;
        public string ImageContent   { get; set; } = string.Empty; // emoji or letter
        public string OptionsJson    { get; set; } = "[]"; // JSON array of {key,emoji,label}
        public string CorrectAnswer  { get; set; } = string.Empty; // "A","B","C","D"
        public string AudioText      { get; set; } = string.Empty; // text for TTS
    }
}
