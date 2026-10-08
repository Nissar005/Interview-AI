namespace PrivateAIOverlay.Audio;

public sealed class AudioDeviceInfo
{
    public int DeviceNumber { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string FriendlyName { get; init; } = string.Empty;
}
