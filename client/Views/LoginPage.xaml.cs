using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;

namespace Client.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage() => InitializeComponent();

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "Подключение...";

            if (await ApiService.LoginAsync(LoginBox.Text, PassBox.Password))
            {
                TokenStorage.SaveToken(Session.Token); 

                if (await ApiService.FetchProfileAsync())
                {
                    if (Session.Role == "teacher")
                        NavigationService.Navigate(new TeacherPage());
                    else if (Session.Role == "student")
                        NavigationService.Navigate(new StudentPage());
                    return;
                }
            }
            
            ErrorText.Text = "Ошибка входа. Проверьте логин/пароль.";
        }
        private void GoToRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }
    }
}