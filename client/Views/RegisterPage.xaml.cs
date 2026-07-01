using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Client.Services;
using Client.Models;

namespace Client.Views
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
            
            // Вызываем локальную загрузку групп
            LoadGroupsLocal();
        }

        private void LoadGroupsLocal()
        {
            // Так как бекенд требует токен для выдачи групп, а при регистрации
            // токена еще нет, мы используем базовый список напрямую в клиенте.
            // (У преподавателя список скачивается с сервера, так как у него токен есть).
            var groups = new List<string> { "A", "B", "C", "Z" };
            
            GroupBox.ItemsSource = groups;
            GroupBox.SelectedIndex = 0;
        }

        // Скрываем или показываем поле выбора группы в зависимости от роли
        private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleBox == null || GroupPanel == null) return;

            var selectedRole = (ComboBoxItem)RoleBox.SelectedItem;
            string roleTag = selectedRole.Tag.ToString()!;

            if (roleTag == "teacher")
            {
                GroupPanel.Visibility = Visibility.Collapsed; // Преподу группа не нужна
            }
            else
            {
                GroupPanel.Visibility = Visibility.Visible; // Студенту показываем
            }
        }

        private async void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "Отправка...";
            
            var selectedRole = (ComboBoxItem)RoleBox.SelectedItem;
            string roleTag = selectedRole.Tag.ToString()!;

            // Берем группу из ComboBox (если это студент). Если препод - отправляем пустую строку
            string selectedGroup = roleTag == "student" && GroupBox.SelectedItem != null 
                ? GroupBox.SelectedItem.ToString()! 
                : "";

            // Отправляем данные на сервер
            if (await ApiService.RegisterAsync(LoginBox.Text, PassBox.Password, FNameBox.Text, LNameBox.Text, roleTag, selectedGroup))
            {
                TokenStorage.SaveToken(Session.Token); 
                if (await ApiService.FetchProfileAsync())
                {
                    if (Session.Role == "teacher") NavigationService.Navigate(new TeacherPage());
                    else if (Session.Role == "student") NavigationService.Navigate(new StudentPage());
                    return;
                }
            }
            ErrorText.Text = "Ошибка! Логин занят, данные неверны или сервер недоступен.";
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }
    }
}