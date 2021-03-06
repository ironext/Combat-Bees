namespace Unity.Entities.Editor
{
    static class UssClasses
    {
        public static class DotsEditorCommon
        {
            public const string CommonResources = "common-resources";
            public const string SettingsIcon = "settings-icon";
            public const string SearchIconContainer = "search-icon-container";
            public const string SearchIcon = "search-icon";

            public const string SearchFieldContainer = "search-field-container";
            public const string SearchField = "search-field";

            const string CenteredMessageElementBase = "centered-message-element";
            public const string CenteredMessageElementTitle = CenteredMessageElementBase + "__title";
            public const string CenteredMessageElementMessage = CenteredMessageElementBase + "__message";
        }

        public static class ComponentView
        {
            const string k_Base = "component-view";

            public const string Icon = k_Base + "__icon";
            public const string Name = k_Base + "__name";
            public const string AccessMode = k_Base + "__access-mode";
            public const string GoTo = k_Base + "__goto-icon";
        }

        public static class FoldoutWithActionButton
        {
            const string k_Base = "foldout-with-action-button";

            public const string ToggleHeaderHoverStyle = k_Base + "__toggle-header--hover";
            public const string Icon = k_Base + "__icon";
            public const string Name = k_Base + "__name";
            public const string ButtonContainer = k_Base + "__button-container";
            public const string Button = k_Base + "__button";
            public const string Count = k_Base + "__count";
            public const string Toggle = k_Base + "__toggle";
            public const string ToggleInput = k_Base + "__toggle-input";
        }

        public static class FoldoutWithoutActionButton
        {
            const string k_Base = "foldout-without-action-button";

            public const string Name = k_Base + "__name";
            public const string Count = k_Base + "__count";
            public const string Toggle = k_Base + "__toggle";
            public const string ToggleInput = k_Base + "__toggle-input";
        }

        public static class QueryView
        {
            const string k_Base = "query-view";
            public const string Name = k_Base + "__name";
            public const string EmptyMessage = k_Base + "__empty";
        }

        public static class SystemDependencyView
        {
            const string k_Base = "system-dependency-view";

            public const string Icon = k_Base + "__icon";
            public const string Name = k_Base + "__name";
            public const string ContentElement = k_Base + "__content";
            public const string GotoButtonContainer = k_Base + "__button-container";
            public const string GotoButton= k_Base + "__button";
        }

        public static class SystemListView
        {
            const string k_Base = "system-list-view";

            public const string ContentElement = k_Base + "__content";
            public const string MoreLabel = k_Base + "__more-label";
        }

        public static class SystemQueriesView
        {
            const string k_Base = "system-queries-view";

            public const string Icon = k_Base + "__icon";
            public const string Name = k_Base + "__name";
            public const string GoTo = k_Base + "__goto-icon";
        }

        public static class QueryWithEntities
        {
            const string k_Base = "query-with-entities";
            public const string Icon = k_Base + "__icon";
            public const string OpenQueryWindowButton = k_Base + "__open-query-window-button";
            public const string SeeAllContainer = k_Base + "__see-all-container";
            public const string SeeAllButton = k_Base + "__see-all";
        }

        public static class EntityView
        {
            const string k_Base = "entity-view";
            public const string EntityName = k_Base + "__name";
            public const string GoTo = k_Base + "__goto";
        }

        public static class ComponentAttribute
        {
            const string k_Base = "component-attribute";
            public const string Name = k_Base + "__name";
            public const string Value = k_Base + "__value";
        }

        public static class SystemScheduleWindow
        {
            const string SystemSchedule = "system-schedule";
            public const string WindowRoot = SystemSchedule + "__root";

            public static class Toolbar
            {
                const string k_Base = SystemSchedule + "-toolbar";
                public const string LeftSide = k_Base + "__left";
                public const string RightSide = k_Base + "__right";
            }

            public static class TreeView
            {
                public const string Header = SystemSchedule + "__tree-view__header";
                public const string System = SystemSchedule + "__tree-view__system-label";
                public const string Matches = SystemSchedule + "__tree-view__matches-label";
                public const string Time = SystemSchedule + "__tree-view__time-label";
            }

            public static class Items
            {
                const string Base = SystemSchedule + "-item";
                public const string Icon = Base + "__icon";
                public const string Enabled = Base + "__enabled-toggle";
                public const string SystemName = Base + "__name-label";
                public const string Matches = Base + "__matches-label";
                public const string Time = Base + "__time-label";

                public const string SystemIcon = Icon + "--system";
                public const string SystemGroupIcon = Icon + "--system-group";
                public const string BeginCommandBufferIcon = Icon + "--begin-command-buffer";
                public const string EndCommandBufferIcon = Icon + "--end-command-buffer";
                public const string UnmanagedSystemIcon = Icon + "--unmanaged-system";

                public const string SystemNameNormal = Base + "__name-label-normal";
                public const string SystemNameBold = Base + "__name-label-bold";
            }
        }

        public static class EntityHierarchyWindow
        {
            const string k_EntityHierarchyBase = "entity-hierarchy";

            public static class Toolbar
            {
                const string k_Base = k_EntityHierarchyBase + "-toolbar";
                public const string Container = k_Base + "__container";
                public const string LeftSide = k_Base + "__left";
                public const string RightSide = k_Base + "__right";
                public const string SearchField = k_Base + "__search-field";
            }

            public static class Item
            {
                const string k_Base = k_EntityHierarchyBase + "-item";

                public const string SceneNode = k_Base + "__scene-node";

                public const string Icon = k_Base + "__icon";
                public const string IconScene = Icon + "--scene";
                public const string IconEntity = Icon + "--entity";

                public const string NameLabel = k_Base + "__name-label";
                public const string NameScene = NameLabel + "--scene";

                public const string SystemButton = k_Base + "__system-button";
                public const string PingGameObjectButton = k_Base + "__ping-gameobject-button";

                public const string VisibleOnHover = k_Base + "__visible-on-hover";

                public const string Prefab = k_Base + "--prefab";
                public const string PrefabRoot = k_Base + "--prefab-root";
            }
        }

        public static class Hierarchy
        {
            const string k_Hierarchy = "hierarchy";
            public const string Root = k_Hierarchy;
            public const string Loading = k_Hierarchy + "-loading";
            public const string Footer = k_Hierarchy + "-footer";

            public static class Toolbar
            {
                const string k_Toolbar = k_Hierarchy + "-toolbar";
                public const string Container = k_Toolbar + "__container";
                public const string LeftSide = k_Toolbar + "__left";
                public const string RightSide = k_Toolbar + "__right";
                public const string SearchField = k_Toolbar + "__search-field";
            }
            
            public static class Item
            {
                const string k_Item = k_Hierarchy + "-item";
                public const string SceneNode = k_Item + "__scene-node";
                public const string Icon = k_Item + "__icon";
                public const string IconScene = Icon + "--scene";
                public const string IconEntity = Icon + "--entity";
                public const string NameLabel = k_Item + "__name-label";
                public const string NameScene = NameLabel + "--scene";
                public const string SystemButton = k_Item + "__system-button";
                public const string PingGameObjectButton = k_Item + "__ping-gameobject-button";
                public const string VisibleOnHover = k_Item + "__visible-on-hover";
                public const string Prefab = k_Item + "--prefab";
                public const string PrefabRoot = k_Item + "--prefab-root";
            }
        }

        public static class Inspector
        {
            public const string EntityInspector = "entity-inspector";

            public static class EntityHeader
            {
                public const string OriginatingGameObject = "originating-game-object";
            }

            public static class Icons
            {
                const string k_Base = "inspector-icon";
                public const string Small = k_Base + "--small";
                public const string Medium = k_Base + "--medium";
                public const string Big = k_Base + "--big";
            }

            public static class Component
            {
                const string k_Base = "component";
                public const string Container = k_Base + "-container";
                public const string Header = k_Base + "-header";
                public const string Name = k_Base + "-name";
                public const string Icon = k_Base + "-icon";
                public const string Category = k_Base + "-category";
                public const string Menu = k_Base + "-menu";
            }

            public static class ComponentTypes
            {
                const string k_PostFix = "-data";
                public const string Component = "component" + k_PostFix;
                public const string Tag = "tag-component" + k_PostFix;
                public const string SharedComponent = "shared-component" + k_PostFix;
                public const string ChunkComponent = "chunk-component" + k_PostFix;
                public const string ManagedComponent = "managed-component" + k_PostFix;
                public const string BufferComponent = "buffer-component" + k_PostFix;
            }

            public static class RelationshipsTab
            {
                const string k_TabBase = EntityInspector + "-relationships-tab";
                public const string Container = k_TabBase + "__container";
                public const string SearchField = k_TabBase + "__search-field";
            }

            public static class ComponentsTab
            {
                const string k_TabBase = EntityInspector + "-components-tab";
                public const string SearchField = k_TabBase + "__search-field";
            }
        }

        public static class Content
        {
            public static class Query
            {
                public static class EntityQuery
                {
                    const string k_EntityQuery = "entity-query";
                    public const string Container = k_EntityQuery + "__container";
                    public const string Foldout = k_EntityQuery + "__foldout";
                    public const string ListView = k_EntityQuery + "__list-view";

                    const string k_EntityQueryFoldout = "entity-query-foldout";
                    public const string FoldoutContainer = k_EntityQueryFoldout + "__container";
                    public const string SystemIcon = k_EntityQueryFoldout + "__system-icon";
                    public const string SystemName = k_EntityQueryFoldout + "__system-name";
                    public const string QueryName = k_EntityQueryFoldout + "__query-name";
                    public const string QueryIcon = k_EntityQueryFoldout + "__query-icon";
                    public const string EntityCount = k_EntityQueryFoldout + "__entity-count";
                    public const string Empty = k_EntityQueryFoldout + "__empty";
                }

                public static class EntityInfo
                {
                    const string k_EntityInfo = "entity-info";
                    public const string Container = k_EntityInfo + "__container";
                    public const string Icon = k_EntityInfo + "__icon";
                    public const string Name = k_EntityInfo + "__name";
                }
            }

            public static class SystemInspector
            {
                public const string SystemContainer = "system__container";
                public const string SystemQueriesEmpty = "system-queries__empty";

                public static class SystemIcons
                {
                    public const string SystemIconBig = "system__icon--big";
                    public const string EcbBeginIconBig = "ecb-begin__icon--big";
                    public const string EcbEndIconBig = "ecb-end__icon--big";
                    public const string GroupIconBig = "group__icon--big";
                }
            }
        }

        public static class UIToolkit
        {
            public const string Disabled = "unity-disabled";

            public static class BaseField
            {
                const string k_Base = "unity-base-field";
                public const string Input = k_Base + "__input";
            }

            public static class ObjectField
            {
                public const string ObjectSelector = "unity-object-field__selector";
                public const string Display = "unity-object-field-display";
            }

            public static class Toggle
            {
                const string k_Base = "unity-toggle";
                public const string Text = k_Base + "__text";
                public const string Input = k_Base + "__input";
            }
        }
    }
}
