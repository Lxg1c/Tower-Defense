using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class BackgroundMusicTests
    {
        [UnityTest]
        public IEnumerator WaveChangesMusicAndSettingsScaleBothTracks()
        {
            var waveObject = new GameObject("Music test waves");
            var spawner = waveObject.AddComponent<WaveSpawner>();
            spawner.enabled = false;
            var owner = new GameObject("Music test");
            owner.SetActive(false);
            var music = owner.AddComponent<BackgroundMusic>();
            var calm = AudioClip.Create("Calm", 48000, 1, 48000, false);
            var combat = AudioClip.Create("Combat", 48000, 1, 48000, false);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BackgroundMusic).GetField("musicClip", fields).SetValue(music, calm);
            typeof(BackgroundMusic).GetField("combatClip", fields).SetValue(music, combat);
            typeof(BackgroundMusic).GetField("volume", fields).SetValue(music, 0.32f);

            bool hadMaster = PlayerPrefs.HasKey(GameAudioSettings.MasterKey);
            bool hadMusic = PlayerPrefs.HasKey(GameAudioSettings.MusicKey);
            bool hadEffects = PlayerPrefs.HasKey(GameAudioSettings.EffectsKey);
            float previousMaster = GameAudioSettings.Master;
            float previousMusic = GameAudioSettings.Music;
            float previousEffects = GameAudioSettings.Effects;
            float previousTimeScale = Time.timeScale;
            try
            {
                GameAudioSettings.SetMaster(0.5f);
                GameAudioSettings.SetMusic(0.5f);
                GameAudioSettings.SetEffects(0f);
                owner.SetActive(true);
                yield return null;
                var sources = owner.GetComponents<AudioSource>();
                Assert.That(sources[0].clip, Is.SameAs(calm));
                Assert.That(sources[1].clip, Is.SameAs(combat));
                Assert.That(sources[0].volume, Is.EqualTo(0.08f).Within(0.001f));
                Assert.That(sources[1].volume, Is.Zero);

                Time.timeScale = 0f;
                spawner.onCombatPhaseStarted.Invoke(0, 2, 0);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(sources[0].volume, Is.InRange(0.001f, 0.079f));
                Assert.That(sources[1].volume, Is.InRange(0.001f, 0.069f));
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(sources[0].volume, Is.Zero);
                Assert.That(sources[1].volume, Is.EqualTo(0.07f).Within(0.001f));

                GameAudioSettings.SetMusic(0f);
                Assert.That(sources[1].volume, Is.Zero);
                GameAudioSettings.SetMusic(0.5f);
                spawner.onDefeated.Invoke();
                yield return new WaitForSecondsRealtime(1.3f);
                Assert.That(sources[0].volume, Is.EqualTo(0.08f).Within(0.001f));
                Assert.That(sources[1].volume, Is.Zero);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                GameAudioSettings.SetMaster(previousMaster);
                GameAudioSettings.SetMusic(previousMusic);
                GameAudioSettings.SetEffects(previousEffects);
                if (!hadMaster) PlayerPrefs.DeleteKey(GameAudioSettings.MasterKey);
                if (!hadMusic) PlayerPrefs.DeleteKey(GameAudioSettings.MusicKey);
                if (!hadEffects) PlayerPrefs.DeleteKey(GameAudioSettings.EffectsKey);
                Object.Destroy(owner);
                Object.Destroy(waveObject);
                Object.Destroy(calm);
                Object.Destroy(combat);
            }
            yield return null;
        }
    }
}
