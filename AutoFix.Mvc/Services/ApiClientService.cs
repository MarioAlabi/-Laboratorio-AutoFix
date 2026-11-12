using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFix.Mvc.Models;

namespace AutoFix.Mvc.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CorrelationId { get; init; }
}

public class ApiClientService
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IConfiguration _config;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClientService(IHttpClientFactory factory, IHttpContextAccessor httpContext, IConfiguration config)
    {
        _factory = factory;
        _httpContext = httpContext;
        _config = config;
    }

    public string ApiBaseUrl => (_config["Api:BaseUrl"] ?? "http://localhost:5215/").TrimEnd('/');

    private ISession? Session => _httpContext.HttpContext?.Session;

    public bool EstaAutenticado => !string.IsNullOrEmpty(ObtenerToken());
    public string? ObtenerToken() => Session?.GetString("JwtToken");
    public string? ObtenerRol() => Session?.GetString("UserRole");
    public string? ObtenerEmail() => Session?.GetString("UserEmail");
    public string? ObtenerNombre() => Session?.GetString("UserName");
    public List<string> ObtenerClaims()
    {
        var raw = Session?.GetString("UserClaims");
        return string.IsNullOrEmpty(raw) ? [] : raw.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public bool TieneRol(params string[] roles) => roles.Contains(ObtenerRol());
    public bool TienePermiso(string claim) => ObtenerClaims().Contains(claim);

    public void GuardarSesion(AuthResponseVm auth)
    {
        Session?.SetString("JwtToken", auth.Token);
        Session?.SetString("UserRole", auth.Rol);
        Session?.SetString("UserEmail", auth.Email);
        Session?.SetString("UserName", auth.NombreCompleto);
        Session?.SetString("UserClaims", string.Join(",", auth.Claims));
    }

    public void CerrarSesion() => Session?.Clear();

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient("AutoFixApi");
        var token = ObtenerToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    public async Task<ApiResult<T>> GetAsync<T>(string endpoint)
    {
        var client = CreateClient();
        var response = await client.GetAsync(endpoint);
        return await ParseResponseAsync<T>(response);
    }

    public async Task<ApiResult<TRes>> PostAsync<TReq, TRes>(string endpoint, TReq payload)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(endpoint, payload);
        return await ParseResponseAsync<TRes>(response);
    }

    public async Task<ApiResult<TRes>> PutAsync<TReq, TRes>(string endpoint, TReq payload)
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync(endpoint, payload);
        return await ParseResponseAsync<TRes>(response);
    }

    public async Task<ApiResult<bool>> DeleteAsync(string endpoint)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync(endpoint);
        var corr = response.Headers.TryGetValues("X-Correlation-ID", out var vals) ? vals.FirstOrDefault() : null;
        if (response.IsSuccessStatusCode)
            return new ApiResult<bool> { Success = true, StatusCode = (int)response.StatusCode, Data = true, CorrelationId = corr };

        var msg = await ExtractErrorMessageAsync(response);
        return new ApiResult<bool> { Success = false, StatusCode = (int)response.StatusCode, ErrorMessage = msg, CorrelationId = corr };
    }

    public async Task<ApiResult<TRes>> PostFileAsync<TRes>(string endpoint, IFormFile archivo, Dictionary<string, string?>? extraFields = null)
    {
        var client = CreateClient();
        using var form = new MultipartFormDataContent();
        await using var stream = archivo.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType);
        form.Add(fileContent, "archivo", archivo.FileName);

        if (extraFields is not null)
        {
            foreach (var kv in extraFields)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    form.Add(new StringContent(kv.Value), kv.Key);
            }
        }

        var response = await client.PostAsync(endpoint, form);
        return await ParseResponseAsync<TRes>(response);
    }

    private static async Task<ApiResult<T>> ParseResponseAsync<T>(HttpResponseMessage response)
    {
        var corr = response.Headers.TryGetValues("X-Correlation-ID", out var vals) ? vals.FirstOrDefault() : null;
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOpts);
            return new ApiResult<T>
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                Data = data,
                CorrelationId = corr
            };
        }

        var error = await ExtractErrorMessageAsync(response);
        return new ApiResult<T>
        {
            Success = false,
            StatusCode = (int)response.StatusCode,
            ErrorMessage = error,
            CorrelationId = corr
        };
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("mensaje", out var m))
                    return m.GetString() ?? raw;
                if (doc.RootElement.TryGetProperty("title", out var t))
                    return t.GetString() ?? raw;
            }
            catch
            {
                return raw;
            }
        }

        return (int)response.StatusCode switch
        {
            401 => "Sesión expirada o no autenticado (401).",
            403 => "No tienes permisos suficientes para realizar esta acción (403).",
            404 => "Recurso no encontrado (404).",
            409 => "Conflicto de concurrencia o registro duplicado (409).",
            _ => $"Error HTTP {(int)response.StatusCode}."
        };
    }
}
