using System.Collections.Generic;
using System.Windows; // Это исправляет ошибку RoutedEventArgs
using System.Windows.Controls;
using Client.Models;
using Client.Services;

namespace Client.Views
{
    public partial class StudentPage : Page
    {
        public StudentPage()
        {
            InitializeComponent();
            StudentNameTxt.Text = $"Студент: {Session.FullName}";
            LoadDynamicQuestion();
        }

        private void LoadDynamicQuestion()
        {
            // Пример вопроса для демонстрации динамических кнопок
            var questionFromCpp = new QuestionDto
            {
                QuestionText = "Какой язык ты используешь для бекенда?",
                Options = new List<string> { "Python", "Java", "C++ (Crow)", "C#", "Go" } 
            };

            QuestionTxt.Text = questionFromCpp.QuestionText;
            DynamicOptionsList.ItemsSource = questionFromCpp.Options; 
        }

        // Обработчик кнопки выхода из аккаунта
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // 1. Удаляем токен с диска
            TokenStorage.Clear();
            
            // 2. Очищаем текущую сессию
            Session.Token = string.Empty;
            Session.Role = string.Empty;
            Session.FullName = string.Empty;

            // 3. Возвращаем пользователя на окно авторизации
            NavigationService.Navigate(new LoginPage());
        }
    }
}