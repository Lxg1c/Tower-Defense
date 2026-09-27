using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class FpsDisplayTests
    {
        [Test]
        public void SettingsButtonTogglesCounterWithoutDuplicatingControls()
        {
            bool visible = GamePerformanceSettings.ShowFps;
            bool hadPreference = PlayerPrefs.HasKey(GamePerformanceSettings.ShowFpsKey);
            int preference = PlayerPrefs.GetInt(GamePerformanceSettings.ShowFpsKey);
            var root = new GameObject("FPS UI test", typeof(RectTransform), typeof(Canvas));
            try
            {
                var panel = new GameObject("Settings", typeof(RectTransform));
                panel.transform.SetParent(root.transform, false);
                new GameObject("Back", typeof(RectTransform)).transform.SetParent(panel.transform, false);
                FpsInterface.AddSettingsButton((RectTransform)panel.transform, TMP_Settings.defaultFontAsset);
                FpsInterface.AddSettingsButton((RectTransform)panel.transform, TMP_Settings.defaultFontAsset);
                FpsInterface.EnsureOverlay(root.GetComponent<Canvas>(), TMP_Settings.defaultFontAsset);
                FpsInterface.EnsureOverlay(root.GetComponent<Canvas>(), TMP_Settings.defaultFontAsset);
                Assert.That(panel.GetComponentsInChildren<FpsSettingsButton>(true).Length, Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<FpsDisplay>(true).Length, Is.EqualTo(1));
                var button = panel.GetComponentInChildren<UnityEngine.UI.Button>();
                button.onClick.Invoke();
                Assert.That(GamePerformanceSettings.ShowFps, Is.EqualTo(!visible));
                Assert.That(PlayerPrefs.GetInt(GamePerformanceSettings.ShowFpsKey), Is.EqualTo(visible ? 0 : 1));
                var counter = root.transform.Find("FpsOverlay/FpsLabel").GetComponent<TMP_Text>();
                Assert.That(counter.enabled, Is.EqualTo(!visible));
                Assert.That(counter.raycastTarget, Is.False);
                panel.SetActive(false);
                panel.SetActive(true);
                button.onClick.Invoke();
                Assert.That(GamePerformanceSettings.ShowFps, Is.EqualTo(visible), "Reopening must not duplicate click listeners.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (GamePerformanceSettings.ShowFps != visible) GamePerformanceSettings.ToggleFps();
                if (hadPreference) PlayerPrefs.SetInt(GamePerformanceSettings.ShowFpsKey, preference);
                else PlayerPrefs.DeleteKey(GamePerformanceSettings.ShowFpsKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator CounterUpdatesWhilePausedAndCanBeHidden()
        {
            bool visible = GamePerformanceSettings.ShowFps;
            bool hadPreference = PlayerPrefs.HasKey(GamePerformanceSettings.ShowFpsKey);
            int preference = PlayerPrefs.GetInt(GamePerformanceSettings.ShowFpsKey);
            float timeScale = Time.timeScale;
            var go = new GameObject("FPS test", typeof(RectTransform));
            go.SetActive(false);
            try
            {
                var label = go.AddComponent<TextMeshProUGUI>();
                var display = go.AddComponent<FpsDisplay>();
                display.Configure(label);
                if (!GamePerformanceSettings.ShowFps) GamePerformanceSettings.ToggleFps();
                Time.timeScale = 0;
                go.SetActive(true);
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(label.enabled, Is.True);
                Assert.That(label.text, Does.StartWith("FPS: ").And.Not.Contains("..."));
                Assert.That(float.Parse(label.text.Substring(5)), Is.GreaterThan(0));
                GamePerformanceSettings.ToggleFps();
                Assert.That(label.enabled, Is.False);
                go.SetActive(false);
                go.SetActive(true);
                Assert.That(label.enabled, Is.False, "Re-enabling must preserve the hidden preference.");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Time.timeScale = timeScale;
                if (GamePerformanceSettings.ShowFps != visible) GamePerformanceSettings.ToggleFps();
                if (hadPreference) PlayerPrefs.SetInt(GamePerformanceSettings.ShowFpsKey, preference);
                else PlayerPrefs.DeleteKey(GamePerformanceSettings.ShowFpsKey);
                PlayerPrefs.Save();
            }
        }
    }
}
