using System.IO;
using NAudio.Wave;

namespace PrivateAIOverlay.Audio;

public sealed class MicrophoneCapture : IAudioInput, IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _audioBuffer;
    private string? _selectedDeviceName;
    private readonly object _syncLock = new();

    public event EventHandler<MicrophoneAudioCapturedEventArgs>? AudioCaptured;

    public bool IsRunning { get; private set; }

    public string? SelectedDeviceName => _selectedDeviceName;

    public Task StartAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        Stop();

        var devices = AudioDeviceManager.GetCaptureDevices();
        var device = string.IsNullOrWhiteSpace(deviceName)
            ? devices.FirstOrDefault()
            : devices.FirstOrDefault(d => string.Equals(d.FriendlyName, deviceName, StringComparison.OrdinalIgnoreCase));

        if (device is null)
        {
            throw new InvalidOperationException("No valid microphone device was found.");
        }

        _selectedDeviceName = device.FriendlyName;
        _audioBuffer = new MemoryStream();

        _waveIn = new WaveInEvent
        {
            DeviceNumber = device.DeviceNumber,
            WaveFormat = new WaveFormat(16000, 1)
        };

        _waveIn.DataAvailable += (_, e) =>
        {
            lock (_syncLock)
            {
                if (_audioBuffer is not null)
                {
                    _audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }

            AudioCaptured?.Invoke(this, new MicrophoneAudioCapturedEventArgs(e.Buffer, e.BytesRecorded, 16000, 1));
        };

        _waveIn.StartRecording();
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Stop();
        return Task.CompletedTask;
    }

    public byte[]? GetCapturedAudio()
    {
        lock (_syncLock)
        {
            return _audioBuffer?.ToArray();
        }
    }

    public void Stop()
    {
        if (_waveIn is null)
        {
            IsRunning = false;
            return;
        }

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
        IsRunning = false;
    }

    public void Dispose()
    {
        Stop();
        _audioBuffer?.Dispose();
    }
}

public interface IAudioInput
{
    event EventHandler<MicrophoneAudioCapturedEventArgs>? AudioCaptured;
    bool IsRunning { get; }
    string? SelectedDeviceName { get; }
    Task StartAsync(string deviceName, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public sealed class MicrophoneAudioCapturedEventArgs : EventArgs
{
    public MicrophoneAudioCapturedEventArgs(byte[] buffer, int bytesRecorded, int sampleRate, int channels)
    {
        Buffer = buffer;
        BytesRecorded = bytesRecorded;
        SampleRate = sampleRate;
        Channels = channels;
    }

    public byte[] Buffer { get; }
    public int BytesRecorded { get; }
    public int SampleRate { get; }
    public int Channels { get; }
}
