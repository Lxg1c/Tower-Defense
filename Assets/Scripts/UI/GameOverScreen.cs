using UnityEngine;

/// <summary>
/// Shown when the Base dies. Freezes time and displays restart / menu actions.
/// </summary>
[DisallowMultipleComponent]
public class GameOverScreen : MenuScreenBase
{
    private Base subscribedBase;

    private void OnEnable()
    {
        Base.OnBaseChanged += BindBase;
        BindBase();
    }

    private void OnDisable()
    {
        Base.OnBaseChanged -= BindBase;
        UnbindBase();
    }

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f;
    }

    private void BindBase()
    {
        Base current = Base.Instance;
        if (subscribedBase == current)
            return;

        UnbindBase();

        subscribedBase = current;
        if (subscribedBase != null)
            subscribedBase.onDied.AddListener(Show);
    }

    private void UnbindBase()
    {
        if (subscribedBase != null)
            subscribedBase.onDied.RemoveListener(Show);

        subscribedBase = null;
    }
}
