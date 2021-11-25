using System;
using JetBrains.Annotations;
using Unity.Properties;

namespace Unity.Entities.Editor
{
    readonly struct SystemContentDisplay
    {
        SystemContent Content { get; }

        [CreateProperty, UsedImplicitly]
        Header Header { get; }

        [CreateProperty, TabView("SystemInspector"), UsedImplicitly]
        ITabContent[] Tabs { get; }

        public SystemContentDisplay(SystemContent content)
        {
            Content = content;
            var systemName = string.Empty;
            if (Content.System.Valid)
                systemName = Content.System.NicifiedDisplayName;

            Header = new Header(systemName, GetSystemIconStyle(Content.System));

            Tabs = new ITabContent[]
            {
                new SystemQueries(content.World, Content.System),
                new SystemRelationships(new SystemEntities(Content.World, Content.System), new SystemDependencies(Content.World, Content.System))
            };
        }

        static string GetSystemIconStyle(SystemProxy system)
        {
            var flags = system.Valid ? system.Category : 0;

            if ((flags & SystemCategory.ECBSystemBegin) != 0)
                return UssClasses.Content.SystemInspector.SystemIcons.EcbBeginIconBig;

            if ((flags & SystemCategory.ECBSystemEnd) != 0)
                return UssClasses.Content.SystemInspector.SystemIcons.EcbEndIconBig;

            if ((flags & SystemCategory.SystemGroup) != 0)
                return UssClasses.Content.SystemInspector.SystemIcons.GroupIconBig;

            // TODO: need to update with unmanaged system icon.
            if ((flags & SystemCategory.Unmanaged) != 0)
                return UssClasses.Content.SystemInspector.SystemIcons.SystemIconBig;

            return UssClasses.Content.SystemInspector.SystemIcons.SystemIconBig;
        }
    }
}
