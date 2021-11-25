using System;

namespace Unity.Entities.Editor
{
    readonly struct SystemDependencyViewData : IEquatable<SystemDependencyViewData>
    {
        // We need this SystemProxy in order to inspect the system in inspector and highlight it within Systems window.
        public readonly SystemProxy SystemProxy;
        public readonly string SystemName;
        public readonly string Content;

        public SystemDependencyViewData(SystemProxy systemProxy, string systemName, string content)
        {
            SystemProxy = systemProxy;
            SystemName = systemName;
            Content = content;
        }

        public bool Equals(SystemDependencyViewData other)
        {
            // We only need to check the system name and its content for the display, therefore no need to compare the
            // SystemProxy instance itself.
            return SystemName == other.SystemName && Content == other.Content;
        }
    }
}
