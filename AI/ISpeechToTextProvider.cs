namespace PrivateAIOverlay.AI;

public interface ISpeechToTextProvider
{
    Task<string> TranscribeAsync(byte[] audioData, CancellationToken cancellationToken = default);
}
