using System;
using TMPro;
using UnityEngine;

/// <summary>Adds FPS controls to the existing settings layout without scene migrations.</summary>
public static class FpsInterface
{
    public static void AddSettingsButton(RectTransform panel, TMP_FontAsset font)
    {
        if (panel.Find("FpsButton") != null) return;
        var back = panel.Find("Back") as RectTransform;
        if (back == null)
            throw new InvalidOperationException("FPS settings require the existing settings panel with its Back button.");
        back.anchoredPosition = new Vector2(back.anchoredPosition.x, -225);

        var go = new GameObject("FpsButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -140);
        rect.sizeDelta = new Vector2(340, 64);
        var background = go.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.12f, 0.28f, 0.34f, 1);
        var button = go.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;
        var label = CreateLabel("Label", rect, font, 30);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        go.AddComponent<FpsSettingsButton>().Configure(button, label);
        go.SetActive(true);
    }

    public static void EnsureOverlay(Canvas canvas, TMP_FontAsset font)
    {
        if (canvas.transform.Find("FpsOverlay") != null) return;
        var go = new GameObject("FpsOverlay", typeof(RectTransform));
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var overlay = go.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 100;
        go.AddComponent<ScreenSafeArea>();
        var label = CreateLabel("FpsLabel", rect, font, 28);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = Vector2.one;
        labelRect.anchoredPosition = new Vector2(-100, -150);
        labelRect.sizeDelta = new Vector2(180, 48);
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.outlineColor = Color.black;
        label.outlineWidth = 0.2f;
        go.AddComponent<FpsDisplay>().Configure(label);
        go.SetActive(true);
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, TMP_FontAsset font, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
}
