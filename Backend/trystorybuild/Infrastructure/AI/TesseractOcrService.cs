using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Tesseract;

namespace Infrastructure.AI
{
    public class TesseractSettings
    {
        /// <summary>
        /// Path to folder containing ara.traineddata.
        /// Windows installer default: C:\Program Files\Tesseract-OCR\tessdata
        /// </summary>
        public string TessDataPath { get; set; } = @"C:\Program Files\Tesseract-OCR\tessdata";
    }

    /// <summary>
    /// Tesseract OCR service for Arabic handwriting.
    /// Receives a SAVED FILE PATH — the stream is never touched here.
    /// WritingCorrectionAgent is responsible for saving the file first.
    /// </summary>
    public class TesseractOcrService(
        IOptions<TesseractSettings> settings,
        ILogger<TesseractOcrService> logger) : IOcrService
    {
        private readonly TesseractSettings _cfg = settings.Value;

        public Task<string> ExtractArabicTextAsync(string imagePath)
        {
            logger.LogInformation("[OCR] Processing file: {Path}", imagePath);

            if (!File.Exists(imagePath))
            {
                logger.LogError("[OCR] File not found: {Path}", imagePath);
                return Task.FromResult(string.Empty);
            }

            try
            {
                // TesseractEngine is not thread-safe; create per-call (lightweight)
                using var engine = new TesseractEngine(_cfg.TessDataPath, "ara", EngineMode.Default);
                engine.SetVariable("preserve_interword_spaces", "1");

                using var pix = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(pix);

                var rawText = page.GetText() ?? string.Empty;
                var confidence = page.GetMeanConfidence();
                var cleaned = CleanOcrOutput(rawText);

                logger.LogInformation(
                    "[OCR] Raw: '{Raw}'  Cleaned: '{Clean}'  Confidence: {Conf:P0}",
                    rawText.Trim(), cleaned, confidence);

                return Task.FromResult(cleaned);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[OCR] Tesseract failed for {File}. " +
                    "Ensure tessdata path '{Path}' contains ara.traineddata. " +
                    "Download from: https://github.com/tesseract-ocr/tessdata/raw/main/ara.traineddata",
                    imagePath, _cfg.TessDataPath);

                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Strip non-Arabic characters that Tesseract hallucinates on canvas drawings.
        /// Keeps Arabic Unicode (U+0600–U+06FF), spaces, Arabic punctuation.
        /// </summary>
        private static string CleanOcrOutput(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var chars = raw
                .Replace('\n', ' ')
                .Replace('\r', ' ')
                .Where(c =>
                    (c >= '\u0600' && c <= '\u06FF') ||  // Arabic block
                    c == ' ' || c == '،' || c == '.')
                .ToArray();

            var result = new string(chars);

            // Collapse multiple spaces
            while (result.Contains("  "))
                result = result.Replace("  ", " ");

            return result.Trim();
        }
    }

}
