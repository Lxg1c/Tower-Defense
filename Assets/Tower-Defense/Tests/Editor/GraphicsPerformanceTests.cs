using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TowerDefense.Tests
{
    public sealed class GraphicsPerformanceTests
    {
        [Test]
        public void DesktopBuildUsesNativeResolutionAndMsaaWithoutChangingAndroidDefault()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").First();
            var settings = new SerializedObject(asset);
            var levels = settings.FindProperty("m_QualitySettings");
            int desktop = Enumerable.Range(0, levels.arraySize).Single(i =>
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Desktop");
            var pipeline = levels.GetArrayElementAtIndex(desktop).FindPropertyRelative("customRenderPipeline")
                .objectReferenceValue;
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(pipeline.name, Is.EqualTo("PC_RPAsset"));
            Assert.That(new SerializedObject(pipeline).FindProperty("m_RenderScale").floatValue, Is.EqualTo(1f));
            Assert.That(new SerializedObject(pipeline).FindProperty("m_MSAA").intValue, Is.EqualTo(4));
            var defaults = settings.FindProperty("m_PerPlatformDefaultQuality");
            int DefaultFor(string platform) => Enumerable.Range(0, defaults.arraySize)
                .Select(i => defaults.GetArrayElementAtIndex(i))
                .Single(p => p.FindPropertyRelative("first").stringValue == platform)
                .FindPropertyRelative("second").intValue;
            Assert.That(DefaultFor("Standalone"), Is.EqualTo(desktop));
            int android = DefaultFor("Android");
            Assert.That(levels.GetArrayElementAtIndex(android).FindPropertyRelative("name").stringValue,
                Is.EqualTo("Mobile"));
        }

        [Test]
        public void UltimateEffectPreservesMobileAlphaAndAdditiveGlow()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Tower-Defense/Prefabs/Effects/UltimateExplosion.prefab");
            var renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            foreach (var renderer in renderers)
            foreach (var material in renderer.sharedMaterials)
            {
                Assert.That(material.mainTexture, Is.Not.Null, material.name);
                var importer = (TextureImporter)AssetImporter.GetAtPath(
                    AssetDatabase.GetAssetPath(material.mainTexture));
                var android = importer.GetPlatformTextureSettings("Android");
                Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True, material.name);
                Assert.That(android.overridden, Is.True, material.name);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ETC2_RGBA8), material.name);
                Assert.That(android.allowsAlphaSplitting, Is.False, material.name);
                Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f), material.name);
                Assert.That(material.GetFloat("_ZWrite"), Is.Zero, material.name);
                Assert.That(material.GetFloat("_SoftParticlesEnabled"), Is.Zero, material.name);
                if (material.name.EndsWith("_ADD"))
                    Assert.That(material.GetFloat("_DstBlend"),
                        Is.EqualTo((float)UnityEngine.Rendering.BlendMode.One), material.name);
            }
        }
        [Test]
        public void DesktopWindowModePersistsAndDefaultsToBorderless()
        {
            const string key = GamePerformanceSettings.WindowModeKey;
            int? saved = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
            try
            {
                PlayerPrefs.DeleteKey(key);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.DesktopWindowMode,
                    Is.EqualTo(GamePerformanceSettings.WindowMode.Borderless));
                foreach (var mode in new[] { GamePerformanceSettings.WindowMode.Fullscreen,
                    GamePerformanceSettings.WindowMode.Windowed, GamePerformanceSettings.WindowMode.Borderless })
                {
                    GamePerformanceSettings.SetWindowMode(mode);
                    GamePerformanceSettings.LoadGraphicsPreferences();
                    Assert.That(GamePerformanceSettings.DesktopWindowMode, Is.EqualTo(mode));
                }
                Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                    GamePerformanceSettings.SetWindowMode((GamePerformanceSettings.WindowMode)99));
            }
            finally
            {
                Restore(key, saved);
                GamePerformanceSettings.LoadGraphicsPreferences();
                PlayerPrefs.Save();
            }
        }

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
        public void AndroidRenderScalePersistsIndependentlyOfQuality()
        {
            const string qualityKey = GamePerformanceSettings.AndroidQualityKey;
            const string scaleKey = GamePerformanceSettings.AndroidRenderScaleKey;
            int? oldQuality = PlayerPrefs.HasKey(qualityKey) ? PlayerPrefs.GetInt(qualityKey) : null;
            int? oldScale = PlayerPrefs.HasKey(scaleKey) ? PlayerPrefs.GetInt(scaleKey) : null;
            try
            {
                PlayerPrefs.DeleteKey(scaleKey);
                GamePerformanceSettings.SetAndroidQuality(GamePerformanceSettings.AndroidQuality.Low);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.RenderScalePercent, Is.EqualTo(80));

                GamePerformanceSettings.SetRenderScalePercent(67);
                GamePerformanceSettings.SetAndroidQuality(GamePerformanceSettings.AndroidQuality.Balanced);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.MobileQuality, Is.EqualTo(GamePerformanceSettings.AndroidQuality.Balanced));
                Assert.That(GamePerformanceSettings.RenderScalePercent, Is.EqualTo(67));
            }
            finally
            {
                Restore(qualityKey, oldQuality);
                Restore(scaleKey, oldScale);
                GamePerformanceSettings.LoadGraphicsPreferences();
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void AmbientOcclusionChoicePersistsAcrossQualityChanges()
        {
            const string key = GamePerformanceSettings.AmbientOcclusionKey;
            const string qualityKey = GamePerformanceSettings.AndroidQualityKey;
            int? original = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
            int? originalQuality = PlayerPrefs.HasKey(qualityKey) ? PlayerPrefs.GetInt(qualityKey) : null;
            try
            {
                PlayerPrefs.DeleteKey(key);
                GamePerformanceSettings.SetAndroidQuality(GamePerformanceSettings.AndroidQuality.High);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.AmbientOcclusion,
                    Is.EqualTo(GamePerformanceSettings.SupportsAndroidQuality));

                GamePerformanceSettings.SetAmbientOcclusion(false);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.AmbientOcclusion, Is.False);

                GamePerformanceSettings.SetAmbientOcclusion(true);
                GamePerformanceSettings.SetAndroidQuality(GamePerformanceSettings.AndroidQuality.Low);
                GamePerformanceSettings.LoadGraphicsPreferences();
                Assert.That(GamePerformanceSettings.AmbientOcclusion, Is.True);
            }
            finally
            {
                Restore(key, original);
                Restore(qualityKey, originalQuality);
                GamePerformanceSettings.LoadGraphicsPreferences();
                PlayerPrefs.Save();
            }
        }

        [TestCase("Mobile_RPAsset", true)]
        [TestCase("Mobile_RPAsset_Balanced", false)]
        [TestCase("Mobile_RPAsset_Low", false)]
        [TestCase("PC_RPAsset", false)]
        public void GraphicsProfilesProvideBothAmbientOcclusionRenderers(string asset, bool defaultEnabled)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"Assets/Tower-Defense/Settings/{asset}.asset");

            Assert.That(GamePerformanceSettings.FindAmbientOcclusionRenderer(pipeline, defaultEnabled), Is.Zero);
            Assert.That(GamePerformanceSettings.FindAmbientOcclusionRenderer(pipeline, !defaultEnabled), Is.EqualTo(1));
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
