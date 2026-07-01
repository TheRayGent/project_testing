using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Models;
using Client.Services;

namespace Client.Views
{
    public partial class StudentPage : Page
    {
        public ObservableCollection<TestItemDto> DisplayedTests { get; set; } = new();

        private List<ServerQuestionDto> _currentTestQuestions = new();
        private int _currentQuestionIndex = 0;
        private int _correctAnswers = 0;
        private string _currentTestId = string.Empty;
        private List<StudentOption> _currentOptions = new();
        private static bool _isDarkTheme = false;

        public StudentPage()
        {
            InitializeComponent();
            StudentNameTxt.Text = $"Студент: {Session.FullName}";
            ThemeToggleBtn.Content = _isDarkTheme ? "☀️ Светлая" : "🌙 Темная";
            TestsListBox.ItemsSource = DisplayedTests;
            _ = LoadTestsAsync();
        }

        private async Task LoadTestsAsync()
        {
            DisplayedTests.Clear();
            NoTestsTxt.Visibility = Visibility.Collapsed;

            await ApiService.FetchProfileAsync();

            if (Session.AvailableTests == null || Session.AvailableTests.Count == 0)
            {
                NoTestsTxt.Visibility = Visibility.Visible;
                return;
            }

            foreach (var testId in Session.AvailableTests)
            {
                var testInfo = await ApiService.GetTestByIdAsync(testId);
                if (testInfo != null)
                {
                    // ИЩЕМ РЕЗУЛЬТАТ В БАЗЕ C++
                    string resultStr = await ApiService.GetTestResultForStudentAsync(testId);
                    if (!string.IsNullOrEmpty(resultStr))
                    {
                        // ТЕСТ ПРОЙДЕН! Блокируем кнопку.
                        testInfo.IsCompleted = true;
                        
                        var parts = resultStr.Split(" из ");
                        if (parts.Length == 2 && double.TryParse(parts[0], out double correct) && double.TryParse(parts[1], out double total) && total > 0)
                        {
                            double percent = (correct / total) * 100;
                            testInfo.ResultText = $"Ваша оценка: {Math.Round(percent)}% ({resultStr})";
                        }
                        else
                        {
                            testInfo.ResultText = $"Результат: {resultStr}";
                        }
                    }
                    DisplayedTests.Add(testInfo);
                }
            }

            if (DisplayedTests.Count == 0) NoTestsTxt.Visibility = Visibility.Visible;
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ThemeToggleBtn.Content = _isDarkTheme ? "☀️ Светлая" : "🌙 Темная";
            string themeFile = _isDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri($"Themes/{themeFile}", System.UriKind.Relative) });
        }

        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            _currentTestId = ((Button)sender).Tag?.ToString() ?? string.Empty;
            var testInfo = await ApiService.GetTestByIdAsync(_currentTestId);

            if (testInfo != null && testInfo.Questions != null && testInfo.Questions.Count > 0)
            {
                _currentTestQuestions = testInfo.Questions;
                _currentQuestionIndex = 0;
                _correctAnswers = 0;
                TestSelectionGrid.Visibility = Visibility.Collapsed;
                TestPassingGrid.Visibility = Visibility.Visible;
                ShowCurrentQuestion();
            }
            else MessageBox.Show("Не удалось загрузить вопросы теста.");
        }

        private void ShowCurrentQuestion()
        {
            var question = _currentTestQuestions[_currentQuestionIndex];
            ProgressTxt.Text = $"Вопрос {_currentQuestionIndex + 1} из {_currentTestQuestions.Count}";
            QuestionTxt.Text = question.text;
            _currentOptions = question.options.Select(opt => new StudentOption { Text = opt, IsSelected = false }).ToList();
            DynamicOptionsList.ItemsSource = _currentOptions;
            NextQuestionBtn.Content = (_currentQuestionIndex == _currentTestQuestions.Count - 1) ? "Завершить тест" : "Следующий вопрос";
        }

        private async void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = _currentOptions.FindIndex(opt => opt.IsSelected);
            if (selectedIndex == -1) { MessageBox.Show("Выберите вариант ответа!"); return; }

            if (selectedIndex == _currentTestQuestions[_currentQuestionIndex].correct_option) _correctAnswers++;
            _currentQuestionIndex++;

            if (_currentQuestionIndex < _currentTestQuestions.Count) ShowCurrentQuestion();
            else
            {
                // ОТПРАВЛЯЕМ РЕЗУЛЬТАТ И БЛОКИРУЕМ
                string resultStr = $"{_correctAnswers} из {_currentTestQuestions.Count}";
                await ApiService.SetTestResultAsync(_currentTestId, resultStr);

                MessageBox.Show($"Тест завершен!\nРезультат: {resultStr}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                TestPassingGrid.Visibility = Visibility.Collapsed;
                TestSelectionGrid.Visibility = Visibility.Visible;

                // Перезагружаем список (тест заблокируется)
                _ = LoadTestsAsync();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            TokenStorage.Clear(); Session.Token = string.Empty; Session.Role = string.Empty;
            if (Session.AvailableTests != null) Session.AvailableTests.Clear();
            NavigationService.Navigate(new LoginPage());
        }
    }
}