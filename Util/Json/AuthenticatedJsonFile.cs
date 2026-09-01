using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace UnityCommonEx
{
    public static class AuthenticatedJsonFile
    {
        private const int CurrentFormatVersion = 1;
        private const string AuthenticationDomain = "UnityCommonEx.AuthenticatedJsonFile";
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        private sealed class Envelope
        {
            [JsonProperty]
            public int FormatVersion { get; set; }

            [JsonProperty]
            public string Context { get; set; }

            [JsonProperty]
            public string Payload { get; set; }

            [JsonProperty]
            public string Mac { get; set; }
        }

        public static T Read<T>(
            string path,
            byte[] key,
            string context,
            JsonReadFailureMode failureMode = JsonReadFailureMode.Error)
        {
            try
            {
                ValidateArguments(path, key, context);

                Envelope envelope = JsonUtil.ReadRaw<Envelope>(File.ReadAllText(path));
                if (envelope == null ||
                    envelope.FormatVersion != CurrentFormatVersion ||
                    !string.Equals(envelope.Context, context, StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(envelope.Payload) ||
                    string.IsNullOrEmpty(envelope.Mac))
                {
                    throw new InvalidDataException("The authenticated JSON envelope is invalid.");
                }

                byte[] actualMac;
                try
                {
                    actualMac = Convert.FromBase64String(envelope.Mac);
                }
                catch (FormatException exception)
                {
                    throw new InvalidDataException("The authenticated JSON MAC is malformed.", exception);
                }

                byte[] expectedMac = ComputeMac(key, context, envelope.Payload);
                bool isValid = FixedTimeEquals(expectedMac, actualMac);
                Array.Clear(expectedMac, 0, expectedMac.Length);
                Array.Clear(actualMac, 0, actualMac.Length);
                if (!isValid)
                    throw new InvalidDataException("The authenticated JSON MAC does not match the payload.");

                return JsonUtil.ReadRaw<T>(envelope.Payload);
            }
            catch (Exception exception)
            {
                LogReadFailure<T>(path, exception, failureMode);
                return default;
            }
        }

        public static void Write<T>(string path, T value, byte[] key, string context)
        {
            ValidateArguments(path, key, context);

            string payload = JsonUtil.WriteRaw(value);
            byte[] mac = ComputeMac(key, context, payload);
            var envelope = new Envelope
            {
                FormatVersion = CurrentFormatVersion,
                Context = context,
                Payload = payload,
                Mac = Convert.ToBase64String(mac),
            };
            Array.Clear(mac, 0, mac.Length);

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtil.WriteRaw(envelope), Utf8);
            ReplaceFile(temporaryPath, path);
        }

        private static byte[] ComputeMac(byte[] key, string context, string payload)
        {
            string authenticatedContent = string.Concat(
                AuthenticationDomain,
                "\0",
                CurrentFormatVersion.ToString(),
                "\0",
                context,
                "\0",
                payload);
            byte[] contentBytes = Utf8.GetBytes(authenticatedContent);
            try
            {
                using (var hmac = new HMACSHA256(key))
                    return hmac.ComputeHash(contentBytes);
            }
            finally
            {
                Array.Clear(contentBytes, 0, contentBytes.Length);
            }
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length)
                return false;

            int difference = 0;
            for (int i = 0; i < expected.Length; i++)
                difference |= expected[i] ^ actual[i];
            return difference == 0;
        }

        private static void ReplaceFile(string temporaryPath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                File.Move(temporaryPath, destinationPath);
                return;
            }

            try
            {
                File.Replace(temporaryPath, destinationPath, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(temporaryPath, destinationPath, true);
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                File.Copy(temporaryPath, destinationPath, true);
                File.Delete(temporaryPath);
            }
        }

        private static void ValidateArguments(string path, byte[] key, string context)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("A save path is required.", nameof(path));
            if (key == null || key.Length < 32)
                throw new ArgumentException("The authentication key must contain at least 32 bytes.", nameof(key));
            if (string.IsNullOrEmpty(context))
                throw new ArgumentException("An authentication context is required.", nameof(context));
        }

        private static void LogReadFailure<T>(
            string path,
            Exception exception,
            JsonReadFailureMode failureMode)
        {
            const string format = "AuthenticatedJsonFile.Read failed for {0} at '{1}': {2}";
            if (failureMode == JsonReadFailureMode.Warning)
            {
                LogUtil.Warn(format, typeof(T).Name, path, exception.Message);
                return;
            }

            LogUtil.Error(format, typeof(T).Name, path, exception.Message);
        }
    }
}
