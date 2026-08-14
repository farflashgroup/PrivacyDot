using Microsoft.Win32;
using System.Globalization;
using System.Security;

namespace PrivacyDot;

internal enum AppText
{
    AppName,
    StatusMicrophoneAndCameraInUse,
    StatusMicrophoneInUse,
    StatusCameraInUse,
    StatusNoDeviceUse,
    ToolTipFormat,
    Microphone,
    Camera,
    NoAppsUsingDevice,
    CoreAudio,
    WindowsPrivacy,
    UnknownApp,
    Refresh,
    OpenMicrophonePrivacySettings,
    OpenCameraPrivacySettings,
    StartWithWindows,
    Settings,
    Language,
    SystemDefault,
    CheckForUpdates,
    CheckingForUpdates,
    DownloadingUpdate,
    UpdateAvailableFormat,
    UpdateAvailableNoteFormat,
    UpToDateFormat,
    UpdateFailedFormat,
    Exit,
    StartupSettingFailed,
    PrivacySettingsOpenFailed
}

internal sealed class LanguageOption
{
    public LanguageOption(string cultureName, string nativeName)
    {
        CultureName = cultureName;
        NativeName = nativeName;
    }

    public string CultureName { get; }

    public string NativeName { get; }
}

internal static class Localization
{
    private const string SettingsKeyPath = @"Software\PrivacyDot";
    private const string LanguageValueName = "Language";
    private const string EnglishCultureName = "en";
    private const string MandarinCultureName = "zh-Hans";

    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;

    private static readonly LanguageOption[] LanguageOptions =
    {
        new(EnglishCultureName, "English"),
        new("fr", "Français"),
        new("es", "Español"),
        new("ja", "日本語"),
        new(MandarinCultureName, "中文（简体）"),
        new("hi", "हिन्दी"),
        new("ar", "العربية"),
        new("ru", "Русский")
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<AppText, string>> Translations =
        new Dictionary<string, IReadOnlyDictionary<AppText, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [EnglishCultureName] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "Microphone and camera in use",
                [AppText.StatusMicrophoneInUse] = "Microphone in use",
                [AppText.StatusCameraInUse] = "Camera in use",
                [AppText.StatusNoDeviceUse] = "No microphone or camera use detected",
                [AppText.ToolTipFormat] = "PrivacyDot: {0}",
                [AppText.Microphone] = "Microphone",
                [AppText.Camera] = "Camera",
                [AppText.NoAppsUsingDevice] = "No apps currently using this device.",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Windows Privacy",
                [AppText.UnknownApp] = "Unknown app",
                [AppText.Refresh] = "Refresh",
                [AppText.OpenMicrophonePrivacySettings] = "Open microphone privacy settings",
                [AppText.OpenCameraPrivacySettings] = "Open camera privacy settings",
                [AppText.StartWithWindows] = "Start with Windows",
                [AppText.Settings] = "Settings",
                [AppText.Language] = "Language",
                [AppText.SystemDefault] = "System default",
                [AppText.CheckForUpdates] = "Check for updates",
                [AppText.CheckingForUpdates] = "Checking for updates...",
                [AppText.DownloadingUpdate] = "Downloading update...",
                [AppText.UpdateAvailableFormat] = "PrivacyDot {0} is available. You have {1}.\n\nDownload and install it now?",
                [AppText.UpdateAvailableNoteFormat] = "Update available: PrivacyDot {0}",
                [AppText.UpToDateFormat] = "You’re using the latest version ({0}).",
                [AppText.UpdateFailedFormat] = "PrivacyDot couldn’t complete the update.\n\n{0}",
                [AppText.Exit] = "Exit",
                [AppText.StartupSettingFailed] = "PrivacyDot startup setting failed",
                [AppText.PrivacySettingsOpenFailed] = "Privacy settings failed to open"
            },
            ["fr"] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "Microphone et caméra en cours d’utilisation",
                [AppText.StatusMicrophoneInUse] = "Microphone en cours d’utilisation",
                [AppText.StatusCameraInUse] = "Caméra en cours d’utilisation",
                [AppText.StatusNoDeviceUse] = "Aucune utilisation du microphone ou de la caméra détectée",
                [AppText.ToolTipFormat] = "PrivacyDot : {0}",
                [AppText.Microphone] = "Microphone",
                [AppText.Camera] = "Caméra",
                [AppText.NoAppsUsingDevice] = "Aucune application n’utilise actuellement cet appareil.",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Confidentialité Windows",
                [AppText.UnknownApp] = "Application inconnue",
                [AppText.Refresh] = "Actualiser",
                [AppText.OpenMicrophonePrivacySettings] = "Ouvrir les paramètres de confidentialité du microphone",
                [AppText.OpenCameraPrivacySettings] = "Ouvrir les paramètres de confidentialité de la caméra",
                [AppText.StartWithWindows] = "Démarrer avec Windows",
                [AppText.Settings] = "Paramètres",
                [AppText.Language] = "Langue",
                [AppText.SystemDefault] = "Langue du système",
                [AppText.CheckForUpdates] = "Rechercher des mises à jour",
                [AppText.CheckingForUpdates] = "Recherche de mises à jour…",
                [AppText.DownloadingUpdate] = "Téléchargement de la mise à jour…",
                [AppText.UpdateAvailableFormat] = "PrivacyDot {0} est disponible. Vous utilisez la version {1}.\n\nVoulez-vous la télécharger et l’installer maintenant ?",
                [AppText.UpdateAvailableNoteFormat] = "Mise à jour disponible : PrivacyDot {0}",
                [AppText.UpToDateFormat] = "Vous utilisez la dernière version ({0}).",
                [AppText.UpdateFailedFormat] = "PrivacyDot n’a pas pu terminer la mise à jour.\n\n{0}",
                [AppText.Exit] = "Quitter",
                [AppText.StartupSettingFailed] = "Échec du réglage du démarrage de PrivacyDot",
                [AppText.PrivacySettingsOpenFailed] = "Impossible d’ouvrir les paramètres de confidentialité"
            },
            ["es"] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "Micrófono y cámara en uso",
                [AppText.StatusMicrophoneInUse] = "Micrófono en uso",
                [AppText.StatusCameraInUse] = "Cámara en uso",
                [AppText.StatusNoDeviceUse] = "No se detectó el uso del micrófono ni de la cámara",
                [AppText.ToolTipFormat] = "PrivacyDot: {0}",
                [AppText.Microphone] = "Micrófono",
                [AppText.Camera] = "Cámara",
                [AppText.NoAppsUsingDevice] = "Ninguna aplicación está usando este dispositivo.",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Privacidad de Windows",
                [AppText.UnknownApp] = "Aplicación desconocida",
                [AppText.Refresh] = "Actualizar",
                [AppText.OpenMicrophonePrivacySettings] = "Abrir la configuración de privacidad del micrófono",
                [AppText.OpenCameraPrivacySettings] = "Abrir la configuración de privacidad de la cámara",
                [AppText.StartWithWindows] = "Iniciar con Windows",
                [AppText.Settings] = "Configuración",
                [AppText.Language] = "Idioma",
                [AppText.SystemDefault] = "Idioma del sistema",
                [AppText.CheckForUpdates] = "Buscar actualizaciones",
                [AppText.CheckingForUpdates] = "Buscando actualizaciones…",
                [AppText.DownloadingUpdate] = "Descargando actualización…",
                [AppText.UpdateAvailableFormat] = "PrivacyDot {0} está disponible. Tienes la versión {1}.\n\n¿Quieres descargarla e instalarla ahora?",
                [AppText.UpdateAvailableNoteFormat] = "Actualización disponible: PrivacyDot {0}",
                [AppText.UpToDateFormat] = "Estás usando la versión más reciente ({0}).",
                [AppText.UpdateFailedFormat] = "PrivacyDot no pudo completar la actualización.\n\n{0}",
                [AppText.Exit] = "Salir",
                [AppText.StartupSettingFailed] = "No se pudo configurar el inicio de PrivacyDot",
                [AppText.PrivacySettingsOpenFailed] = "No se pudo abrir la configuración de privacidad"
            },
            ["ja"] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "マイクとカメラを使用中",
                [AppText.StatusMicrophoneInUse] = "マイクを使用中",
                [AppText.StatusCameraInUse] = "カメラを使用中",
                [AppText.StatusNoDeviceUse] = "マイクまたはカメラの使用は検出されていません",
                [AppText.ToolTipFormat] = "PrivacyDot: {0}",
                [AppText.Microphone] = "マイク",
                [AppText.Camera] = "カメラ",
                [AppText.NoAppsUsingDevice] = "現在このデバイスを使用しているアプリはありません。",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Windows プライバシー",
                [AppText.UnknownApp] = "不明なアプリ",
                [AppText.Refresh] = "更新",
                [AppText.OpenMicrophonePrivacySettings] = "マイクのプライバシー設定を開く",
                [AppText.OpenCameraPrivacySettings] = "カメラのプライバシー設定を開く",
                [AppText.StartWithWindows] = "Windows の起動時に開始",
                [AppText.Settings] = "設定",
                [AppText.Language] = "言語",
                [AppText.SystemDefault] = "システムの既定",
                [AppText.CheckForUpdates] = "更新を確認",
                [AppText.CheckingForUpdates] = "更新を確認しています…",
                [AppText.DownloadingUpdate] = "更新をダウンロードしています…",
                [AppText.UpdateAvailableFormat] = "PrivacyDot {0} を利用できます。現在のバージョンは {1} です。\n\n今すぐダウンロードしてインストールしますか？",
                [AppText.UpdateAvailableNoteFormat] = "PrivacyDot {0} の更新があります",
                [AppText.UpToDateFormat] = "最新バージョン（{0}）を使用しています。",
                [AppText.UpdateFailedFormat] = "PrivacyDot の更新を完了できませんでした。\n\n{0}",
                [AppText.Exit] = "終了",
                [AppText.StartupSettingFailed] = "PrivacyDot の起動設定に失敗しました",
                [AppText.PrivacySettingsOpenFailed] = "プライバシー設定を開けませんでした"
            },
            [MandarinCultureName] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "麦克风和摄像头正在使用",
                [AppText.StatusMicrophoneInUse] = "麦克风正在使用",
                [AppText.StatusCameraInUse] = "摄像头正在使用",
                [AppText.StatusNoDeviceUse] = "未检测到麦克风或摄像头使用",
                [AppText.ToolTipFormat] = "PrivacyDot：{0}",
                [AppText.Microphone] = "麦克风",
                [AppText.Camera] = "摄像头",
                [AppText.NoAppsUsingDevice] = "当前没有应用正在使用此设备。",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Windows 隐私",
                [AppText.UnknownApp] = "未知应用",
                [AppText.Refresh] = "刷新",
                [AppText.OpenMicrophonePrivacySettings] = "打开麦克风隐私设置",
                [AppText.OpenCameraPrivacySettings] = "打开摄像头隐私设置",
                [AppText.StartWithWindows] = "随 Windows 启动",
                [AppText.Settings] = "设置",
                [AppText.Language] = "语言",
                [AppText.SystemDefault] = "跟随系统",
                [AppText.CheckForUpdates] = "检查更新",
                [AppText.CheckingForUpdates] = "正在检查更新…",
                [AppText.DownloadingUpdate] = "正在下载更新…",
                [AppText.UpdateAvailableFormat] = "PrivacyDot {0} 已发布。当前版本为 {1}。\n\n是否立即下载并安装？",
                [AppText.UpdateAvailableNoteFormat] = "PrivacyDot {0} 更新可用",
                [AppText.UpToDateFormat] = "你正在使用最新版本（{0}）。",
                [AppText.UpdateFailedFormat] = "PrivacyDot 无法完成更新。\n\n{0}",
                [AppText.Exit] = "退出",
                [AppText.StartupSettingFailed] = "无法更改 PrivacyDot 启动设置",
                [AppText.PrivacySettingsOpenFailed] = "无法打开隐私设置"
            },
            ["hi"] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "माइक्रोफ़ोन और कैमरा उपयोग में हैं",
                [AppText.StatusMicrophoneInUse] = "माइक्रोफ़ोन उपयोग में है",
                [AppText.StatusCameraInUse] = "कैमरा उपयोग में है",
                [AppText.StatusNoDeviceUse] = "माइक्रोफ़ोन या कैमरे का उपयोग नहीं मिला",
                [AppText.ToolTipFormat] = "PrivacyDot: {0}",
                [AppText.Microphone] = "माइक्रोफ़ोन",
                [AppText.Camera] = "कैमरा",
                [AppText.NoAppsUsingDevice] = "फ़िलहाल कोई ऐप इस डिवाइस का उपयोग नहीं कर रहा है।",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Windows गोपनीयता",
                [AppText.UnknownApp] = "अज्ञात ऐप",
                [AppText.Refresh] = "रीफ़्रेश करें",
                [AppText.OpenMicrophonePrivacySettings] = "माइक्रोफ़ोन गोपनीयता सेटिंग खोलें",
                [AppText.OpenCameraPrivacySettings] = "कैमरा गोपनीयता सेटिंग खोलें",
                [AppText.StartWithWindows] = "Windows के साथ शुरू करें",
                [AppText.Settings] = "सेटिंग्स",
                [AppText.Language] = "भाषा",
                [AppText.SystemDefault] = "सिस्टम डिफ़ॉल्ट",
                [AppText.CheckForUpdates] = "अपडेट की जाँच करें",
                [AppText.CheckingForUpdates] = "अपडेट की जाँच हो रही है…",
                [AppText.DownloadingUpdate] = "अपडेट डाउनलोड हो रहा है…",
                [AppText.UpdateAvailableFormat] = "PrivacyDot {0} उपलब्ध है। आपके पास {1} है।\n\nक्या इसे अभी डाउनलोड और इंस्टॉल करना है?",
                [AppText.UpdateAvailableNoteFormat] = "PrivacyDot {0} अपडेट उपलब्ध है",
                [AppText.UpToDateFormat] = "आप नवीनतम संस्करण ({0}) का उपयोग कर रहे हैं।",
                [AppText.UpdateFailedFormat] = "PrivacyDot अपडेट पूरा नहीं कर सका।\n\n{0}",
                [AppText.Exit] = "बाहर निकलें",
                [AppText.StartupSettingFailed] = "PrivacyDot की स्टार्टअप सेटिंग बदल नहीं सकी",
                [AppText.PrivacySettingsOpenFailed] = "गोपनीयता सेटिंग नहीं खुल सकीं"
            },
            ["ar"] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "الميكروفون والكاميرا قيد الاستخدام",
                [AppText.StatusMicrophoneInUse] = "الميكروفون قيد الاستخدام",
                [AppText.StatusCameraInUse] = "الكاميرا قيد الاستخدام",
                [AppText.StatusNoDeviceUse] = "لم يتم اكتشاف استخدام الميكروفون أو الكاميرا",
                [AppText.ToolTipFormat] = "PrivacyDot: {0}",
                [AppText.Microphone] = "الميكروفون",
                [AppText.Camera] = "الكاميرا",
                [AppText.NoAppsUsingDevice] = "لا توجد تطبيقات تستخدم هذا الجهاز حالياً.",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "خصوصية Windows",
                [AppText.UnknownApp] = "تطبيق غير معروف",
                [AppText.Refresh] = "تحديث",
                [AppText.OpenMicrophonePrivacySettings] = "فتح إعدادات خصوصية الميكروفون",
                [AppText.OpenCameraPrivacySettings] = "فتح إعدادات خصوصية الكاميرا",
                [AppText.StartWithWindows] = "بدء التشغيل مع Windows",
                [AppText.Settings] = "الإعدادات",
                [AppText.Language] = "اللغة",
                [AppText.SystemDefault] = "إعداد النظام الافتراضي",
                [AppText.CheckForUpdates] = "التحقق من وجود تحديثات",
                [AppText.CheckingForUpdates] = "جارٍ التحقق من وجود تحديثات…",
                [AppText.DownloadingUpdate] = "جارٍ تنزيل التحديث…",
                [AppText.UpdateAvailableFormat] = "يتوفر PrivacyDot {0}. لديك الإصدار {1}.\n\nهل تريد تنزيله وتثبيته الآن؟",
                [AppText.UpdateAvailableNoteFormat] = "يتوفر تحديث PrivacyDot {0}",
                [AppText.UpToDateFormat] = "أنت تستخدم أحدث إصدار ({0}).",
                [AppText.UpdateFailedFormat] = "تعذر على PrivacyDot إكمال التحديث.\n\n{0}",
                [AppText.Exit] = "خروج",
                [AppText.StartupSettingFailed] = "تعذر تغيير إعداد بدء تشغيل PrivacyDot",
                [AppText.PrivacySettingsOpenFailed] = "تعذر فتح إعدادات الخصوصية"
            },
            ["ru"] = new Dictionary<AppText, string>
            {
                [AppText.AppName] = "PrivacyDot",
                [AppText.StatusMicrophoneAndCameraInUse] = "Микрофон и камера используются",
                [AppText.StatusMicrophoneInUse] = "Микрофон используется",
                [AppText.StatusCameraInUse] = "Камера используется",
                [AppText.StatusNoDeviceUse] = "Использование микрофона или камеры не обнаружено",
                [AppText.ToolTipFormat] = "PrivacyDot: {0}",
                [AppText.Microphone] = "Микрофон",
                [AppText.Camera] = "Камера",
                [AppText.NoAppsUsingDevice] = "Сейчас это устройство не использует ни одно приложение.",
                [AppText.CoreAudio] = "Core Audio",
                [AppText.WindowsPrivacy] = "Конфиденциальность Windows",
                [AppText.UnknownApp] = "Неизвестное приложение",
                [AppText.Refresh] = "Обновить",
                [AppText.OpenMicrophonePrivacySettings] = "Открыть параметры конфиденциальности микрофона",
                [AppText.OpenCameraPrivacySettings] = "Открыть параметры конфиденциальности камеры",
                [AppText.StartWithWindows] = "Запускать вместе с Windows",
                [AppText.Settings] = "Настройки",
                [AppText.Language] = "Язык",
                [AppText.SystemDefault] = "Язык системы",
                [AppText.CheckForUpdates] = "Проверить обновления",
                [AppText.CheckingForUpdates] = "Проверка обновлений…",
                [AppText.DownloadingUpdate] = "Загрузка обновления…",
                [AppText.UpdateAvailableFormat] = "Доступна версия PrivacyDot {0}. У вас установлена версия {1}.\n\nСкачать и установить обновление сейчас?",
                [AppText.UpdateAvailableNoteFormat] = "Доступно обновление PrivacyDot {0}",
                [AppText.UpToDateFormat] = "Вы используете последнюю версию ({0}).",
                [AppText.UpdateFailedFormat] = "PrivacyDot не удалось завершить обновление.\n\n{0}",
                [AppText.Exit] = "Выход",
                [AppText.StartupSettingFailed] = "Не удалось изменить настройку запуска PrivacyDot",
                [AppText.PrivacySettingsOpenFailed] = "Не удалось открыть параметры конфиденциальности"
            }
        };

    private static string _translationCultureName = EnglishCultureName;

    static Localization()
    {
        ValidateTranslations();
    }

    public static IReadOnlyList<LanguageOption> SupportedLanguages => LanguageOptions;

    public static string? SelectedCultureName { get; private set; }

    public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo(EnglishCultureName);

    public static bool IsRightToLeft => CurrentCulture.TextInfo.IsRightToLeft;

    public static void Initialize()
    {
        var savedCultureName = ReadSavedCultureName();
        SetLanguage(IsSelectableCulture(savedCultureName) ? savedCultureName : null, persist: false);
    }

    public static void SetLanguage(string? cultureName, bool persist = true)
    {
        if (!string.IsNullOrWhiteSpace(cultureName) && !IsSelectableCulture(cultureName))
        {
            throw new ArgumentException($"Unsupported UI culture: {cultureName}", nameof(cultureName));
        }

        SelectedCultureName = string.IsNullOrWhiteSpace(cultureName) ? null : cultureName;
        CurrentCulture = SelectedCultureName is null
            ? SystemUiCulture
            : CultureInfo.GetCultureInfo(SelectedCultureName);
        _translationCultureName = ResolveSupportedCultureName(CurrentCulture);

        CultureInfo.CurrentUICulture = CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;

        if (persist)
        {
            SaveCultureName(SelectedCultureName);
        }
    }

    public static string Get(AppText text)
    {
        return GetForCulture(text, _translationCultureName);
    }

    public static string Format(AppText text, params object[] arguments)
    {
        return string.Format(CurrentCulture, Get(text), arguments);
    }

    internal static string GetForCulture(AppText text, string cultureName)
    {
        var resolvedCultureName = ResolveSupportedCultureName(CultureInfo.GetCultureInfo(cultureName));

        if (Translations.TryGetValue(resolvedCultureName, out var localized)
            && localized.TryGetValue(text, out var value))
        {
            return value;
        }

        return Translations[EnglishCultureName][text];
    }

    internal static string ResolveSupportedCultureName(CultureInfo culture)
    {
        if (culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return MandarinCultureName;
        }

        var languageName = culture.TwoLetterISOLanguageName;
        return Translations.ContainsKey(languageName) ? languageName : EnglishCultureName;
    }

    private static bool IsSelectableCulture(string? cultureName)
    {
        return !string.IsNullOrWhiteSpace(cultureName)
            && LanguageOptions.Any(option =>
                string.Equals(option.CultureName, cultureName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadSavedCultureName()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
            return key?.GetValue(LanguageValueName) as string;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void SaveCultureName(string? cultureName)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);

            if (cultureName is null)
            {
                key?.DeleteValue(LanguageValueName, throwOnMissingValue: false);
            }
            else
            {
                key?.SetValue(LanguageValueName, cultureName, RegistryValueKind.String);
            }
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
        {
            // The selected language still applies for this session when settings cannot be saved.
        }
    }

    private static void ValidateTranslations()
    {
        foreach (var language in LanguageOptions)
        {
            if (!Translations.TryGetValue(language.CultureName, out var localized))
            {
                throw new InvalidOperationException($"Translations are missing for {language.CultureName}.");
            }

            foreach (AppText text in Enum.GetValues(typeof(AppText)))
            {
                if (!localized.TryGetValue(text, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        $"Translation {text} is missing for {language.CultureName}.");
                }
            }
        }
    }
}
