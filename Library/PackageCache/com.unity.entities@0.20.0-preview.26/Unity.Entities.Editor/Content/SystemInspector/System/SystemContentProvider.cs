using JetBrains.Annotations;
using Unity.Properties.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemContentProvider : ContentProvider
    {
        [CreateProperty, HideInInspector] string m_TypeName;

        // Unity.Entities.World is not serializable by default, so we use the world's name to find it again. This is
        // clearly not enough to guarantee that we can survive domain reload, but it should cover most cases.
        [CreateProperty, HideInInspector] string m_WorldName;

        SystemProxy m_System;
        World m_World;
        public SystemGraph LocalSystemGraph { get; set; }

        public SystemProxy System
        {
            get => m_System;
            set
            {
                if (value == m_System)
                    return;

                m_System = value;

                m_TypeName = m_System != default
                    ? m_System.TypeName
                    : default;
            }
        }

        public World World
        {
            get => m_World;
            set
            {
                if (value == m_World)
                    return;

                m_World = value;
                m_WorldName = null != m_World
                    ? m_World.Name
                    : string.Empty;
            }
        }

        protected override ContentStatus GetStatus()
        {
            if (System.Valid && null != World && World.IsCreated)
                return ContentStatus.ContentReady;

            if (null != World && !World.IsCreated)
            {
                World = null;
                return ContentStatus.ReloadContent;
            }

            // If either of those are null or empty, we won't be able to recover previous state.
            if (string.IsNullOrEmpty(m_TypeName) || string.IsNullOrEmpty(m_WorldName) || LocalSystemGraph == null)
                return ContentStatus.ContentUnavailable;

            if (!System.Valid)
            {
                // Try to load the type from it's remember type name. If it fails, we likely won't be able to recover previous state.
                foreach (var system in LocalSystemGraph.AllSystems)
                {
                    if (system.TypeName == m_TypeName)
                        System = system;
                }
                if (!System.Valid)
                    return ContentStatus.ContentUnavailable;
            }

            // Worlds are lazily created, so we'll spin until we can find it again.
            World = ContentUtilities.FindLastWorld(m_WorldName);

            return null != World
                ? ContentStatus.ContentReady
                : ContentStatus.ContentNotReady;
        }

        public override string Name { get; } = "System";
        public override object GetContent()
        {
            return this;
        }
    }

    [UsedImplicitly]
    class SystemContentInspector : Inspector<SystemContentProvider>
    {
        public override VisualElement Build()
        {
            var element = new PropertyElement();
            Resources.AddCommonVariables(element);

            Resources.Templates.ContentProvider.System.AddStyles(element);
            element.AddToClassList(UssClasses.Content.SystemInspector.SystemContainer);

            var content = new SystemContent(Target.World, Target.System);
            element.SetTarget(new SystemContentDisplay(content));
            return element;
        }
    }
}
