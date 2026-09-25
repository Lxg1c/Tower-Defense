using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class GameAudioSettingsTests
    {
        bool hadMusic, hadEffects;
        float savedMusic, savedEffects, listenerVolume;
        GameObject music;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadMusic = PlayerPrefs.HasKey(GameAudioSettings.MusicKey);
            hadEffects = PlayerPrefs.HasKey(GameAudioSettings.EffectsKey);
            savedMusic = PlayerPrefs.GetFloat(GameAudioSettings.MusicKey);
            savedEffects = PlayerPrefs.GetFloat(GameAudioSettings.EffectsKey);
            listenerVolume = AudioListener.volume;
            GameAudioSettings.SetMusic(1); GameAudioSettings.SetEffects(1);
            music = new GameObject("Music settings fixture"); music.SetActive(false);
            var player = music.AddComponent<BackgroundMusic>();
            typeof(BackgroundMusic).GetField("playOnAwake", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, false);
            music.SetActive(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MusicAndEffectsCanBeMutedIndependently()
        {
            var source = music.GetComponent<AudioSource>();
            Assert.That(source.ignoreListenerVolume, Is.True);
            GameAudioSettings.SetEffects(0);
            Assert.That(AudioListener.volume, Is.Zero);
            Assert.That(source.volume, Is.EqualTo(.5f).Within(.001f));
            GameAudioSettings.SetMusic(0);
            GameAudioSettings.SetEffects(.6f);
            Assert.That(source.volume, Is.Zero);
            Assert.That(AudioListener.volume, Is.EqualTo(.6f).Within(.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SavedValuesReloadAndReenabledMusicUsesCurrentSetting()
        {
            GameAudioSettings.SetMusic(.25f); GameAudioSettings.SetEffects(.75f);
            GameAudioSettings.Save(); GameAudioSettings.Load();
            Assert.That(GameAudioSettings.Music, Is.EqualTo(.25f));
            Assert.That(GameAudioSettings.Effects, Is.EqualTo(.75f));
            music.SetActive(false); GameAudioSettings.SetMusic(.8f); music.SetActive(true);
            Assert.That(music.GetComponent<AudioSource>().volume, Is.EqualTo(.4f).Within(.001f));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(music);
            yield return null;
            if (hadMusic) PlayerPrefs.SetFloat(GameAudioSettings.MusicKey, savedMusic); else PlayerPrefs.DeleteKey(GameAudioSettings.MusicKey);
            if (hadEffects) PlayerPrefs.SetFloat(GameAudioSettings.EffectsKey, savedEffects); else PlayerPrefs.DeleteKey(GameAudioSettings.EffectsKey);
            GameAudioSettings.Load(); GameAudioSettings.Save(); AudioListener.volume = listenerVolume;
        }
    }
}
