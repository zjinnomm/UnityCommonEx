using System;
using Newtonsoft.Json;
using UnityEngine.InputSystem.LowLevel;

namespace UnityCommonEx
{
    public sealed class GamepadButtonJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(GamepadButton);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String ||
                !Enum.TryParse(reader.Value?.ToString(), true, out GamepadButton button) ||
                InputDeviceState.GetButtonPath(button) == null)
                throw new JsonSerializationException("Unknown controller button: " + reader.Value);
            return button;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            string path = InputDeviceState.GetButtonPath((GamepadButton)value);
            if (path == null) throw new JsonSerializationException("Unknown controller button: " + value);
            string name = path.StartsWith("button") ? path.Substring(6)
                : path.StartsWith("dpad/") ? "Dpad" + char.ToUpperInvariant(path[5]) + path.Substring(6)
                : path.EndsWith("Press") ? path.Substring(0, path.Length - 5) : path;
            writer.WriteValue(char.ToUpperInvariant(name[0]) + name.Substring(1));
        }
    }
}
