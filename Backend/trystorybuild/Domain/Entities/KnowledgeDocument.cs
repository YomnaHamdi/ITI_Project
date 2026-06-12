namespace Domain.Entities
{
    public class KnowledgeDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty; // pdf | pptx | image
        public string? Letter { get; set; }
        public int? Level { get; set; }
        public string? Tags { get; set; }
        public int ChunkCount { get; set; }
        public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
        public string FilePath { get; set; } = string.Empty;
    }
}
