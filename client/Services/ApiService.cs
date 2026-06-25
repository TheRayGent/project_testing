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
                return true;
            }
            return false;
        }

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