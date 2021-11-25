using System;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemInformationVisualElement : BindableElement, IBinding
    {
        internal static readonly BasicPool<SystemInformationVisualElement> Pool = new BasicPool<SystemInformationVisualElement>(() => new SystemInformationVisualElement());

        public World World;
        SystemTreeViewItem m_Target;
        public SystemTreeView TreeView { get; set; }
        const float k_SystemNameLabelWidth = 100f;
        const float k_SingleIndentWidth = 12f;

        public SystemTreeViewItem Target
        {
            get => m_Target;
            set
            {
                if (m_Target == value)
                    return;
                m_Target = value;
                Update();
            }
        }

        readonly Toggle m_SystemEnableToggle;
        readonly VisualElement m_Icon;
        readonly Label m_SystemNameLabel;
        readonly Label m_EntityMatchLabel;
        readonly Label m_RunningTimeLabel;

        SystemInformationVisualElement()
        {
            Resources.Templates.SystemScheduleItem.Clone(this);
            binding = this;

            AddToClassList(UssClasses.DotsEditorCommon.CommonResources);

            m_SystemEnableToggle = this.Q<Toggle>(className: UssClasses.SystemScheduleWindow.Items.Enabled);
            m_SystemEnableToggle.RegisterCallback<ChangeEvent<bool>, SystemInformationVisualElement>(
                OnSystemTogglePress, this);

            m_Icon = this.Q(className: UssClasses.SystemScheduleWindow.Items.Icon);

            m_SystemNameLabel = this.Q<Label>(className: UssClasses.SystemScheduleWindow.Items.SystemName);
            m_EntityMatchLabel = this.Q<Label>(className: UssClasses.SystemScheduleWindow.Items.Matches);
            m_RunningTimeLabel = this.Q<Label>(className: UssClasses.SystemScheduleWindow.Items.Time);
        }

        public static SystemInformationVisualElement Acquire(SystemTreeView treeView, World world)
        {
            var item = Pool.Acquire();

            item.TreeView = treeView;
            item.World = world;
            return item;
        }

        public void Release()
        {
            World = null;
            Target = null;
            TreeView = null;
            Pool.Release(this);
        }

        static void SetText(Label label, string text)
        {
            if (label.text != text)
                label.text = text;
        }

        public void Update()
        {
            if (null == Target)
                return;

            if (Target.SystemProxy.Valid && Target.SystemProxy.World == null)
                return;

            m_Icon.style.display = string.Empty == GetSystemClass(Target.SystemProxy) ? DisplayStyle.None : DisplayStyle.Flex;

            SetText(m_SystemNameLabel, Target.GetSystemName(World));
            SetSystemNameLabelWidth(m_SystemNameLabel, k_SystemNameLabelWidth);
            SetText(m_EntityMatchLabel, Target.GetEntityMatches());
            SetText(m_RunningTimeLabel, Target.GetRunningTime());
            SetSystemClass(m_Icon, Target.SystemProxy);
            SetGroupNodeLabelBold(m_SystemNameLabel, Target.SystemProxy);

            if (!Target.SystemProxy.Valid) // player loop system without children
            {
                SetEnabled(Target.HasChildren);
                m_SystemEnableToggle.style.display = DisplayStyle.None;
            }
            else
            {
                SetEnabled(true);
                m_SystemEnableToggle.style.display = DisplayStyle.Flex;
                var systemState = Target.SystemProxy.Enabled;

                if (m_SystemEnableToggle.value != systemState)
                    m_SystemEnableToggle.SetValueWithoutNotify(systemState);

                var groupState = systemState && Target.GetParentState();

                m_SystemNameLabel.SetEnabled(groupState);
                m_EntityMatchLabel.SetEnabled(groupState);
                m_RunningTimeLabel.SetEnabled(groupState);
            }
        }

        void SetSystemNameLabelWidth(VisualElement label, float fixedWidth)
        {
            var treeViewItemVisualElement = parent?.parent;
            var itemIndentsContainerName = treeViewItemVisualElement?.Q("unity-tree-view__item-indents");
            label.style.width = itemIndentsContainerName == null ? fixedWidth : fixedWidth - itemIndentsContainerName.childCount * k_SingleIndentWidth;
        }

        static void SetSystemClass(VisualElement element, SystemProxy systemProxy)
        {
            var flags = systemProxy.Valid ? systemProxy.Category : 0;

            element.EnableInClassList(
                UssClasses.SystemScheduleWindow.Items.BeginCommandBufferIcon,
                (flags & SystemCategory.ECBSystemBegin) != 0);
            element.EnableInClassList(
                UssClasses.SystemScheduleWindow.Items.EndCommandBufferIcon,
                (flags & SystemCategory.ECBSystemEnd) != 0);
            element.EnableInClassList(
                UssClasses.SystemScheduleWindow.Items.UnmanagedSystemIcon,
                (flags & SystemCategory.Unmanaged) != 0);
            element.EnableInClassList(
                UssClasses.SystemScheduleWindow.Items.SystemIcon,
                (flags & SystemCategory.SystemBase) != 0 && (flags & SystemCategory.EntityCommandBufferSystem) == 0);
            element.EnableInClassList(
                UssClasses.SystemScheduleWindow.Items.SystemGroupIcon,
                (flags & SystemCategory.SystemGroup) != 0);
        }

        static void SetGroupNodeLabelBold(VisualElement element, SystemProxy systemProxy)
        {
            var flags = systemProxy.Valid ? systemProxy.Category : 0;

            var isBold = flags == 0 || (flags & SystemCategory.SystemGroup) != 0;
            element.EnableInClassList(UssClasses.SystemScheduleWindow.Items.SystemNameBold, isBold);
            element.EnableInClassList(UssClasses.SystemScheduleWindow.Items.SystemNameNormal, !isBold);
        }

        static string GetSystemClass(SystemProxy systemProxy)
        {
            var flags = systemProxy.Valid ? systemProxy.Category : 0;

            if ((flags & SystemCategory.ECBSystemBegin) != 0)
                return UssClasses.SystemScheduleWindow.Items.BeginCommandBufferIcon;
            if ((flags & SystemCategory.ECBSystemEnd) != 0)
                return UssClasses.SystemScheduleWindow.Items.EndCommandBufferIcon;
            if ((flags & SystemCategory.EntityCommandBufferSystem) != 0)
                return string.Empty;
            if ((flags & SystemCategory.Unmanaged) != 0)
                return UssClasses.SystemScheduleWindow.Items.UnmanagedSystemIcon;
            if ((flags & SystemCategory.SystemGroup) != 0)
                return UssClasses.SystemScheduleWindow.Items.SystemGroupIcon;
            if ((flags & SystemCategory.SystemBase) != 0)
                return UssClasses.SystemScheduleWindow.Items.SystemIcon;

            return string.Empty;
        }

        static void OnSystemTogglePress(ChangeEvent<bool> evt, SystemInformationVisualElement item)
        {
            if (item.Target.SystemProxy.Valid)
            {
                item.Target.SetSystemState(evt.newValue);
            }
            else
            {
                item.Target.SetPlayerLoopSystemState(evt.newValue);
            }
        }

        void IBinding.PreUpdate() { }

        void IBinding.Release() { }
    }
}
