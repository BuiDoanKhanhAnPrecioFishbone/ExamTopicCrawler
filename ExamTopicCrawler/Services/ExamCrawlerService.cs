using ExamTopicCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ExamTopicCrawler.Services
{
    public class ExamCrawlerService
    {
        private readonly PlaywrightService _playwright;
        private readonly AppConfig _config;

        public ExamCrawlerService(PlaywrightService playwright, AppConfig config)
        {
            _playwright = playwright;
            _config = config;
        }

        public async Task<List<QuestionItem>> CrawlExamAsync()
        {
            var results = new List<QuestionItem>();

            await _playwright.Page.GotoAsync(_config.StartExamUrl);

            int questionNumber = 1;
            bool hasNext = true;

            while (hasNext)
            {
                Console.WriteLine($"Scraping question {questionNumber}...");

                var q = await ParseQuestionPageAsync(questionNumber);
                results.Add(q);

                // Try click “Next” button
                var nextBtn = await _playwright.Page.QuerySelectorAsync(".next-question");
                if (nextBtn == null)
                {
                    hasNext = false;
                    break;
                }

                await nextBtn.ClickAsync();
                await _playwright.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await Task.Delay(_config.DelayBetweenRequestsMs);
                questionNumber++;
            }

            return results;
        }


        private async Task<QuestionItem> ParseQuestionPageAsync(int number)
        {
            var p = _playwright.Page;

            // Adjust these selectors depending on site HTML
            string questionText = (await p.TextContentAsync(".question-text"))?.Trim();
            var options = (await p.EvaluateAsync<string[]>(
                @"Array.from(document.querySelectorAll('.answer-option'))
                .map(o => o.textContent.trim())"))
                ?.ToList();

            string correctAnswer = (await p.TextContentAsync(".correct-answer"))?.Trim();
            string explanation = (await p.TextContentAsync(".explanation-block"))?.Trim();

            var discussions = await ExtractDiscussionsAsync();

            return new QuestionItem
            {
                Number = number,
                QuestionText = questionText ?? "",
                Options = options ?? new List<string>(),
                CorrectAnswer = correctAnswer ?? "",
                Explanation = explanation ?? "",
                Discussions = discussions,
                Url = p.Url
            };
        }

        private async Task<List<DiscussionItem>> ExtractDiscussionsAsync()
        {
            var p = _playwright.Page;

            var raw = await p.EvaluateAsync<dynamic>(
                @"Array.from(document.querySelectorAll('.discussion-post')).map(post => ({
                user: post.querySelector('.user')?.textContent.trim(),
                content: post.querySelector('.content')?.textContent.trim(),
                timestamp: post.querySelector('.timestamp')?.textContent.trim()
            }))"
            );

            var list = new List<DiscussionItem>();

            foreach (var x in raw)
            {
                list.Add(new DiscussionItem
                {
                    User = x.user,
                    Content = x.content,
                    Timestamp = x.timestamp
                });
            }

            return list;
        }
    }
}
