using Microsoft.Win32;
using System.Security;

namespace PrivacyDot;

internal sealed class ThemePalette
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private ThemePalette(
        bool isDark,
        Color windowBack,
        Color panelBack,
        Color menuBack,
        Color menuHover,
        Color primaryText,
        Color secondaryText,
        Color disabledText,
        Color border,
        Color separator,
        Color checkMark)
    {
        IsDark = isDark;
        WindowBack = windowBack;
        PanelBack = panelBack;
        MenuBack = menuBack;
        MenuHover = menuHover;
        PrimaryText = primaryText;
        SecondaryText = secondaryText;
        DisabledText = disabledText;
        Border = border;
        Separator = separator;
        CheckMark = checkMark;
    }

    public bool IsDark { get; }

    public Color WindowBack { get; }

    public Color PanelBack { get; }

    public Color MenuBack { get; }

    public Color MenuHover { get; }

    public Color PrimaryText { get; }

    public Color SecondaryText { get; }

    public Color DisabledText { get; }

    public Color Border { get; }

    public Color Separator { get; }

    public Color CheckMark { get; }

    public static ThemePalette Current
    {
        get
        {
            return IsSystemAppThemeDark() ? Dark : Light;
        }
    }

    public static ThemePalette Light { get; } = new(
        isDark: false,
        windowBack: Color.White,
        panelBack: Color.White,
        menuBack: Color.White,
        menuHover: Color.FromArgb(242, 244, 247),
        primaryText: Color.FromArgb(32, 35, 39),
        secondaryText: Color.FromArgb(101, 108, 118),
        disabledText: Color.FromArgb(135, 141, 150),
        border: Color.FromArgb(196, 202, 211),
        separator: Color.FromArgb(226, 230, 235),
        checkMark: Color.FromArgb(36, 211, 102));

    public static ThemePalette Dark { get; } = new(
        isDark: true,
        windowBack: Color.FromArgb(17, 24, 32),
        panelBack: Color.FromArgb(17, 24, 32),
        menuBack: Color.FromArgb(22, 27, 34),
        menuHover: Color.FromArgb(33, 38, 45),
        primaryText: Color.FromArgb(240, 246, 252),
        secondaryText: Color.FromArgb(139, 148, 158),
        disabledText: Color.FromArgb(110, 118, 129),
        border: Color.FromArgb(72, 79, 88),
        separator: Color.FromArgb(48, 54, 61),
        checkMark: Color.FromArgb(36, 211, 102));

    private static bool IsSystemAppThemeDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            var value = key?.GetValue("AppsUseLightTheme");

            return value switch
            {
                int intValue => intValue == 0,
                uint uintValue => uintValue == 0,
                string stringValue when int.TryParse(stringValue, out var parsed) => parsed == 0,
                _ => false
            };
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
