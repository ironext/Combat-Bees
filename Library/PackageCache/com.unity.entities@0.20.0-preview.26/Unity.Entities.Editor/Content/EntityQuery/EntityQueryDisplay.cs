using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Properties.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class EntityQueryDisplay
    {
        public readonly EntityQueryContent Content;

        public readonly List<EntityViewData> Source;
        public readonly List<EntityViewData> Filtered;

        public World World => Content.World;
        public EntityQuery Query => Content.Query;

        public EntityQueryDisplay(EntityQueryContent content)
        {
            Content = content;
            Source = new List<EntityViewData>(content.Query.CalculateEntityCount());
            using (var entities = content.Query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    Source.Add(new EntityViewData(content.World, entity));
                }
            }
            Filtered = new List<EntityViewData>(Source);
        }
    }

    [UsedImplicitly]
    class EntityQueryDisplayInspector : Inspector<EntityQueryDisplay>
    {
        int m_LastHash;
        SearchElement m_SearchElement;
        ListView m_ListView;
        Label m_EntityCountLabel;

        public override VisualElement Build()
        {
            var root = Resources.Templates.ContentProvider.EntityQuery.Clone();
            root.AddToClassList(UssClasses.Content.Query.EntityQuery.Container);
            Resources.AddCommonVariables(root);

            m_LastHash = Target.Query.GetCombinedComponentOrderVersion();

            m_ListView = root.Q<ListView>(className: UssClasses.Content.Query.EntityQuery.ListView);
            m_ListView.makeItem += () => new PropertyElement();
            m_ListView.bindItem += (element, i) =>
            {
                if (element is PropertyElement propertyElement)
                    propertyElement.SetTarget(Target.Filtered[i]);
            };
            m_ListView.itemsSource = Target.Filtered;

            m_SearchElement = root.Q<SearchElement>();
            m_SearchElement.RegisterSearchQueryHandler<EntityViewData>(search =>
            {
                Target.Filtered.Clear();
                Target.Filtered.AddRange(search.Apply(Target.Source));
                m_ListView.Refresh();
            });

            var foldout = new FoldoutWithHeader { Header = Resources.Templates.ContentProvider.EntityQueryFoldout.Clone() };
            var header = foldout.Header;
            foldout.SetValueWithoutNotify(false);
            header.AddToClassList(UssClasses.Content.Query.EntityQuery.FoldoutContainer);

            root.Q<VisualElement>(className: UssClasses.Content.Query.EntityQuery.Foldout).Add(foldout);
            var fromComponent = Target.Content.SystemName == null;
            header.Q<Label>(className: UssClasses.Content.Query.EntityQuery.SystemName).text = fromComponent ? "Query" : Target.Content.SystemName;
            header.Q<Label>(className: UssClasses.Content.Query.EntityQuery.QueryName).text = fromComponent ? "" :  $"Query #{Target.Content.QueryOrder}";

            if (fromComponent)
            {
                header.Remove(header.Q<VisualElement>(className: UssClasses.Content.Query.EntityQuery.SystemIcon));
                header.Remove( header.Q<VisualElement>(className: UssClasses.Content.Query.EntityQuery.QueryIcon));
            }

            m_EntityCountLabel = foldout.Header.Q<Label>(className: UssClasses.Content.Query.EntityQuery.EntityCount);
            m_EntityCountLabel.text = $"Entity Count: {Target.Source.Count}";

            using var allComponents = Target.Query.GetComponentDataFromQuery().ToPooledList();
            if (allComponents.List.Count == 0)
            {
                var allEntitiesLabel = new Label(L10n.Tr("All Entities"));
                allEntitiesLabel.AddToClassList(UssClasses.Content.Query.EntityQuery.Empty);
                foldout.Add(allEntitiesLabel);
            }
            else
            {
                foreach (var component in allComponents.List)
                {
                    var componentView = new ComponentView(component);
                    if (fromComponent) componentView.m_AccessMode.Hide();
                    foldout.Add(componentView);
                }
            }

            return root;
        }

        public override void Update()
        {
            if (Target.World == null || !Target.World.IsCreated || !Target.World.EntityManager.IsQueryValid(Target.Query))
                return;

            var currentHash = Target.Query.GetCombinedComponentOrderVersion();
            if (currentHash == m_LastHash)
                return;

            m_LastHash = currentHash;
            Target.Source.Clear();
            using (var entities = Target.Query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    Target.Source.Add(new EntityViewData(Target.World, entity));
                }

                m_EntityCountLabel.text = $"Entity Count: {entities.Length}";
            }

            m_SearchElement.Search();
            m_ListView.Refresh();
            m_ListView.ForceUpdateBindings();
        }
    }
}
