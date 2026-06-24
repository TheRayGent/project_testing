using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Models;

namespace Client.Services
{
    public static class ApiService
    {
        // Адрес сервера
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

        // 1. ВХОД
        public static async Task<bool> LoginAsync(string username, string password)
        {
            var req = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("api/users/login", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    Session.Token = doc.RootElement.GetProperty("token").GetString();
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        // 2. ПОЛУЧЕНИЕ ПРОФИЛЯ (Узнаем кто это: teacher или student)
        public static async Task<bool> FetchProfileAsync()
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/users/profile", content);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                Session.Role = doc.RootElement.GetProperty("role").GetString();
                Session.FullName = $"{doc.RootElement.GetProperty("firstname").GetString()} {doc.RootElement.GetProperty("lastname").GetString()}";
                return true;
            }
            return false;
        }

        // 3. СОЗДАНИЕ ТЕСТА (Для учителя)
        public static async Task<bool> CreateTestAsync(string title, string desc, object[] questions)
        {
            var req = new 
            { 
                token = Session.Token, 
                title, 
                description = desc, 
                access = "public", 
                questions 
            };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/tests/create", content);
            return response.IsSuccessStatusCode;
        }
    }
}