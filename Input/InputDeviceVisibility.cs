using UnityEngine;

namespace UnityCommonEx
{
    [DisallowMultipleComponent]
    public sealed class InputDeviceVisibility : MonoBehaviour
    {
        public GameObject[] GamepadObjects;
        public GameObject[] MouseKeyboardObjects;

        private void OnEnable()
        {
            InputDeviceState.UsingGamepadChanged += Refresh;
            Refresh(InputDeviceState.UsingGamepad);
        }

        private void OnDisable() => InputDeviceState.UsingGamepadChanged -= Refresh;

        private void Refresh(bool gamepad)
        {
            SetVisible(GamepadObjects, gamepad);
            SetVisible(MouseKeyboardObjects, !gamepad);
        }

        private void SetVisible(GameObject[] objects, bool visible)
        {
            if (objects == null) return;
            foreach (var target in objects)
                if (target != null && target != gameObject) target.SetActive(visible);
        }
    }
}
