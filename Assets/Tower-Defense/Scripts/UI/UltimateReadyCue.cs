/// <summary>Shows one brief highlight when the ultimate becomes usable again.</summary>
internal sealed class UltimateReadyCue
{
    private const float Duration = 0.55f;
    private bool initialized;
    private bool wasReady;
    private float remaining;

    internal bool Update(bool ready, bool visible, float deltaTime)
    {
        if (!ready) remaining = 0f;
        else if (initialized && !wasReady) remaining = Duration;
        initialized = true;
        wasReady = ready;

        if (!visible || remaining <= 0f) return false;
        remaining -= deltaTime;
        return true;
    }
}
