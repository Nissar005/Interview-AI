using System.Windows;
using System.Windows.Input;
using PrivateAIOverlay.AI;
using PrivateAIOverlay.Audio;
using PrivateAIOverlay.Memory;
using PrivateAIOverlay.Security;
using PrivateAIOverlay.Settings;
using PrivateAIOverlay.Windows;

namespace PrivateAIOverlay;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly MemoryManager _memoryManager;
    private readonly HotkeyManager _hotkeyManager;
    private readonly WindowsSpeechToTextProvider _speechToTextProvider;
    private MicrophoneCapture? _microphoneCapture;
    private bool _isListening;

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _memoryManager = new MemoryManager();
        _hotkeyManager = new HotkeyManager(this);
        _speechToTextProvider = new WindowsSpeechToTextProvider
        {
            BaseUrl = _settings.BaseUrl,
            Model = "whisper-1"
        };

        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
        Left = settings.WindowLeft;
        Top = settings.WindowTop;
        Opacity = settings.Opacity;
        Topmost = settings.AlwaysOnTop;

        Loaded += OnLoaded;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
        ListenButton.Click += ListenButton_Click;
        SendButton.Click += SendButton_Click;
        SettingsButton.Click += (_, _) => OpenSettings();
        MinimizeToggleButton.Click += (_, _) => WindowState = WindowState.Minimized;
        CloseButton.Click += (_, _) => Hide();
        InputTextBox.Text = string.Empty;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowPolicies();

        if (string.IsNullOrWhiteSpace(CredentialManager.LoadApiKey()))
        {
            ResponseTextBlock.Text = "No API key is configured. Add your provider key in Settings to enable AI.";
            OpenSettings();
        }

        RegisterHotkeys();
    }

    private void ApplyWindowPolicies()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        var updatedStyle = exStyle | (nint)NativeMethods.WS_EX_TOOLWINDOW | (nint)NativeMethods.WS_EX_NOACTIVATE;
        updatedStyle &= ~(nint)NativeMethods.WS_EX_APPWINDOW;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, updatedStyle);
        NativeMethods.SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_settings.RememberPosition)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
        }

        if (_settings.RememberSize)
        {
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
        }

        _settings.Opacity = Opacity;
        _settings.AlwaysOnTop = Topmost;
        SettingsService.Save(_settings);
        _hotkeyManager.Dispose();
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void RegisterHotkeys()
    {
        try
        {
            _hotkeyManager.Register("ToggleOverlay", Key.Space, ModifierKeys.Control | ModifierKeys.Shift, ToggleOverlay);
            _hotkeyManager.Register("Listen", Key.V, ModifierKeys.Control | ModifierKeys.Shift, ToggleListeningState);
            _hotkeyManager.Register("AnalyzeScreen", Key.S, ModifierKeys.Control | ModifierKeys.Shift, () => _ = AnalyzeCurrentScreenAsync());
        }
        catch (Exception ex)
        {
            ResponseTextBlock.Text = ex.Message;
        }
    }

    private void ToggleOverlay()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        Show();
        Activate();
        InputTextBox.Focus();
    }

    private async void ToggleListeningState()
    {
        _isListening = !_isListening;
        ListenButton.Content = _isListening ? "🔴 LISTENING..." : "🎙 LISTEN";
        ResponseTextBlock.Text = _isListening ? "Listening..." : "Ready.";

        if (_isListening)
        {
            try
            {
                _microphoneCapture = new MicrophoneCapture();
                await _microphoneCapture.StartAsync(_settings.SelectedMicrophone);
            }
            catch (Exception ex)
            {
                _isListening = false;
                ListenButton.Content = "🎙 LISTEN";
                ResponseTextBlock.Text = ex.Message;
                _microphoneCapture?.Dispose();
                _microphoneCapture = null;
            }

            return;
        }

        if (_microphoneCapture is null)
        {
            return;
        }

        try
        {
            await _microphoneCapture.StopAsync();
            var audio = _microphoneCapture.GetCapturedAudio();
            if (audio is null || audio.Length == 0)
            {
                ResponseTextBlock.Text = "No audio was captured.";
                _microphoneCapture.Dispose();
                _microphoneCapture = null;
                return;
            }

            ResponseTextBlock.Text = "Transcribing...";
            _speechToTextProvider.BaseUrl = _settings.BaseUrl;
            _speechToTextProvider.Model = _settings.AIModel;
            var transcript = await _speechToTextProvider.TranscribeAsync(audio);

            if (string.IsNullOrWhiteSpace(transcript) || transcript.Contains("No API key is configured", StringComparison.OrdinalIgnoreCase) || transcript.Contains("Transcription failed", StringComparison.OrdinalIgnoreCase))
            {
                ResponseTextBlock.Text = transcript;
            }
            else
            {
                InputTextBox.Text = transcript;
                ResponseTextBlock.Text = $"Heard: {transcript}";
                await SendAsync(transcript);
            }
        }
        catch (Exception ex)
        {
            ResponseTextBlock.Text = ex.Message;
        }
        finally
        {
            if (_microphoneCapture is not null)
            {
                _microphoneCapture.Dispose();
            }

            _microphoneCapture = null;
        }
    }

    private void ListenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleListeningState();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var question = InputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            ResponseTextBlock.Text = "Ask me anything...";
            return;
        }

        await SendAsync(question);
    }

    private async Task AnalyzeCurrentScreenAsync()
    {
        try
        {
            ResponseTextBlock.Text = "Capturing screen...";
            var screenPath = ScreenCaptureService.CaptureCurrentScreen(this);
            var provider = new OpenAICompatibleProvider { BaseUrl = _settings.BaseUrl, Model = _settings.AIModel };

            var relevant = _memoryManager.GetRelevant("screen context");
            var memorySummary = string.Join("\n", relevant.Select(x => x.Content));

            var result = await provider.AnalyzeImageAsync(new AIRequest(
                "Describe the most important visible information on the screen, including any actionable task or context. Keep it short and useful.",
                screenPath,
                null,
                "Current desktop context",
                memorySummary,
                new Dictionary<string, object> { ["source"] = "screen" }));

            ResponseTextBlock.Text = result;
        }
        catch (Exception ex)
        {
            ResponseTextBlock.Text = ex.Message;
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_settings);
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private async Task SendAsync(string question)
    {
        if (MemoryExtractor.TryExtractMemoryInstruction(question, out var memoryText, out var action))
        {
            if (string.Equals(action, "store", StringComparison.OrdinalIgnoreCase))
            {
                var memory = new MemoryRecord
                {
                    Content = memoryText,
                    Category = "Preference",
                    Scope = "Global",
                    Importance = 5,
                    Source = "voice"
                };

                var saved = _memoryManager.Save(memory);
                ResponseTextBlock.Text = saved ? $"Got it. I’ll remember that: {memoryText}" : "I couldn’t save that memory because persistent storage is unavailable right now.";
                InputTextBox.Text = string.Empty;
                return;
            }

            if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
            {
                var matches = _memoryManager.GetAll().Where(x => x.Content.Contains(memoryText, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var entry in matches)
                {
                    _memoryManager.Delete(entry.Id);
                }

                ResponseTextBlock.Text = "Okay, I’ve removed that from memory.";
                InputTextBox.Text = string.Empty;
                return;
            }
        }

        var relevant = _memoryManager.GetRelevant(question);
        var memorySummary = string.Join("\n", relevant.Select(x => x.Content));
        var request = new AIRequest(question, null, null, "Current conversation", memorySummary, new Dictionary<string, object> { ["source"] = "overlay" });
        var provider = new OpenAICompatibleProvider { BaseUrl = _settings.BaseUrl, Model = _settings.AIModel };
        var response = await provider.GenerateTextAsync(request);

        ResponseTextBlock.Text = response;
        InputTextBox.Text = string.Empty;
    }
}
