using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PrivateAIOverlay.Security;

public static class CredentialManager
{
    private static readonly string AppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrivateAIOverlay");
    private static readonly string ApiKeyPath = Path.Combine(AppDirectory, "api.key");

    public static void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        Directory.CreateDirectory(AppDirectory);
        var data = Encoding.UTF8.GetBytes(apiKey);
        var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(ApiKeyPath, protectedData);
    }

    public static string? LoadApiKey()
    {
        if (!File.Exists(ApiKeyPath))
        {
            return null;
        }

        try
        {
            var protectedData = File.ReadAllBytes(ApiKeyPath);
            var raw = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(raw);
        }
        catch
        {
            return null;
        }
    }

    public static void ClearApiKey()
    {
        if (File.Exists(ApiKeyPath))
        {
            File.Delete(ApiKeyPath);
        }
    }
}
