namespace Application.DTOs
{
    public record PlacementQuestionDto(
        Guid   Id,
        int    Part,
        int    Order,
        string QuestionText,
        string ImageContent,
        List<PlacementOptionDto> Options,
        string AudioText);

    public record PlacementOptionDto(
        string Key,
        string Emoji,
        string Label);

    public record PlacementSubmitRequest(
        List<PlacementAnswerDto> Answers);

    public record PlacementAnswerDto(
        Guid   QuestionId,
        string Answer);

    public record PlacementResultDto(
        int    TotalScore,
        int    Part1Score,
        int    Part2Score,
        int    Part3Score,
        int    AssignedLevel,
        string LevelName,
        string Message);
}
