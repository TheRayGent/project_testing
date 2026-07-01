using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.Models
{
    public class TestItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ServerQuestionDto> Questions { get; set; } = new();

        public bool IsCompleted { get; set; } = false;
        public string ResultText { get; set; } = string.Empty;
        public Visibility ButtonVisibility => IsCompleted ? Visibility.Collapsed : Visibility.Visible;
    }

    public class ServerQuestionDto
    {
        public string text { get; set; } = string.Empty;
        public List<string> options { get; set; } = new();
        public int correct_option { get; set; }
    }

    public class StudentOption
    {
        public string Text { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = false;
    }

    public class StudentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsChecked { get; set; } = false;
        public List<string> AvailableTests { get; set; } = new(); 
    }

    // НОВАЯ МОДЕЛЬ: Для отображения списка результатов без всплывающего окна
    public class StudentResultDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
    }

    public class EditableOption { public string Text { get; set; } = string.Empty; public bool IsCorrect { get; set; } = false; }
    public class EditableQuestion { public string GroupId { get; } = Guid.NewGuid().ToString(); public string Text { get; set; } = string.Empty; public ObservableCollection<EditableOption> Options { get; set; } = new(); }
}