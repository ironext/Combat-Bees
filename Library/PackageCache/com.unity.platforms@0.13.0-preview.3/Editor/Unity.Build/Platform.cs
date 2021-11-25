using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties.Editor;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Build
{
    /// <summary>
    /// Platform base class.
    /// </summary>
    public abstract partial class Platform : IEquatable<Platform>
    {
        static readonly Platform[] s_AvailablePlatforms;
        internal readonly PlatformInfo m_PlatformInfo;

        /// <summary>
        /// All available platforms.
        /// </summary>
        public static IEnumerable<Platform> AvailablePlatforms => s_AvailablePlatforms;

        /// <summary>
        /// Platform short name. Used by serialization.
        /// </summary>
        public string Name => m_PlatformInfo.Name;

        /// <summary>
        /// Platform display name. Used for displaying on user interface.
        /// </summary>
        public string DisplayName => m_PlatformInfo.DisplayName;

        /// <summary>
        /// Platform icon file path.
        /// </summary>
        public string IconPath => m_PlatformInfo.IconPath;

        /// <summary>
        /// Platform package identifier.
        /// </summary>
        public string PackageId => m_PlatformInfo.PackageId;

        /// <summary>
        /// Determine if the platform has a known package.
        /// </summary>
        public bool HasPackage => !string.IsNullOrEmpty(PackageId);

        /// <summary>
        /// Determine if the platform package is installed.
        /// </summary>
        public bool IsPackageInstalled => HasPackage ? PackageInfo.FindForAssetPath($"Packages/{PackageId}/package.json") != null : false;

        /// <summary>
        /// Determine if the platform is public, or closed (require license to use).
        /// </summary>
        public bool IsPublic => KnownPlatforms.IsPublicPlatform(Name);

        /// <summary>
        /// Start installation of platform package.
        /// </summary>
        public void InstallPackage()
        {
            if (!HasPackage || IsPackageInstalled)
                return;

            var request = UnityEditor.PackageManager.Client.Add(PackageId);
            if (request.Status == UnityEditor.PackageManager.StatusCode.Failure)
                Debug.LogError(request.Error.message);
            else
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Started installation of {0} platform package [{1}].", DisplayName, PackageId);
        }

        /// <summary>
        /// Get platform by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A <see cref="Platform"/> instance if found, <see langword="null"/> otherwise.</returns>
        public static Platform GetPlatformByName(string name)
        {
            var platform = s_AvailablePlatforms.FirstOrDefault(p => p.Name == name);

            // Check for platform former name.
            // Dot not change these values, they are only used to deserialize old build assets.
            // This list does not need to be updated when adding new platforms.
            if (platform == null)
            {
                name = name.ToLowerInvariant();
                if (name == "windows")
                    name = KnownPlatforms.Windows.Name;
                else if (name == "osx")
                    name = KnownPlatforms.macOS.Name;
                else if (name == "linux")
                    name = KnownPlatforms.Linux.Name;
                else if (name == "ios")
                    name = KnownPlatforms.iOS.Name;
                else if (name == "android")
                    name = KnownPlatforms.Android.Name;
                else if (name == "webgl")
                    name = KnownPlatforms.Web.Name;
                else if (name == "wsa")
                    name = KnownPlatforms.UniversalWindowsPlatform.Name;
                else if (name == "ps4")
                    name = KnownPlatforms.PlayStation4.Name;
                else if (name == "xboxone")
                    name = KnownPlatforms.XboxOne.Name;
                else if (name == "tvos")
                    name = KnownPlatforms.tvOS.Name;
                else if (name == "switch")
                    name = KnownPlatforms.Switch.Name;
                else if (name == "stadia")
                    name = KnownPlatforms.Stadia.Name;
                else if (name == "lumin")
                    name = KnownPlatforms.Lumin.Name;

                platform = s_AvailablePlatforms.FirstOrDefault(p => p.Name == name);
            }
            return platform;
        }

        static Platform()
        {
            var platforms = TypeCache.GetTypesDerivedFrom<Platform>()
                .Where(type => type != typeof(MissingPlatform))
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .Where(type => !type.HasAttribute<ObsoleteAttribute>())
                .Where(TypeConstruction.CanBeConstructed)
                .Select(TypeConstruction.Construct<Platform>);

            var platformsByName = new Dictionary<string, Platform>();
            foreach (var platform in platforms)
            {
                if (platformsByName.TryGetValue(platform.Name, out var registeredPlatform))
                    throw new InvalidOperationException($"Duplicate platform name found. Platform named '{platform.Name}' is already registered by class '{registeredPlatform.GetType().FullName}'.");

                platformsByName.Add(platform.Name, platform);
            }

            // Fill up missing platforms
            foreach (var buildTarget in Enum.GetValues(typeof(BuildTarget)).Cast<BuildTarget>())
            {
                if (buildTarget == BuildTarget.NoTarget ||
                    buildTarget == BuildTarget.StandaloneWindows)
                    continue;

                if (buildTarget.HasAttribute<ObsoleteAttribute>())
                    continue;

                var name = buildTarget.GetPlatformName();
                if (platformsByName.ContainsKey(name))
                    continue;

                platformsByName.Add(name, new MissingPlatform(name));
            }

            s_AvailablePlatforms = platformsByName.Values.ToArray();
        }

        internal Platform(PlatformInfo info)
        {
            m_PlatformInfo = info;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Platform);
        }

        public bool Equals(Platform other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (ReferenceEquals(null, other))
                return false;

            return m_PlatformInfo.Equals(other.m_PlatformInfo);
        }

        public static bool operator ==(Platform lhs, Platform rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Platform lhs, Platform rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_PlatformInfo.GetHashCode();
        }
    }
}
