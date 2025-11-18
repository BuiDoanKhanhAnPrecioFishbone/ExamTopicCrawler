using ExamTopicCrawler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExamTopicCrawler.Services
{
    public class ExportService
    {
        private readonly AppConfig _config;

        public ExportService(AppConfig config)
        {
            _config = config;
        }

        public void Save(List<QuestionItem> items)
        {
            // Clean whitespace from all questions before saving
            Console.WriteLine("Cleaning whitespace from questions...");
            foreach (var item in items)
            {
                item.CleanWhitespace();
                item.FixImageUrls();
            }

            Directory.CreateDirectory(_config.OutputFolder);

            string path = Path.Combine(_config.OutputFolder, "exam.json");
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);

            Console.WriteLine($"Saved {items.Count} questions to {path}");
        }
    }
}
