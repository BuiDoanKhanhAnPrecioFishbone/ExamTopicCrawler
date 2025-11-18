using ExamTopicCrawler.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExamTopicCrawler.Services
{
    public class JsonCleanerService
    {
        public void CleanJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ File not found: {filePath}");
                return;
            }

            try
            {
                Console.WriteLine($"Reading file: {filePath}");
                var jsonContent = File.ReadAllText(filePath);

                Console.WriteLine("Deserializing JSON...");
                var questions = JsonSerializer.Deserialize<List<QuestionItem>>(jsonContent);

                if (questions == null || questions.Count == 0)
                {
                    Console.WriteLine("❌ No questions found in file");
                    return;
                }

                Console.WriteLine($"Cleaning {questions.Count} questions...");
                foreach (var question in questions)
                {
                    MigrateCorrectAnswerFormat(question);
                    question.CleanWhitespace();
                    question.FixImageUrls();
                }

                Console.WriteLine("Serializing cleaned data...");
                var cleanedJson = JsonSerializer.Serialize(questions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Create backup of original file
                string backupPath = filePath + ".backup";
                File.Copy(filePath, backupPath, overwrite: true);
                Console.WriteLine($"✓ Backup created: {backupPath}");

                // Save cleaned version
                File.WriteAllText(filePath, cleanedJson);
                Console.WriteLine($"✓ Cleaned file saved: {filePath}");
                Console.WriteLine($"✓ Successfully cleaned {questions.Count} questions");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cleaning file: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void CleanJsonFile(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"❌ File not found: {inputPath}");
                return;
            }

            try
            {
                Console.WriteLine($"Reading file: {inputPath}");
                var jsonContent = File.ReadAllText(inputPath);

                Console.WriteLine("Deserializing JSON...");
                var questions = JsonSerializer.Deserialize<List<QuestionItem>>(jsonContent);

                if (questions == null || questions.Count == 0)
                {
                    Console.WriteLine("❌ No questions found in file");
                    return;
                }

                Console.WriteLine($"Cleaning {questions.Count} questions...");
                foreach (var question in questions)
                {
                    MigrateCorrectAnswerFormat(question);
                    question.CleanWhitespace();
                    question.FixImageUrls();
                }

                Console.WriteLine("Serializing cleaned data...");
                var cleanedJson = JsonSerializer.Serialize(questions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Save to output file
                File.WriteAllText(outputPath, cleanedJson);
                Console.WriteLine($"✓ Cleaned file saved: {outputPath}");
                Console.WriteLine($"✓ Successfully cleaned {questions.Count} questions");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cleaning file: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void MigrateCorrectAnswerFormat(QuestionItem question)
        {
            // Check if CorrectAnswer contains the old [IMAGE: url] format
            if (!string.IsNullOrEmpty(question.CorrectAnswer))
            {
                var match = Regex.Match(question.CorrectAnswer, @"^\[IMAGE:\s*(.+?)\]$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // Extract the URL and move to CorrectAnswerImageUrl
                    question.CorrectAnswerImageUrl = match.Groups[1].Value.Trim();
                    question.CorrectAnswer = string.Empty;
                }
            }
        }
    }
}
