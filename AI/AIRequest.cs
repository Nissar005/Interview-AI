namespace PrivateAIOverlay.AI;

public sealed record AIRequest(
    string Text,
    string? Image = null,
    string? AudioContext = null,
    string? ConversationContext = null,
    string? RelevantMemory = null,
    IReadOnlyDictionary<string, object>? Metadata = null);
