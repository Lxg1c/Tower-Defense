using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class UIConfigurationTests
    {
        [Test]
        public void ButtonsInGameplayMenuAndTowerCardHaveNoMissingScripts()
        {
            foreach (string path in new[]
                     {
                         "Assets/Tower-Defense/Scenes/Level.unity",
                         "Assets/Tower-Defense/Scenes/MainMenu.unity"
                     })
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (var button in scene.GetRootGameObjects().SelectMany(r =>
                                 r.GetComponentsInChildren<UnityEngine.UI.Button>(true)))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(button.gameObject),
                            Is.Zero, $"{path}: {button.name}");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }

            var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tower-Defense/Prefabs/UI/TowerCard.prefab");
            foreach (var button in card.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(button.gameObject),
                    Is.Zero, $"TowerCard: {button.name}");
        }

        [Test]
        public void BuildingPanelsLeaveWorldInputAvailableAndHaveNoCloseButtons()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level.unity");
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true));
                var modals = components.Where(c => c is TowerSelectionModal).ToArray();
                Assert.That(modals, Has.Length.EqualTo(1));
                foreach (var controller in modals)
                {
                    var data = new SerializedObject(controller);
                    var root = (GameObject)data.FindProperty("modalRoot").objectReferenceValue;
                    Assert.That(root, Is.Not.Null);
                    Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b => b.name == "Close"), Is.False);
                    var backdrop = root.GetComponent<UnityEngine.UI.Image>();
                    Assert.That(backdrop.enabled, Is.False, "The world must not be dimmed.");
                    Assert.That(backdrop.raycastTarget, Is.False, "The fullscreen root must not intercept joystick input.");
                    string[] fields = { "content", "heading" };
                    foreach (string field in fields)
                    {
                        var child = (Component)data.FindProperty(field).objectReferenceValue;
                        Assert.That(child, Is.Not.Null, field);
                        Assert.That(child.transform.IsChildOf(root.transform), Is.True, field);
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void MainMenuHasLevelReferencesAndWorkingPersistentActions()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/MainMenu.unity");
            try
            {
                var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenu>(true)).Single();
                var data = new SerializedObject(menu);
                foreach (string field in new[] { "levelLabel", "levelDescription", "wavePreview", "levelButtonTemplate", "levelButtonContainer", "startButton" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var packs = data.FindProperty("levels");
                Assert.That(packs.arraySize, Is.EqualTo(3));
                for (int i = 0; i < packs.arraySize; i++)
                    Assert.That(packs.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                var buttons = menu.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (string method in new[] { "StartGame", "ExitGame" })
                    Assert.That(buttons.Any(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i =>
                        b.onClick.GetPersistentTarget(i) == menu && b.onClick.GetPersistentMethodName(i) == method)), Is.True, method);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void UltimateProgressAndCountdownAreWiredToTheButton()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level.unity");
            try
            {
                var control = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<UltimateButton>(true)).Single();
                var data = new SerializedObject(control);
                var fill = (UnityEngine.UI.Image)data.FindProperty("fill").objectReferenceValue;
                var label = (Component)data.FindProperty("cooldownLabel").objectReferenceValue;

                Assert.That(fill, Is.Not.Null);
                Assert.That(fill.transform.parent, Is.EqualTo(control.transform));
                Assert.That(fill.transform.GetSiblingIndex(), Is.Zero,
                    "The charge fill must render behind the icon.");
                Assert.That(fill.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(fill.rectTransform.sizeDelta,
                    Is.EqualTo(((RectTransform)control.transform).rect.size));
                Assert.That(label, Is.Not.Null);
                Assert.That(label.transform.parent.parent, Is.EqualTo(control.transform));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
