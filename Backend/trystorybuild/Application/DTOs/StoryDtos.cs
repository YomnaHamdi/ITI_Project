using Domain.Entities;

namespace Application.DTOs
{
    // ── Story Generation ───────────────────────────────────────────────────────────
    public record GenerateStoryRequest(string ChildName, string Character, string Theme);

    public record GenerateStoryResponse(
        Guid Id,
        string Title,
        bool IsApproved,
        List<StoryPageDto> Pages);

    public record StoryPageDto(
        Guid PageId,
        int PageNumber,
        string Sentence,
        string ImageUrl,
        bool IsUnlocked);

    // ── PDF Lesson Import ──────────────────────────────────────────────────────────
    public record ImportBookResponse(
        Guid Id,
        string Title,
        int Level,
        string Letter,
        string LetterName,
        int PageCount);

    public record LessonSummaryDto(
        Guid Id,
        int Level,
        string Letter,
        string LetterName,
        string Title,
        string CoverImageUrl,
        int PageCount);

    public record LessonPageDto(
        Guid PageId,
        int PageNumber,
        string Sentence,
        string ImageUrl,
        bool IsUnlocked,
        bool IsCoverPage);

    public record LessonDetailResponse(
        Guid Id,
        int Level,
        string Letter,
        string LetterName,
        string Title,
        string CoverImageUrl,
        List<LessonPageDto> Pages);

    // ── Internal AI output ─────────────────────────────────────────────────────────
    public record AiStoryOutput(string Title, List<AiStoryPage> Pages);

    public record AiStoryPage(int PageNumber, string Sentence, string ImagePrompt);

    // ── Judge ──────────────────────────────────────────────────────────────────────
    public record JudgeResult(bool IsApproved, string Reason);

    // ── Exam ──────────────────────────────────────────────────────────────────────
    public record ExamResponse(Guid ExamId, Guid StoryId, List<QuestionDto> Questions);

    public record QuestionDto(
        Guid QuestionId,
        int QuestionNumber,
        QuizType Type,
        string Text,
        // MCQ only
        string? OptionA,
        string? OptionB,
        string? OptionC,
        string? OptionD,
        // Non-MCQ raw JSON
        string? DataJson);

    // Internal AI output
    public record AiExamOutput(List<AiQuestion> Questions);

    public record AiQuestion(
        string Type,            // "MCQ" | "Matching" | "DragDrop" | "Ordering"
        string Text,
        // MCQ
        string? OptionA,
        string? OptionB,
        string? OptionC,
        string? OptionD,
        string? CorrectAnswer,
        // Matching
        List<AiMatchPair>? Pairs,
        // DragDrop
        string? Sentence,
        List<string>? Options,
        string? DragAnswer,
        // Ordering
        List<string>? Words,
        List<string>? CorrectOrder);

    public record AiMatchPair(string Left, string Right);

    // ── Exam Submission ────────────────────────────────────────────────────────────
    public record SubmitExamRequest(Guid ExamId, string ChildName, List<SubmitAnswer> Answers);

    public record SubmitAnswer(Guid QuestionId, string ChosenAnswer);

    public record ExamResultResponse(
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        List<AnswerFeedback> Feedback);

    public record AnswerFeedback(
        Guid QuestionId,
        QuizType Type,
        string ChosenAnswer,
        string CorrectAnswer,
        bool IsCorrect);

    // ── Writing Correction ─────────────────────────────────────────────────────────
    public record WritingCorrectionResponse(
        string ExtractedText,
        string ExpectedSentence,
        double SimilarityScore,
        bool IsAccepted,
        string Message);

    // ── Student Groups ─────────────────────────────────────────────────────────────
    public record StudentGroupDto(
        Guid Id,
        string Name,
        Guid TeacherId,
        int MemberCount,
        DateTime CreatedAt,
        List<StudentGroupMemberDto> Members);

    public record StudentGroupMemberDto(Guid StudentId, string StudentName, DateTime AddedAt);

    public record CreateGroupRequest(string Name);

    public record AddGroupMemberRequest(Guid StudentId);

    public record AssignLessonRequest(
        Guid LessonId,
        string TargetType,    // "Student" | "Group"
        Guid? TargetStudentId,
        Guid? TargetGroupId);

    public record LessonAssignmentDto(
        Guid Id,
        Guid LessonId,
        string LessonTitle,
        string TargetType,
        Guid? TargetStudentId,
        string? TargetStudentName,
        Guid? TargetGroupId,
        string? TargetGroupName,
        DateTime AssignedAt);

    // ── Level Word Config ──────────────────────────────────────────────────────────
    public record LevelWordConfigDto(int Level, int WordCount, string ExampleSentence);

    // ── Admin Book Management ──────────────────────────────────────────────────────
    public record AdminBooksPageDto(
        List<LessonSummaryDto> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    public record ManualPageDto(string Sentence);

    public record CreateManualBookRequest(
        string Title,
        string LetterName,
        string Letter,
        int Level,
        List<ManualPageDto> Pages);

    public record UpdatePageSentenceRequest(string Sentence);

    // ── Progress ───────────────────────────────────────────────────────────────────
    public record ProgressResponse(
        Guid StoryId,
        string ChildName,
        int CurrentPage,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        bool ExamCompleted);

    public record LessonProgressRequest(
        Guid LessonId,
        string ChildName,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        bool ExamCompleted);
}
