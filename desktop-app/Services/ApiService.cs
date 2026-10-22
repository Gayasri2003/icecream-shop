using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IceCreamShop.Desktop.Models;

namespace IceCreamShop.Desktop.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }
}

public class ApiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public User? CurrentUser { get; private set; }
    public string? AuthToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AuthToken) && CurrentUser != null;

    public ApiService(string baseAddress = "http://127.0.0.1:8000/api/")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // -------------------------------------------------------------
    // Authentication
    // -------------------------------------------------------------
    public async Task<ApiResult<User>> LoginAsync(string email, string password)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("auth/login", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody, JsonOptions);
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    AuthToken = loginResponse.AccessToken;
                    CurrentUser = loginResponse.User;
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);

                    return new ApiResult<User>
                    {
                        Success = true,
                        Data = CurrentUser,
                        Message = loginResponse.Message,
                        StatusCode = (int)response.StatusCode
                    };
                }
            }

            return ParseErrorResponse<User>(response.StatusCode, responseBody);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<User>
            {
                Success = false,
                Message = $"Unable to connect to Laravel backend at {_httpClient.BaseAddress}. Please ensure 'php artisan serve' is running.",
                StatusCode = 0
            };
        }
        catch (Exception ex)
        {
            return new ApiResult<User> { Success = false, Message = ex.Message, StatusCode = 0 };
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            if (IsAuthenticated)
            {
                await _httpClient.PostAsync("auth/logout", null);
            }
        }
        catch
        {
            // Ignore connection errors on logout
        }
        finally
        {
            AuthToken = null;
            CurrentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        return true;
    }

    // -------------------------------------------------------------
    // Inventory Items
    // -------------------------------------------------------------
    public async Task<ApiResult<List<Item>>> GetItemsAsync(string? search = null, string? category = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category) && !category.StartsWith("All", StringComparison.OrdinalIgnoreCase))
        {
            query.Add($"category={Uri.EscapeDataString(category)}");
        }

        var endpoint = "items" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return await SendRequestAsync<ListResponse<Item>, List<Item>>(HttpMethod.Get, endpoint, null, r => r.Data);
    }

    public async Task<ApiResult<Item>> CreateItemAsync(string name, string category, decimal price, int quantity, string? description)
    {
        var payload = new
        {
            name,
            category,
            price,
            quantity,
            description
        };
        return await SendRequestAsync<SingleResponse<Item>, Item>(HttpMethod.Post, "items", payload, r => r.Data!);
    }

    public async Task<ApiResult<Item>> UpdateItemAsync(long id, string name, string category, decimal price, int quantity, string? description)
    {
        var payload = new
        {
            name,
            category,
            price,
            quantity,
            description
        };
        return await SendRequestAsync<SingleResponse<Item>, Item>(HttpMethod.Put, $"items/{id}", payload, r => r.Data!);
    }

    public async Task<ApiResult<bool>> DeleteItemAsync(long id)
    {
        return await SendRequestAsync<SingleResponse<object>, bool>(HttpMethod.Delete, $"items/{id}", null, _ => true);
    }

    // -------------------------------------------------------------
    // Customers
    // -------------------------------------------------------------
    public async Task<ApiResult<List<Customer>>> GetCustomersAsync(string? search = null)
    {
        var endpoint = "customers" + (!string.IsNullOrWhiteSpace(search) ? $"?search={Uri.EscapeDataString(search)}" : "");
        return await SendRequestAsync<ListResponse<Customer>, List<Customer>>(HttpMethod.Get, endpoint, null, r => r.Data);
    }

    public async Task<ApiResult<Customer>> CreateCustomerAsync(string name, string email, string? phone, string? address)
    {
        var payload = new { name, email, phone, address };
        return await SendRequestAsync<SingleResponse<Customer>, Customer>(HttpMethod.Post, "customers", payload, r => r.Data!);
    }

    public async Task<ApiResult<Customer>> UpdateCustomerAsync(long id, string name, string email, string? phone, string? address)
    {
        var payload = new { name, email, phone, address };
        return await SendRequestAsync<SingleResponse<Customer>, Customer>(HttpMethod.Put, $"customers/{id}", payload, r => r.Data!);
    }

    public async Task<ApiResult<bool>> DeleteCustomerAsync(long id)
    {
        return await SendRequestAsync<SingleResponse<object>, bool>(HttpMethod.Delete, $"customers/{id}", null, _ => true);
    }

    // -------------------------------------------------------------
    // Generic Helper
    // -------------------------------------------------------------
    private async Task<ApiResult<TTarget>> SendRequestAsync<TApiResponse, TTarget>(
        HttpMethod method,
        string endpoint,
        object? body,
        Func<TApiResponse, TTarget> selector)
    {
        try
        {
            using var request = new HttpRequestMessage(method, endpoint);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TApiResponse>(responseBody, JsonOptions);
                return new ApiResult<TTarget>
                {
                    Success = true,
                    Data = result != null ? selector(result) : default,
                    StatusCode = (int)response.StatusCode
                };
            }

            return ParseErrorResponse<TTarget>(response.StatusCode, responseBody);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<TTarget>
            {
                Success = false,
                Message = "Connection failed. Please ensure the Laravel backend server is running (php artisan serve).",
                StatusCode = 0
            };
        }
        catch (Exception ex)
        {
            return new ApiResult<TTarget>
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 0
            };
        }
    }

    private static ApiResult<T> ParseErrorResponse<T>(HttpStatusCode statusCode, string responseBody)
    {
        var statusCodeInt = (int)statusCode;
        string defaultMsg = statusCode switch
        {
            HttpStatusCode.Unauthorized => "Invalid email or password.",
            HttpStatusCode.Forbidden => "Action unauthorized: Your role does not have permission for this operation.",
            HttpStatusCode.NotFound => "The requested resource was not found.",
            HttpStatusCode.UnprocessableEntity => "Validation error. Please verify your inputs.",
            _ => $"HTTP {(int)statusCode} Error"
        };

        try
        {
            var err = JsonSerializer.Deserialize<ErrorResponse>(responseBody, JsonOptions);
            if (err != null)
            {
                var combinedMsg = err.Message ?? defaultMsg;
                if (err.Errors != null && err.Errors.Count > 0)
                {
                    var details = string.Join("; ", err.Errors.Values.SelectMany(v => v));
                    combinedMsg = $"{combinedMsg} ({details})";
                }

                return new ApiResult<T>
                {
                    Success = false,
                    Message = combinedMsg,
                    StatusCode = statusCodeInt,
                    ValidationErrors = err.Errors
                };
            }
        }
        catch
        {
            // ignored
        }

        return new ApiResult<T>
        {
            Success = false,
            Message = defaultMsg,
            StatusCode = statusCodeInt
        };
    }
}
