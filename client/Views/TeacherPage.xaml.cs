using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;

namespace Client.Views
{
    public partial class TeacherPage : Page
    {
        // Динамический список вопросов, к которому привязан интерфейс
        public ObservableCollection<EditableQuestion> TestQuestions { get; set; }

        public TeacherPage()
        {
            InitializeComponent();
            TeacherNameTxt.Text = $"Преподаватель:\n{Session.FullName}";

            // Инициализируем список и привязываем его к интерфейсу
            TestQuestions = new ObservableCollection<EditableQuestion>();
            QuestionsList.ItemsSource = TestQuestions;

            // При открытии сразу добавляем один пустой вопрос для удобства
            AddNewQuestion();
        }

        private void AddNewQuestion()
        {
            var newQuestion = new EditableQuestion();
            // По умолчанию даем 2 пустых варианта ответа
            newQuestion.Options.Add(new EditableOption());
            newQuestion.Options.Add(new EditableOption());
            TestQuestions.Add(newQuestion);
        }

        // --- ОБРАБОТЧИКИ КНОПОК ИНТЕРФЕЙСА ---

        private void AddQuestion_Click(object sender, RoutedEventArgs e) => AddNewQuestion();

        private void RemoveQuestion_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var question = (EditableQuestion)btn.Tag;
            TestQuestions.Remove(question);
        }

        private void AddOption_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var question = (EditableQuestion)btn.Tag;
            question.Options.Add(new EditableOption());
        }

        private void RemoveOption_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var option = (EditableOption)btn.Tag;
            // Ищем, какому вопросу принадлежит этот вариант ответа
            var question = TestQuestions.FirstOrDefault(q => q.Options.Contains(option));
            if (question != null && question.Options.Count > 2)
            {
                question.Options.Remove(option);
            }
            else
            {
                MessageBox.Show("В вопросе должно быть минимум 2 варианта ответа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --- ОТПРАВКА НА СЕРВЕР C++ ---

        private async void SaveTest_Click(object sender, RoutedEventArgs e)
        {
            // 1. ВАЛИДАЦИЯ ПЕРЕД ОТПРАВКОЙ (чтобы C++ не выдал ошибку 400)
            if (string.IsNullOrWhiteSpace(TestTitle.Text))
            {
                MessageBox.Show("Введите название теста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (TestQuestions.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один вопрос!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var apiQuestionsList = new List<object>();

            foreach (var q in TestQuestions)
            {
                if (string.IsNullOrWhiteSpace(q.Text))
                {
                    MessageBox.Show("Один из вопросов не содержит текста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ищем индекс правильного ответа (где IsCorrect == true)
                int correctIndex = q.Options.ToList().FindIndex(opt => opt.IsCorrect);
                
                if (correctIndex == -1)
                {
                    MessageBox.Show($"В вопросе '{q.Text}' не выбран правильный ответ!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Формируем структуру ровно так, как ждет твой C++ код
                apiQuestionsList.Add(new
                {
                    text = q.Text,
                    options = q.Options.Select(opt => string.IsNullOrWhiteSpace(opt.Text) ? "Пустой вариант" : opt.Text).ToArray(),
                    correct_option = correctIndex
                });
            }

            // 2. ОТПРАВКА
            bool isSuccess = await ApiService.CreateTestAsync(TestTitle.Text, TestDesc.Text, apiQuestionsList.ToArray());

            if (isSuccess)
            {
                MessageBox.Show("Тест успешно сохранен на сервере!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                // Очищаем форму после успешного создания
                TestTitle.Text = "";
                TestDesc.Text = "";
                TestQuestions.Clear();
                AddNewQuestion();
            }
            else
            {
                MessageBox.Show("Ошибка сохранения. Проверьте подключение к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Выход из аккаунта
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            TokenStorage.Clear();
            Session.Token = string.Empty;
            Session.Role = string.Empty;
            Session.FullName = string.Empty;
            NavigationService.Navigate(new LoginPage());
        }
    }
}