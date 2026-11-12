using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace TowerDefense.Tests
{
    public class UIConfigurationTests
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Tower-Defense/Scenes/MainMenu.unity",
            "Assets/Tower-Defense/Scenes/Level 1.unity",
            "Assets/Tower-Defense/Scenes/GameplayLab.unity"
        };

        [Test]
        public void PlayableScenesUseToolkitWithoutCanvas()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/Tower-Defense/UI/Toolkit/PanelSettings.asset");
            Assert.That(panel, Is.Not.Null);
            foreach (string path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)), Is.Empty, path);
                    var document = roots.SelectMany(r => r.GetComponentsInChildren<UIDocument>(true)).Single();
                    Assert.That(document.panelSettings, Is.SameAs(panel), path);
                    Assert.That(document.visualTreeAsset, Is.Not.Null, path);
                    Assert.That(document.GetComponent<MenuToolkitView>() != null ||
                        document.GetComponent<GameplayToolkitView>() != null, Is.True, path);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        [Test]
        public void MigratedScenesAndPrefabsHaveNoMissingScripts()
        {
            foreach (string path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (Transform node in scene.GetRootGameObjects().SelectMany(r =>
                                 r.GetComponentsInChildren<Transform>(true)))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject),
                            Is.Zero, path + ": " + node.name);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab",
                         new[] { "Assets/Tower-Defense/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Transform node in prefab.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject),
                        Is.Zero, path + ": " + node.name);
            }
        }

        [Test]
        public void GameplayScenesKeepControllersAndWorldPresentation()
        {
            foreach (string path in ScenePaths.Skip(1))
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var view = roots.SelectMany(r => r.GetComponentsInChildren<GameplayToolkitView>(true)).Single();
                    var data = new SerializedObject(view);
                    foreach (string field in new[] { "responsiveHud", "spawner", "wallet", "timer",
                                 "movement", "ultimate", "teleport", "pauseMenu", "victory", "defeat",
                                 "levelSetup", "buildModal", "orders", "slowAmmo", "playerHealth",
                                 "companionTurrets" })
                        Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, path + ": " + field);
                    Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<HealthBarManager>(true)).Count(), Is.EqualTo(1), path);
                    Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Button>(true)),
                        Is.Empty, path);
                    var waveCue = data.FindProperty("waveStartAudio").objectReferenceValue as AudioSource;
                    Assert.That(waveCue, Is.Not.Null, path);
                    Assert.That(waveCue.clip, Is.Not.Null, path);
                    Assert.That(waveCue.playOnAwake || waveCue.loop || waveCue.ignoreListenerVolume, Is.False, path);
                    Assert.That(waveCue.spatialBlend, Is.Zero, path);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        [Test]
        public void GameplayToolkitContainsBuildCombatAndOrderControls()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Tower-Defense/UI/Toolkit/Gameplay.uxml");
            var root = tree.CloneTree();
            foreach (string name in new[] { "spawnPreview", "buildPanel", "buildCards",
                         "startWaveButton", "joystick", "ultimateButton", "desktopUltimate", "orderCancel",
                         "pausePanel", "victoryPanel", "defeatPanel", "fpsToggle", "fpsLabel",
                         "tutorialPanel", "tutorialTitle", "tutorialText" })
                Assert.That(root.Q(name), Is.Not.Null, name);
            Assert.That(root.Q("zoneLayer"), Is.Null, "Building markers belong on the floor, not in the HUD.");
        }

        [Test]
        public void LevelOneHasZoneIconsAndToolkitMovement()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePaths[1]);
            try
            {
                var roots = scene.GetRootGameObjects();
                var zones = roots.SelectMany(r => r.GetComponentsInChildren<TowerPlacementZone>(true)).ToArray();
                Assert.That(zones, Is.Not.Empty);
                foreach (var zone in zones)
                {
                    var data = new SerializedObject(zone);
                    Assert.That(data.FindProperty("townHallIcon").objectReferenceValue, Is.Not.Null);
                    Assert.That(data.FindProperty("defenseIcon").objectReferenceValue, Is.Not.Null);
                }
                var player = roots.SelectMany(r => r.GetComponentsInChildren<PlayerMovement>(true)).Single();
                var view = roots.SelectMany(r => r.GetComponentsInChildren<GameplayToolkitView>(true)).Single();
                Assert.That(new SerializedObject(view).FindProperty("movement").objectReferenceValue,
                    Is.SameAs(player));
                Assert.That(typeof(PlayerMovement).GetField("joystick",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
                    Is.Null, "Touch movement must only come from UI Toolkit.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void MainMenuHasRepeatHintsAndBuildingGuideData()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePaths[0]);
            try
            {
                var view = scene.GetRootGameObjects()
                    .SelectMany(gameObject => gameObject.GetComponentsInChildren<MenuToolkitView>(true)).Single();
                var loadout = new SerializedObject(view).FindProperty("guideLoadout").objectReferenceValue as TowerLoadout;
                Assert.That(loadout, Is.Not.Null);
                var guideBuildings = new[] { "Ratusha", "Mine", "Cannon Turret", "Pulse" };
                foreach (string name in guideBuildings)
                {
                    var option = loadout.options.SingleOrDefault(entry => entry?.displayName == name);
                    Assert.That(option, Is.Not.Null, name);
                    Assert.That(option.icon, Is.Not.Null, name);
                }
                var root = view.GetComponent<UIDocument>().visualTreeAsset.CloneTree();
                Assert.That(root.Q<Button>("repeatHints"), Is.Not.Null);
                Assert.That(root.Q<Button>("guideButton"), Is.Not.Null);
                Assert.That(root.Q<Button>("guideBack"), Is.Not.Null);
                Assert.That(root.Q<ScrollView>("guideEntries"), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void MenuKeepsLevelAndSandboxEntryData()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePaths[0]);
            try
            {
                var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenu>(true)).Single();
                var data = new SerializedObject(menu);
                var levels = data.FindProperty("levels");
                Assert.That(levels.arraySize, Is.EqualTo(3));
                for (int i = 0; i < levels.arraySize; i++)
                    Assert.That(levels.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                var lab = data.FindProperty("gameplayLabLevel").objectReferenceValue as LevelDefinition;
                Assert.That(lab, Is.Not.Null);
                Assert.That(lab.progressionId, Is.EqualTo("gameplay-lab"));
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePaths[2]), Is.True);
                var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Tower-Defense/UI/Toolkit/Menu.uxml");
                Assert.That(tree.CloneTree().Q<Button>("labButton"), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
