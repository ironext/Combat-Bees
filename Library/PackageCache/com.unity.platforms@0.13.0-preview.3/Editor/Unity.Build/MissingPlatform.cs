using System;

namespace Unity.Build
{
    /// <summary>
    /// Describes a platform for which the type could not be resolved.
    /// </summary>
    sealed class MissingPlatform : Platform, ICloneable
    {
        public MissingPlatform(string name) : base(KnownPlatforms.GetPlatformInfo(name) ?? new PlatformInfo(name, name, null, null)) { }
        MissingPlatform(PlatformInfo info) : base(info) { }
        public object Clone() => new MissingPlatform(new PlatformInfo(Name, DisplayName, PackageId, IconPath));
    }
}
