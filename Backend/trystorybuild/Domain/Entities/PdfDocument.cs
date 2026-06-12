namespace Domain.Entities
{
    public class PdfDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Letter { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public int EmbeddedPageCount { get; set; }
        public bool EmbeddingsGenerated { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public List<PdfPage> Pages { get; set; } = new();
    }
}
