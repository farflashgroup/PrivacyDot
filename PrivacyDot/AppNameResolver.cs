using System.Diagnostics;
using System.Text;

namespace PrivacyDot;

internal static class AppNameResolver
{
    public static bool LooksLikeEncodedPath(string identity)
    {
        return identity.Length > 2
            && char.IsLetter(identity[0])
            && identity[1] == ':'
            && identity.IndexOf('#') >= 0;
    }

    public static string DecodeRegistryIdentity(string registryKeyName)
    {
        if (LooksLikeEncodedPath(registryKeyName))
        {
            return registryKeyName.Replace('#', '\\');
        }

        return registryKeyName;
    }

    public static string ResolveDisplayName(string identity, string? fallbackProcessName = null)
    {
        if (LooksLikeWindowsPath(identity))
        {
            return ResolvePathDisplayName(identity, fallbackProcessName);
        }

        if (!string.IsNullOrWhiteSpace(fallbackProcessName))
        {
            return CleanToken(fallbackProcessName!);
        }

        if (identity.StartsWith("pid:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = identity.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 ? CleanToken(parts[parts.Length - 1]) : identity;
        }

        return CleanPackageName(identity);
    }

    private static string ResolvePathDisplayName(string path, string? fallbackProcessName)
    {
        try
        {
            if (File.Exists(path))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);

                if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                {
                    return versionInfo.FileDescription.Trim();
                }

                if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
                {
                    return versionInfo.ProductName.Trim();
                }
            }
        }
        catch
        {
            // Fall back to the executable name when metadata is unavailable.
        }

        if (!string.IsNullOrWhiteSpace(fallbackProcessName))
        {
            return CleanToken(fallbackProcessName!);
        }

        var fileName = Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(fileName) ? path : CleanToken(fileName);
    }

    public static bool LooksLikeWindowsPath(string identity)
    {
        return identity.Length > 2
            && char.IsLetter(identity[0])
            && identity[1] == ':'
            && (identity[2] == '\\' || identity[2] == '/');
    }

    private static string CleanPackageName(string identity)
    {
        var packageName = identity.Split(new[] { '_' }, 2)[0];
        packageName = packageName.Replace('.', ' ').Replace('-', ' ');
        return CleanToken(packageName);
    }

    private static string CleanToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown app";
        }

        var builder = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            var previous = i > 0 ? value[i - 1] : '\0';
            var next = i + 1 < value.Length ? value[i + 1] : '\0';
            var shouldInsertSpace = i > 0
                && char.IsUpper(current)
                && (char.IsLower(previous) || (char.IsUpper(previous) && char.IsLower(next)));

            if (shouldInsertSpace)
            {
                builder.Append(' ');
            }

            builder.Append(current is '_' or '-' ? ' ' : current);
        }

        return string.Join(" ", builder.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }
}
