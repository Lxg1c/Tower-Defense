using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerDefense.Tests
{
    public sealed class UiNavigationInputTests : InputTestFixture
    {
        [Test]
        public void GenericJoystickCannotNavigateTheUi()
        {
            var joystick = InputSystem.AddDevice<Joystick>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            using var actions = new InputSystem_Actions();
            actions.UI.Enable();

            Set(joystick.stick, Vector2.up);
            Assert.That(actions.UI.Navigate.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
            Assert.That(actions.UI.Navigate.triggered, Is.False);

            Press(keyboard.upArrowKey);
            Assert.That(actions.UI.Navigate.ReadValue<Vector2>(), Is.EqualTo(Vector2.up));
            Press(mouse.leftButton);
            Assert.That(actions.UI.Click.ReadValue<float>(), Is.EqualTo(1f));
        }
    }

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
