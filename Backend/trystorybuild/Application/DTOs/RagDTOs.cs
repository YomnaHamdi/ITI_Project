namespace Application.DTOs
{
    public record IngestDocumentRequest(
        string? Letter,
        int? Level,
        string? Tags);

    public record IngestDocumentResponse(
        Guid Id,
        string FileName,
        string DocumentType,
        int ChunkCount,
        string Message);

    public record KnowledgeDocumentDto(
        Guid Id,
        string FileName,
        string DocumentType,
        string? Letter,
        int? Level,
        string? Tags,
        int ChunkCount,
        DateTime IngestedAt);

    public record RagSearchResult(
        string Text,
        string SourceFile,
        int? PageNumber,
        double Distance);

    public record GenerateLessonRequest(
        string Topic,
        int Level,
        string? Letter,
        Guid? CreatorId = null,
        string CreatorRole = "Student",
        Guid? TargetStudentId = null,
        Guid? TargetGroupId = null);

    public record RagPageChunkDto(
        Guid Id,
        string SourceFile,
        int PageNumber,
        string Sentence,
        int WordCount,
        string ImageUrl,
        int Level,
        string Letter,
        string LetterName);

    public record GeneratedLessonDto(
        string Title,
        string Letter,
        int Level,
        List<GeneratedLessonPageDto> Pages);

    public record GeneratedLessonPageDto(
        int PageNumber,
        string Sentence,
        string ImagePrompt,
        string? ActivityType);
}
