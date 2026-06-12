using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Repositories
{
    public class PlacementRepository(AppDbContext db) : IPlacementRepository
    {
        public async Task<List<PlacementQuestion>> GetAllAsync() =>
            await db.PlacementQuestions
                .OrderBy(q => q.Part)
                .ThenBy(q => q.Order)
                .ToListAsync();

        public async Task<List<PlacementQuestion>> GetByPartAsync(int part) =>
            await db.PlacementQuestions
                .Where(q => q.Part == part)
                .OrderBy(q => q.Order)
                .ToListAsync();

        public async Task SeedAsync()
        {
            if (await db.PlacementQuestions.AnyAsync()) return;

            var questions = new List<PlacementQuestion>
            {
                // Part 1 — Visual Recognition
                new() { Part=1, Order=1, QuestionText="أي حرف هذا؟", ImageContent="أ",
                    AudioText="أي حرف هذا؟", CorrectAnswer="B",
                    OptionsJson=Opts("ب","أ","ج","د") },
                new() { Part=1, Order=2, QuestionText="ما الحيوان في الصورة؟", ImageContent="🦁",
                    AudioText="ما الحيوان في الصورة؟", CorrectAnswer="B",
                    OptionsJson=OptsEmoji(("🐘","فيل"),("🦁","أسد"),("🐸","ضفدع"),("🐳","حوت")) },
                new() { Part=1, Order=3, QuestionText="أي حرف هذا؟", ImageContent="ب",
                    AudioText="أي حرف هذا؟", CorrectAnswer="C",
                    OptionsJson=Opts("ت","ث","ب","ن") },
                new() { Part=1, Order=4, QuestionText="ما لون التفاحة؟", ImageContent="🍎",
                    AudioText="ما لون التفاحة؟", CorrectAnswer="C",
                    OptionsJson=Opts("أزرق","أخضر","أحمر","أصفر") },
                new() { Part=1, Order=5, QuestionText="أي حرف هذا؟", ImageContent="ج",
                    AudioText="أي حرف هذا؟", CorrectAnswer="C",
                    OptionsJson=Opts("ح","خ","ج","ع") },

                // Part 2 — Letter Knowledge
                new() { Part=2, Order=1, QuestionText="ما أول حرف في كلمة (فيل)؟", ImageContent="🐘",
                    AudioText="ما أول حرف في كلمة فيل؟", CorrectAnswer="B",
                    OptionsJson=Opts("ق","ف","ب","م") },
                new() { Part=2, Order=2, QuestionText="ما أول حرف في كلمة (قمر)؟", ImageContent="🌙",
                    AudioText="ما أول حرف في كلمة قمر؟", CorrectAnswer="A",
                    OptionsJson=Opts("ق","ك","م","ر") },
                new() { Part=2, Order=3, QuestionText="ما أول حرف في كلمة (بيت)؟", ImageContent="🏠",
                    AudioText="ما أول حرف في كلمة بيت؟", CorrectAnswer="C",
                    OptionsJson=Opts("ت","ي","ب","هـ") },
                new() { Part=2, Order=4, QuestionText="ما أول حرف في كلمة (قطة)؟", ImageContent="🐱",
                    AudioText="ما أول حرف في كلمة قطة؟", CorrectAnswer="C",
                    OptionsJson=Opts("ط","ة","ق","ه") },
                new() { Part=2, Order=5, QuestionText="ما أول حرف في كلمة (زهرة)؟", ImageContent="🌸",
                    AudioText="ما أول حرف في كلمة زهرة؟", CorrectAnswer="B",
                    OptionsJson=Opts("هـ","ز","ر","ة") },

                // Part 3 — Sentence Construction
                new() { Part=3, Order=1, QuestionText="رتب الكلمات: يلعب / الأسد / في / الغابة", ImageContent="📚",
                    AudioText="رتب الكلمات الصحيحة", CorrectAnswer="A",
                    OptionsJson=Opts("الأسد يلعب في الغابة","يلعب الغابة في الأسد","في الأسد الغابة يلعب","الغابة في الأسد يلعب") },
                new() { Part=3, Order=2, QuestionText="أكمل الجملة: الفراشة ___", ImageContent="🦋",
                    AudioText="أكمل الجملة", CorrectAnswer="B",
                    OptionsJson=Opts("تسبح","تطير","تنام","تأكل الحجر") },
                new() { Part=3, Order=3, QuestionText="ما معنى كلمة (بحر)؟", ImageContent="🌊",
                    AudioText="ما معنى كلمة بحر؟", CorrectAnswer="C",
                    OptionsJson=Opts("جبل","غابة","ماء كثير","نهر صغير") },
                new() { Part=3, Order=4, QuestionText="أين نقرأ القصص؟", ImageContent="📖",
                    AudioText="أين نقرأ القصص؟", CorrectAnswer="A",
                    OptionsJson=Opts("في الكتاب","في المطبخ","في الملعب","في الحديقة") },
                new() { Part=3, Order=5, QuestionText="كم عدد حروف كلمة (أسد)؟", ImageContent="🦁",
                    AudioText="كم عدد حروف كلمة أسد؟", CorrectAnswer="B",
                    OptionsJson=Opts("حرفان","ثلاثة حروف","أربعة حروف","خمسة حروف") },
            };

            db.PlacementQuestions.AddRange(questions);
            await db.SaveChangesAsync();
        }

        private static string Opts(string a, string b, string c, string d) =>
            JsonSerializer.Serialize(new[]
            {
                new { key = "A", emoji = "", label = a },
                new { key = "B", emoji = "", label = b },
                new { key = "C", emoji = "", label = c },
                new { key = "D", emoji = "", label = d }
            });

        private static string OptsEmoji(
            (string emoji, string label) a,
            (string emoji, string label) b,
            (string emoji, string label) c,
            (string emoji, string label) d) =>
            JsonSerializer.Serialize(new[]
            {
                new { key = "A", emoji = a.emoji, label = a.label },
                new { key = "B", emoji = b.emoji, label = b.label },
                new { key = "C", emoji = c.emoji, label = c.label },
                new { key = "D", emoji = d.emoji, label = d.label }
            });
    }
}
