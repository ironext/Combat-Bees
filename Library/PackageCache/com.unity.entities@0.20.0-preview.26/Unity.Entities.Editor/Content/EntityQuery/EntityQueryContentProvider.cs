using System;
using JetBrains.Annotations;
using Unity.Properties;
using Unity.Properties.UI;
using Unity.Serialization;
using Unity.Serialization.Json;
using Unity.Serialization.Json.Adapters;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class EntityQueryContentProvider : ContentProvider
    {
        // Unity.Entities.World is not serializable by default, so we use the world's name to find it again. This is
        // clearly not enough to guarantee that we can survive domain reload, but it should cover most cases.
        [CreateProperty, HideInInspector] string m_WorldName;
        [CreateProperty, HideInInspector] EntityQueryOptions m_Options;
        [CreateProperty, HideInInspector] string[] m_NoneComponentTypes;
        [CreateProperty, HideInInspector] string[] m_AnyComponentTypes;
        [CreateProperty, HideInInspector] string[] m_AllComponentTypes;

        bool Any => m_AllComponentTypes != null || m_AnyComponentTypes != null || m_NoneComponentTypes != null;

        public override string Name { get; } = L10n.Tr("Query");

        World m_World;
        EntityQuery m_Query;
        [SerializeField] string m_SystemName;
        [SerializeField] int m_QueryOrder;

        [CreateProperty, DontSerialize]
        public int EntityCount => IsValid ? Query.CalculateEntityCount() : 0;

        [CreateProperty, DontSerialize]
        public string[] AllTypes => IsValid ? m_AllComponentTypes : Array.Empty<string>();

        [CreateProperty, DontSerialize]
        public string[] AnyTypes => IsValid ? m_AnyComponentTypes : Array.Empty<string>();

        [CreateProperty, DontSerialize]
        public string[] NoneTypes => IsValid ? m_NoneComponentTypes : Array.Empty<string>();

        public World World
        {
            get => m_World;
            set
            {
                if (value == m_World)
                    return;

                m_World = value;
                m_WorldName = m_World?.Name ?? string.Empty;
            }
        }

        public string SystemName
        {
            get => m_SystemName;
            set => m_SystemName = value;
        }

        public int QueryOrder
        {
            get => m_QueryOrder;
            set => m_QueryOrder = value;
        }

        public EntityQuery Query
        {
            get => m_Query;
            set
            {
                if (m_World == null || !m_World.EntityManager.IsQueryValid(value))
                    return;

                var query = value.GetEntityQueryDesc();

                var all = query.All;
                m_AllComponentTypes = new string[all.Length];
                for (var i = 0; i < all.Length; ++i)
                {
                    if (!ComponentTypeAdapter.TrySerialize(all[i], out m_AllComponentTypes[i]))
                        return;
                }

                var any = query.Any;
                m_AnyComponentTypes = new string[any.Length];
                for (var i = 0; i < any.Length; ++i)
                {
                    if (!ComponentTypeAdapter.TrySerialize(any[i], out m_AnyComponentTypes[i]))
                        return;
                }

                var none = query.None;
                m_NoneComponentTypes = new string[none.Length];
                for (var i = 0; i < none.Length; ++i)
                {
                    if (!ComponentTypeAdapter.TrySerialize(none[i], out m_NoneComponentTypes[i]))
                        return;
                }

                m_Query = value;
                m_Options = query.Options;
            }
        }

        public bool IsValid => m_World != null && m_World.IsCreated && m_World.EntityManager.IsQueryValid(m_Query);

        protected override ContentStatus GetStatus()
        {
            if (World != null && World.IsCreated && World.EntityManager.IsQueryValid(Query))
                return ContentStatus.ContentReady;

            if (null != World && !World.IsCreated)
            {
                World = null;
                return ContentStatus.ReloadContent;
            }

            // If either of those are null or empty, we won't be able to recover previous state.
            if (string.IsNullOrEmpty(m_WorldName))
                return ContentStatus.ContentUnavailable;

            // Worlds are lazily created, so we'll spin until we can find it again.
            World = ContentUtilities.FindLastWorld(m_WorldName);

            // Recreate the Query
            if (m_World != null && Any)
            {
                var allTypes = new ComponentType[m_AllComponentTypes.Length];
                for (var i = 0; i < m_AllComponentTypes.Length; ++i)
                {
                    if (!ComponentTypeAdapter.TryDeserialize(m_AllComponentTypes[i], out allTypes[i]))
                        return ContentStatus.ContentUnavailable;
                }

                var noneTypes = new ComponentType[m_NoneComponentTypes.Length];
                for (var i = 0; i < m_NoneComponentTypes.Length; ++i)
                {
                    if (!ComponentTypeAdapter.TryDeserialize(m_NoneComponentTypes[i], out noneTypes[i]))
                        return ContentStatus.ContentUnavailable;
                }

                var anyTypes = new ComponentType[m_AnyComponentTypes.Length];
                for (var i = 0; i < m_AnyComponentTypes.Length; ++i)
                {
                    if (!ComponentTypeAdapter.TryDeserialize(m_AnyComponentTypes[i], out anyTypes[i]))
                        return ContentStatus.ContentUnavailable;
                }

                var desc = new EntityQueryDesc
                {
                    All = allTypes,
                    None = noneTypes,
                    Any = anyTypes
                };
                desc.Options = m_Options;
                m_Query = m_World.EntityManager.CreateEntityQuery(desc);
            }

            return m_World != null && m_World.IsCreated && m_World.EntityManager.IsQueryValid(m_Query) ?
                ContentStatus.ContentReady : ContentStatus.ContentNotReady;
        }

        public override object GetContent() => this;

        class ComponentTypeAdapter : IJsonAdapter<ComponentType>
        {
            struct ComponentTypeContainer
            {
                public string Type;
                public ComponentType.AccessMode AccessMode;
                public bool IsChunkComponent;
            }

            internal static bool TrySerialize(ComponentType componentType, out string value)
            {
                var type = componentType.GetManagedType();
                if (type == null)
                {
                    value = default;
                    return false;
                }

                value = JsonSerialization.ToJson(new ComponentTypeContainer
                {
                    Type = $"{type}, {type.Assembly.GetName().Name}",
                    AccessMode = componentType.AccessModeType,
                    IsChunkComponent = componentType.IsChunkComponent
                }, new JsonSerializationParameters
                {
                    Minified = true
                });
                return true;
            }

            internal static bool TryDeserialize(string json, out ComponentType value)
            {
                if (JsonSerialization.TryFromJson<ComponentTypeContainer>(json, out var container, out _))
                {
                    var type = Type.GetType(container.Type);
                    var typeIndex = TypeManager.GetTypeIndex(type);

                    if (typeIndex != -1)
                    {
                        if (container.IsChunkComponent)
                        {
                            var valueTemp = new ComponentType
                            {
                                TypeIndex = typeIndex,
                                AccessModeType = container.AccessMode
                            };

                            value = ComponentType.ChunkComponent(valueTemp.GetManagedType());
                        }
                        else
                        {
                            value = new ComponentType
                            {
                                TypeIndex = typeIndex,
                                AccessModeType = container.AccessMode
                            };
                        }

                        return true;
                    }
                }

                value = default;
                return false;
            }

            public void Serialize(JsonStringBuffer writer, ComponentType value)
            {
                writer.Write(TrySerialize(value, out var json) ? json : null);
            }

            public ComponentType Deserialize(SerializedValueView view)
            {
                return TryDeserialize(view.AsStringView().ToString(), out var componentType) ? componentType : default;
            }
        }
    }

    [UsedImplicitly]
    class EntityQueryContentProviderInspector : Inspector<EntityQueryContentProvider>
    {
        public override VisualElement Build()
        {
            var element = new PropertyElement();
            Resources.AddCommonVariables(element);
            Resources.Templates.ContentProvider.EntityQuery.AddStyles(element);

            var content = new EntityQueryContent(Target.World, Target.Query, Target.SystemName, Target.QueryOrder);
            element.SetTarget(new EntityQueryDisplay(content));
            return element;
        }
    }
}
