using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TowerDefense.Tests
{
    public class LevelSwipePagerTests
    {
        GameObject root, enemy;
        MainMenu menu;
        LevelSwipePager pager;
        RectTransform card;
        Button start;
        TMP_Text title, description;
        EventSystem events;
        LevelDefinition[] levels;
        EnemyOption enemyOption;
        static void Set(object owner, string name, object value) => owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        GameObject UI(string name) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(root.transform, false); return go; }
        TMP_Text Label(string name) => UI(name).AddComponent<TextMeshProUGUI>();
        Button Button(string name) { var go = UI(name); var image = go.AddComponent<Image>(); var button = go.AddComponent<Button>(); button.targetGraphic = image; return button; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = new GameObject("Pager fixture", typeof(RectTransform));
            root.SetActive(false);
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            events = root.AddComponent<EventSystem>();
            enemy = new GameObject("Inactive enemy"); enemy.SetActive(false);
            enemyOption = ScriptableObject.CreateInstance<EnemyOption>();
            enemyOption.prefab = enemy.AddComponent<MobCore>(); enemyOption.displayName = "Spider";
            levels = new LevelDefinition[3];
            for (int i = 0; i < 2; i++)
            {
                levels[i] = ScriptableObject.CreateInstance<LevelDefinition>();
                levels[i].displayName = $"Level {i + 1}";
                levels[i].description = $"Description {i + 1}";
                levels[i].waves = new[] { new LevelDefinition.LevelWave { spawnGroups = new[] {
                    new LevelDefinition.LevelSpawnGroup { entries = new[] {
                        new LevelDefinition.LevelEnemyEntry { enemy = enemyOption, count = i + 1 } } } } } };
            }
            menu = root.AddComponent<MainMenu>();
            var template = Button("Page template");
            Label("Page label").transform.SetParent(template.transform, false);
            title = Label("Title"); description = Label("Description"); start = Button("Start");
            Set(menu, "levels", levels); Set(menu, "levelButtonTemplate", template);
            Set(menu, "levelButtonContainer", UI("Pages").transform);
            Set(menu, "levelLabel", title); Set(menu, "levelDescription", description);
            Set(menu, "wavePreview", Label("Preview")); Set(menu, "startButton", start);
            var view = UI("Viewport"); card = (RectTransform)UI("Card").transform;
            card.SetParent(view.transform, false); card.sizeDelta = new Vector2(800, 500);
            pager = view.AddComponent<LevelSwipePager>();
            Set(pager, "menu", menu); Set(pager, "card", card);
            Set(pager, "previousButton", Button("Previous")); Set(pager, "nextButton", Button("Next"));
            root.SetActive(true);
            yield return null;
        }

        void Swipe(Vector2 delta)
        {
            var data = new PointerEventData(events) { position = new Vector2(600, 300), pointerId = 0 };
            pager.OnBeginDrag(data); data.position += delta; pager.OnDrag(data); pager.OnEndDrag(data);
        }

        [UnityTest]
        public IEnumerator HorizontalSwipesUpdateLevelDetailsInBothDirections()
        {
            Swipe(new Vector2(-300, 0));
            Assert.That(menu.SelectedIndex, Is.EqualTo(1));
            Assert.That(title.text, Does.StartWith("Level 2"));
            var pages = (Transform)typeof(MainMenu).GetField("levelButtonContainer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            Assert.That(pages.GetChild(1).GetComponent<Button>().targetGraphic.color,
                Is.Not.EqualTo(pages.GetChild(0).GetComponent<Button>().targetGraphic.color));
            Assert.That(pages.GetChild(1).GetComponent<Button>().interactable, Is.True);
            Assert.That(start.interactable, Is.True);
            Swipe(new Vector2(300, 0));
            Assert.That(menu.SelectedIndex, Is.Zero);
            Assert.That(description.text, Is.EqualTo("Description 1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShortAndVerticalGesturesDoNotSwitchAndEdgesDoNotWrap()
        {
            Swipe(new Vector2(-30, 0));
            Swipe(new Vector2(-150, 300));
            Swipe(new Vector2(300, 0));
            pager.Previous();
            Assert.That(menu.SelectedIndex, Is.Zero);
            pager.Next(); pager.Next(); pager.Next();
            Assert.That(menu.SelectedIndex, Is.EqualTo(2));
            Assert.That(start.interactable, Is.False, "An unconfigured page cannot launch an old level.");
            Assert.That(description.text, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CardSettlesEvenWhenGameTimeIsPaused()
        {
            Time.timeScale = 0;
            Swipe(new Vector2(-300, 0));
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(menu.SelectedIndex, Is.EqualTo(1));
            Assert.That(Mathf.Abs(card.anchoredPosition.x), Is.LessThan(2f));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            Object.Destroy(root); Object.Destroy(enemy); Object.Destroy(enemyOption);
            foreach (var level in levels) if (level != null) Object.Destroy(level);
            RunSelection.Clear();
            yield return null;
        }
    }
}
