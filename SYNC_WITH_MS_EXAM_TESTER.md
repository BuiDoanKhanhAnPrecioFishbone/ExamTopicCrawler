# ExamTopicCrawler - Database Sync Updates

## Overview
The ExamTopicCrawler has been updated to synchronize with the ms-exam-tester database structure, ensuring all fields are properly populated for seamless import without errors.

## Changes Made

### 1. QuestionItem Model Updates (`Models/QuestionItem.cs`)

#### New Fields Added:
- **OriginalAnswer** - Original answer from ExamTopics (replaces legacy CorrectAnswer)
- **PrecioAnswer** - Verified answer by Precio team (editable in app)
- **OriginalAnswerImageUrl** - Original answer image URL
- **PrecioAnswerImageUrl** - Precio verified answer image URL
- **IsPrecioVerified** - Flag indicating Precio verification status
- **OriginalOrder** - Preserves question order from crawling (1-based index)
- **TopicQuestionNumber** - Question number within each topic (auto-calculated)
- **ExamTopicsId** - ExamTopics discussion ID (extracted from DataId)
- **AppearedInRealExam** - Flag for questions confirmed in real exams (default: false)
- **IsUnofficial** - Flag for unofficial questions (default: false for ExamTopics)

#### Legacy Fields (Maintained for Compatibility):
- **CorrectAnswer** - Kept for backward compatibility, populated with OriginalAnswer
- **CorrectAnswerImageUrl** - Kept for backward compatibility, populated with OriginalAnswerImageUrl

### 2. ExamCrawlerService Updates (`Services/ExamCrawlerService.cs`)

#### Enhanced ParseQuestionCardAsync:
- Populates both legacy and new answer fields
- Sets `ExamTopicsId` from the `DataId` attribute
- Initializes `AppearedInRealExam` to `false` (can be updated manually later)
- Sets `IsUnofficial` to `false` (ExamTopics questions are official)

### 3. ExportService Updates (`Services/ExportService.cs`)

#### New Processing Logic:
- **OriginalOrder Assignment**: Assigns sequential 1-based order to all questions
- **TopicQuestionNumber Calculation**: Tracks and assigns question numbers within each topic
- **Topic Counter**: Maintains dictionary of question counts per topic
- **Enhanced Logging**: Shows total questions and topic count

## Database Field Mapping

| Crawler Field | Database Field | Purpose |
|--------------|----------------|---------|
| OriginalAnswer | original_answer | Original answer from source |
| PrecioAnswer | precio_answer | Verified answer (null initially) |
| OriginalAnswerImageUrl | original_answer_image_url | Original image answer |
| PrecioAnswerImageUrl | precio_answer_image_url | Verified image answer |
| IsPrecioVerified | is_precio_verified | Verification status |
| OriginalOrder | original_order | Question sequence number |
| TopicQuestionNumber | topic_question_number | Per-topic question number |
| ExamTopicsId | examtopics_id | ExamTopics discussion ID |
| AppearedInRealExam | appeared_in_real_exam | Real exam confirmation |
| IsUnofficial | is_unofficial | Unofficial question flag |

## Usage

### 1. Crawl Exam Data
```bash
cd ExamTopicCrawler/ExamTopicCrawler
dotnet run
```

This will:
- Crawl questions from ExamTopics
- Populate all fields including new database fields
- Assign OriginalOrder (1, 2, 3, ...)
- Calculate TopicQuestionNumber per topic
- Export to `bin/Debug/net9.0/<exam-code>/exam.json`

### 2. Import to ms-exam-tester Database

Use the new import script designed for ExamTopics crawler output:

```bash
cd ms-exam-tester

# Create new exam
npx tsx scripts/import-examtopics-crawled.ts AZ-104 ../ExamTopicCrawler/ExamTopicCrawler/bin/Debug/net9.0/az104/exam.json

# Append to existing exam (skips duplicates)
npx tsx scripts/import-examtopics-crawled.ts AZ-104 ../ExamTopicCrawler/ExamTopicCrawler/bin/Debug/net9.0/az104/exam.json --append
```

The import script will:
- ✅ Create exam if it doesn't exist
- ✅ Skip import if exam exists (unless --append)
- ✅ Detect and skip duplicate questions in append mode
- ✅ Map all crawler fields to database fields correctly
- ✅ Preserve OriginalOrder and TopicQuestionNumber from crawler
- ✅ Set examtopics_id for discussion linking
- ✅ Initialize verification flags appropriately

## Error Prevention

### Duplicate Detection
The import script checks for duplicates using both `data_id` and `examtopics_id` fields, ensuring no question is imported twice even in append mode.

### Field Validation
All required fields are now properly populated:
- ✅ original_answer (not null)
- ✅ original_order (sequential numbering)
- ✅ topic_question_number (per-topic numbering)
- ✅ examtopics_id (for discussion linking)

### Backward Compatibility
Legacy fields are maintained and populated:
- ✅ correct_answer (mirrors original_answer)
- ✅ correct_answer_image_url (renamed to original_answer_image_url in migration)

## Configuration

Edit `examcrawler.json` to configure:
```json
{
  "StartExamUrl": "https://www.examtopics.com/exams/microsoft/az-104/view/",
  "OutputFolder": "az104",
  "DelayBetweenRequestsMs": 2000
}
```

## Output Structure

The crawler now exports JSON with this structure:
```json
[
  {
    "QuestionNumber": "Question #1",
    "Topic": "Topic 1",
    "DataId": "12345",
    "QuestionText": "<p>Question text here...</p>",
    "Options": [...],
    "OriginalAnswer": "A",
    "PrecioAnswer": null,
    "OriginalAnswerImageUrl": null,
    "PrecioAnswerImageUrl": null,
    "IsPrecioVerified": false,
    "AnswerDescription": "Explanation here...",
    "VotedAnswers": [...],
    "Discussions": [],
    "Url": "https://www.examtopics.com/...",
    "OriginalOrder": 1,
    "TopicQuestionNumber": 1,
    "ExamTopicsId": "12345",
    "AppearedInRealExam": false,
    "IsUnofficial": false
  },
  ...
]
```

## Troubleshooting

### Import Errors
If you encounter import errors:
1. Check that the JSON file exists
2. Verify all questions have `OriginalOrder` and `TopicQuestionNumber` set
3. Ensure `ExamTopicsId` or `DataId` is populated
4. Confirm database migrations are up to date

### Duplicate Questions
The import script automatically detects duplicates in append mode by checking:
- Existing questions' `data_id`
- Existing questions' `examtopics_id`

### Missing Fields
If fields are missing after import:
1. Re-run the crawler to get latest field structure
2. Use the `import-examtopics-crawled.ts` script (not the old import scripts)
3. Check that you're using the latest version of the crawler

## Migration from Old Data

If you have old crawled data:
1. Re-crawl using the updated crawler
2. The new crawler will populate all required fields
3. Use the new import script for proper field mapping

## Notes

- **AppearedInRealExam**: Set to `false` by default. Update manually in the app for questions confirmed in real exams.
- **IsUnofficial**: Always `false` for ExamTopics questions. Set to `true` only for user-added practice questions.
- **PrecioAnswer**: Initially `null`. The app displays `original_answer` until a Precio-verified answer is set.
- **Topic Numbering**: Automatically calculated during export based on question order and topic grouping.
