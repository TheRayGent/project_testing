using System;
using System.IO;
using System.Text.Json;

namespace Client.Services
{
    public static class TokenStorage
    {
        // Путь: C:\Users\<Имя>\AppData\Local\TestingSystem\session.json
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestingSystem");
        private static readonly string FilePath = Path.Combine(FolderPath, "session.json");

        // Сохранить токен в формате JSON
        public static void SaveToken(string token)
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            // Создаем анонимный объект, который сериализуется ровно в {"token": "твой_токен"}
            var data = new { token = token };
            
            File.WriteAllText(FilePath, json);
        }

        // Прочитать токен из JSON
        public static string GetToken()
        {
            if (!File.Exists(FilePath))
                return string.Empty;

            try
            {
                string json = File.ReadAllText(FilePath);
                
                // Парсим JSON и достаем значение ключа "token"
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
                Clear();
                return string.Empty;
            }
        }

        // Удалить файл (при выходе из аккаунта)
        public static void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}