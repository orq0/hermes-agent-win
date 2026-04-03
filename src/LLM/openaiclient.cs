namespace Hermes.Agent.LLM;

using Hermes.Agent.Core;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public sealed class OpenAiClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmConfig _config;
    
    public OpenAiClient(LlmConfig config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
        
        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
        }
    }
    
    public async Task<string> CompleteAsync(IEnumerable<Message> messages, CancellationToken ct)
    {
        var payload = new
        {
            model = _config.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = 0.7,
        };
        
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_config.BaseUrl}/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
