using ExamTopicCrawler.Services;
using Microsoft.Playwright;

var config = AppConfig.Load();
var playwright = new PlaywrightService();
await playwright.InitAsync(headless: false); // Set to true for headless mode

Console.WriteLine("Logging in...");
await playwright.LoginAsync(config.StartExamUrl, config.Email, config.Password);
await playwright.SettingExam(config.SettingExamUrl);
Console.WriteLine("Starting crawler...");
var crawler = new ExamCrawlerService(playwright, config);
var results = await crawler.CrawlExamAsync();

var exporter = new ExportService(config);
exporter.Save(results);

Console.WriteLine("Done!");


//Example usage of JsonCleanerService
//var cleaner = new JsonCleanerService();
//cleaner.CleanJsonFile($"C:\\D\\ExamTopicCrawler\\ExamTopicCrawler\\bin\\Debug\\net9.0\\az204\\exam.json");
