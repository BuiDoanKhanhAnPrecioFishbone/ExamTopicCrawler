using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public string CorrectAnswer { get; set; }
        public string AnswerDescription { get; set; }
        public int DiscussionCount { get; set; }
        public List<VotedAnswer> VotedAnswers { get; set; }
        public List<DiscussionItem> Discussions { get; set; }
        public string Url { get; set; }
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
}
