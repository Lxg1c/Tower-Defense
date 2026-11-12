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
    private const string PreferenceKey = "TowerDefense.Language";
    private const string TextPrefix = "l10n-";
    private const string TooltipPrefix = "l10n-tip-";
    private static StringTable table;
    private static int tableRequest;

    public static event Action Changed;
    public static bool IsReady => table != null;
    public static string Language => table?.LocaleIdentifier.Code ?? "ru";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        tableRequest++;
        table = null;
        if (!LocalizationSettings.HasSettings) return;
        LocalizationSettings.SelectedLocaleChanged -= LoadTable;
        LocalizationSettings.SelectedLocaleChanged += LoadTable;
        var initialization = LocalizationSettings.InitializationOperation;
        if (initialization.IsDone) OnInitialized(initialization);
        else initialization.Completed += OnInitialized;
    }

    private static void OnInitialized(AsyncOperationHandle<LocalizationSettings> operation)
    {
        if (operation.Status == AsyncOperationStatus.Succeeded)
            LoadTable(LocalizationSettings.SelectedLocale);
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

    public static string Text(string key, string fallback)
    {
        var entry = table?.GetEntry(key);
        return entry != null && !string.IsNullOrEmpty(entry.LocalizedValue) ? entry.LocalizedValue : fallback;
    }

    public static string Format(string key, string fallback, params object[] args) =>
        string.Format(table?.LocaleIdentifier.CultureInfo ?? CultureInfo.InvariantCulture, Text(key, fallback), args);

    public static void ApplyTo(VisualElement root)
    {
        foreach (var marker in root.GetClasses())
        {
            if (marker.StartsWith(TooltipPrefix, StringComparison.Ordinal))
                root.tooltip = Text(marker.Substring(TooltipPrefix.Length), root.tooltip);
            else if (root is TextElement label && marker.StartsWith(TextPrefix, StringComparison.Ordinal))
                label.text = Text(marker.Substring(TextPrefix.Length), label.text);
        }

        foreach (var child in root.Children())
            ApplyTo(child);
    }
}
