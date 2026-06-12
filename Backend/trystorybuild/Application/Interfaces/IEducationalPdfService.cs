using Application.DTOs;

namespace Application.Interfaces
{
    public interface IEducationalPdfService
    {
        Task<PdfDocumentDto> UploadAsync(
            Stream fileStream, string fileName,
            string letter, int level,
            CancellationToken ct = default);

        Task<EmbedResultDto> GenerateEmbeddingsAsync(
            Guid pdfId, CancellationToken ct = default);

        Task<List<PdfDocumentDto>> GetAllAsync();
        Task<PdfDocumentDetailDto> GetDetailAsync(Guid id);
        Task DeleteAsync(Guid id);
        Task<PdfLibraryStatsDto> GetStatsAsync();
    }
}
