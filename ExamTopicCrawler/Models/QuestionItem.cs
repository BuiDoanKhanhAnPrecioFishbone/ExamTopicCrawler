using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamTopicCrawler.Models
{
    public class QuestionItem
    {
        public int Number { get; set; }
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public List<DiscussionItem> Discussions { get; set; }
        public string Url { get; set; }
    }
}
