namespace PrivateAIOverlay.AI;

public interface IAIProvider
{
    Task<string> GenerateTextAsync(AIRequest request, CancellationToken cancellationToken = default);
}

public interface IAIStreamingProvider
{
    IAsyncEnumerable<string> StreamTextAsync(AIRequest request, CancellationToken cancellationToken = default);
}

public interface IVisionProvider
{
    Task<string> AnalyzeImageAsync(AIRequest request, CancellationToken cancellationToken = default);
}
