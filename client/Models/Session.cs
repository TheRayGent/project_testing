namespace Client.Models
{
    public static class Session
    {
        public static string Token { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static List<string> AvailableTests { get; set; } = new List<string>();
    }
}