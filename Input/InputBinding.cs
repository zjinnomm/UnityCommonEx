using System;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityCommonEx
{
    [Serializable]
    public class InputBinding
    {
        private const float ClickInterval = 0.2f;
        private static readonly float[] LastMouseDownTimes = new float[3];

        [JsonProperty(Required = Required.Default)]
        public InputBindingDeviceType DeviceType;
        [JsonProperty(Required = Required.Default)]
        public KeyCode Key;
        [JsonProperty(Required = Required.Default)]
        public InputMouseButton MouseButton;
        [JsonProperty(Required = Required.Default)]
        public InputBindingTriggerType TriggerType;
        [JsonProperty(Required = Required.Default)]
        public bool Rebindable = true;

        public static InputBinding Keyboard(KeyCode key, InputBindingTriggerType triggerType)
        {
            return new InputBinding
            {
                DeviceType = InputBindingDeviceType.KeyboardKey,
                Key = key,
                TriggerType = triggerType
            };
        }

        public static InputBinding MouseButtonBinding(InputMouseButton mouseButton, InputBindingTriggerType triggerType)
        {
            return new InputBinding
            {
                DeviceType = InputBindingDeviceType.MouseButton,
                MouseButton = mouseButton,
                TriggerType = triggerType
            };
        }

        public static InputBinding MouseWheelUp()
        {
            return new InputBinding
            {
                DeviceType = InputBindingDeviceType.MouseWheelUp,
                TriggerType = InputBindingTriggerType.Pressed
            };
        }

        public static InputBinding MouseWheelDown()
        {
            return new InputBinding
            {
                DeviceType = InputBindingDeviceType.MouseWheelDown,
                TriggerType = InputBindingTriggerType.Pressed
            };
        }

        public InputBinding Clone()
        {
            return new InputBinding
            {
                DeviceType = DeviceType,
                Key = Key,
                MouseButton = MouseButton,
                TriggerType = TriggerType,
                Rebindable = Rebindable
            };
        }

        public string GetBindingKey()
        {
            switch (DeviceType)
            {
                case InputBindingDeviceType.KeyboardKey:
                    return "Keyboard:" + Key;
                case InputBindingDeviceType.MouseButton:
                    return "MouseButton:" + MouseButton;
                case InputBindingDeviceType.MouseWheelUp:
                    return "MouseWheel:Up";
                case InputBindingDeviceType.MouseWheelDown:
                    return "MouseWheel:Down";
                default:
                    return string.Empty;
            }
        }

        public string GetDisplayText()
        {
            switch (DeviceType)
            {
                case InputBindingDeviceType.KeyboardKey:
                    return Key.ToString();
                case InputBindingDeviceType.MouseButton:
                    return MouseButton.ToString();
                case InputBindingDeviceType.MouseWheelUp:
                    return "Wheel Up";
                case InputBindingDeviceType.MouseWheelDown:
                    return "Wheel Down";
                default:
                    return string.Empty;
            }
        }

        public bool MatchesPhysicalInput(InputBinding other)
        {
            return other != null && GetBindingKey() == other.GetBindingKey();
        }

        public bool MatchesExact(InputBinding other)
        {
            return other != null &&
                DeviceType == other.DeviceType &&
                Key == other.Key &&
                MouseButton == other.MouseButton &&
                TriggerType == other.TriggerType &&
                Rebindable == other.Rebindable;
        }

        public bool MatchesKeyboardKey(KeyCode key)
        {
            return DeviceType == InputBindingDeviceType.KeyboardKey && Key == key;
        }

        public bool MatchesMouseButton(InputMouseButton mouseButton)
        {
            return DeviceType == InputBindingDeviceType.MouseButton && MouseButton == mouseButton;
        }

        public bool IsTriggeredThisFrame()
        {
            switch (DeviceType)
            {
                case InputBindingDeviceType.KeyboardKey:
                    return EvaluateDigital(
                        Input.GetKeyDown(Key),
                        Input.GetKey(Key),
                        Input.GetKeyUp(Key),
                        Input.GetKeyDown(Key));
                case InputBindingDeviceType.MouseButton:
                    return EvaluateMouseButton();
                case InputBindingDeviceType.MouseWheelUp:
                    return Input.mouseScrollDelta.y > 0f;
                case InputBindingDeviceType.MouseWheelDown:
                    return Input.mouseScrollDelta.y < 0f;
                default:
                    return false;
            }
        }

        public bool IsActuated()
        {
            switch (DeviceType)
            {
                case InputBindingDeviceType.KeyboardKey:
                    return Input.GetKey(Key);
                case InputBindingDeviceType.MouseButton:
                    return Input.GetMouseButton((int)MouseButton);
                case InputBindingDeviceType.MouseWheelUp:
                    return Input.mouseScrollDelta.y > 0f;
                case InputBindingDeviceType.MouseWheelDown:
                    return Input.mouseScrollDelta.y < 0f;
                default:
                    return false;
            }
        }

        private bool EvaluateMouseButton()
        {
            int button = (int)MouseButton;
            bool pressed = Input.GetMouseButtonDown(button);
            bool pressing = Input.GetMouseButton(button);
            bool released = Input.GetMouseButtonUp(button);

            if (pressed && button >= 0 && button < LastMouseDownTimes.Length)
                LastMouseDownTimes[button] = Time.realtimeSinceStartup;

            bool clicked = released &&
                button >= 0 &&
                button < LastMouseDownTimes.Length &&
                Time.realtimeSinceStartup - LastMouseDownTimes[button] < ClickInterval;

            return EvaluateDigital(
                pressed,
                pressing,
                released,
                clicked);
        }

        private bool EvaluateDigital(bool pressed, bool pressing, bool released, bool clicked)
        {
            switch (TriggerType)
            {
                case InputBindingTriggerType.Pressed:
                    return pressed;
                case InputBindingTriggerType.Released:
                    return released;
                case InputBindingTriggerType.Clicked:
                    return clicked;
                case InputBindingTriggerType.Pressing:
                    return pressing;
                default:
                    return false;
            }
        }
    }
}
