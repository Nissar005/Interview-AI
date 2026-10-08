using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PrivateAIOverlay.Windows;

public static class ScreenCaptureService
{
    public static string CaptureCurrentScreen(System.Windows.Window? window = null)
    {
        var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bitmap.Size);

        if (window is not null)
        {
            var overlayRect = new Rectangle(
                (int)Math.Max(0, window.Left - bounds.X),
                (int)Math.Max(0, window.Top - bounds.Y),
                (int)Math.Max(1, window.Width),
                (int)Math.Max(1, window.Height));

            using var blackBrush = new SolidBrush(Color.Black);
            graphics.FillRectangle(blackBrush, overlayRect);
        }

        var filePath = Path.Combine(Path.GetTempPath(), $"private-ai-screen-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
        bitmap.Save(filePath, ImageFormat.Png);
        return filePath;
    }
}
