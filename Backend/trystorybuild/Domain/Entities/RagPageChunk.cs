namespace Domain.Entities
{
    public class RagPageChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SourceFile { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Sentence { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Letter { get; set; } = string.Empty;
        public string LetterName { get; set; } = string.Empty;
        public string ChromaChunkId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
