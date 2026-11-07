using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace TowerDefense.Tests
{
    public sealed class SkillTreeConfigurationTests
    {
        [Test]
        public void PlayerPrefabHasAssignedBonusTargets()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tower-Defense/Prefabs/Player.prefab");
            var bonus = player.GetComponent<PlayerSkillBonus>();
            Assert.That(bonus, Is.Not.Null);
            var fields = new SerializedObject(bonus);
            Assert.That(fields.FindProperty("health").objectReferenceValue, Is.EqualTo(player.GetComponent<PlayerHealth>()));
            Assert.That(fields.FindProperty("shooter").objectReferenceValue, Is.EqualTo(player.GetComponent<Shooter>()));
        }

        [Test]
        public void MenuAndVictoryScreensHaveSkillUiBindings()
        {
            var menuScene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/MainMenu.unity");
            try
            {
                var menu = menuScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<MainMenuScreens>(true)).Single();
                var menuFields = new SerializedObject(menu);
                Assert.That(menuFields.FindProperty("skills").objectReferenceValue, Is.Not.Null);
                Assert.That(menuFields.FindProperty("skillsButton").objectReferenceValue, Is.Not.Null);
                var levelMenu = menuScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<MainMenu>(true)).Single();
                var difficultyButtons = new SerializedObject(levelMenu).FindProperty("difficultyButtons");
                Assert.That(difficultyButtons.arraySize, Is.EqualTo(3));
                for (int i = 0; i < 3; i++)
                    Assert.That(difficultyButtons.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                var tree = menuScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<SkillTreePanel>(true)).Single();
                var treeFields = new SerializedObject(tree);
                foreach (var field in new[] { "upgradeButtons", "details", "prices" })
                {
                    var items = treeFields.FindProperty(field);
                    Assert.That(items.arraySize, Is.EqualTo(3));
                    for (int i = 0; i < 3; i++)
                        Assert.That(items.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(menuScene); }

            var levelScene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level 1.unity");
            try
            {
                var victory = levelScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<VictoryScreen>(true)).Single();
                var document = levelScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<UIDocument>(true)).Single();
                var view = document.GetComponent<GameplayToolkitView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(new SerializedObject(view).FindProperty("victory").objectReferenceValue,
                    Is.SameAs(victory));
                Assert.That(document.visualTreeAsset, Is.Not.Null);
                var tree = document.visualTreeAsset.CloneTree();
                Assert.That(tree.Q<Label>("victoryReward"), Is.Not.Null);
                Assert.That(tree.Q<Button>("victorySkills"), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(levelScene); }
        }
    }
}
