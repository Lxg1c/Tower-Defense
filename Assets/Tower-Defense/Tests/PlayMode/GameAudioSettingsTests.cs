using NUnit.Framework;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class GameAudioSettingsTests
    {
        bool hadMaster, hadEffects;
        float savedMaster, savedEffects, listenerVolume;

        [SetUp]
        public void Setup()
        {
            hadMaster = PlayerPrefs.HasKey(GameAudioSettings.MasterKey);
            savedMaster = PlayerPrefs.GetFloat(GameAudioSettings.MasterKey);
            hadEffects = PlayerPrefs.HasKey(GameAudioSettings.EffectsKey);
            savedEffects = PlayerPrefs.GetFloat(GameAudioSettings.EffectsKey);
            listenerVolume = AudioListener.volume;
            GameAudioSettings.SetMaster(1f);
            GameAudioSettings.SetEffects(1f);
        }

        [Test]
        public void EitherVolumeControlCanMuteEffects()
        {
            GameAudioSettings.SetEffects(0f);
            Assert.That(AudioListener.volume, Is.Zero);

            GameAudioSettings.SetEffects(.6f);
            Assert.That(AudioListener.volume, Is.EqualTo(.6f).Within(.001f));

            GameAudioSettings.SetMaster(0f);
            Assert.That(AudioListener.volume, Is.Zero);
        }

        [Test]
        public void MasterScalesEffectsAndBothVolumesReloadFromPreferences()
        {
            PlayerPrefs.SetFloat(GameAudioSettings.MasterKey, .5f);
            PlayerPrefs.SetFloat(GameAudioSettings.EffectsKey, .6f);
            GameAudioSettings.Load();

            Assert.That(GameAudioSettings.Master, Is.EqualTo(.5f));
            Assert.That(GameAudioSettings.Effects, Is.EqualTo(.6f));
            Assert.That(AudioListener.volume, Is.EqualTo(.3f).Within(.001f));
        }

        [TearDown]
        public void Cleanup()
        {
            if (hadMaster) PlayerPrefs.SetFloat(GameAudioSettings.MasterKey, savedMaster);
            else PlayerPrefs.DeleteKey(GameAudioSettings.MasterKey);
            if (hadEffects) PlayerPrefs.SetFloat(GameAudioSettings.EffectsKey, savedEffects);
            else PlayerPrefs.DeleteKey(GameAudioSettings.EffectsKey);
            GameAudioSettings.Load();
            AudioListener.volume = listenerVolume;
        }
    }
}
