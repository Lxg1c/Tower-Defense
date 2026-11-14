using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

/// <summary>Loads the active Unity Localization string table and refreshes Toolkit views.</summary>
public static class GameLocalization
{
    private const string TableName = "Game UI";
    private const string FallbackLanguage = "ru";
    private const string PreferenceKey = "TowerDefense.Language";
    private const string TextPrefix = "l10n-";
    private const string TooltipPrefix = "l10n-tip-";
    private static StringTable table;
    private static StringTable fallbackTable;
    private static int tableRequest;
#if UNITY_EDITOR
    private static StringTable editorTable;
#endif

    public static event Action Changed;
    public static bool IsReady => ActiveTable != null || FallbackTable != null;
    public static string Language => ActiveTable?.LocaleIdentifier.Code ?? FallbackLanguage;
    private static StringTable ActiveTable => Application.isPlaying ? table : null;

    private static StringTable FallbackTable
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (editorTable == null)
                    editorTable = UnityEditor.AssetDatabase.LoadAssetAtPath<StringTable>(
                        $"Assets/Tower-Defense/Localization/{TableName}_{FallbackLanguage}.asset");
                return editorTable;
            }
#endif
            return fallbackTable;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        tableRequest++;
        table = null;
        fallbackTable = null;
        if (!LocalizationSettings.HasSettings) return;
        LocalizationSettings.SelectedLocaleChanged -= LoadTable;
        LocalizationSettings.SelectedLocaleChanged += LoadTable;
        var initialization = LocalizationSettings.InitializationOperation;
        if (initialization.IsDone) OnInitialized(initialization);
        else initialization.Completed += OnInitialized;
    }

    private static void OnInitialized(AsyncOperationHandle<LocalizationSettings> operation)
    {
        if (operation.Status != AsyncOperationStatus.Succeeded) return;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(FallbackLanguage));
        if (locale != null)
        {
            var load = LocalizationSettings.StringDatabase.GetTableAsync(TableName, locale);
            if (load.IsDone) UseFallback(load);
            else load.Completed += UseFallback;
        }
        LoadTable(LocalizationSettings.SelectedLocale);

        void UseFallback(AsyncOperationHandle<StringTable> result)
        {
            if (result.Status != AsyncOperationStatus.Succeeded) return;
            fallbackTable = result.Result;
            if (table != fallbackTable) Changed?.Invoke();
        }
    }

    private static void LoadTable(Locale locale)
    {
        int request = ++tableRequest;
        if (locale == null || table?.LocaleIdentifier == locale.Identifier) return;
        var load = LocalizationSettings.StringDatabase.GetTableAsync(TableName, locale);
        if (load.IsDone) UseTable(load);
        else load.Completed += UseTable;

        void UseTable(AsyncOperationHandle<StringTable> operation)
        {
            if (request != tableRequest || operation.Status != AsyncOperationStatus.Succeeded) return;
            table = operation.Result;
            Changed?.Invoke();
        }
    }

    public static void SelectLanguage(string code)
    {
        if (!IsReady) return;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        if (locale == null) return;
        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString(PreferenceKey, code);
        PlayerPrefs.Save();
    }

    public static string Text(string key)
    {
        return Resolve(key, ActiveTable, FallbackTable, out _);
    }

    public static string Format(string key, params object[] args)
    {
        string template = Resolve(key, ActiveTable, FallbackTable, out var culture);
        return string.Format(culture, template, args);
    }

    internal static string Resolve(string key, StringTable active, StringTable fallback, out CultureInfo culture)
    {
        var source = active;
        var value = source?.GetEntry(key)?.LocalizedValue;
        if (string.IsNullOrEmpty(value) && source != fallback)
        {
            source = fallback;
            value = source?.GetEntry(key)?.LocalizedValue;
        }
        culture = source?.LocaleIdentifier.CultureInfo ?? CultureInfo.InvariantCulture;
        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static void ApplyTo(VisualElement root)
    {
        if (!IsReady) return;
        Apply(root);
    }

    private static void Apply(VisualElement root)
    {
        foreach (var marker in root.GetClasses())
        {
            if (marker.StartsWith(TooltipPrefix, StringComparison.Ordinal))
                root.tooltip = Text(marker.Substring(TooltipPrefix.Length));
            else if (root is TextElement label && marker.StartsWith(TextPrefix, StringComparison.Ordinal))
                label.text = Text(marker.Substring(TextPrefix.Length));
        }

        foreach (var child in root.Children())
            Apply(child);
    }
}
