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

        public static async Task<(bool Success, string ErrorMessage)> LoginAsync(string username, string password)
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
                    return (true, "");
                }
                return (false, "Неверный логин или пароль");
            }
            catch (HttpRequestException) { return (false, "Сервер недоступен. Проверьте подключение."); }
            catch (Exception ex) { return (false, $"Внутренняя ошибка: {ex.Message}"); }
        }

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
            }
            catch { }
            return false;
        }

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

        public static async Task<bool> CreateTestAsync(string title, string desc, object[] questions)
        {
            var req = new { token = Session.Token, title, description = desc, access = "private", questions };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/tests/create", content);
            return response.IsSuccessStatusCode;
        }

        public static async Task<List<string>> GetGroupsAsync()
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var list = new List<string>();
            try
            {
                var response = await client.PostAsync("api/groups", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.EnumerateArray()) list.Add(item.GetString() ?? string.Empty);
                }
            }
            catch { }
            return list;
        }

        public static async Task<List<StudentUserDto>> GetStudentsByGroupAsync(string group)
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var list = new List<StudentUserDto>();
            try
            {
                var response = await client.PostAsync($"api/users/list/student/{group}", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var student = new StudentUserDto
                            {
                                Id = item.GetProperty("id").GetString() ?? string.Empty,
                                FullName = $"{item.GetProperty("firstname").GetString()} {item.GetProperty("lastname").GetString()}"
                            };

                            // Читаем, какие тесты уже доступны этому студенту
                            if (item.TryGetProperty("available_tests", out var avTests) && avTests.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var t in avTests.EnumerateArray()) student.AvailableTests.Add(t.ToString());
                            }
                            list.Add(student);
                        }
                    }
                }
            }
            catch { }
            return list;
        }

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

        public static async Task<bool> SetTestResultAsync(string testId, string result)
        {
            var req = new { token = Session.Token, result = result };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"api/tests/{testId}/set_result", content);
            return response.IsSuccessStatusCode;
        }

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

        // НОВЫЙ МЕТОД: Получение результата конкретного студента
        public static async Task<string> GetTestResultForStudentAsync(string testId)
        {
            var req = new { token = Session.Token };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync($"api/tests/{testId}/get_result", content);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    return doc.RootElement.GetProperty("result").GetString() ?? "";
                }
            }
            catch { }
            return string.Empty;
        }
    }
}