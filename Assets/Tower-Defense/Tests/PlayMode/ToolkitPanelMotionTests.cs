using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace TowerDefense.Tests
{
    public sealed class ToolkitPanelMotionTests
    {
        private GameObject owner;
        private PanelSettings settings;
        private VisualElement panel;
        private ToolkitPanelMotion motion;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previousTimeScale = Time.timeScale;
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            owner = new GameObject("UI motion test");
            owner.SetActive(false);
            var document = owner.AddComponent<UIDocument>();
            document.panelSettings = settings;
            owner.SetActive(true);
            panel = new VisualElement();
            panel.style.width = 200;
            panel.style.height = 100;
            document.rootVisualElement.Add(panel);
            motion = new ToolkitPanelMotion();
            motion.SetVisible(panel, false, animate: false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosingDisablesInteractionAndFinishesWhileTheGameIsPaused()
        {
            var disabledChild = new VisualElement();
            disabledChild.SetEnabled(false);
            panel.Add(disabledChild);
            Time.timeScale = 0f;
            motion.SetVisible(panel, true);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(panel.ClassListContains("ui-motion-entering"), Is.False);
            motion.SetVisible(panel, false);
            Assert.That(panel.enabledInHierarchy, Is.False);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(panel.ClassListContains("is-hidden"), Is.True);
            motion.SetVisible(panel, true);
            Assert.That(panel.enabledSelf, Is.True);
            Assert.That(disabledChild.enabledSelf, Is.False,
                "Parent transitions must preserve individual control states.");
        }

        [UnityTest]
        public IEnumerator ReopeningCancelsTheOldDelayedHide()
        {
            motion.SetVisible(panel, true);
            yield return new WaitForSecondsRealtime(.04f);
            motion.SetVisible(panel, false);
            motion.SetVisible(panel, true);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(panel.ClassListContains("is-hidden"), Is.False);
            Assert.That(panel.enabledInHierarchy, Is.True);
            Assert.That(panel.ClassListContains("ui-motion-exiting"), Is.False);
        }

        [UnityTest]
        public IEnumerator SwitchingOverlayPanelsFinishesTheOutgoingLayoutImmediately()
        {
            var settingsPanel = new VisualElement();
            panel.parent.Add(settingsPanel);
            var panels = new[] { panel, settingsPanel };
            Time.timeScale = 0f;
            motion.ShowExclusive(panels, panel);
            yield return new WaitForSecondsRealtime(.1f);

            motion.ShowExclusive(panels, null);
            Assert.That(panel.ClassListContains("ui-motion-exiting"), Is.True);
            motion.ShowExclusive(panels, settingsPanel);
            Assert.That(panel.ClassListContains("is-hidden"), Is.True);
            Assert.That(settingsPanel.ClassListContains("is-hidden"), Is.False);

            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(panel.ClassListContains("is-hidden"), Is.True);
            Assert.That(settingsPanel.ClassListContains("is-hidden"), Is.False);
        }

        [UnityTest]
        public IEnumerator ClearingCancelsCallbacksAndRestoresClosedPanelState()
        {
            motion.SetVisible(panel, true);
            motion.SetVisible(panel, false);
            motion.Clear();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(panel.ClassListContains("is-hidden"), Is.True);
            Assert.That(panel.enabledSelf, Is.True);
            Assert.That(panel.ClassListContains("ui-motion-panel"), Is.False);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            motion?.Clear();
            Time.timeScale = previousTimeScale;
            if (owner != null) Object.Destroy(owner);
            if (settings != null) Object.Destroy(settings);
            yield return null;
        }
    }
}
