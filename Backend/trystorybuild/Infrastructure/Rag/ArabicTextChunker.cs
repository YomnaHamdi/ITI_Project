namespace Infrastructure.Rag
{
    public static class ArabicTextChunker
    {
        private static readonly char[] SentenceBreaks = { '.', '،', '؟', '!', '\n' };

        public static List<string> Split(string text, int chunkSize = 400, int overlap = 60)
        {
            if (string.IsNullOrWhiteSpace(text)) return new();

            text = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");

            var sentences = SplitIntoSentences(text);
            var chunks    = new List<string>();
            var current   = new System.Text.StringBuilder();

            foreach (var sentence in sentences)
            {
                if (current.Length + sentence.Length > chunkSize && current.Length > 0)
                {
                    chunks.Add(current.ToString().Trim());
                    var overlapText = current.Length > overlap
                        ? current.ToString()[^overlap..]
                        : current.ToString();
                    current.Clear();
                    current.Append(overlapText);
                    current.Append(' ');
                }
                current.Append(sentence);
                current.Append(' ');
            }

            if (current.Length > 0)
                chunks.Add(current.ToString().Trim());

            return chunks.Where(c => c.Length > 10).ToList();
        }

        private static List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();
            var start     = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (SentenceBreaks.Contains(text[i]))
                {
                    var sentence = text[start..(i + 1)].Trim();
                    if (sentence.Length > 0)
                        sentences.Add(sentence);
                    start = i + 1;
                }
            }

            if (start < text.Length)
            {
                var remaining = text[start..].Trim();
                if (remaining.Length > 0)
                    sentences.Add(remaining);
            }

            return sentences;
        }
    }
}
