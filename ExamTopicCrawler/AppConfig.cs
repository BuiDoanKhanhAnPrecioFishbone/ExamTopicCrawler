using System.Text.Json;

public class AppConfig
{
    public string BaseUrl { get; set; }
    public string LoginUrl { get; set; }
    public string StartExamUrl { get; set; }
    public string SettingExamUrl { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int DelayBetweenRequestsMs { get; set; }
    public string OutputFolder { get; set; }

    public static AppConfig Load(string filePath = "examcrawler.json")
    {
        return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(filePath));
    }
}
