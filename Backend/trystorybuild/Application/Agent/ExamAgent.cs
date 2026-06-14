using Application.DTOs;
using Application.Interfaces;
using Application.Prompts;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Agent
{
    public sealed class ExamAgent(
        IExamGeneratorService examGenerator,
        IExamRepository examRepository,
        IStoryRepository storyRepository,
        ILessonRepository lessonRepository,
        IStudentProgressRepository progressRepository,
        ILogger<ExamAgent> logger)
    {
        // ── Generate from story ───────────────────────────────────────────────

        public async Task<ExamResponse> GenerateAsync(Guid storyId)
        {
            logger.LogInformation("[ExamAgent] Generating exam for story {Id}", storyId);

            var story = await storyRepository.GetByIdAsync(storyId)
                ?? throw new InvalidOperationException($"Story {storyId} not found.");

            var sentences = story.Pages
                .OrderBy(p => p.PageNumber)
                .Select(p => p.Sentence)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(3)
                .ToList();

            var aiOutput = await examGenerator.GenerateAsync(AgentPrompts.ExamUserPrompt(sentences));

            var exam = new Exam { StoryId = storyId };
            int qNum = 1;

            foreach (var q in aiOutput.Questions)
            {
                var question = BuildQuestion(q, exam.Id, qNum++);
                exam.Questions.Add(question);
            }

            await examRepository.SaveAsync(exam);
            logger.LogInformation("[ExamAgent] Story exam saved — {Count} questions", exam.Questions.Count);

            return MapToResponse(exam, storyId);
        }

        // ── Generate from lesson ──────────────────────────────────────────────

        public async Task<ExamResponse> GenerateFromLessonAsync(Guid lessonId)
        {
            logger.LogInformation("[ExamAgent] Generating exam for lesson {Id}", lessonId);

            var lesson = await lessonRepository.GetByIdAsync(lessonId)
                ?? throw new InvalidOperationException($"Lesson {lessonId} not found.");

            // Pages with images — used to attach to "ما هذا؟" questions
            var pagesWithImages = lesson.Pages
                .OrderBy(p => p.PageNumber)
                .Where(p => !p.IsCoverPage && !string.IsNullOrWhiteSpace(p.ImagePath))
                .ToList();

            var sentences = lesson.Pages
                .OrderBy(p => p.PageNumber)
                .Select(p => p.Sentence)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(3)
                .ToList();

            var aiOutput = await examGenerator.GenerateLessonAsync(AgentPrompts.LessonExamUserPrompt(sentences));

            var exam = new Exam { LessonId = lessonId };
            int qNum = 1;

            foreach (var q in aiOutput.Questions)
            {
                var question = BuildQuestion(q, exam.Id, qNum++);

                // Attach a page image for "ما هذا؟" questions
                if (question.Type == QuizType.MCQ
                    && question.Text.Contains("ما هذا", StringComparison.OrdinalIgnoreCase)
                    && pagesWithImages.Count > 0)
                {
                    var imgPath = pagesWithImages[0].ImagePath;
                    question.DataJson = JsonSerializer.Serialize(new { imageUrl = imgPath });
                }

                exam.Questions.Add(question);
            }

            await examRepository.SaveAsync(exam);
            logger.LogInformation("[ExamAgent] Lesson exam saved — {Count} questions", exam.Questions.Count);

            return MapToResponse(exam, lessonId);
        }

        // ── Submit ────────────────────────────────────────────────────────────

        public async Task<ExamResultResponse> SubmitAsync(SubmitExamRequest request)
        {
            logger.LogInformation("[ExamAgent] Submitting exam {ExamId}", request.ExamId);

            var exam = await examRepository.GetByIdAsync(request.ExamId)
                ?? throw new InvalidOperationException($"Exam {request.ExamId} not found.");

            var feedback   = new List<AnswerFeedback>();
            var newAnswers = new List<StudentAnswer>();
            int correct    = 0;

            foreach (var submitted in request.Answers)
            {
                var question = exam.Questions.FirstOrDefault(q => q.Id == submitted.QuestionId);
                if (question is null) continue;

                var isCorrect = EvaluateAnswer(question, submitted.ChosenAnswer);
                if (isCorrect) correct++;

                newAnswers.Add(new StudentAnswer
                {
                    Id           = Guid.NewGuid(),
                    QuestionId   = question.Id,
                    ChildName    = request.ChildName,
                    ChosenAnswer = submitted.ChosenAnswer,
                    IsCorrect    = isCorrect,
                    AnsweredAt   = DateTime.UtcNow
                });

                feedback.Add(new AnswerFeedback(
                    question.Id,
                    question.Type,
                    submitted.ChosenAnswer,
                    question.CorrectAnswer,
                    isCorrect));
            }

            await examRepository.SaveAnswersAsync(exam.Id, newAnswers);

            int total = exam.Questions.Count;
            double score = total > 0 ? Math.Round((double)correct / total * 100.0, 1) : 0;

            if (!string.IsNullOrWhiteSpace(request.ChildName))
            {
                if (exam.StoryId.HasValue)
                {
                    await progressRepository.SaveAsync(new StudentProgress
                    {
                        StoryId         = exam.StoryId.Value,
                        ChildName       = request.ChildName,
                        TotalQuestions  = total,
                        CorrectAnswers  = correct,
                        ScorePercentage = score,
                        ExamCompleted   = true,
                        CurrentPage     = 3
                    });
                }
                else if (exam.LessonId.HasValue)
                {
                    await progressRepository.SaveAsync(new StudentProgress
                    {
                        LessonId        = exam.LessonId.Value,
                        ChildName       = request.ChildName,
                        TotalQuestions  = total,
                        CorrectAnswers  = correct,
                        ScorePercentage = score,
                        ExamCompleted   = true,
                        CurrentPage     = 1
                    });
                }
            }

            logger.LogInformation("[ExamAgent] Score: {C}/{T} = {S}%", correct, total, score);
            return new ExamResultResponse(total, correct, score, feedback);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Question BuildQuestion(AiQuestion ai, Guid examId, int number)
        {
            var type = ai.Type?.Trim().ToUpper() switch
            {
                "MATCHING" => QuizType.Matching,
                "DRAGDROP" => QuizType.DragDrop,
                "ORDERING" => QuizType.Ordering,
                _          => QuizType.MCQ
            };

            var question = new Question
            {
                ExamId         = examId,
                QuestionNumber = number,
                Type           = type,
                Text           = ai.Text ?? string.Empty,
                CorrectAnswer  = string.Empty
            };

            switch (type)
            {
                case QuizType.MCQ:
                    question.OptionA       = ai.OptionA;
                    question.OptionB       = ai.OptionB;
                    question.OptionC       = ai.OptionC;
                    question.OptionD       = ai.OptionD;
                    question.CorrectAnswer = (ai.CorrectAnswer ?? "A").Trim().ToUpper();
                    question.DataJson      = "{}";
                    break;

                case QuizType.Matching:
                    var pairs = ai.Pairs ?? new();
                    question.CorrectAnswer = JsonSerializer.Serialize(pairs.Select(p => p.Right).ToList());
                    question.DataJson      = JsonSerializer.Serialize(new
                    {
                        pairs = pairs.Select(p => new { left = p.Left, right = p.Right }).ToList()
                    });
                    break;

                case QuizType.DragDrop:
                    question.CorrectAnswer = ai.DragAnswer ?? string.Empty;
                    question.DataJson      = JsonSerializer.Serialize(new
                    {
                        sentence = ai.Sentence ?? "___",
                        options  = ai.Options ?? new List<string>()
                    });
                    break;

                case QuizType.Ordering:
                    var correctOrder = ai.CorrectOrder ?? new();
                    question.CorrectAnswer = JsonSerializer.Serialize(correctOrder);
                    question.DataJson      = JsonSerializer.Serialize(new
                    {
                        words = (ai.Words ?? correctOrder).OrderBy(_ => Guid.NewGuid()).ToList()
                    });
                    break;
            }

            return question;
        }

        private static bool EvaluateAnswer(Question question, string chosenAnswer)
        {
            if (string.IsNullOrWhiteSpace(chosenAnswer)) return false;

            switch (question.Type)
            {
                case QuizType.MCQ:
                    return string.Equals(
                        chosenAnswer.Trim().ToUpper(),
                        question.CorrectAnswer.Trim().ToUpper(),
                        StringComparison.Ordinal);

                case QuizType.DragDrop:
                    return string.Equals(
                        chosenAnswer.Trim(),
                        question.CorrectAnswer.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                case QuizType.Matching:
                case QuizType.Ordering:
                    try
                    {
                        var chosen  = JsonSerializer.Deserialize<List<string>>(chosenAnswer)  ?? new();
                        var correct = JsonSerializer.Deserialize<List<string>>(question.CorrectAnswer) ?? new();
                        if (chosen.Count != correct.Count) return false;
                        return chosen.Zip(correct)
                            .All(pair => string.Equals(
                                pair.First.Trim(), pair.Second.Trim(),
                                StringComparison.OrdinalIgnoreCase));
                    }
                    catch { return false; }

                default:
                    return false;
            }
        }

        public static ExamResponse MapToResponse(Exam exam, Guid storyId) =>
            new(exam.Id,
                storyId,
                exam.Questions
                    .OrderBy(q => q.QuestionNumber)
                    .Select(q => new QuestionDto(
                        q.Id,
                        q.QuestionNumber,
                        q.Type,
                        q.Text,
                        q.OptionA,
                        q.OptionB,
                        q.OptionC,
                        q.OptionD,
                        q.DataJson == "{}" ? null : q.DataJson))
                    .ToList());
    }
}
