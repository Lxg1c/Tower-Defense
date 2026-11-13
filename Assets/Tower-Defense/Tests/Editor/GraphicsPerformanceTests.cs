using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.Tests
{
    public sealed class GraphicsPerformanceTests
    {
        [Test]
        public void LightingAndNormalMapsAreIndependentAndPersist()
        {
            var lightingKey = GamePerformanceSettings.LightingKey;
            var normalsKey = GamePerformanceSettings.NormalMapsKey;
            int? oldLighting = PlayerPrefs.HasKey(lightingKey) ? PlayerPrefs.GetInt(lightingKey) : null;
            int? oldNormals = PlayerPrefs.HasKey(normalsKey) ? PlayerPrefs.GetInt(normalsKey) : null;
            try
            {
                PlayerPrefs.DeleteKey(lightingKey);
                PlayerPrefs.DeleteKey(normalsKey);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.Lighting, Is.EqualTo(GamePerformanceSettings.LightingMode.Standard));
                Assert.That(GamePerformanceSettings.NormalMaps, Is.True);

                GamePerformanceSettings.SetLighting(GamePerformanceSettings.LightingMode.Simple);
                Assert.That(GamePerformanceSettings.NormalMaps, Is.True);
                GamePerformanceSettings.ToggleNormalMaps();
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.Lighting, Is.EqualTo(GamePerformanceSettings.LightingMode.Simple));
                Assert.That(GamePerformanceSettings.NormalMaps, Is.False);
            }
            finally
            {
                Restore(lightingKey, oldLighting);
                Restore(normalsKey, oldNormals);
                GamePerformanceSettings.LoadGraphicsPreferences();
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void AuthoredVariantsKeepSourceAndRemoveNormalSamplingIndependently()
        {
            var profiles = AssetDatabase.LoadAssetAtPath<GraphicsMaterialProfiles>(
                "Assets/Tower-Defense/Resources/GraphicsMaterialProfiles.asset");
            Assert.That(profiles, Is.Not.Null);
            var variant = profiles.variants.First(v => v.simple != null && v.simpleWithoutNormals != null);
            var standard = GamePerformanceSettings.LightingMode.Standard;
            var simple = GamePerformanceSettings.LightingMode.Simple;
            Assert.That(variant.Select(standard, true), Is.SameAs(variant.source));
            Assert.That(variant.Select(simple, true).shader.name, Is.EqualTo("Universal Render Pipeline/Simple Lit"));
            Assert.That(variant.Select(simple, true).IsKeywordEnabled("_NORMALMAP"), Is.True);
            foreach (var mode in new[] { standard, simple })
            {
                var noNormals = variant.Select(mode, false);
                Assert.That(noNormals.IsKeywordEnabled("_NORMALMAP"), Is.False);
                Assert.That(noNormals.GetTexture("_BumpMap"), Is.Null);
                Assert.That(noNormals.GetTexture("_BaseMap"), Is.EqualTo(variant.source.GetTexture("_BaseMap")));
            }
            Assert.That(variant.source.IsKeywordEnabled("_NORMALMAP"), Is.True);
        }

        [Test]
        public void MeasurementKeepsTheEntireRunAndItsWorstFrame()
        {
            using var measurement = new PerformanceMeasurement();
            Assert.That(measurement.HasFrames, Is.False);
            measurement.Tick(0f, 1, false, 0);
            Assert.That(measurement.HasFrames, Is.False);
            for (int i = 0; i < 6400; i++)
                measurement.Tick(.015625f, 1, false, 0);
            measurement.Tick(.5f, 2, true, 18);

            Assert.That(measurement.Elapsed, Is.EqualTo(100.5d));
            Assert.That(measurement.Report(), Does.Contain("1–2").And.Contain("18").And.Contain("500"));
            Assert.That(measurement.Summary(), Does.Contain("P95").And.Contain("15"));
        }

        private static void Restore(string key, int? oldValue)
        {
            if (oldValue.HasValue) PlayerPrefs.SetInt(key, oldValue.Value);
            else PlayerPrefs.DeleteKey(key);
        }
    }
}
