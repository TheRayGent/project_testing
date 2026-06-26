using System;
using System.IO;
using System.Text.Json;

namespace Client.Services
{
    public static class TokenStorage
    {
        //Путь
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestingSystem");
        private static readonly string FilePath = Path.Combine(FolderPath, "session.json");

        public static void SaveToken(string token)
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            var data = new { token = token };
            string jsonString = JsonSerializer.Serialize(data);
            
            File.WriteAllText(FilePath, jsonString);
        }

        public static string GetToken()
        {
            if (!File.Exists(FilePath))
                return string.Empty;

            try
            {
                string jsonString = File.ReadAllText(FilePath);
                using var doc = JsonDocument.Parse(jsonString);
                return doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
            }
            catch
            {
                Clear();
                return string.Empty;
            }
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}