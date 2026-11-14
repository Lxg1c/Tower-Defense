using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace TowerDefense.Tests
{
    public sealed class GameLocalizationTests
    {
        private const string Directory = "Assets/Tower-Defense/Localization";

        [TestCase("ru")]
        [TestCase("en")]
        public void EveryCodeKeyHasATranslation(string language)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>($"{Directory}/Game UI_{language}.asset");
            Assert.That(table, Is.Not.Null);
            foreach (var field in typeof(LocalizationKeys).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                var key = (string)field.GetRawConstantValue();
                Assert.That(table.GetEntry(key)?.LocalizedValue, Is.Not.Null.And.Not.Empty, $"{language}: {key}");
            }
        }

        [Test]
        public void FallbackUsesItsOwnLocaleForTheTemplate()
        {
            var english = AssetDatabase.LoadAssetAtPath<StringTable>($"{Directory}/Game UI_en.asset");
            var russian = AssetDatabase.LoadAssetAtPath<StringTable>($"{Directory}/Game UI_ru.asset");
            var empty = ScriptableObject.CreateInstance<StringTable>();
            empty.SharedData = english.SharedData;
            empty.LocaleIdentifier = russian.LocaleIdentifier;
            try
            {
                string key = LocalizationKeys.WaveNumber;
                Assert.That(GameLocalization.Resolve(key, russian, english, out var culture),
                    Is.EqualTo(russian.GetEntry(key).LocalizedValue));
                Assert.That(culture.TwoLetterISOLanguageName, Is.EqualTo("ru"));
                string template = GameLocalization.Resolve(key, empty, english, out culture);
                Assert.That(string.Format(culture, template, 2), Is.EqualTo("WAVE 02"));
                Assert.That(culture.TwoLetterISOLanguageName, Is.EqualTo("en"));
                Assert.That(GameLocalization.Resolve(key, null, null, out culture), Is.EqualTo(key));
                Assert.That(culture, Is.EqualTo(CultureInfo.InvariantCulture));
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }
    }
}
