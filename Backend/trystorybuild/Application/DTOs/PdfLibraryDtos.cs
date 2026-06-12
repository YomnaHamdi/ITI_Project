namespace Application.DTOs
{
    public record PdfDocumentDto(
        Guid Id,
        string Title,
        string Letter,
        int Level,
        int PageCount,
        int EmbeddedPageCount,
        bool EmbeddingsGenerated,
        DateTime UploadedAt);

    public record PdfPageDto(
        Guid Id,
        int PageNumber,
        string Sentence,
        string ImageUrl,
        bool IsEmbedded);

    public record PdfDocumentDetailDto(
        Guid Id,
        string Title,
        string Letter,
        int Level,
        int PageCount,
        int EmbeddedPageCount,
        bool EmbeddingsGenerated,
        DateTime UploadedAt,
        List<PdfPageDto> Pages);

    public record EmbedResultDto(int EmbeddedCount, string Message);

    public record PdfLibraryStatsDto(
        int TotalPdfs,
        int TotalPages,
        int TotalEmbedded,
        DateTime? LastUpdated);
}
