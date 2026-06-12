using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.AI
{
    public class ArabicSimilarityService(ILogger<ArabicSimilarityService> logger) : ITextSimilarityService
    {
        public double Calculate(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
            {
                logger.LogWarning("[Similarity] One or both strings are empty.");
                return 0;
            }

            var normalExpected = NormaliseArabic(expected);
            var normalActual = NormaliseArabic(actual);

            if (normalExpected == normalActual) return 100.0;

            int distance = LevenshteinDistance(normalExpected, normalActual);
            int maxLen = Math.Max(normalExpected.Length, normalActual.Length);

            if (maxLen == 0) return 100.0;

            double similarity = Math.Round((1.0 - (double)distance / maxLen) * 100.0, 1);
            similarity = Math.Max(0, similarity);

            logger.LogInformation("[Similarity] Expected:'{E}' Actual:'{A}' → {Score:F1}%",
                normalExpected, normalActual, similarity);

            return similarity;
        }

        // Remove Arabic diacritics (harakat/tashkeel) for forgiving comparison
        private static string NormaliseArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Strip tashkeel (U+064B–U+065F) and tatweel (U+0640)
            var chars = text
                .Where(c => !((c >= '\u064B' && c <= '\u065F') || c == '\u0640'))
                .ToArray();

            return new string(chars).Trim();
        }

        // Standard Levenshtein distance
        private static int LevenshteinDistance(string s, string t)
        {
            int m = s.Length, n = t.Length;
            int[,] dp = new int[m + 1, n + 1];

            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;

            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = s[i - 1] == t[j - 1]
                        ? dp[i - 1, j - 1]
                        : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));

            return dp[m, n];
        }
    }

}
