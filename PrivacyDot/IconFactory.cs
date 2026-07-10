using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PrivacyDot;

internal static class IconFactory
{
    private static readonly Color MicrophoneColor = Color.FromArgb(36, 211, 102);
    private static readonly Color CameraColor = Color.FromArgb(255, 149, 0);
    private static readonly Color IdleColor = Color.FromArgb(142, 142, 147);

    public static Icon Create(DeviceUsageSnapshot snapshot)
    {
        var size = Math.Max(32, Math.Max(SystemInformation.SmallIconSize.Width, SystemInformation.SmallIconSize.Height));
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var padding = Math.Max(3, size / 8);
        var rect = new RectangleF(padding, padding, size - padding * 2, size - padding * 2);

        using var path = new GraphicsPath();
        path.AddEllipse(rect);

        using var shadow = new SolidBrush(Color.FromArgb(55, Color.Black));
        graphics.FillEllipse(shadow, rect.X + 1, rect.Y + 1, rect.Width, rect.Height);

        if (snapshot.IsMicrophoneActive && snapshot.IsCameraActive)
        {
            graphics.SetClip(path);

            using (var micBrush = new SolidBrush(MicrophoneColor))
            using (var cameraBrush = new SolidBrush(CameraColor))
            {
                graphics.FillRectangle(micBrush, rect.X, rect.Y, rect.Width / 2, rect.Height);
                graphics.FillRectangle(cameraBrush, rect.X + rect.Width / 2, rect.Y, rect.Width / 2, rect.Height);
            }

            graphics.ResetClip();
        }
        else
        {
            var color = snapshot.IsMicrophoneActive
                ? MicrophoneColor
                : snapshot.IsCameraActive
                    ? CameraColor
                    : IdleColor;

            using var brush = new SolidBrush(color);
            graphics.FillEllipse(brush, rect);
        }

        using var outline = new Pen(Color.FromArgb(210, Color.White), Math.Max(1, size / 18f));
        graphics.DrawEllipse(outline, rect);

        var iconHandle = bitmap.GetHicon();

        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
