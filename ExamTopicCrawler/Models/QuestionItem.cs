using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExamTopicCrawler.Models
{
    public class QuestionItem
    {
        public string QuestionNumber { get; set; }
        public string Topic { get; set; }
        public string DataId { get; set; }
        public string QuestionText { get; set; }
        public List<AnswerOption> Options { get; set; }
        
        // Legacy fields (kept for backward compatibility)
        public string CorrectAnswer { get; set; }
        public string CorrectAnswerImageUrl { get; set; }
        
        // New answer fields aligned with ms-exam-tester database
        public string OriginalAnswer { get; set; }
        public string PrecioAnswer { get; set; }
        public string OriginalAnswerImageUrl { get; set; }
        public string PrecioAnswerImageUrl { get; set; }
        public bool IsPrecioVerified { get; set; }
        
        public string AnswerDescription { get; set; }
        public int DiscussionCount { get; set; }
        public List<VotedAnswer> VotedAnswers { get; set; }
        public List<DiscussionItem> Discussions { get; set; }
        public string Url { get; set; }
        
        // Additional fields for database sync
        public int OriginalOrder { get; set; }
        public int TopicQuestionNumber { get; set; }
        public string ExamTopicsId { get; set; }
        public bool AppearedInRealExam { get; set; }
        public bool IsUnofficial { get; set; }
    }

    public class AnswerOption
    {
        public string Letter { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class VotedAnswer
    {
        public string Answer { get; set; }
        public int VoteCount { get; set; }
        public bool IsMostVoted { get; set; }
    }

    public static class QuestionItemExtensions
    {
        private const string BaseUrl = "https://www.examtopics.com";

        public static void CleanWhitespace(this QuestionItem question)
        {
            question.QuestionText = CleanText(question.QuestionText);
            question.AnswerDescription = CleanText(question.AnswerDescription);
            
            // Clean legacy fields
            question.CorrectAnswer = CleanText(question.CorrectAnswer);
            question.CorrectAnswerImageUrl = CleanText(question.CorrectAnswerImageUrl);
            
            // Clean new answer fields
            question.OriginalAnswer = CleanText(question.OriginalAnswer);
            question.PrecioAnswer = CleanText(question.PrecioAnswer);
            question.OriginalAnswerImageUrl = CleanText(question.OriginalAnswerImageUrl);
            question.PrecioAnswerImageUrl = CleanText(question.PrecioAnswerImageUrl);
            
            question.QuestionNumber = CleanText(question.QuestionNumber);
            question.Topic = CleanText(question.Topic);
            question.DataId = CleanText(question.DataId);
            question.Url = CleanText(question.Url);
            question.ExamTopicsId = CleanText(question.ExamTopicsId);

            if (question.Options != null)
            {
                foreach (var option in question.Options)
                {
                    option.Letter = CleanText(option.Letter);
                    option.Text = CleanText(option.Text);
                    // Remove voting indicators from option text
                    option.Text = RemoveVotingIndicators(option.Text);
                }
            }

            if (question.VotedAnswers != null)
            {
                foreach (var votedAnswer in question.VotedAnswers)
                {
                    votedAnswer.Answer = CleanText(votedAnswer.Answer);
                }
            }

            if (question.Discussions != null)
            {
                foreach (var discussion in question.Discussions)
                {
                    discussion.User = CleanText(discussion.User);
                    discussion.Content = CleanText(discussion.Content);
                    discussion.Timestamp = CleanText(discussion.Timestamp);
                }
            }
        }

        public static void FixImageUrls(this QuestionItem question)
        {
            question.QuestionText = FixImagesInText(question.QuestionText);
            question.AnswerDescription = FixImagesInText(question.AnswerDescription);
            
            // Fix legacy fields
            question.CorrectAnswer = FixImagesInText(question.CorrectAnswer);
            question.CorrectAnswerImageUrl = FixImageUrl(question.CorrectAnswerImageUrl);
            
            // Fix new answer fields
            question.OriginalAnswer = FixImagesInText(question.OriginalAnswer);
            question.PrecioAnswer = FixImagesInText(question.PrecioAnswer);
            question.OriginalAnswerImageUrl = FixImageUrl(question.OriginalAnswerImageUrl);
            question.PrecioAnswerImageUrl = FixImageUrl(question.PrecioAnswerImageUrl);

            if (question.Options != null)
            {
                foreach (var option in question.Options)
                {
                    option.Text = FixImagesInText(option.Text);
                }
            }

            if (question.Discussions != null)
            {
                foreach (var discussion in question.Discussions)
                {
                    discussion.Content = FixImagesInText(discussion.Content);
                }
            }
        }

        private static string FixImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            // If already absolute URL, return as-is
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            // If relative URL starting with /, prepend base URL
            if (url.StartsWith("/"))
                return BaseUrl + url;

            return url;
        }

        private static string FixImagesInText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Fix img src attributes with relative paths (only if not already absolute)
            // Pattern: src="/assets/..." or src='/assets/...'
            text = Regex.Replace(text, 
                @"src=[""'](?!https?://)/(assets/[^""']+)[""']", 
                $@"src=""{BaseUrl}/$1""", 
                RegexOptions.IgnoreCase);

            // Fix [IMAGE: /assets/...] format (only if not already absolute)
            text = Regex.Replace(text, 
                @"\[IMAGE:\s*(?!https?://)/(assets/[^\]]+)\]", 
                $@"[IMAGE: {BaseUrl}/$1]", 
                RegexOptions.IgnoreCase);

            // Fix standalone relative URLs that start with /assets (only if not already absolute)
            text = Regex.Replace(text, 
                @"(?<![""':=/])(?<!https?://)/(assets/[^\s""'<>]+)", 
                $@"{BaseUrl}/$1");

            return text;
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove leading and trailing whitespace
            text = text.Trim();

            // Replace multiple spaces with single space
            text = Regex.Replace(text, @" {2,}", " ");

            // Replace multiple newlines with single newline
            text = Regex.Replace(text, @"\n{2,}", "\n");

            // Remove leading/trailing whitespace from each line
            text = Regex.Replace(text, @"^\s+|\s+$", "", RegexOptions.Multiline);

            // Replace tabs with spaces
            text = text.Replace("\t", " ");

            return text;
        }

        private static string RemoveVotingIndicators(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove common voting indicators that appear in options
            // Patterns: "Most Voted", "Highly Voted", "Community Vote", etc.
            text = Regex.Replace(text, @"\s*\n\s*Most\s+Voted\s*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s*\n\s*Highly\s+Voted\s*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s*\n\s*Community\s+Vote\s*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s*Most\s+Voted\s*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s*Highly\s+Voted\s*$", "", RegexOptions.IgnoreCase);
            
            return text.Trim();
        }
    }
}
