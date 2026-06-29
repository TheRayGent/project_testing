using System;
using System.Collections.Generic;
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
                    Session.Token = doc.RootElement.GetProperty("token").GetString() ?? string.Empty; 
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        // 2. Добавлено поле group
        public static async Task<bool> RegisterAsync(string user, string pass, string fname, string lname, string role, string group)
        {
            var req = new { username = user, password = pass, firstname = fname, lastname = lname, role = role, group = group };
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
        
        // 3. Получение профиля
        public static async Task<bool> FetchProfileAsync()
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            
            try 
            {
                var response = await client.PostAsync("api/users/profile", content);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    
                    Session.Role = doc.RootElement.GetProperty("role").GetString() ?? string.Empty;
                    Session.FullName = $"{doc.RootElement.GetProperty("firstname").GetString()} {doc.RootElement.GetProperty("lastname").GetString()}";

                    Session.AvailableTests.Clear();
                    if (doc.RootElement.TryGetProperty("available_tests", out var avArray) && avArray.ValueKind == JsonValueKind.Array)
                        foreach (var t in avArray.EnumerateArray()) Session.AvailableTests.Add(t.ToString());

                    Session.CreatedTests.Clear();
                    if (doc.RootElement.TryGetProperty("created_tests", out var crArray) && crArray.ValueKind == JsonValueKind.Array)
                        foreach (var t in crArray.EnumerateArray()) Session.CreatedTests.Add(t.ToString());

                    return true;
                }
            }
            catch { }
            return false;
        }

        //4. Получить конкретный тест по ID
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
                    if (testDto != null) testDto.Id = testId; 
                    return testDto;
                }
            }
            catch { }
            return null;
        }

        // 5. Создание теста
        public static async Task<bool> CreateTestAsync(string title, string desc, object[] questions)
        {
            var req = new { token = Session.Token, title, description = desc, access = "private", questions };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/tests/create", content);
            return response.IsSuccessStatusCode;
        }

        // 6. Список студентов
        public static async Task<List<StudentUserDto>> GetStudentsListAsync()
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var list = new List<StudentUserDto>();
            try
            {
                var response = await client.PostAsync("api/users/list/student", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            list.Add(new StudentUserDto
                            {
                                Id = item.GetProperty("id").GetString() ?? string.Empty,
                                FullName = $"{item.GetProperty("firstname").GetString()} {item.GetProperty("lastname").GetString()}"
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public static async Task<bool> GiveAccessAsync(string testId, List<string> studentIds)
        {
            var req = new { token = Session.Token, users_ids = studentIds };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"api/tests/{testId}/give-access", content);
            return response.IsSuccessStatusCode;
        }

        

        // 7. Для студента: отправить результат
        public static async Task<bool> SetTestResultAsync(string testId, string result)
        {
            var req = new { token = Session.Token, result = result };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"api/tests/{testId}/set_result", content);
            return response.IsSuccessStatusCode;
        }

        // 8. Для учителя: получить результаты 
        public static async Task<Dictionary<string, string>> GetTestResultsAsync(string testId)
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync($"api/tests/{testId}/results", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
            }
            catch { }
            return new Dictionary<string, string>();
        }
    }
}