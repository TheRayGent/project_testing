using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;

namespace Client.Views
{
    public partial class TeacherPage : Page
    {
        public TeacherPage()
        {
            InitializeComponent();
            TeacherNameTxt.Text = $"Преподаватель:\n{Session.FullName}";
        }

        private async void SaveTest_Click(object sender, RoutedEventArgs e)
        {
            // Генерируем массив вопросов в том виде, в котором его ждет твой C++ код
            var questions = new object[]
            {
                new { text = "Вопрос 1", options = new[] {"A", "B"}, correct = 1 },
                new { text = "Вопрос 2", options = new[] {"Да", "Нет", "Возможно"}, correct = 0 }
            };

            bool ok = await ApiService.CreateTestAsync(TestTitle.Text, TestDesc.Text, questions);
            MessageBox.Show(ok ? "Тест успешно создан в tests_db.json!" : "Ошибка создания!");
        }
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