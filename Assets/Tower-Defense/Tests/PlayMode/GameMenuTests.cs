using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TowerDefense.Tests
{
    public class GameMenuTests
    {
        GameObject fixture, pauseRoot, options, settings;
        PauseMenu pause;
        WaveSpawner spawner;
        float oldScale, musicVolume, effectsVolume;
        bool hadMusic, hadEffects;

        static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        GameObject Child(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : fixture.transform);
            return go;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            oldScale = Time.timeScale;
            hadMusic = PlayerPrefs.HasKey(GameAudioSettings.MusicKey);
            hadEffects = PlayerPrefs.HasKey(GameAudioSettings.EffectsKey);
            musicVolume = GameAudioSettings.Music; effectsVolume = GameAudioSettings.Effects;
            fixture = new GameObject("Menu tests"); fixture.SetActive(false);
            spawner = Child("Spawner").AddComponent<WaveSpawner>(); spawner.enabled = false;
            pauseRoot = Child("Pause"); options = Child("Options", pauseRoot.transform);
            settings = Child("Settings", pauseRoot.transform); settings.SetActive(false);
            pause = fixture.AddComponent<PauseMenu>();
            Set(pause, "root", pauseRoot); Set(pause, "menuContent", options);
            Set(pause, "settingsContent", settings); Set(pause, "spawner", spawner);
            fixture.SetActive(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsKeepGamePausedAndReopeningPauseShowsOptions()
        {
            pause.Pause(); pause.ShowSettings();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(settings.activeInHierarchy, Is.True);
            Assert.That(options.activeSelf, Is.False);
            pause.ShowPauseOptions();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(options.activeInHierarchy, Is.True);
            pause.ShowSettings(); pause.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(pauseRoot.activeSelf, Is.False);
            pause.Pause();
            Assert.That(options.activeInHierarchy, Is.True);
            Assert.That(settings.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioControlsWorkAtZeroTimeScaleAndRefreshOnReopen()
        {
            fixture.AddComponent<Canvas>();
            Child("Back", settings.transform).AddComponent<Button>();
            var music = Child("Music", settings.transform).AddComponent<Slider>();
            var effects = Child("Effects", settings.transform).AddComponent<Slider>();
            var ml = Child("Music value", settings.transform).AddComponent<TextMeshProUGUI>();
            var el = Child("Effects value", settings.transform).AddComponent<TextMeshProUGUI>();
            var panel = settings.AddComponent<AudioSettingsPanel>();
            Set(panel,"music",music); Set(panel,"effects",effects);
            Set(panel,"musicValue",ml); Set(panel,"effectsValue",el);
            pause.Pause(); pause.ShowSettings(); music.value=.25f; effects.value=.6f;
            Assert.That(GameAudioSettings.Music,Is.EqualTo(.25f));
            Assert.That(GameAudioSettings.Effects,Is.EqualTo(.6f));
            Assert.That(ml.text,Is.EqualTo("25%")); Assert.That(el.text,Is.EqualTo("60%"));
            Assert.That(Time.timeScale,Is.Zero);
            pause.ShowPauseOptions(); GameAudioSettings.SetMusic(.8f); pause.ShowSettings();
            Assert.That(music.value,Is.EqualTo(.8f)); Assert.That(ml.text,Is.EqualTo("80%"));
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
                var root=Child("Victory"); var button=Child("Next",root.transform).AddComponent<Button>();
                Set(screen,"root",root); Set(screen,"spawner",spawner);
                Set(screen,"levelSetup",setup); Set(screen,"nextLevelButton",button);
                screen.Show(); Assert.That(button.gameObject.activeSelf,Is.False);
                var session=new GameSession(new[]{0},_=>{}); session.TryDefeat(); Set(spawner,"session",session);
                screen.Show(); Assert.That(screen.CanPlayNextLevel,Is.False);
                Set(spawner,"session",new GameSession(new int[0],_=>{}));
                screen.Show(); Assert.That(button.gameObject.activeSelf,Is.True);
                Assert.That(Time.timeScale,Is.Zero);
                pause.Pause(); Assert.That(pause.IsPaused,Is.False,"Terminal result must not open pause.");
                current.nextLevel=null; screen.Show(); Assert.That(button.gameObject.activeSelf,Is.False);
            }
            finally { Object.Destroy(current); Object.Destroy(next); }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(fixture); yield return null;
            if(hadMusic)PlayerPrefs.SetFloat(GameAudioSettings.MusicKey,musicVolume);else PlayerPrefs.DeleteKey(GameAudioSettings.MusicKey);
            if(hadEffects)PlayerPrefs.SetFloat(GameAudioSettings.EffectsKey,effectsVolume);else PlayerPrefs.DeleteKey(GameAudioSettings.EffectsKey);
            GameAudioSettings.Load();GameAudioSettings.Save();Time.timeScale=oldScale;
        }
    }
}
