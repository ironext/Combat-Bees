using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Properties.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemDependencies
    {
        readonly World m_World;
        SystemProxy m_System;
        readonly List<SystemDependencyViewData> m_SystemDependencyViewDataList;

        public SystemDependencies(World world, SystemProxy system)
        {
            m_World = world;
            m_System = system;
            m_SystemDependencyViewDataList = new List<SystemDependencyViewData>();
        }

        public List<SystemDependencyViewData> GetSystemDependencyViewDataList()
        {
            m_SystemDependencyViewDataList.Clear();

            if (m_World == null || !m_World.IsCreated || !m_System.Valid)
                return m_SystemDependencyViewDataList;

            foreach (var after in m_System.UpdateAfterSet)
                m_SystemDependencyViewDataList.Add(new SystemDependencyViewData(after,
                    after.NicifiedDisplayName, L10n.Tr("Before")));

            m_SystemDependencyViewDataList.Add(new SystemDependencyViewData(m_System,
                m_System.NicifiedDisplayName, L10n.Tr("Selected")));

            foreach (var before in m_System.UpdateBeforeSet)
                m_SystemDependencyViewDataList.Add(new SystemDependencyViewData(before,
                    before.NicifiedDisplayName, L10n.Tr("After")));

            return m_SystemDependencyViewDataList;
        }
    }

    [UsedImplicitly]
    class SystemDependenciesInspector : Inspector<SystemDependencies>
    {
        static readonly string k_SystemDependenciesSection = L10n.Tr("Scheduling Dependencies");

        public override VisualElement Build()
        {
            var systemDependencyViewDataList = Target.GetSystemDependencyViewDataList();
            var sectionElement = new FoldoutWithoutActionButton
            {
                HeaderName = { text = k_SystemDependenciesSection },
                MatchingCount = { text = systemDependencyViewDataList.Count.ToString() }
            };

            foreach (var systemDependencyInfo in systemDependencyViewDataList)
            {
                sectionElement.Add(new SystemDependencyView(systemDependencyInfo));
            }

            return sectionElement;
        }
    }
}
