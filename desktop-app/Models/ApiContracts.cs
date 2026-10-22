using System.Text.Json.Serialization;

namespace IceCreamShop.Desktop.Models;

public class LoginResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public User? User { get; set; }
}

public class ListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class SingleResponse<T>
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "An error occurred.";

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }
}
