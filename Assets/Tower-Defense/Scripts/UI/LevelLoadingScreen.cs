using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
internal sealed class LevelLoadingScreen : MonoBehaviour
{
    private const float FadeInSeconds = 0.24f;
    private const float FadeOutSeconds = 0.38f;
    private const float CompletionHoldSeconds = 0.12f;
    private const float SceneReadyProgress = 0.9f;
    private static LevelLoadingScreen active;
    internal static bool IsLoading => active != null;

    internal static void Begin(PanelSettings panel, VisualTreeAsset tree, VisualElement menuRoot,
        string scene, string title, int index = 0, int? difficulty = null)
    {
        var host = new GameObject("Level Loading Screen");
        host.SetActive(false);
        var loader = host.AddComponent<LevelLoadingScreen>();
        var document = host.GetComponent<UIDocument>();
        document.panelSettings = panel;
        document.visualTreeAsset = tree;
        document.sortingOrder = 100;
        DontDestroyOnLoad(host);
        active = loader;
        host.SetActive(true);
        loader.StartCoroutine(loader.Load(document.rootVisualElement, menuRoot, scene, title, index, difficulty));
    }

    private IEnumerator Load(VisualElement documentRoot, VisualElement menuRoot, string scene,
        string title, int index, int? difficulty)
    {
        documentRoot.style.position = Position.Absolute;
        documentRoot.style.left = 0;
        documentRoot.style.right = 0;
        documentRoot.style.top = 0;
        documentRoot.style.bottom = 0;
        var screen = documentRoot.Q("loadingScreen");
        var percentage = screen.Q<Label>("loadingPercent");
        var fill = screen.Q("loadingFill");
        var difficultyKey = difficulty switch
        {
            2 => LocalizationKeys.Hard,
            3 => LocalizationKeys.Nightmare,
            _ => LocalizationKeys.Normal
        };
        screen.Q<Label>("loadingTitle").text = title;
        var difficultyLabel = screen.Q<Label>("loadingDifficulty");
        var cover = screen.Q("loadingCover");
        MenuToolkitView.SetLevelArt(cover, index);
        if (difficulty.HasValue)
        {
            difficultyLabel.text =
                GameLocalization.Format(LocalizationKeys.LoadingDifficulty, GameLocalization.Text(difficultyKey));
        }
        else
        {
            difficultyLabel.style.display = DisplayStyle.None;
        }
        screen.Q<Label>("loadingStatus").text = GameLocalization.Text(
            difficulty.HasValue ? LocalizationKeys.LoadingLevel : LocalizationKeys.LoadingMenu);

        screen.style.opacity = 0f;
        menuRoot.SetEnabled(false);
        yield return Fade(screen, 0f, 1f, FadeInSeconds);
        yield return null;

        var operation = SceneManager.LoadSceneAsync(scene);
        if (operation == null)
        {
            menuRoot.SetEnabled(true);
            if (difficulty.HasValue) RunSelection.Clear();
            yield return Fade(screen, 1f, 0f, FadeOutSeconds);
            Destroy(gameObject);
            yield break;
        }

        operation.allowSceneActivation = false;
        float displayed = 0f;
        while (operation.progress < SceneReadyProgress || displayed < SceneReadyProgress)
        {
            displayed = Mathf.MoveTowards(displayed, operation.progress, Time.unscaledDeltaTime * 2.8f);
            SetProgress(fill, percentage, displayed);
            yield return null;
        }

        operation.allowSceneActivation = true;
        yield return operation;
        SetProgress(fill, percentage, 1f);
        yield return new WaitForSecondsRealtime(CompletionHoldSeconds);
        yield return Fade(screen, 1f, 0f, FadeOutSeconds);
        Destroy(gameObject);
    }

    private static void SetProgress(VisualElement fill, Label percentage, float progress)
    {
        int value = Mathf.RoundToInt(progress * 100f);
        fill.style.width = Length.Percent(value);
        percentage.text = $"{value}%";
    }

    private static IEnumerator Fade(VisualElement screen, float from, float to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / duration;
            screen.style.opacity = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        screen.style.opacity = to;
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
