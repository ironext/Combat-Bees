using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class ComponentRelationshipWorldView : FoldoutWithoutActionButton
    {
        static readonly string k_Title = L10n.Tr("Entities");
        static readonly string k_MoreLabel = L10n.Tr("More systems are matching this component. You can use the search to filter systems by name.");
        static readonly string k_MoreLabelWithFilter = L10n.Tr("More systems are matching this search. Refine the search terms to find a particular system.");

        ComponentRelationshipWorldViewData m_Data;

        readonly QueryWithEntitiesView m_EntitiesSection;
        readonly SystemQueriesListView m_SystemQueriesListView;

        public ComponentRelationshipWorldView(ComponentRelationshipWorldViewData data)
        {
            HeaderName.text = data.World.Name;
            m_Data = data;

            m_EntitiesSection = new QueryWithEntitiesView(data.QueryWithEntitiesViewData);
            var entitySectionName = m_EntitiesSection.Q<Label>(className: UssClasses.FoldoutWithActionButton.Name);
            entitySectionName.text = k_Title;
            entitySectionName.style.unityFontStyleAndWeight = FontStyle.Normal;
            m_EntitiesSection.Q<VisualElement>(className: UssClasses.FoldoutWithActionButton.Icon).Hide();

            m_SystemQueriesListView = new SystemQueriesListView(new List<SystemQueriesViewData>(), new List<string>(),k_MoreLabel, k_MoreLabelWithFilter);
            m_SystemQueriesListView.Q<FoldoutWithoutActionButton>().HeaderName.style.unityFontStyleAndWeight = FontStyle.Normal;
            m_SystemQueriesListView.Q<FoldoutWithoutActionButton>().MatchingCount.style.unityFontStyleAndWeight = FontStyle.Normal;

            Add(m_EntitiesSection);
            Add(m_SystemQueriesListView);
        }

        public void Update()
        {
            m_EntitiesSection.Update();
            m_Data.ComponentMatchingSystems.Update();
            m_SystemQueriesListView.Update(m_Data.ComponentMatchingSystems.Systems);
        }

        public bool IsEmpty => m_Data.QueryWithEntitiesViewData.TotalEntityCount == 0 && m_Data.ComponentMatchingSystems.Systems.Count == 0;
    }
}
