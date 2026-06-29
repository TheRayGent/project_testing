using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;

namespace Client.Views
{
    public partial class TeacherPage : Page
    {
        public ObservableCollection<EditableQuestion> TestQuestions { get; set; } = new();
        public ObservableCollection<TestItemDto> DisplayedTests { get; set; } = new();

        private string _selectedTestIdForAccess = string.Empty;
        private List<StudentUserDto> _currentStudentsList = new();
        private static bool _isDarkTheme = false;

        public TeacherPage()
        {
            InitializeComponent();
            TeacherNameTxt.Text = $"Преподаватель: {Session.FullName}";
            ThemeToggleBtn.Content = _isDarkTheme ? "☀️ Светлая" : "🌙 Темная";

            QuestionsList.ItemsSource = TestQuestions;
            CreatedTestsList.ItemsSource = DisplayedTests;

            // === ИСПРАВЛЕНИЕ: МГНОВЕННАЯ ЗАГРУЗКА ===
            // Вызываем загрузку прямо в конструкторе (в фоне)
            _ = LoadCreatedTestsAsync();
        }

        private async Task LoadCreatedTestsAsync()
        {
            DisplayedTests.Clear();
            NoTestsTxt.Visibility = Visibility.Collapsed;

            await ApiService.FetchProfileAsync();

            if (Session.CreatedTests == null || Session.CreatedTests.Count == 0)
            {
                NoTestsTxt.Visibility = Visibility.Visible;
                return;
            }

            foreach (var id in Session.CreatedTests)
            {
                var test = await ApiService.GetTestByIdAsync(id);
                if (test != null) DisplayedTests.Add(test);
            }

            if (DisplayedTests.Count == 0) NoTestsTxt.Visibility = Visibility.Visible;
        }

        // --- ЛОГИКА СТАТИСТИКИ (НОВОЕ) ---
        private async void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            string testId = ((Button)sender).Tag.ToString()!;
            
            // Получаем результаты от C++
            var results = await ApiService.GetTestResultsAsync(testId);
            if (results.Count == 0)
            {
                MessageBox.Show("Этот тест еще никто не прошел.", "Статистика", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Получаем список всех студентов, чтобы сопоставить ID с Именем
            var allStudents = await ApiService.GetStudentsListAsync();
            var statsBuilder = new StringBuilder("Результаты прохождения:\n\n");

            foreach (var kvp in results)
            {
                string studentId = kvp.Key;
                string score = kvp.Value;
                
                // Ищем имя студента
                var student = allStudents.FirstOrDefault(s => s.Id == studentId);
                string studentName = student != null ? student.FullName : $"Студент (ID {studentId})";

                statsBuilder.AppendLine($"👤 {studentName} -> {score}");
            }

            MessageBox.Show(statsBuilder.ToString(), "Статистика теста", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Остальные методы без изменений
        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ThemeToggleBtn.Content = _isDarkTheme ? "☀️ Светлая" : "🌙 Темная";
            string themeFile = _isDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri($"Themes/{themeFile}", System.UriKind.Relative) });
        }

        private void ShowCreateTest_Click(object sender, RoutedEventArgs e)
        {
            MainPanelGrid.Visibility = Visibility.Collapsed;
            GiveAccessGrid.Visibility = Visibility.Collapsed;
            CreateTestGrid.Visibility = Visibility.Visible;
            if (TestQuestions.Count == 0) AddNewQuestion();
        }

        private async void ShowGiveAccess_Click(object sender, RoutedEventArgs e)
        {
            _selectedTestIdForAccess = ((Button)sender).Tag.ToString()!;
            _currentStudentsList = await ApiService.GetStudentsListAsync();
            StudentsList.ItemsSource = _currentStudentsList;

            if (_currentStudentsList.Count == 0) MessageBox.Show("Нет студентов!");

            MainPanelGrid.Visibility = Visibility.Collapsed;
            CreateTestGrid.Visibility = Visibility.Collapsed;
            GiveAccessGrid.Visibility = Visibility.Visible;
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            CreateTestGrid.Visibility = Visibility.Collapsed; GiveAccessGrid.Visibility = Visibility.Collapsed; MainPanelGrid.Visibility = Visibility.Visible;
        }

        private async void SaveAccess_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudentIds = _currentStudentsList.Where(s => s.IsChecked).Select(s => s.Id).ToList();
            if (selectedStudentIds.Count == 0) { MessageBox.Show("Выберите студента!"); return; }

            if (await ApiService.GiveAccessAsync(_selectedTestIdForAccess, selectedStudentIds))
            {
                MessageBox.Show("Доступ выдан!"); BackToMain_Click(null!, null!);
            }
        }

        private void AddNewQuestion() { var q = new EditableQuestion(); q.Options.Add(new EditableOption()); q.Options.Add(new EditableOption()); TestQuestions.Add(q); }
        private void AddQuestion_Click(object sender, RoutedEventArgs e) => AddNewQuestion();
        private void RemoveQuestion_Click(object sender, RoutedEventArgs e) => TestQuestions.Remove((EditableQuestion)((Button)sender).Tag);
        private void AddOption_Click(object sender, RoutedEventArgs e) => ((EditableQuestion)((Button)sender).Tag).Options.Add(new EditableOption());
        private void RemoveOption_Click(object sender, RoutedEventArgs e)
        {
            var option = (EditableOption)((Button)sender).Tag;
            var question = TestQuestions.FirstOrDefault(q => q.Options.Contains(option));
            if (question != null && question.Options.Count > 2) question.Options.Remove(option);
        }

        private async void SaveTest_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TestTitle.Text)) return;
            var apiQuestionsList = new List<object>();

            foreach (var q in TestQuestions)
            {
                int correctIndex = q.Options.ToList().FindIndex(opt => opt.IsCorrect);
                if (correctIndex == -1) return;
                apiQuestionsList.Add(new { text = q.Text, options = q.Options.Select(opt => string.IsNullOrWhiteSpace(opt.Text) ? "Пусто" : opt.Text).ToArray(), correct_option = correctIndex });
            }

            if (await ApiService.CreateTestAsync(TestTitle.Text, TestDesc.Text, apiQuestionsList.ToArray()))
            {
                MessageBox.Show("Тест сохранен!");
                TestTitle.Text = ""; TestDesc.Text = ""; TestQuestions.Clear();
                await LoadCreatedTestsAsync();
                BackToMain_Click(null!, null!);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            TokenStorage.Clear(); 
            Session.Token = string.Empty; 
            Session.Role = string.Empty;
            if (Session.CreatedTests != null) Session.CreatedTests.Clear();
            NavigationService.Navigate(new LoginPage());
        }
    }
}