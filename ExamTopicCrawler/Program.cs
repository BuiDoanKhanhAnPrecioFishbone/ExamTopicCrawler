using ExamTopicCrawler.Services;
using Microsoft.Playwright;

var config = AppConfig.Load();
var playwright = new PlaywrightService();
await playwright.InitAsync(headless: true);

Console.WriteLine("Logging in...");
await playwright.LoginAsync(config.LoginUrl, config.Email, config.Password);

Console.WriteLine("Starting crawler...");
var crawler = new ExamCrawlerService(playwright, config);
var results = await crawler.CrawlExamAsync();

var exporter = new ExportService(config);
exporter.Save(results);

Console.WriteLine("Done!");
