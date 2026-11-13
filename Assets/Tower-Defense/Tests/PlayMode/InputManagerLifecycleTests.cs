using NUnit.Framework;

namespace TowerDefense.Tests
{
    public sealed class InputManagerLifecycleTests
    {
        [Test]
        public void DisablingManagerReleasesBothMapsAndRestoresOnlyTheSelectedMap()
        {
            InputManager.Init();
            var manager = InputManager.Instance;
            var actions = manager.actions;
            bool playerWasEnabled = actions.Player.enabled;
            bool uiWasEnabled = actions.UI.enabled;
            try
            {
                manager.ChangeInputMap(InputManager.InputType.UI);
                manager.enabled = false;
                Assert.That(actions.Player.enabled, Is.False);
                Assert.That(actions.UI.enabled, Is.False);

                manager.enabled = true;
                Assert.That(manager.actions, Is.SameAs(actions));
                Assert.That(actions.Player.enabled, Is.False);
                Assert.That(actions.UI.enabled, Is.True);
            }
            finally
            {
                manager.enabled = true;
                if (playerWasEnabled) actions.Player.Enable();
                else actions.Player.Disable();
                if (uiWasEnabled) actions.UI.Enable();
                else actions.UI.Disable();
            }
        }
    }
}
