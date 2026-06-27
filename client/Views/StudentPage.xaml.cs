using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Client.Models;
using Client.Services;

namespace Client.Views
{
    public partial class StudentPage : Page
    {
        private List<ServerQuestionDto> _currentTestQuestions = new();
        private int _currentQuestionIndex = 0;
        private int _correctAnswers = 0;
        private List<StudentOption> _currentOptions = new();

        public StudentPage()
        {
            InitializeComponent();
            StudentNameTxt.Text = $"Студент: {Session.FullName}";
            
            // Загружаем список
            LoadTestsFromProfile(); 
        }

        private async void LoadTestsFromProfile()
        {
            var testsToDisplay = new List<TestItemDto>();

            if (Session.AvailableTests != null)
            {
                foreach (var testId in Session.AvailableTests)
                {
                    var testInfo = await ApiService.GetTestByIdAsync(testId);
                    if (testInfo != null)
                    {
                        testsToDisplay.Add(testInfo);
                    }
                }
            }

            TestsListBox.ItemsSource = testsToDisplay;

            if (testsToDisplay.Count == 0)
            {
                ProgressTxt.Text = "У вас пока нет доступных тестов.";
            }
        }

        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            string testId = btn.Tag?.ToString() ?? string.Empty;
            
            var testInfo = await ApiService.GetTestByIdAsync(testId);

            if (testInfo != null && testInfo.Questions != null && testInfo.Questions.Count > 0)
            {
                // Забираем массив вопросов из теста
                _currentTestQuestions = testInfo.Questions;
                _currentQuestionIndex = 0;
                _correctAnswers = 0;
                
                TestSelectionGrid.Visibility = Visibility.Collapsed;
                TestPassingGrid.Visibility = Visibility.Visible;
                ShowCurrentQuestion();
            }
            else
            {
                MessageBox.Show("Не удалось загрузить вопросы теста.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = _currentOptions.FindIndex(opt => opt.IsSelected);
            if (selectedIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите вариант ответа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedIndex == _currentTestQuestions[_currentQuestionIndex].correct_option) _correctAnswers++;
            _currentQuestionIndex++;

            if (_currentQuestionIndex < _currentTestQuestions.Count) 
            {
                ShowCurrentQuestion();
            }
            else
            {
                MessageBox.Show($"Тест завершен!\nВаш результат: {_correctAnswers} правильных из {_currentTestQuestions.Count}", "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                TestPassingGrid.Visibility = Visibility.Collapsed;
                TestSelectionGrid.Visibility = Visibility.Visible;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            TokenStorage.Clear();
            Session.Token = string.Empty;
            Session.Role = string.Empty;
            if (Session.AvailableTests != null) Session.AvailableTests.Clear();
            NavigationService.Navigate(new LoginPage());
        }
    }
}