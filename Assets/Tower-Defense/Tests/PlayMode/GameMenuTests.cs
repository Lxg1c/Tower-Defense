using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class GameMenuTests
    {
        GameObject fixture;
        PauseMenu pause;
        WaveSpawner spawner;
        float oldScale, effectsVolume;
        bool hadEffects;

        static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        GameObject Child(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : fixture.transform);
            return go;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            oldScale = Time.timeScale;
            hadEffects = PlayerPrefs.HasKey(GameAudioSettings.EffectsKey);
            effectsVolume = GameAudioSettings.Effects;
            fixture = new GameObject("Menu tests"); fixture.SetActive(false);
            spawner = Child("Spawner").AddComponent<WaveSpawner>(); spawner.enabled = false;
            pause = fixture.AddComponent<PauseMenu>();
            Set(pause, "spawner", spawner);
            fixture.SetActive(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsKeepGamePausedAndReopeningPauseShowsOptions()
        {
            pause.Pause(); pause.ShowSettings();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(pause.IsVisible, Is.True);
            Assert.That(pause.IsSettingsOpen, Is.True);
            pause.ShowPauseOptions();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(pause.IsSettingsOpen, Is.False);
            pause.ShowSettings(); pause.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(pause.IsVisible, Is.False);
            pause.Pause();
            Assert.That(pause.IsVisible, Is.True);
            Assert.That(pause.IsSettingsOpen, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RestartAndMenuRequestsWaitForExplicitConfirmation()
        {
            pause.Restart();
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(pause.PendingExitAction, Is.EqualTo(PauseMenu.ExitAction.Restart));
            Assert.That(spawner.CurrentPhase, Is.EqualTo(WaveSpawner.Phase.Build));

            pause.CancelExit();
            Assert.That(pause.PendingExitAction, Is.EqualTo(PauseMenu.ExitAction.None));
            Assert.That(pause.IsPaused, Is.True);

            pause.GoToMainMenu();
            Assert.That(pause.PendingExitAction, Is.EqualTo(PauseMenu.ExitAction.MainMenu));
            pause.Resume();
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(pause.PendingExitAction, Is.EqualTo(PauseMenu.ExitAction.None));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioControlsWorkAtZeroTimeScaleAndRefreshOnReopen()
        {
            pause.Pause(); pause.ShowSettings();
            GameAudioSettings.SetEffects(.6f);
            Assert.That(GameAudioSettings.Effects,Is.EqualTo(.6f));
            Assert.That(Time.timeScale,Is.Zero);
            pause.ShowPauseOptions(); GameAudioSettings.SetEffects(.8f); pause.ShowSettings();
            Assert.That(GameAudioSettings.Effects, Is.EqualTo(.8f));
            Assert.That(pause.IsSettingsOpen, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VictoryOnlyOffersNextLevelAfterWinAndNotAtCampaignEnd()
        {
            var setupObject=Child("Inactive setup"); setupObject.SetActive(false);
            var setup=setupObject.AddComponent<LevelSetup>();
            var current=ScriptableObject.CreateInstance<LevelDefinition>();
            var next=ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                current.nextLevel=next;
                Set(setup,"<CurrentLevel>k__BackingField",current);
                var screenObject=Child("Victory controller"); screenObject.SetActive(false);
                var screen=screenObject.AddComponent<VictoryScreen>();
                Set(screen,"spawner",spawner);
                Set(screen,"levelSetup",setup);
                screen.Show(); Assert.That(screen.CanPlayNextLevel,Is.False);
                var session=new GameSession(new[]{0},_=>{}); session.TryDefeat(); Set(spawner,"session",session);
                screen.Show(); Assert.That(screen.CanPlayNextLevel,Is.False);
                Set(spawner,"session",new GameSession(new int[0],_=>{}));
                screen.Show(); Assert.That(screen.CanPlayNextLevel,Is.True);
                Assert.That(screen.IsVisible, Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                pause.Pause(); Assert.That(pause.IsPaused,Is.False,"Terminal result must not open pause.");
                current.nextLevel=null; screen.Show(); Assert.That(screen.CanPlayNextLevel,Is.False);
            }
            finally { Object.Destroy(current); Object.Destroy(next); }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(fixture); yield return null;
            if(hadEffects)PlayerPrefs.SetFloat(GameAudioSettings.EffectsKey,effectsVolume);else PlayerPrefs.DeleteKey(GameAudioSettings.EffectsKey);
            GameAudioSettings.Load();GameAudioSettings.Save();Time.timeScale=oldScale;
        }
    }
}
