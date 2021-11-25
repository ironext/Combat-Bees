using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using Unity.Properties.UI;
using Unity.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemQueries : ITabContent
    {
        public string TabName { get; } = L10n.Tr("Queries");

        UnsafeList<EntityQuery> m_LastQueries;

        public void OnTabVisibilityChanged(bool isVisible) { }

        [CreateProperty, HideInInspector, DontSerialize]
        public int Count => QueriesFromSystem.Length;

        public SystemQueries(World world, SystemProxy system)
        {
            World = world;
            System = system;
        }

        public World World { get; }
        public SystemProxy System { get; }

        public unsafe UnsafeList<EntityQuery> QueriesFromSystem
        {
            get
            {
                if (!World.IsCreated || System == default || !System.Valid)
                    return default;

                var ptr = System.StatePointerForQueryResults;
                if (ptr == null)
                    return default;

                var currentQueries = ptr->EntityQueries;
                if (m_LastQueries.Equals(currentQueries))
                    return m_LastQueries;

                m_LastQueries = currentQueries;
                return currentQueries.Length > 0 ? currentQueries : default;
            }
        }
    }

    [UsedImplicitly]
    class SystemQueriesInspector : Inspector<SystemQueries>
    {
        public override VisualElement Build()
        {
            var root = new VisualElement();

            var queries = Target.QueriesFromSystem;
            if (queries.Length == 0)
            {
                var noQueryLabel = new Label(L10n.Tr("No Queries"));
                noQueryLabel.AddToClassList(UssClasses.Content.SystemInspector.SystemQueriesEmpty);
                root.Add(noQueryLabel);
            }

            for (var i = 0; i < queries.Length; ++i)
            {
                var queryView = new QueryView(new QueryViewData(i + 1, queries[i], Target.System.NicifiedDisplayName,
                    Target.World));
                queryView.Header.style.unityFontStyleAndWeight = FontStyle.Bold;
                root.Add(queryView);
            }

            return root;
        }
    }
}
