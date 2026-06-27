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
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

        // 1. Вход
        public static async Task<bool> LoginAsync(string username, string password)
        {
            var req = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("api/users/login", content);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    // Добавлено ?? string.Empty для защиты от null
                    Session.Token = doc.RootElement.GetProperty("token").GetString() ?? string.Empty; 
                    return true;
                }
                return false;
            }
            catch { return false; }
        }
        
        // 2. Получение профиля
        public static async Task<bool> FetchProfileAsync()
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/users/profile", content);
            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
        
                Session.Role = doc.RootElement.GetProperty("role").GetString() ?? string.Empty;
                string fName = doc.RootElement.GetProperty("firstname").GetString() ?? string.Empty;
                string lName = doc.RootElement.GetProperty("lastname").GetString() ?? string.Empty;
                Session.FullName = $"{fName} {lName}";

                // ЧИТАЕМ МАССИВ ДОСТУПНЫХ ТЕСТОВ ИЗ БЕКЕНДА
                Session.AvailableTests.Clear();
                if (doc.RootElement.TryGetProperty("available_tests", out var testsArray) && testsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in testsArray.EnumerateArray())
                    {
                        Session.AvailableTests.Add(t.GetString() ?? string.Empty);
                    }
                }
                return true;
            }
            return false;
        }

        
        // 3. Создание теста
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

        
        public static async Task<bool> RegisterAsync(string user, string pass, string fname, string lname, string role)
        {
            var req = new { username = user, password = pass, firstname = fname, lastname = lname, role = role };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("api/users/register", content);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    Session.Token = doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }
        
        
        //4. Получить список всех тестов
        public static async Task<List<TestItemDto>> GetTestsListAsync()
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("api/tests/list", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<TestItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TestItemDto>();
                }
            }
            catch { }
            return new List<TestItemDto>();
        }

        //5. Получить конкретный тест по ID
        public static async Task<TestItemDto?> GetTestByIdAsync(string testId)
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"api/tests/{testId}", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var testDto = JsonSerializer.Deserialize<TestItemDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (testDto != null) testDto.Id = testId; // Сохраняем ID
                    return testDto;
                }
            }
            catch { }
            return null;
        }
    }
    
    
    
    
}