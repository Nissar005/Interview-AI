namespace PrivateAIOverlay.Settings;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool TrayIconEnabled { get; set; } = true;
    public bool RememberPosition { get; set; } = true;
    public bool RememberSize { get; set; } = true;
    public double Opacity { get; set; } = 0.85;
    public string Theme { get; set; } = "Dark";
    public double WindowWidth { get; set; } = 500;
    public double WindowHeight { get; set; } = 600;
    public double WindowLeft { get; set; } = 160;
    public double WindowTop { get; set; } = 120;
    public bool ScreenContextEnabled { get; set; }
    public string SelectedMicrophone { get; set; } = "Default";
    public string SelectedOutputDevice { get; set; } = "Default";
    public string AIProvider { get; set; } = "OpenAICompatible";
    public string AIModel { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ResponseLength { get; set; } = "Balanced";
    public bool PersistentMemoryEnabled { get; set; } = true;
    public bool AutomaticMemoryEnabled { get; set; }
    public bool MemoryUseEnabled { get; set; } = true;
}
