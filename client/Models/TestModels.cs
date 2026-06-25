using System.Collections.Generic;

namespace Client.Models
{
    public class QuestionDto
    {
        // Добавляем = string.Empty, чтобы компилятор не ругался на возможный null
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>(); 
    }
}