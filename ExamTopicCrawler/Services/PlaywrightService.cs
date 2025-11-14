using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace ExamTopicCrawler.Services
{
    public class PlaywrightService
    {
        public IPlaywright Playwright { get; private set; }
        public IBrowser Browser { get; private set; }
        public IPage Page { get; private set; }

        public async Task InitAsync(bool headless = true)
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new()
            {
                Headless = headless,
                Args = new[] { "--no-sandbox" }
            });

            Page = await Browser.NewPageAsync();
        }

        public async Task LoginAsync(string loginUrl, string email, string password)
        {
            await Page.GotoAsync(loginUrl);
            await Page.FillAsync("#email", email);
            await Page.FillAsync("#password", password);
            await Page.ClickAsync("#loginButton");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
