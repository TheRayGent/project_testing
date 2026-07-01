using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;
using LiveCharts;
using LiveCharts.Wpf;

namespace Client.Views
{
    public partial class TeacherPage : Page
    {
        public ObservableCollection<EditableQuestion> TestQuestions { get; set; } = new();
        public ObservableCollection<TestItemDto> DisplayedTests { get; set; } = new();
        
        // НОВОЕ: Список для визуального отображения результатов
        public ObservableCollection<StudentResultDto> CurrentTestResults { get; set; } = new();

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
            StatsListControl.ItemsSource = CurrentTestResults; // Привязка результатов к UI
            
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

       private async void ShowGiveAccess_Click(object sender, RoutedEventArgs e)
{
    _selectedTestIdForAccess = ((Button)sender).Tag.ToString()!;
    
    // ПРИНУДИТЕЛЬНЫЙ СБРОС:
    GroupComboBox.ItemsSource = null;
    StudentsList.ItemsSource = null;
    SelectAllCheckBox.IsChecked = false;

    var groups = await ApiService.GetGroupsAsync();
    GroupComboBox.ItemsSource = groups;
    if (groups.Count > 0) GroupComboBox.SelectedIndex = 0; // Вызовет загрузку студентов

    MainPanelGrid.Visibility = Visibility.Collapsed;
    CreateTestGrid.Visibility = Visibility.Collapsed;
    StatisticsGrid.Visibility = Visibility.Collapsed;
    GiveAccessGrid.Visibility = Visibility.Visible;
}

private async void GroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (GroupComboBox.SelectedItem == null) return;
    string selectedGroup = GroupComboBox.SelectedItem.ToString()!;
    
    // Получаем свежий список
    var allStudentsInGroup = await ApiService.GetStudentsByGroupAsync(selectedGroup);
    
    // Отфильтровываем тех, у кого УЖЕ ЕСТЬ доступ
    _currentStudentsList = allStudentsInGroup
        .Where(student => student.AvailableTests == null || !student.AvailableTests.Contains(_selectedTestIdForAccess))
        .ToList();

    // Обновляем визуальный список
    StudentsList.ItemsSource = null;
    StudentsList.ItemsSource = _currentStudentsList;
    SelectAllCheckBox.IsChecked = false;

    // ПРОВЕРКА НА ПУСТОЙ СПИСОК (чтобы избежать вылетов и ошибок)
    if (_currentStudentsList.Count == 0)
    {
        SelectAllCheckBox.IsEnabled = false; // Блокируем галочку
        SaveAccessBtn.IsEnabled = false;     // Блокируем кнопку сохранения
    }
    else
    {
        SelectAllCheckBox.IsEnabled = true;  // Разблокируем
        SaveAccessBtn.IsEnabled = true;      // Разблокируем
    }
}


        

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllCheckBox.IsChecked == true;
            foreach (var student in _currentStudentsList)
            {
                student.IsChecked = isChecked;
            }
            StudentsList.ItemsSource = null;
            StudentsList.ItemsSource = _currentStudentsList;
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

        // ИСПРАВЛЕНИЕ: Вывод результатов прямо в окно (без MessageBox)
        private async void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            string testId = ((Button)sender).Tag.ToString()!;
            var results = await ApiService.GetTestResultsAsync(testId);
            
            CurrentTestResults.Clear();

            if (results.Count == 0)
            {
                MessageBox.Show("Этот тест еще никто не прошел.", "Статистика пуста", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int count5 = 0, count4 = 0, count3 = 0, count2 = 0;
            var allStudents = await ApiService.GetStudentsListAsync();

            foreach (var res in results)
            {
                var parts = res.Value.Split(" из ");
                if (parts.Length == 2 && double.TryParse(parts[0], out double correct) && double.TryParse(parts[1], out double total) && total > 0)
                {
                    double percent = correct / total;
                    if (percent >= 0.85) count5++;
                    else if (percent >= 0.70) count4++;
                    else if (percent >= 0.50) count3++;
                    else count2++;
                }

                var student = allStudents.FirstOrDefault(s => s.Id == res.Key);
                string sName = student != null ? student.FullName : $"Студент (ID {res.Key})";
                
                // Добавляем результат студента в красивый UI-список
                CurrentTestResults.Add(new StudentResultDto { StudentName = sName, Score = res.Value });
            }

            StatsChart.Series = new SeriesCollection
            {
                new PieSeries { Title = "Оценка 5", Values = new ChartValues<int> { count5 }, Fill = System.Windows.Media.Brushes.Green },
                new PieSeries { Title = "Оценка 4", Values = new ChartValues<int> { count4 }, Fill = System.Windows.Media.Brushes.DodgerBlue },
                new PieSeries { Title = "Оценка 3", Values = new ChartValues<int> { count3 }, Fill = System.Windows.Media.Brushes.Orange },
                new PieSeries { Title = "Оценка 2", Values = new ChartValues<int> { count2 }, Fill = System.Windows.Media.Brushes.Red }
            };

            StatsTitleTxt.Text = $"Статистика (Всего сдач: {results.Count})";

            MainPanelGrid.Visibility = Visibility.Collapsed;
            GiveAccessGrid.Visibility = Visibility.Collapsed;
            CreateTestGrid.Visibility = Visibility.Collapsed;
            StatisticsGrid.Visibility = Visibility.Visible;
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            CreateTestGrid.Visibility = Visibility.Collapsed;
            GiveAccessGrid.Visibility = Visibility.Collapsed;
            StatisticsGrid.Visibility = Visibility.Collapsed;
            MainPanelGrid.Visibility = Visibility.Visible;
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ThemeToggleBtn.Content = _isDarkTheme ? "☀️ Светлая" : "🌙 Темная";
            string themeFile = _isDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri($"Themes/{themeFile}", System.UriKind.Relative) });
        }
        
        private void ShowCreateTest_Click(object sender, RoutedEventArgs e) { MainPanelGrid.Visibility = Visibility.Collapsed; CreateTestGrid.Visibility = Visibility.Visible; if (TestQuestions.Count == 0) AddNewQuestion(); }
        private void AddNewQuestion() { var q = new EditableQuestion(); q.Options.Add(new EditableOption()); q.Options.Add(new EditableOption()); TestQuestions.Add(q); }
        private void AddQuestion_Click(object sender, RoutedEventArgs e) => AddNewQuestion();
        private void RemoveQuestion_Click(object sender, RoutedEventArgs e) => TestQuestions.Remove((EditableQuestion)((Button)sender).Tag);
        private void AddOption_Click(object sender, RoutedEventArgs e) => ((EditableQuestion)((Button)sender).Tag).Options.Add(new EditableOption());
        private void RemoveOption_Click(object sender, RoutedEventArgs e) { var option = (EditableOption)((Button)sender).Tag; var question = TestQuestions.FirstOrDefault(q => q.Options.Contains(option)); if (question != null && question.Options.Count > 2) question.Options.Remove(option); }
        
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
                MessageBox.Show("Тест сохранен!"); TestTitle.Text = ""; TestDesc.Text = ""; TestQuestions.Clear();
                await LoadCreatedTestsAsync(); BackToMain_Click(null!, null!);
            }
        }
        private void Logout_Click(object sender, RoutedEventArgs e) { TokenStorage.Clear(); Session.Token = string.Empty; Session.Role = string.Empty; if (Session.CreatedTests != null) Session.CreatedTests.Clear(); NavigationService.Navigate(new LoginPage()); }
    }
}