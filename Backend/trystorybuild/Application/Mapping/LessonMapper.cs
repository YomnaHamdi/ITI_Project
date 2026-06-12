using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class LessonMapper
{
    public static LessonSummaryDto ToSummary(Lesson lesson) =>
     new(
         lesson.Id,
         lesson.Level,
         lesson.Letter,
         lesson.LetterName,
         lesson.Title,
         lesson.CoverImagePath,
         lesson.Pages.Count);

    public static LessonDetailResponse ToDetail(Lesson lesson) =>
        new(
            lesson.Id,
            lesson.Level,
            lesson.Letter,
            lesson.LetterName,
            lesson.Title,
            lesson.CoverImagePath,
            lesson.Pages
                .OrderBy(p => p.PageNumber)
                .Select(p => new LessonPageDto(
                    p.Id,
                    p.PageNumber,
                    p.Sentence,
                    p.ImagePath,
                    p.IsUnlocked,
                    p.IsCoverPage))
                .ToList());
}
