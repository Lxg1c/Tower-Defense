using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class WallBuildRevealTests
    {
        [UnityTest]
        public IEnumerator WallSectionsRiseFromLeftToRightWhileBlockingTheFullSpan()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            var root = new GameObject("WallBuildRevealFixture");
            try
            {
                var model = new GameObject("Section").transform;
                model.SetParent(root.transform, false);
                var body = root.AddComponent<BoxCollider>();
                var obstacle = root.AddComponent<NavMeshObstacle>();
                var wall = root.AddComponent<WallSpan>();
                SetField(wall, "model", model);
                SetField(wall, "body", body);
                SetField(wall, "obstacle", obstacle);
                SetField(wall, "oneCellVisualWidth", 1f);

                wall.Configure(3, 1f);
                Vector3 fullScale = model.localScale;
                wall.PlayBuildReveal();
                Transform last = root.transform.Find("Wall segment 3");
                Assert.That(last, Is.Not.Null);
                Assert.That(model.localScale.y, Is.LessThan(fullScale.y));
                Assert.That(last.localScale.y, Is.LessThan(fullScale.y));
                Assert.That(body.size.x, Is.EqualTo(3f).Within(0.001f));
                Assert.That(obstacle.size.x, Is.EqualTo(3f).Within(0.001f));

                yield return new WaitForSeconds(0.1f);
                Assert.That(model.localScale.y, Is.GreaterThan(last.localScale.y));

                yield return new WaitForSeconds(1f);
                for (int i = 0; i < 3; i++)
                {
                    Transform section = i == 0 ? model : root.transform.Find($"Wall segment {i + 1}");
                    Assert.That(section.localScale, Is.EqualTo(fullScale));
                }
            }
            finally
            {
                Object.Destroy(root);
                Time.timeScale = originalTimeScale;
            }
        }

        private static void SetField(WallSpan target, string name, object value)
        {
            typeof(WallSpan).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);
        }
    }
}
