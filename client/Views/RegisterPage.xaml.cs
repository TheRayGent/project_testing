using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;

namespace Client.Views
{
    public partial class RegisterPage : Page
    {
        public RegisterPage() => InitializeComponent();

        private async void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "Отправка...";
            var selectedRole = (ComboBoxItem)RoleBox.SelectedItem;
            string roleTag = selectedRole.Tag.ToString()!;

            // Отправляем данные вместе с группой
            if (await ApiService.RegisterAsync(LoginBox.Text, PassBox.Password, FNameBox.Text, LNameBox.Text, roleTag, GroupBox.Text))
            {
                TokenStorage.SaveToken(Session.Token); 
                if (await ApiService.FetchProfileAsync())
                {
                    if (Session.Role == "teacher") NavigationService.Navigate(new TeacherPage());
                    else if (Session.Role == "student") NavigationService.Navigate(new StudentPage());
                    return;
                }
            }
            ErrorText.Text = "Ошибка! Логин занят или данные неверны.";
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new LoginPage());
    }
}