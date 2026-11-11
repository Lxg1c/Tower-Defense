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
                var document = menuScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<UIDocument>(true)).Single();
                var view = document.GetComponent<MenuToolkitView>();
                Assert.That(view, Is.Not.Null);
                var tree = document.visualTreeAsset.CloneTree();
                Assert.That(tree.Q("skillsPage"), Is.Not.Null);
                Assert.That(tree.Q<Button>("skillsButton"), Is.Not.Null);
                Assert.That(tree.Q("skillList"), Is.Not.Null);
                var levelMenu = menuScene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<MainMenu>(true)).Single();
                Assert.That(new SerializedObject(view).FindProperty("menu").objectReferenceValue,
                    Is.SameAs(levelMenu));
                foreach (string name in new[] { "normalButton", "hardButton", "nightmareButton" })
                    Assert.That(tree.Q<Button>(name), Is.Not.Null, name);
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
