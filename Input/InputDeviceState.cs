using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace UnityCommonEx
{
    public static class InputDeviceState
    {
        public static Gamepad Gamepad { get; private set; }
        public static bool UsingGamepad { get; private set; }
        public static event Action<bool> UsingGamepadChanged;
        public static int Revision { get; private set; }
        public static bool SuppressGamepadActions { get; set; }
        public static readonly GamepadButton[] Buttons =
        {
            GamepadButton.South, GamepadButton.East, GamepadButton.West, GamepadButton.North,
            GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.LeftTrigger, GamepadButton.RightTrigger,
            GamepadButton.Start, GamepadButton.Select, GamepadButton.LeftStick, GamepadButton.RightStick,
            GamepadButton.DpadUp, GamepadButton.DpadDown, GamepadButton.DpadLeft, GamepadButton.DpadRight
        };

        public static void Update(float deadzone)
        {
            bool gamepadInput = false;
            foreach (var pad in UnityEngine.InputSystem.Gamepad.all)
            {
                if (!pad.added || !pad.enabled) continue;
                bool active = pad.leftStick.ReadUnprocessedValue().magnitude > Mathf.Max(0.25f, deadzone)
                    || pad.rightStick.ReadUnprocessedValue().magnitude > Mathf.Max(0.25f, deadzone);
                foreach (var button in Buttons)
                    active |= pad[button].wasPressedThisFrame;
                if (!active) continue;
                if (Gamepad != pad)
                {
                    Gamepad?.ResetHaptics();
                    Revision++;
                }
                Gamepad = pad;
                gamepadInput = true;
            }
            if (Gamepad == null || !Gamepad.added || !Gamepad.enabled)
            {
                var previous = Gamepad;
                Gamepad = null;
                foreach (var pad in UnityEngine.InputSystem.Gamepad.all)
                    if (pad.added && pad.enabled) { Gamepad = pad; break; }
                if (previous != Gamepad) Revision++;
                if (Gamepad == null && UsingGamepad) SetUsingGamepad(false);
            }
            bool mouseInput = Mouse.current != null &&
                (Mouse.current.delta.ReadValue().sqrMagnitude > 4f
                 || Mouse.current.leftButton.wasPressedThisFrame
                 || Mouse.current.rightButton.wasPressedThisFrame
                 || Mouse.current.scroll.ReadValue().sqrMagnitude > 0f);
            bool keyboardInput = Keyboard.current?.anyKey.wasPressedThisFrame == true;
            if (gamepadInput) SetUsingGamepad(true);
            else if (mouseInput || keyboardInput) SetUsingGamepad(false);
        }

        private static void SetUsingGamepad(bool value)
        {
            if (UsingGamepad == value) return;
            UsingGamepad = value;
            Revision++;
            Cursor.visible = !value;
            UsingGamepadChanged?.Invoke(value);
        }

        public static ButtonControl GetButton(GamepadButton button)
        {
            return Gamepad == null || !Gamepad.added || !Gamepad.enabled || GetButtonPath(button) == null
                ? null : Gamepad[button];
        }

        public static string GetButtonPath(GamepadButton button)
        {
            switch (button)
            {
                case GamepadButton.South: return "buttonSouth";
                case GamepadButton.East: return "buttonEast";
                case GamepadButton.West: return "buttonWest";
                case GamepadButton.North: return "buttonNorth";
                case GamepadButton.LeftShoulder: return "leftShoulder";
                case GamepadButton.RightShoulder: return "rightShoulder";
                case GamepadButton.LeftTrigger: return "leftTrigger";
                case GamepadButton.RightTrigger: return "rightTrigger";
                case GamepadButton.Start: return "start";
                case GamepadButton.Select: return "select";
                case GamepadButton.LeftStick: return "leftStickPress";
                case GamepadButton.RightStick: return "rightStickPress";
                case GamepadButton.DpadUp: return "dpad/up";
                case GamepadButton.DpadDown: return "dpad/down";
                case GamepadButton.DpadLeft: return "dpad/left";
                case GamepadButton.DpadRight: return "dpad/right";
                default: return null;
            }
        }

        public static bool TryParseButtonPath(string path, out GamepadButton button)
        {
            foreach (var candidate in Buttons)
                if (GetButtonPath(candidate) == path) { button = candidate; return true; }
            button = default;
            return false;
        }

        public static Vector2 ReadStick(GamepadStick stick, float deadzone = 0f)
        {
            var control = GetStick(stick);
            if (control == null) return Vector2.zero;
            Vector2 value = control.ReadUnprocessedValue();
            float magnitude = value.magnitude;
            return magnitude > 0f ? value / magnitude * ApplyDeadzone(magnitude, deadzone) : Vector2.zero;
        }

        public static float ReadStickAxis(GamepadStick stick, GamepadStickAxis axis, float deadzone = 0f)
        {
            var control = GetStick(stick);
            if (control == null) return 0f;
            Vector2 value = control.ReadUnprocessedValue();
            return ApplyDeadzone(axis == GamepadStickAxis.Horizontal ? value.x : value.y, deadzone);
        }

        private static StickControl GetStick(GamepadStick stick)
        {
            if (Gamepad == null || !Gamepad.added || !Gamepad.enabled) return null;
            return stick == GamepadStick.Left ? Gamepad.leftStick : Gamepad.rightStick;
        }

        public static Vector2 ReadDpad() => Gamepad != null && Gamepad.added && Gamepad.enabled
            ? Gamepad.dpad.ReadValue() : Vector2.zero;

        public static string GetButtonLabel(GamepadButton button)
        {
            bool sony = GamepadFamily == "PlayStation";
            bool nintendo = GamepadFamily == "Switch";
            switch (button)
            {
                case GamepadButton.South: return sony ? "Cross" : nintendo ? "B" : "A";
                case GamepadButton.East: return sony ? "Circle" : nintendo ? "A" : "B";
                case GamepadButton.West: return sony ? "Square" : nintendo ? "Y" : "X";
                case GamepadButton.North: return sony ? "Triangle" : nintendo ? "X" : "Y";
                case GamepadButton.LeftShoulder: return sony ? "L1" : nintendo ? "L" : "LB";
                case GamepadButton.RightShoulder: return sony ? "R1" : nintendo ? "R" : "RB";
                case GamepadButton.LeftTrigger: return sony ? "L2" : nintendo ? "ZL" : "LT";
                case GamepadButton.RightTrigger: return sony ? "R2" : nintendo ? "ZR" : "RT";
                case GamepadButton.Start: return sony ? "Options" : "Menu";
                case GamepadButton.Select: return sony ? "Share" : "View";
                case GamepadButton.LeftStick: return "L3";
                case GamepadButton.RightStick: return "R3";
                case GamepadButton.DpadUp: return "D-Pad Up";
                case GamepadButton.DpadDown: return "D-Pad Down";
                case GamepadButton.DpadLeft: return "D-Pad Left";
                case GamepadButton.DpadRight: return "D-Pad Right";
                default: return button.ToString();
            }
        }

        public static string GamepadFamily
        {
            get
            {
                string layout = Gamepad?.layout ?? string.Empty;
                if (layout.Contains("DualShock") || layout.Contains("DualSense")) return "PlayStation";
                return layout.Contains("Switch") ? "Switch" : "Xbox";
            }
        }

        public static float ApplyDeadzone(float value, float deadzone)
        {
            deadzone = Mathf.Clamp(deadzone, 0f, 0.9f);
            return Mathf.Abs(value) <= deadzone ? 0f
                : Mathf.Sign(value) * Mathf.Clamp01((Mathf.Abs(value) - deadzone) / (1f - deadzone));
        }
    }
}
