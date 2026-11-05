using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Repeats a wall section in every reserved cell while keeping one building and obstacle.</summary>
[DisallowMultipleComponent]
public sealed class WallSpan : MonoBehaviour
{
    [SerializeField] private Transform model;
    [SerializeField] private BoxCollider body;
    [SerializeField] private NavMeshObstacle obstacle;
    [SerializeField, Min(0.01f)] private float oneCellVisualWidth = 2.66f;
    [SerializeField, Min(1f)] private float heightMultiplier = 1.5f;
    [SerializeField] private int lengthCells = 1;
    [Header("Build reveal")]
    [SerializeField, Min(0.01f)] private float sectionRiseDuration = 0.2f;
    [SerializeField, Min(0f)] private float sectionDelay = 0.06f;

    private readonly List<Transform> extraSegments = new();
    private Coroutine buildRoutine;
    private Vector3[] settledPositions;
    private Vector3[] settledScales;
    private bool capturedOriginals;
    private Vector3 originalModelPosition;
    private Vector3 originalModelScale;
    private Vector3 originalBodySize;
    private Vector3 originalBodyCenter;
    private Vector3 originalObstacleSize;
    private Vector3 originalObstacleCenter;

    public int LengthCells => lengthCells;

    public void Configure(int cells, float cellSize)
    {
        if (cells < 1 || cellSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cells), "Wall span needs positive cells and cell size.");
        if (model == null || body == null || obstacle == null)
            throw new InvalidOperationException("WallSpan needs its model, collider, and NavMesh obstacle assigned.");
        if (oneCellVisualWidth <= 0f || heightMultiplier < 1f)
            throw new InvalidOperationException("WallSpan needs a positive section width and height multiplier of at least one.");

        CompleteBuildReveal();
        CaptureOriginals();
        ResizeSegments(cells);

        Vector3 segmentScale = originalModelScale;
        segmentScale.x *= cellSize / oneCellVisualWidth;
        segmentScale.y *= heightMultiplier;
        for (int i = 0; i < cells; i++)
        {
            Transform segment = i == 0 ? model : extraSegments[i - 1];
            segment.localScale = segmentScale;
            Vector3 position = originalModelPosition;
            position.x += (i - (cells - 1) * 0.5f) * cellSize;
            segment.localPosition = position;
        }

        float width = cells * cellSize;
        Vector3 bodySize = originalBodySize;
        bodySize.x = width;
        bodySize.y *= heightMultiplier;
        body.size = bodySize;
        Vector3 bodyCenter = originalBodyCenter;
        bodyCenter.y += (bodySize.y - originalBodySize.y) * 0.5f;
        body.center = bodyCenter;

        Vector3 obstacleSize = originalObstacleSize;
        obstacleSize.x = width;
        obstacleSize.y *= heightMultiplier;
        obstacle.size = obstacleSize;
        Vector3 obstacleCenter = originalObstacleCenter;
        obstacleCenter.y += (obstacleSize.y - originalObstacleSize.y) * 0.5f;
        obstacle.center = obstacleCenter;
        lengthCells = cells;
    }

    public void PlayBuildReveal()
    {
        CompleteBuildReveal();
        settledPositions = new Vector3[lengthCells];
        settledScales = new Vector3[lengthCells];
        for (int i = 0; i < lengthCells; i++)
        {
            Transform segment = GetSegment(i);
            settledPositions[i] = segment.localPosition;
            settledScales[i] = segment.localScale;
            segment.localPosition -= Vector3.up * 0.45f;
            Vector3 small = segment.localScale;
            small.y *= 0.05f;
            segment.localScale = small;
        }
        buildRoutine = StartCoroutine(RevealSections());
    }

    private IEnumerator RevealSections()
    {
        for (int i = 0; i < lengthCells; i++)
        {
            Transform segment = GetSegment(i);
            Vector3 startPosition = segment.localPosition;
            Vector3 startScale = segment.localScale;
            float elapsed = 0f;
            while (elapsed < sectionRiseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sectionRiseDuration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                segment.localPosition = Vector3.Lerp(startPosition, settledPositions[i], eased);
                segment.localScale = Vector3.Lerp(startScale, settledScales[i], eased);
                yield return null;
            }
            segment.localPosition = settledPositions[i];
            segment.localScale = settledScales[i];
            if (sectionDelay > 0f && i < lengthCells - 1)
                yield return new WaitForSeconds(sectionDelay);
        }
        buildRoutine = null;
    }

    private void OnDisable() => CompleteBuildReveal();

    private void CompleteBuildReveal()
    {
        if (buildRoutine != null)
        {
            StopCoroutine(buildRoutine);
            buildRoutine = null;
        }
        if (settledPositions == null) return;
        for (int i = 0; i < settledPositions.Length; i++)
        {
            Transform segment = GetSegment(i);
            if (segment == null) continue;
            segment.localPosition = settledPositions[i];
            segment.localScale = settledScales[i];
        }
        settledPositions = null;
        settledScales = null;
    }

    private Transform GetSegment(int index) => index == 0 ? model : extraSegments[index - 1];

    private void CaptureOriginals()
    {
        if (capturedOriginals) return;
        originalModelPosition = model.localPosition;
        originalModelScale = model.localScale;
        originalBodySize = body.size;
        originalBodyCenter = body.center;
        originalObstacleSize = obstacle.size;
        originalObstacleCenter = obstacle.center;
        capturedOriginals = true;
    }

    private void ResizeSegments(int cells)
    {
        while (extraSegments.Count < cells - 1)
        {
            Transform segment = Instantiate(model, model.parent);
            segment.name = $"Wall segment {extraSegments.Count + 2}";
            extraSegments.Add(segment);
        }
        while (extraSegments.Count > cells - 1)
        {
            int last = extraSegments.Count - 1;
            Transform segment = extraSegments[last];
            extraSegments.RemoveAt(last);
            if (Application.isPlaying) Destroy(segment.gameObject);
            else DestroyImmediate(segment.gameObject);
        }
    }
}
