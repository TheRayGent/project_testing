using System.Collections.Generic;

namespace Client.Models
{
    public class QuestionDto
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; } // Может быть 2, 3, 5 вариантов
    }
}