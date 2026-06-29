using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Client.Models
{
    // Модель для списка тестов
    public class TestItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ServerQuestionDto> Questions { get; set; } = new();
    }

    // Модель вопроса, приходящего с сервера
    public class ServerQuestionDto
    {
        public string text { get; set; } = string.Empty;
        public List<string> options { get; set; } = new();
        public int correct_option { get; set; }
    }

    // Модель варианта ответа для экрана студента (чтобы ставить галочки)
    public class StudentOption
    {
        public string Text { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = false;
    }

    public class StudentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsChecked { get; set; } = false; // Для галочки в интерфейсе
    }

    public class EditableOption { public string Text { get; set; } = string.Empty; public bool IsCorrect { get; set; } = false; }
    public class EditableQuestion { public string GroupId { get; } = Guid.NewGuid().ToString(); public string Text { get; set; } = string.Empty; public ObservableCollection<EditableOption> Options { get; set; } = new(); }
}