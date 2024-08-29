using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyriaLeaderboard.Requests
{
    public enum Platforms
    {
        None
        , Guest
        , WhiteLabel
    }

    public class CurrentPlatform
    {
        static CurrentPlatform()
        {
            if (PlatformStrings.Length != Enum.GetNames(typeof(Platforms)).Length)
            {
                throw new ArrayTypeMismatchException($"A Platform is missing a string representation, {PlatformStrings.Length} vs {Enum.GetNames(typeof(Platforms)).Length}");
            }

            if (PlatformFriendlyStrings.Length != Enum.GetNames(typeof(Platforms)).Length)
            {
                throw new ArrayTypeMismatchException($"A Platform is missing a friendly name, {PlatformFriendlyStrings.Length} vs {Enum.GetNames(typeof(Platforms)).Length}");
            }
        }

        private static readonly string[] PlatformStrings = new[]
        {
            "" // None
            ,"guest" // Guest
            ,"white_label_login" // WhiteLabel
        };

        private static readonly string[] PlatformFriendlyStrings = new[]
        {
            "None" // None
            ,"Guest" // Guest
            ,"White Label" // WhiteLabel
        };

        public struct PlatformRepresentation
        {
            public Platforms Platform { get; set; }
            public string PlatformString { get; set; }
            public string PlatformFriendlyString { get; set; }
        }

        private static PlatformRepresentation current;

        public override string ToString()
        {
            return current.PlatformString;
        }

        public static Platforms Get()
        {
            return current.Platform;
        }

        public static string GetString()
        {
            return current.PlatformString;
        }

        public static string GetFriendlyString()
        {
            return current.PlatformFriendlyString;
        }

        public static void Set(Platforms platform)
        {
            PlayerPrefs.SetInt("LastActivePlatform", (int)platform);
            current = GetPlatformRepresentation(platform);
        }

        public static void Reset()
        {
            current = GetPlatformRepresentation(Platforms.None);
        }

        public static PlatformRepresentation GetPlatformRepresentation(Platforms platform)
        {
            return new PlatformRepresentation
            {
                Platform = platform,
                PlatformString = PlatformStrings[(int)platform],
                PlatformFriendlyString = PlatformFriendlyStrings[(int)platform]
            };
        }


#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            Reset();
        }
#endif
    }
}