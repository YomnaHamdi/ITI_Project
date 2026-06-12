namespace Domain.Entities
{
    public class PdfPage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PdfDocumentId { get; set; }
        public PdfDocument Document { get; set; } = null!;
        public int PageNumber { get; set; }
        public string Sentence { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? ChromaId { get; set; }
        public bool IsEmbedded { get; set; }
    }
}
