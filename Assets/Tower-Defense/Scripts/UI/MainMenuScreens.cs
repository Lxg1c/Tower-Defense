using UnityEngine;

/// <summary>Home, level selection and audio settings within the MainMenu scene.</summary>
public sealed class MainMenuScreens : MonoBehaviour
{
    public enum Screen { Home, Levels, Settings, Skills }
    public Screen CurrentScreen { get; private set; }

    private void Awake()
    {
        if (PlayerProgression.ConsumeSkillsScreenRequest()) ShowSkills();
        else ShowHome();
    }

    private void OnDisable() => GameAudioSettings.Save();

    public void ShowHome() => Show(Screen.Home);
    public void ShowLevels() => Show(Screen.Levels);
    public void ShowSettings() => Show(Screen.Settings);
    public void ShowSkills() => Show(Screen.Skills);

    private void Show(Screen screen)
    {
        GameAudioSettings.Save();
        CurrentScreen = screen;
    }
}
