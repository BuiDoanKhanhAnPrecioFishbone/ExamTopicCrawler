using ExamTopicCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

            try
            {
                Console.WriteLine($"Navigating to: {_config.StartExamUrl}");
                await _playwright.Page.GotoAsync(_config.StartExamUrl, new() { Timeout = 60000 });
                await _playwright.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                int pageNumber = 1;
                bool hasMorePages = true;

                while (hasMorePages)
                {
                    Console.WriteLine($"\n=== Processing Page {pageNumber} ===");
                    
                    // Extract all questions from the current page
                    var questions = await ParseAllQuestionsOnPageAsync();
                    results.AddRange(questions);

                    Console.WriteLine($"Scraped {questions.Count} questions from page {pageNumber}");
                    Console.WriteLine($"Total questions so far: {results.Count}");

                    // Check if there's a "Next" button/link
                    var nextButton = await FindNextButtonAsync();
                    
                    if (nextButton != null)
                    {
                        Console.WriteLine("Found next page button, navigating...");
                        
                        // Click the next button
                        await nextButton.ClickAsync();
                        await _playwright.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        
                        // Add delay between requests
                        await Task.Delay(_config.DelayBetweenRequestsMs);
                        
                        pageNumber++;
                    }
                    else
                    {
                        Console.WriteLine("No next page button found. This is the last page.");
                        hasMorePages = false;
                    }
                }

                Console.WriteLine($"\n=== Crawling Complete ===");
                Console.WriteLine($"Total pages processed: {pageNumber}");
                Console.WriteLine($"Total questions extracted: {results.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during crawling: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return results;
        }

        private async Task<IElementHandle?> FindNextButtonAsync()
        {
            var p = _playwright.Page;
            
            // Try multiple selectors for the next button
            var selectors = new[]
            {
                "a.btn.btn-success:has-text('Next Questions')",
                "a:has-text('Next Questions')",
                ".page-navigation-bar a.pull-right",
                "a[href*='/view/']:has-text('Next')",
                ".pagination .next:not(.disabled) a",
                "a.next-page"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var element = await p.QuerySelectorAsync(selector);
                    if (element != null)
                    {
                        // Check if the element is visible and enabled
                        var isVisible = await element.IsVisibleAsync();
                        if (isVisible)
                        {
                            Console.WriteLine($"Found next button using selector: {selector}");
                            return element;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Selector '{selector}' failed: {ex.Message}");
                }
            }

            return null;
        }

        private async Task<List<QuestionItem>> ParseAllQuestionsOnPageAsync()
        {
            var p = _playwright.Page;
            var questions = new List<QuestionItem>();

            // Wait for page to be fully loaded
            await Task.Delay(2000);

            // Find all question cards
            try
            {
                var questionCards = await p.QuerySelectorAllAsync(".exam-question-card");
                Console.WriteLine($"Found {questionCards.Count} question cards");

                if (questionCards.Count == 0)
                {
                    Console.WriteLine("⚠ No question cards found. Page might require login or has different structure.");
                    return questions;
                }

                foreach (var card in questionCards)
                {
                    try
                    {
                        var question = await ParseQuestionCardAsync(card);
                        questions.Add(question);
                        Console.WriteLine($"✓ Parsed: {question.QuestionNumber} - Topic: {question.Topic}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error parsing question card: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error finding question cards: {ex.Message}");
            }

            return questions;
        }

        private async Task<QuestionItem> ParseQuestionCardAsync(IElementHandle card)
        {
            var question = new QuestionItem
            {
                Options = new List<AnswerOption>(),
                VotedAnswers = new List<VotedAnswer>(),
                Discussions = new List<DiscussionItem>()
            };

            // Extract question number and topic from header
            var header = await card.QuerySelectorAsync(".card-header");
            if (header != null)
            {
                var headerText = await header.TextContentAsync();
                question.QuestionNumber = headerText?.Split('\n').FirstOrDefault()?.Trim() ?? "";
                
                var topicSpan = await header.QuerySelectorAsync(".question-title-topic");
                question.Topic = topicSpan != null ? (await topicSpan.TextContentAsync())?.Trim() ?? "" : "";
            }

            // Extract data-id from question body
            var questionBody = await card.QuerySelectorAsync(".question-body");
            if (questionBody != null)
            {
                question.DataId = await questionBody.GetAttributeAsync("data-id") ?? "";
            }

            // Extract question text
            var questionTextElement = await card.QuerySelectorAsync(".card-text");
            if (questionTextElement != null)
            {
                question.QuestionText = await GetInnerHtmlAsync(questionTextElement);
            }

            // Extract voted answers from JSON script
            var votedAnswersScript = await card.QuerySelectorAsync("script[type='application/json']");
            if (votedAnswersScript != null)
            {
                var scriptId = await votedAnswersScript.GetAttributeAsync("id");
                var jsonContent = await votedAnswersScript.TextContentAsync();
                try
                {
                    var votedData = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent ?? "[]");
                    if (votedData != null)
                    {
                        foreach (var item in votedData)
                        {
                            question.VotedAnswers.Add(new VotedAnswer
                            {
                                Answer = item.GetProperty("voted_answers").GetString() ?? "",
                                VoteCount = item.GetProperty("vote_count").GetInt32(),
                                IsMostVoted = item.GetProperty("is_most_voted").GetBoolean()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing voted answers JSON: {ex.Message}");
                }
            }

            // Extract answer options
            var choiceItems = await card.QuerySelectorAllAsync(".multi-choice-item");
            foreach (var item in choiceItems)
            {
                var letterSpan = await item.QuerySelectorAsync(".multi-choice-letter");
                var letter = letterSpan != null ? (await letterSpan.GetAttributeAsync("data-choice-letter")) ?? "" : "";
                
                var text = await item.TextContentAsync();
                text = text?.Replace(letter + ".", "").Trim() ?? "";

                var isCorrect = (await item.GetAttributeAsync("class"))?.Contains("correct-hidden") ?? false;

                question.Options.Add(new AnswerOption
                {
                    Letter = letter,
                    Text = text,
                    IsCorrect = isCorrect
                });
            }

            // Extract correct answer
            var correctAnswerSpan = await card.QuerySelectorAsync(".correct-answer");
            if (correctAnswerSpan != null)
            {
                // Check if it's an image or text
                var answerImage = await correctAnswerSpan.QuerySelectorAsync("img");
                if (answerImage != null)
                {
                    question.CorrectAnswer = "[IMAGE: " + (await answerImage.GetAttributeAsync("src")) + "]";
                }
                else
                {
                    question.CorrectAnswer = (await correctAnswerSpan.TextContentAsync())?.Trim() ?? "";
                }
            }

            // Extract answer description/explanation
            var answerDescription = await card.QuerySelectorAsync(".answer-description");
            if (answerDescription != null)
            {
                question.AnswerDescription = await GetInnerHtmlAsync(answerDescription);
            }

            // Extract discussion count
            var discussionBadge = await card.QuerySelectorAsync(".question-discussion-button .badge");
            if (discussionBadge != null)
            {
                var countText = await discussionBadge.TextContentAsync();
                if (int.TryParse(countText?.Trim(), out int count))
                {
                    question.DiscussionCount = count;
                }
            }

            question.Url = _playwright.Page.Url;

            return question;
        }

        private async Task<string> GetInnerHtmlAsync(IElementHandle element)
        {
            return await element.EvaluateAsync<string>("el => el.innerHTML") ?? "";
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
