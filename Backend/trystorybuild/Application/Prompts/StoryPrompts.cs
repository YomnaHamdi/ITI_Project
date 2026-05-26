using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Prompts
{
    public static class StoryPrompts
    {
        /// <summary>
        /// System prompt instructing Qwen3 to act as a children's Arabic teacher
        /// and return a strictly-structured JSON story.
        /// </summary>
        public const string SystemPrompt =
            """
        أنت معلم لغة عربية للأطفال من عمر 3 إلى 6 سنوات.
        مهمتك إنشاء قصة أطفال قصيرة وممتعة.

        القواعد الصارمة:
        - اكتب الجمل بالعربية الفصحى البسيطة جداً
        - كل جملة يجب أن تكون من 3 إلى 5 كلمات فقط
        - المحتوى آمن تماماً ومناسب للأطفال الصغار
        - استخدم مفردات بسيطة جداً يفهمها طفل عمره 3 سنوات
        - وصف الصورة يجب أن يكون باللغة الإنجليزية بأسلوب كرتوني مبهج

        أعد فقط JSON صحيح بالتنسيق التالي، بدون أي نص إضافي أو مقدمة أو شرح:
        {
          "title": "عنوان القصة هنا",
          "pages": [
            {
              "pageNumber": 1,
              "sentence": "جملة عربية قصيرة هنا",
              "imagePrompt": "cartoon style, bright colors, child-friendly image description in English"
            },
            {
              "pageNumber": 2,
              "sentence": "جملة عربية قصيرة هنا",
              "imagePrompt": "cartoon style, bright colors, child-friendly image description in English"
            },
            {
              "pageNumber": 3,
              "sentence": "جملة عربية قصيرة هنا",
              "imagePrompt": "cartoon style, bright colors, child-friendly image description in English"
            }
          ]
        }
        """;

        /// <summary>Builds the per-request user message with child's info injected.</summary>
        public static string BuildUserPrompt(string childName, string character, string theme) =>
            $"""
        اسم الطفل: {childName}
        الشخصية الرئيسية في القصة: {character}
        موضوع القصة: {theme}

        أنشئ قصة من 3 صفحات فقط. لا تضف أي نص خارج JSON.
        """;
    }

}
