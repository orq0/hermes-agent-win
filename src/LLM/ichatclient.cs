namespace Hermes.Agent.LLM;

using Hermes.Agent.Core;

public interface IChatClient
{
    Task<string> CompleteAsync(IEnumerable<Message> messages, CancellationToken ct);
}

public sealed class LlmConfig
{
    public required string Provider { get; init; }
    public required string Model { get; init; }
    public string? BaseUrl { get; init; }
    public string? ApiKey { get; init; }
}
