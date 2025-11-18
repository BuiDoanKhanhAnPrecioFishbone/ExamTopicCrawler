# ExamTopic Crawler

A web scraper built with C# and Playwright to extract exam questions from ExamTopics-style HTML pages.

## Features

- Extracts complete question data including:
  - Question number and topic
  - Question text (with HTML formatting and images)
  - Multiple choice options
  - Correct answers (text or image references)
  - Answer descriptions/explanations
  - Voted answers with vote counts
  - Discussion counts
- **Automatic pagination**: Crawls through all pages of the exam
- Handles multiple pages (typically 50 questions per page)
- Detects the last page automatically when no "Next" button is found
  
## Structure

The crawler extracts questions from HTML pages with this structure:
- **Question cards**: `.exam-question-card`
- **Question header**: Contains question number and topic
- **Question body**: Contains the question text and data-id
- **Answer options**: `.multi-choice-item` with letters and text
- **Correct answer**: `.correct-answer` (can be text or image)
- **Voted answers**: JSON script with community votes
- **Discussion count**: Badge showing number of discussions

## Configuration

Edit `examcrawler.json`:

```json
{
  "StartExamUrl": "https://your-exam-site.com/view",
  "OutputFolder": "./output",
  "DelayBetweenRequestsMs": 1000,
  "Email": "your_email@example.com",
  "Password": "your_password"
}
```

### Login Configuration (Optional)

If the exam requires authentication:
1. Set your credentials in `examcrawler.json` (Email and Password)
2. Uncomment the login lines in `Program.cs`:
   ```csharp
   Console.WriteLine("Logging in...");
   await playwright.LoginAsync(config.StartExamUrl, config.Email, config.Password);
   ```

The login system handles modal-based authentication automatically:
- Detects the login modal (`#login-modal`)
- Fills in email/username and password fields
- Submits the form and waits for successful authentication

## Usage

1. Install dependencies:
```bash
dotnet restore
dotnet build
```

2. Install Playwright browsers:
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

3. Run the crawler:
```bash
dotnet run
```

4. Output will be saved to `./output/exam.json`

## Output Format

```json
[
  {
    "QuestionNumber": "Question #1",
    "Topic": "Topic 1",
    "DataId": "816230",
    "QuestionText": "<html content>",
    "Options": [
      {
        "Letter": "A",
        "Text": "Option text",
        "IsCorrect": false
      }
    ],
    "CorrectAnswer": "C",
    "AnswerDescription": "<explanation html>",
    "DiscussionCount": 30,
    "VotedAnswers": [
      {
        "Answer": "C",
        "VoteCount": 38,
        "IsMostVoted": true
      }
    ],
    "Url": "https://..."
  }
]
```

## Model Classes

### QuestionItem
- `QuestionNumber`: The question number (e.g., "Question #1")
- `Topic`: The topic name (e.g., "Topic 1")
- `DataId`: Unique identifier from data-id attribute
- `QuestionText`: Full HTML content of the question
- `Options`: List of AnswerOption objects
- `CorrectAnswer`: The correct answer letter(s) or image reference
- `AnswerDescription`: HTML explanation/description
- `DiscussionCount`: Number of discussions
- `VotedAnswers`: Community voting data
- `Url`: Source page URL

### AnswerOption
- `Letter`: Answer choice letter (A, B, C, D, etc.)
- `Text`: Answer text content
- `IsCorrect`: Whether this is marked as correct (from correct-hidden class)

### VotedAnswer
- `Answer`: The voted answer choice(s)
- `VoteCount`: Number of votes
- `IsMostVoted`: Whether this is the most voted answer

## Notes

- Images in correct answers are stored as `[IMAGE: /path/to/image.jpg]`
- The crawler automatically navigates through all pages until no "Next" button is found
- Default is 50 questions per page (varies by exam)
- HTML content is preserved for question text and explanations
- Handles both text and image-based answers
- Progress is shown for each page: page number, questions per page, and total questions
- Configurable delay between page requests to avoid rate limiting (default: 1000ms)
