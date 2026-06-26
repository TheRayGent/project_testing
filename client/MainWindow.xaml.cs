using System.Windows;
using Client.Models;
using Client.Services;

namespace Client
{
    public partial class MainWindow : Window
    {
        private bool isDarkTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        // Логика смены темы
        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ThemeToggleBtn.Content = isDarkTheme ? "☀️ Светлая" : "🌙 Темная";

            string themeFile = isDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
            var uri = new Uri($"Themes/{themeFile}", UriKind.Relative);

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Читаем сохраненный токен
            string savedToken = TokenStorage.GetToken();

            if (!string.IsNullOrEmpty(savedToken))
            {
                // Записываем токен в сессию
                Session.Token = savedToken;

                // 2. Стучимся на сервер в C++, чтобы проверить, жив ли токен
                bool isTokenValid = await ApiService.FetchProfileAsync();

                if (isTokenValid)
                {
                    // ТОКЕН ЖИВ! Сервер нас узнал. Открываем нужный экран:
                    if (Session.Role == "teacher")
                        MainFrame.Navigate(new Views.TeacherPage());
                    else if (Session.Role == "student")
                        MainFrame.Navigate(new Views.StudentPage());
                    
                    return; // Прерываем выполнение, логин не нужен
                }
                else
                {
                    // Токен протух (например, прошло 24 часа). Удаляем его.
                    TokenStorage.Clear();
                }
            }

            // 3. Если токена нет или он истек — показываем страницу входа
            MainFrame.Navigate(new Views.LoginPage());
        }
    }
}