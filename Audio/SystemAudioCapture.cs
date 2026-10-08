using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace PrivateAIOverlay.Audio;

public sealed class SystemAudioCapture : IDisposable
{
    private WasapiLoopbackCapture? _capture;
    private MemoryStream? _audioBuffer;
    private readonly object _syncLock = new();

    public event EventHandler<SystemAudioCapturedEventArgs>? AudioCaptured;

    public bool IsRunning { get; private set; }

    public Task StartAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        Stop();

        var enumerator = new MMDeviceEnumerator();
        MMDevice? device = null;

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(d => string.Equals(d.FriendlyName, deviceName, StringComparison.OrdinalIgnoreCase));
        }

        device ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        _audioBuffer = new MemoryStream();
        _capture = new WasapiLoopbackCapture(device);
        _capture.WaveFormat = new WaveFormat(16000, 1);

        _capture.DataAvailable += (_, e) =>
        {
            lock (_syncLock)
            {
                if (_audioBuffer is not null)
                {
                    _audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }

            AudioCaptured?.Invoke(this, new SystemAudioCapturedEventArgs(e.Buffer, e.BytesRecorded, 16000, 1));
        };

        _capture.StartRecording();
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
        if (_capture is null)
        {
            IsRunning = false;
            return;
        }

        _capture.StopRecording();
        _capture.Dispose();
        _capture = null;
        IsRunning = false;
    }

    public void Dispose()
    {
        Stop();
        _audioBuffer?.Dispose();
    }
}

public sealed class SystemAudioCapturedEventArgs : EventArgs
{
    public SystemAudioCapturedEventArgs(byte[] buffer, int bytesRecorded, int sampleRate, int channels)
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
