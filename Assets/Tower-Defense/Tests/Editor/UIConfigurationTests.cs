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
        public void BothLevelModalsHavePersistedCloseCallbacksAndDescendantContent()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level.unity");
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true));
                var modals = components.Where(c => c is TowerSelectionModal || c is TowerUpgradeModal).ToArray();
                Assert.That(modals, Has.Length.EqualTo(2));
                foreach (var controller in modals)
                {
                    var data = new SerializedObject(controller);
                    var root = (GameObject)data.FindProperty("modalRoot").objectReferenceValue;
                    Assert.That(root, Is.Not.Null);
                    var close = root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Close");
                    Assert.That(close.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                    Assert.That(close.onClick.GetPersistentTarget(0), Is.EqualTo(controller));
                    Assert.That(close.onClick.GetPersistentMethodName(0), Is.EqualTo("Close"));
                    string[] fields = controller is TowerSelectionModal
                        ? new[] { "content" } : new[] { "currentStats", "nextStats", "costLabel", "upgradeButton" };
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
    }
}
