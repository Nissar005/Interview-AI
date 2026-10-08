using System.Windows;
using PrivateAIOverlay.AI;
using PrivateAIOverlay.Audio;
using PrivateAIOverlay.Security;
using PrivateAIOverlay.Settings;

namespace PrivateAIOverlay;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        Loaded += (_, _) => LoadDevices();
        RefreshButton.Click += (_, _) => LoadDevices();
        TestButton.Click += async (_, _) => await TestProviderAsync();
        GeminiPresetButton.Click += (_, _) => ApplyGeminiPreset();
        SaveButton.Click += (_, _) => Save();

        ApiKeyPasswordBox.Password = CredentialManager.LoadApiKey() ?? string.Empty;
        ModelTextBox.Text = settings.AIModel;
        BaseUrlTextBox.Text = settings.BaseUrl;
    }

    private void ApplyGeminiPreset()
    {
        ModelTextBox.Text = "gemini-3.8-flash";
        BaseUrlTextBox.Text = "https://generativelanguage.googleapis.com/v1beta/openai/";
    }

    private void LoadDevices()
    {
        var inputDevices = AudioDeviceManager.GetCaptureDevices();
        MicrophoneComboBox.Items.Clear();

        foreach (var device in inputDevices)
        {
            MicrophoneComboBox.Items.Add(device.FriendlyName);
        }

        if (inputDevices.Count == 0)
        {
            MicrophoneComboBox.Items.Add("No microphones detected");
            MicrophoneComboBox.SelectedIndex = 0;
        }
        else
        {
            var selected = string.IsNullOrWhiteSpace(_settings.SelectedMicrophone)
                ? inputDevices[0].FriendlyName
                : inputDevices.FirstOrDefault(d => string.Equals(d.FriendlyName, _settings.SelectedMicrophone, StringComparison.OrdinalIgnoreCase))?.FriendlyName ?? inputDevices[0].FriendlyName;

            MicrophoneComboBox.SelectedItem = selected;
        }

        var outputDevices = AudioDeviceManager.GetRenderDevices();
        OutputDeviceComboBox.Items.Clear();

        foreach (var device in outputDevices)
        {
            OutputDeviceComboBox.Items.Add(device.FriendlyName);
        }

        if (outputDevices.Count == 0)
        {
            OutputDeviceComboBox.Items.Add("No output devices detected");
            OutputDeviceComboBox.SelectedIndex = 0;
        }
        else
        {
            var selected = string.IsNullOrWhiteSpace(_settings.SelectedOutputDevice)
                ? outputDevices[0].FriendlyName
                : outputDevices.FirstOrDefault(d => string.Equals(d.FriendlyName, _settings.SelectedOutputDevice, StringComparison.OrdinalIgnoreCase))?.FriendlyName ?? outputDevices[0].FriendlyName;

            OutputDeviceComboBox.SelectedItem = selected;
        }
    }

    private async Task TestProviderAsync()
    {
        var baseUrl = BaseUrlTextBox.Text.Trim();
        var apiKey = ApiKeyPasswordBox.Password.Trim();
        var model = ModelTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
        {
            System.Windows.MessageBox.Show("Enter an API key and model before testing the provider.", "Private AI Overlay", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TestButton.IsEnabled = false;
        try
        {
            var provider = new OpenAICompatibleProvider(apiKeyOverride: apiKey)
            {
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1" : baseUrl,
                Model = model
            };

            var result = await provider.GenerateTextAsync(new AIRequest("Reply with exactly: OK"));
            var isOk = !string.IsNullOrWhiteSpace(result) &&
                       !result.StartsWith("No API key is configured", StringComparison.OrdinalIgnoreCase) &&
                       !result.StartsWith("AI request failed:", StringComparison.OrdinalIgnoreCase) &&
                       !result.StartsWith("AI service is still busy", StringComparison.OrdinalIgnoreCase);

            System.Windows.MessageBox.Show(isOk ? $"Provider check passed. Response: {result}" : $"Provider check failed. {result}", "Private AI Overlay", MessageBoxButton.OK, isOk ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Provider check failed: {ex.Message}", "Private AI Overlay", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void Save()
    {
        var selectedMicrophone = MicrophoneComboBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selectedMicrophone) && !selectedMicrophone.Equals("No microphones detected", StringComparison.OrdinalIgnoreCase))
        {
            _settings.SelectedMicrophone = selectedMicrophone;
        }

        var selectedOutput = OutputDeviceComboBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selectedOutput) && !selectedOutput.Equals("No output devices detected", StringComparison.OrdinalIgnoreCase))
        {
            _settings.SelectedOutputDevice = selectedOutput;
        }

        _settings.BaseUrl = BaseUrlTextBox.Text.Trim();
        _settings.AIModel = ModelTextBox.Text.Trim();
        var apiKey = ApiKeyPasswordBox.Password.Trim();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            CredentialManager.SaveApiKey(apiKey);
        }
        else
        {
            CredentialManager.ClearApiKey();
        }

        SettingsService.Save(_settings);
        Close();
    }
}
