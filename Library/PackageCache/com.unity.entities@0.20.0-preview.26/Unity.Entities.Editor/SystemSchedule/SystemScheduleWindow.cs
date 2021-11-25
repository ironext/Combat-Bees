using System;
using Unity.Profiling;
using Unity.Properties;
using Unity.Properties.UI;
using Unity.Serialization.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemScheduleWindow : DOTSEditorWindow, IHasCustomMenu
    {
        static readonly ProfilerMarker k_OnUpdateMarker =
            new ProfilerMarker($"{nameof(SystemScheduleWindow)}.{nameof(OnUpdate)}");

        readonly Cooldown m_Cooldown = new Cooldown(TimeSpan.FromMilliseconds(Constants.Inspector.CoolDownTime));

        static readonly string k_WindowName = L10n.Tr("Systems");
        static readonly string k_ShowFullPlayerLoopString = L10n.Tr("Show Full Player Loop");
        static readonly string k_FilterComponentType = L10n.Tr("Component type");
        static readonly string k_FilterComponentTypeTooltip = L10n.Tr("Filter systems that have the specified component type in queries");
        static readonly string k_FilterSystemDependencies = L10n.Tr("System dependencies");
        static readonly string k_FilterSystemDependenciesTooltip = L10n.Tr("Filter systems by their direct dependencies");

        VisualElement m_Root;
        CenteredMessageElement m_NoWorld;
        SystemTreeView m_SystemTreeView;
        VisualElement m_WorldSelector;
        VisualElement m_EmptySelectorWhenShowingFullPlayerLoop;
        SearchElement m_SearchElement;
        internal WorldProxyManager WorldProxyManager; // internal for tests.
        PlayerLoopSystemGraph m_LocalSystemGraph;
        int m_LastWorldVersion;
        bool m_ViewChange;
        bool m_GraphChange;

        /// <summary>
        /// Helper container to store session state data.
        /// </summary>
        class State
        {
            /// <summary>
            /// This field controls the showing of full player loop state.
            /// </summary>
            public bool ShowFullPlayerLoop;
        }

        /// <summary>
        /// State data for <see cref="SystemScheduleWindow"/>. This data is persisted between domain reloads.
        /// </summary>
        State m_State;

        [MenuItem(Constants.MenuItems.SystemScheduleWindow, false, Constants.MenuItems.WindowPriority)]
        static void OpenWindow()
        {
            var window = GetWindow<SystemScheduleWindow>();
            window.Show();
        }

        /// <summary>
        /// Build the GUI for the system window.
        /// </summary>
        void OnEnable()
        {
            Resources.AddCommonVariables(rootVisualElement);

            titleContent = EditorGUIUtility.TrTextContent(k_WindowName, EditorIcons.System);
            minSize = Constants.MinWindowSize;

            m_Root = new VisualElement();
            m_Root.AddToClassList(UssClasses.SystemScheduleWindow.WindowRoot);
            rootVisualElement.Add(m_Root);

            m_NoWorld = new CenteredMessageElement() { Message = NoWorldMessageContent };
            rootVisualElement.Add(m_NoWorld);
            m_NoWorld.Hide();

            m_State = SessionState<State>.GetOrCreate($"{typeof(SystemScheduleWindow).FullName}+{nameof(State)}+{EditorWindowInstanceKey}");

            Resources.Templates.SystemSchedule.AddStyles(m_Root);
            Resources.Templates.DotsEditorCommon.AddStyles(m_Root);

            WorldProxyManager = new WorldProxyManager();
            m_LocalSystemGraph = new PlayerLoopSystemGraph
            {
                WorldProxyManager = WorldProxyManager
            };

            CreateToolBar(m_Root);
            CreateTreeViewHeader(m_Root);
            CreateTreeView(m_Root);

            if (!string.IsNullOrEmpty(SearchFilter))
                m_SearchElement.Search(SearchFilter);
        }

        void OnDisable()
        {
            WorldProxyManager.Dispose();
            m_SystemTreeView?.Dispose();
        }

        void CreateToolBar(VisualElement root)
        {
            var toolbar = new VisualElement();
            Resources.Templates.SystemScheduleToolbar.Clone(toolbar);
            var leftSide = toolbar.Q(className: UssClasses.SystemScheduleWindow.Toolbar.LeftSide);
            var rightSide = toolbar.Q(className: UssClasses.SystemScheduleWindow.Toolbar.RightSide);

            m_WorldSelector = CreateWorldSelector();
            m_EmptySelectorWhenShowingFullPlayerLoop = new ToolbarMenu { text = k_ShowFullPlayerLoopString };
            leftSide.Add(m_WorldSelector);
            leftSide.Add(m_EmptySelectorWhenShowingFullPlayerLoop);

            AddSearchIcon(rightSide, UssClasses.DotsEditorCommon.SearchIcon);

            var dropdownSettings = CreateDropdownSettings(UssClasses.DotsEditorCommon.SettingsIcon);
            dropdownSettings.menu.AppendAction(k_ShowFullPlayerLoopString, a =>
            {
                m_State.ShowFullPlayerLoop = !m_State.ShowFullPlayerLoop;
                WorldProxyManager.IsFullPlayerLoop = m_State.ShowFullPlayerLoop;

                UpdateWorldSelectorDisplay();

                if (World.All.Count > 0)
                    RebuildTreeView();
            }, a => m_State.ShowFullPlayerLoop ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            UpdateWorldSelectorDisplay();
            rightSide.Add(dropdownSettings);

            root.Add(toolbar);
            AddSearchElement(root);
        }

        void AddSearchElement(VisualElement root)
        {
            m_SearchElement = AddSearchElement<SystemForSearch>(root, UssClasses.DotsEditorCommon.SearchFieldContainer);
            m_SearchElement.RegisterSearchQueryHandler<SystemForSearch>(query =>
            {
                var parseResult = SearchQueryParser.ParseSearchQuery(query);
                m_SystemTreeView.SetFilter(query, parseResult);
            });

            m_SearchElement.AddSearchFilterPopupItem(Constants.SystemSchedule.k_ComponentToken.Substring(0, 1), k_FilterComponentType, k_FilterComponentTypeTooltip);
            m_SearchElement.AddSearchFilterPopupItem(Constants.SystemSchedule.k_SystemDependencyToken.Substring(0, 2), k_FilterSystemDependencies, k_FilterSystemDependenciesTooltip);

            m_SearchElement.AddSearchDataProperty(new PropertyPath(nameof(SystemForSearch.SystemName)));
            m_SearchElement.AddSearchFilterProperty(Constants.SystemSchedule.k_ComponentToken.Substring(0, 1), new PropertyPath(nameof(SystemForSearch.ComponentNamesInQuery)));
            m_SearchElement.AddSearchFilterProperty(Constants.SystemSchedule.k_SystemDependencyToken.Substring(0, 2), new PropertyPath(nameof(SystemForSearch.SystemDependency)));
            m_SearchElement.EnableAutoComplete(ComponentTypeAutoComplete.Instance);
        }

        void UpdateWorldSelectorDisplay()
        {
            m_WorldSelector.SetVisibility(!m_State.ShowFullPlayerLoop);
            m_EmptySelectorWhenShowingFullPlayerLoop.SetVisibility(m_State.ShowFullPlayerLoop);
        }

        /// <summary>
        ///  Manually create header for the tree view.
        /// </summary>
        /// <param name="root"></param>
        void CreateTreeViewHeader(VisualElement root)
        {
            var systemTreeViewHeader = new Toolbar();
            systemTreeViewHeader.AddToClassList(UssClasses.SystemScheduleWindow.TreeView.Header);

            var systemHeaderLabel = new Label("Systems");
            systemHeaderLabel.AddToClassList(UssClasses.SystemScheduleWindow.TreeView.System);

            var entityHeaderLabel = new Label("Matches")
            {
                tooltip = "The number of entities that match the queries at the end of the frame."
            };
            entityHeaderLabel.AddToClassList(UssClasses.SystemScheduleWindow.TreeView.Matches);

            var timeHeaderLabel = new Label("Time (ms)")
            {
                tooltip = "Average running time."
            };
            timeHeaderLabel.AddToClassList(UssClasses.SystemScheduleWindow.TreeView.Time);

            systemTreeViewHeader.Add(systemHeaderLabel);
            systemTreeViewHeader.Add(entityHeaderLabel);
            systemTreeViewHeader.Add(timeHeaderLabel);

            root.Add(systemTreeViewHeader);
        }

        void CreateTreeView(VisualElement root)
        {
            m_SystemTreeView = new SystemTreeView
            {
                viewDataKey = nameof(SystemScheduleWindow),
                style = { flexGrow = 1 },
                LocalSystemGraph = m_LocalSystemGraph
            };
            root.Add(m_SystemTreeView);
        }

        // internal for test.
        internal void RebuildTreeView()
        {
            m_SystemTreeView.Refresh(m_State.ShowFullPlayerLoop ? null : SelectedWorld);
        }

        protected override void OnUpdate()
        {
            using (k_OnUpdateMarker.Auto())
            {
                if (!m_Cooldown.Update(DateTime.Now))
                    return;

                foreach (var updater in WorldProxyManager.GetAllWorldProxyUpdaters())
                {
                    if (!updater.IsActive() || !updater.IsDirty())
                        continue;

                    m_GraphChange = true;
                    updater.SetClean();
                }

                if (m_GraphChange)
                    m_LocalSystemGraph.BuildCurrentGraph();

                if (m_GraphChange || m_ViewChange)
                    RebuildTreeView();

                m_GraphChange = false;
                m_ViewChange = false;
            }
        }

        protected override void OnWorldsChanged(bool containsAnyWorld)
        {
            m_Root.SetVisibility(containsAnyWorld);
            m_NoWorld.SetVisibility(!containsAnyWorld);

            if (m_SystemTreeView == null)
                return;

            WorldProxyManager.IsFullPlayerLoop = m_State.ShowFullPlayerLoop;
            WorldProxyManager.CreateWorldProxiesForAllWorlds();

            if (m_State.ShowFullPlayerLoop)
                m_GraphChange = true;
        }

        protected override void OnWorldSelected(World world)
        {
            if (m_State.ShowFullPlayerLoop)
                return;

            WorldProxyManager.SelectedWorld = world;
            m_ViewChange = true;
        }

        public static void HighlightSystem(SystemProxy systemProxy)
        {
            SystemTreeView.SelectedSystem = systemProxy;

            if (HasOpenInstances<SystemScheduleWindow>())
                GetWindow<SystemScheduleWindow>().m_SystemTreeView.SetSelection();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(new GUIContent($"Debug..."), false, () =>
                    SelectionUtility.ShowInWindow(new SystemsWindowDebugContentProvider()));
            }
        }
    }
}
