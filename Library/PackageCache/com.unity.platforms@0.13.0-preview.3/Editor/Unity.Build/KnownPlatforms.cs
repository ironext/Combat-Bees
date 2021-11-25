using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Build
{
    /// <summary>
    /// Contains constants for known platforms.
    /// </summary>
    public static class KnownPlatforms
    {
        /// <summary>
        /// Constant platform information for Windows platform.
        /// </summary>
        public static class Windows
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "win";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Windows";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = "com.unity.platforms.windows";

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Standalone.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for macOS platform.
        /// </summary>
        public static class macOS
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "macos";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "macOS";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = "com.unity.platforms.macos";

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Standalone.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Linux platform.
        /// </summary>
        public static class Linux
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "linux";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Linux";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = "com.unity.platforms.linux";

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Standalone.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for iOS platform.
        /// </summary>
        public static class iOS
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "ios";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "iOS";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = "com.unity.platforms.ios";

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.iPhone.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Android platform.
        /// </summary>
        public static class Android
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "android";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Android";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = "com.unity.platforms.android";

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Android.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Web platform.
        /// </summary>
        public static class Web
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "web";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Web";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = "com.unity.platforms.web";

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.WebGL.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Universal Windows Platform (UWP) platform.
        /// </summary>
        public static class UniversalWindowsPlatform
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "uwp";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Universal Windows Platform";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Metro.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for PlayStation 4 platform.
        /// </summary>
        public static class PlayStation4
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "ps4";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "PlayStation 4";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.PS4.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Xbox One platform.
        /// </summary>
        public static class XboxOne
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "xb1";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Xbox One";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.XboxOne.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for tvOS platform.
        /// </summary>
        public static class tvOS
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "tvos";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "tvOS";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.tvOS.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Switch platform.
        /// </summary>
        public static class Switch
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "switch";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Switch";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Switch.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Stadia platform.
        /// </summary>
        public static class Stadia
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "stadia";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Stadia";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Stadia.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        /// <summary>
        /// Constant platform information for Lumin platform.
        /// </summary>
        public static class Lumin
        {
            /// <summary>
            /// Platform name, used for serialization.
            /// </summary>
            public const string Name = "lumin";

            /// <summary>
            /// Platform display name, used for display.
            /// </summary>
            public const string DisplayName = "Lumin";

            /// <summary>
            /// Platform package name.
            /// </summary>
            public const string PackageId = null;

            /// <summary>
            /// Platform icon name.
            /// </summary>
            public const string IconPath = "Icons/BuildSettings.Lumin.png";

            /// <summary>
            /// Attempt to retrieve the platform using known information.
            /// </summary>
            /// <returns>The platform instance if found, <see langword="null"/> otherwise.</returns>
            public static Platform GetPlatform() => Platform.GetPlatformByName(Name);

            internal static PlatformInfo PlatformInfo { get; } = new PlatformInfo(Name, DisplayName, PackageId, IconPath);
        }

        // Keep these lists updated when adding a new platform
        static readonly PlatformInfo[] s_AllPlatforms = new[]
        {
            Windows.PlatformInfo,
            macOS.PlatformInfo,
            Linux.PlatformInfo,
            iOS.PlatformInfo,
            Android.PlatformInfo,
            Web.PlatformInfo,
            UniversalWindowsPlatform.PlatformInfo,
            PlayStation4.PlatformInfo,
            XboxOne.PlatformInfo,
            tvOS.PlatformInfo,
            Switch.PlatformInfo,
            Stadia.PlatformInfo,
            Lumin.PlatformInfo,
        };

        static readonly PlatformInfo[] s_PublicPlatforms = new[]
        {
            Windows.PlatformInfo,
            macOS.PlatformInfo,
            Linux.PlatformInfo,
            iOS.PlatformInfo,
            Android.PlatformInfo,
            Web.PlatformInfo,
            UniversalWindowsPlatform.PlatformInfo,
            tvOS.PlatformInfo,
            Lumin.PlatformInfo,
        };

        static KnownPlatforms()
        {
            // Verify known platforms have unique names
            var knownPlatformsByName = new Dictionary<string, PlatformInfo>();
            foreach (var platform in s_AllPlatforms)
            {
                if (knownPlatformsByName.TryGetValue(platform.Name, out var registeredPlatform))
                    throw new InvalidOperationException($"Duplicate platform info found. Platform info with name '{platform.Name}' already registered.");

                knownPlatformsByName.Add(platform.Name, platform);
            }
        }

        internal static PlatformInfo GetPlatformInfo(string name)
        {
            return s_AllPlatforms.FirstOrDefault(info => info.Name == name);
        }

        internal static bool IsKnownPlatform(string name)
        {
            return s_AllPlatforms.Any(info => info.Name == name);
        }

        internal static bool IsPublicPlatform(string name)
        {
            return s_PublicPlatforms.Any(info => info.Name == name);
        }
    }
}
