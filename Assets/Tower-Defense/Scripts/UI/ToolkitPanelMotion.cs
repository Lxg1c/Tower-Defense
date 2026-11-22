using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>Short panel transitions with cancellable, unscaled UI scheduling.</summary>
internal sealed class ToolkitPanelMotion
{
    private const string Hidden = "is-hidden";
    private const string Animated = "ui-motion-panel";
    private const string Entering = "ui-motion-entering";
    private const string Exiting = "ui-motion-exiting";
    private const int ExitMilliseconds = 100;

    private sealed class State
    {
        internal bool Visible;
        internal bool Closing;
        internal bool EnabledBeforeClose;
        internal IVisualElementScheduledItem Pending;
    }

    private readonly Dictionary<VisualElement, State> states = new();

    internal void ShowExclusive(IReadOnlyList<VisualElement> panels, VisualElement active)
    {
        foreach (var panel in panels)
            if (panel != active) SetVisible(panel, false, animate: active == null);
        if (active != null) SetVisible(active, true);
    }

    internal void SetVisible(VisualElement element, bool visible, bool animate = true, bool fadeOnly = false)
    {
        if (!states.TryGetValue(element, out State state))
        {
            state = new State { Visible = false };
            states.Add(element, state);
            if (!visible)
            {
                element.AddToClassList(Hidden);
                return;
            }
        }
        else if (state.Visible == visible && (animate || state.Pending == null))
            return;

        bool reopening = visible && state.Closing;
        state.Pending?.Pause();
        state.Pending = null;
        if (state.Closing)
        {
            element.SetEnabled(state.EnabledBeforeClose);
            state.Closing = false;
        }
        state.Visible = visible;
        element.EnableInClassList(Animated, animate);
        element.EnableInClassList("ui-motion-fade", fadeOnly);
        element.RemoveFromClassList(Entering);
        element.RemoveFromClassList(Exiting);

        if (!animate)
        {
            element.EnableInClassList(Hidden, !visible);
            return;
        }
        if (visible)
        {
            element.RemoveFromClassList(Hidden);
            if (reopening) return;
            element.AddToClassList(Entering);
            state.Pending = element.schedule.Execute(() =>
            {
                // Let UI Toolkit resolve the initial pose before starting the transition.
                state.Pending = element.schedule.Execute(() =>
                {
                    element.RemoveFromClassList(Entering);
                    state.Pending = null;
                });
            });
        }
        else
        {
            state.EnabledBeforeClose = element.enabledSelf;
            state.Closing = true;
            element.SetEnabled(false);
            element.AddToClassList(Exiting);
            state.Pending = element.schedule.Execute(() =>
            {
                element.AddToClassList(Hidden);
                element.RemoveFromClassList(Exiting);
                element.SetEnabled(state.EnabledBeforeClose);
                state.Closing = false;
                state.Pending = null;
            }).StartingIn(ExitMilliseconds);
        }
    }

    internal void Clear()
    {
        foreach (var entry in states)
        {
            VisualElement element = entry.Key;
            State state = entry.Value;
            state.Pending?.Pause();
            if (state.Closing) element.SetEnabled(state.EnabledBeforeClose);
            element.EnableInClassList(Hidden, !state.Visible);
            element.RemoveFromClassList(Entering);
            element.RemoveFromClassList(Exiting);
            element.RemoveFromClassList(Animated);
            element.RemoveFromClassList("ui-motion-fade");
        }
        states.Clear();
    }
}
