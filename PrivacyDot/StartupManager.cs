using System.Reflection;
using System.Runtime.InteropServices;

namespace PrivacyDot;

internal static class StartupManager
{
    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "Privacy Dot.lnk");

    public static bool IsEnabled()
    {
        return File.Exists(ShortcutPath);
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            CreateShortcut();
            return;
        }

        if (File.Exists(ShortcutPath))
        {
            File.Delete(ShortcutPath);
        }
    }

    private static void CreateShortcut()
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Could not create a shortcut.");

            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: new object[] { ShortcutPath })
                ?? throw new InvalidOperationException("Could not create a shortcut.");

            SetShortcutProperty(shortcut, "TargetPath", Application.ExecutablePath);
            SetShortcutProperty(shortcut, "WorkingDirectory", AppContext.BaseDirectory);
            SetShortcutProperty(shortcut, "IconLocation", Application.ExecutablePath);

            shortcut.GetType().InvokeMember(
                "Save",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shortcut,
                args: null);
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void SetShortcutProperty(object shortcut, string propertyName, object value)
    {
        shortcut.GetType().InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: shortcut,
            args: new object[] { value });
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
