using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace PrivateAIOverlay.Audio;

public static class AudioDeviceManager
{
    public static IReadOnlyList<AudioDeviceInfo> GetCaptureDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        for (var i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo
            {
                DeviceNumber = i,
                ProductName = caps.ProductName,
                FriendlyName = caps.ProductName
            });
        }

        return devices;
    }

    public static IReadOnlyList<AudioDeviceInfo> GetRenderDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        try
        {
            var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                devices.Add(new AudioDeviceInfo
                {
                    DeviceNumber = i,
                    ProductName = endpoint.FriendlyName,
                    FriendlyName = endpoint.FriendlyName
                });
            }
        }
        catch
        {
            // If the OS APIs are unavailable, return an empty list rather than failing the app.
        }

        return devices;
    }

    public static string GetDefaultCaptureDeviceName()
    {
        var devices = GetCaptureDevices();
        return devices.Count == 0 ? "No microphone detected" : devices[0].FriendlyName;
    }

    public static string GetDefaultRenderDeviceName()
    {
        var devices = GetRenderDevices();
        return devices.Count == 0 ? "No output device detected" : devices[0].FriendlyName;
    }
}
