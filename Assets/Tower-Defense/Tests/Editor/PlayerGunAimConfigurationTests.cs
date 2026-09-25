using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class PlayerGunAimConfigurationTests
    {
        [Test]
        public void PlayerGunsAndMuzzlesAreConnectedToTheAnimatedRig()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tower-Defense/Prefabs/Player.prefab");
            var aim = prefab.GetComponent<PlayerGunAim>();
            Assert.That(aim, Is.Not.Null);
            var so = new SerializedObject(aim);
            var first = (Transform)so.FindProperty("firstGun").objectReferenceValue;
            var second = (Transform)so.FindProperty("secondGun").objectReferenceValue;
            var head = (Transform)so.FindProperty("head").objectReferenceValue;
            Assert.That(head, Is.Not.Null);
            Assert.That(head.name, Is.EqualTo("head"));
            Assert.That(head.IsChildOf(first), Is.False);
            Assert.That(head.IsChildOf(second), Is.False);
            var shooter = (Shooter)so.FindProperty("shooter").objectReferenceValue;
            Assert.That(first.name, Is.EqualTo("main_gun"));
            Assert.That(second.name, Is.EqualTo("sec_gun"));
            Assert.That(first.IsChildOf(prefab.GetComponentInChildren<Animator>().transform), Is.True);
            Assert.That(second.IsChildOf(prefab.GetComponentInChildren<Animator>().transform), Is.True);
            var muzzles = new SerializedObject(shooter).FindProperty("firePoints");
            Assert.That(muzzles.arraySize, Is.EqualTo(2));
            for (int i = 0; i < muzzles.arraySize; i++)
                Assert.That(((Transform)muzzles.GetArrayElementAtIndex(i).objectReferenceValue).IsChildOf(second), Is.True);
            var ultimate = new SerializedObject(prefab.GetComponent<PlayerUltimate>());
            Assert.That(((Transform)ultimate.FindProperty("firePoint").objectReferenceValue).IsChildOf(first), Is.True);
        }
    }
}
