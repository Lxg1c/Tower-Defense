using NUnit.Framework;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class ScreenSafeAreaTests
    {
        [Test]
        public void LayoutIsAppliedBeforePlayAndRestoredAfterReenable()
        {
            var root = new GameObject("Safe area preview", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.offsetMin = new Vector2(35, 20);
                rect.offsetMax = new Vector2(-35, -20);
                var safeArea = root.AddComponent<ScreenSafeArea>();

                Assert.That(Application.isPlaying, Is.False);
                Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero), "Preview must run before Play.");
                Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
                var min = rect.anchorMin;
                var max = rect.anchorMax;
                var size = new Vector2(Screen.width, Screen.height);
                var actual = Rect.MinMaxRect(min.x * size.x, min.y * size.y,
                    max.x * size.x, max.y * size.y);
                Assert.That(actual.xMin, Is.EqualTo(Screen.safeArea.xMin).Within(0.01f));
                Assert.That(actual.xMax, Is.EqualTo(Screen.safeArea.xMax).Within(0.01f));
                Assert.That(actual.yMin, Is.EqualTo(Screen.safeArea.yMin).Within(0.01f));
                Assert.That(actual.yMax, Is.EqualTo(Screen.safeArea.yMax).Within(0.01f));

                safeArea.enabled = false;
                rect.anchorMin = Vector2.one * 0.25f;
                rect.anchorMax = Vector2.one * 0.75f;
                safeArea.enabled = true;
                Assert.That(rect.anchorMin, Is.EqualTo(min));
                Assert.That(rect.anchorMax, Is.EqualTo(max));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
