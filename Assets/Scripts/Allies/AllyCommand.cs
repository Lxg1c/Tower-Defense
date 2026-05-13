using System;

public static class AllyCommand
{
    public static event Action<bool> OnFollowPlayerChanged;

    public static bool FollowPlayer { get; private set; }

    public static void SetFollowPlayer(bool follow)
    {
        if (FollowPlayer == follow)
            return;

        FollowPlayer = follow;
        OnFollowPlayerChanged?.Invoke(FollowPlayer);
    }

    public static void ToggleFollowPlayer()
    {
        SetFollowPlayer(!FollowPlayer);
    }
}
