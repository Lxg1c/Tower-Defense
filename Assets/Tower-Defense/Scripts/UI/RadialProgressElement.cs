using UnityEngine;
using UnityEngine.UIElements;

/// <summary>UI Toolkit progress ring for HUD controls and projected world points.</summary>
public sealed class RadialProgressElement : VisualElement
{
    private float progress;

    public Color TrackColor { get; set; } = new(0.13f, 0.34f, 0.41f, 0.8f);
    public Color FillColor { get; set; } = new(0.24f, 0.97f, 0.95f, 1f);
    public float StrokeWidth { get; set; } = 6f;

    public float Progress
    {
        get => progress;
        set
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(progress, next)) return;
            progress = next;
            MarkDirtyRepaint();
        }
    }

    public RadialProgressElement()
    {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += Draw;
    }

    private void Draw(MeshGenerationContext context)
    {
        float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f - StrokeWidth;
        if (radius <= 0f) return;
        Vector2 center = new(contentRect.width * 0.5f, contentRect.height * 0.5f);
        var painter = context.painter2D;
        painter.lineWidth = StrokeWidth - 1f;
        painter.strokeColor = TrackColor;
        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(270f), ArcDirection.Clockwise);
        painter.Stroke();
        if (progress <= 0f) return;
        painter.lineWidth = StrokeWidth;
        painter.strokeColor = FillColor;
        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + progress * 360f),
            ArcDirection.Clockwise);
        painter.Stroke();
    }
}
