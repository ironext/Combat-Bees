using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Editor.Bridge;
using Unity.Scenes;

namespace Unity.Entities.Editor
{
    class SystemTreeViewItem : ITreeViewItem
    {
        internal static readonly BasicPool<SystemTreeViewItem> Pool = new BasicPool<SystemTreeViewItem>(() => new SystemTreeViewItem());

        readonly List<ITreeViewItem> m_CachedChildren = new List<ITreeViewItem>();
        public IPlayerLoopNode Node;
        public SystemGraph Graph;
        public World World;

        SystemTreeViewItem() { }

        public static SystemTreeViewItem Acquire(SystemGraph graph, IPlayerLoopNode node, SystemTreeViewItem parent, World world)
        {
            var item = Pool.Acquire();

            item.World = world;
            item.Graph = graph;
            item.Node = node;
            item.parent = parent;

            return item;
        }

        public SystemProxy SystemProxy
        {
            get
            {
                if (Node is ISystemHandleNode systemHandleNode)
                    return systemHandleNode.SystemProxy;

                return default;
            }
        }

        public bool HasChildren => Node.Children.Count > 0;

        public string GetSystemName(World world = null)
        {
            if (world == null ||
                (Node is ISystemHandleNode systemHandleNode && systemHandleNode.SystemProxy.World.Name != world.Name))
            {
                return Node.NameWithWorld;
            }

            return Node?.Name;
        }

        public bool GetParentState()
        {
            return Node.EnabledInHierarchy;
        }

        public void SetPlayerLoopSystemState(bool state)
        {
            Node.Enabled = state;
        }

        public void SetSystemState(bool state)
        {
            if (Node.Enabled == state)
                return;

            Node.Enabled = state;
            EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();
        }

        public string GetEntityMatches()
        {
            if (HasChildren) // Group system do not need entity matches.
                return string.Empty;

            if (!SystemProxy.Valid)
                return string.Empty;

            if (!Node.Enabled || !NodeParentsAllEnabled(Node))
            {
                return Constants.SystemSchedule.k_Dash;
            }

            return SystemProxy.TotalEntityMatches.ToString();
        }

        float GetAverageRunningTime(SystemProxy systemProxy)
        {
            return Graph.GetAverageRunningTime(systemProxy);
        }

        public string GetRunningTime()
        {
            if (Node is IPlayerLoopSystemData)
                return string.Empty;

            // if the node is disabled, not running, or if its parents are all disabled
            // TODO this isn't really great; we should read the raw profile data, if something is taking up time we should know it regardless of enabled etc state
            if (!Node.IsRunning || !Node.Enabled || !NodeParentsAllEnabled(Node))
                return Constants.SystemSchedule.k_Dash;

            // if the node is a group node, it's just its own time (it has its own profiler marker)
            if (Node is ComponentGroupNode groupNode)
                return GetAverageRunningTime(groupNode.SystemProxy).ToString("f2");

            // if it has any children, it's the sum of all of its children that are SystemHandleNodes
            if (children.Any())
                return Node.Children
                    .OfType<ISystemHandleNode>()
                    .Sum(child => GetAverageRunningTime(child.SystemProxy))
                    .ToString("f2");

            // if it's not a system handle at this point, we can't show anything useful
            if (Node is ISystemHandleNode sysNode)
                return sysNode.SystemProxy.RunTimeMillisecondsForDisplay.ToString("f2");

            return Constants.SystemSchedule.k_Dash;
        }

        bool NodeParentsAllEnabled(IPlayerLoopNode node)
        {
            if (node.Parent != null)
            {
                if (!node.Parent.Enabled) return false;
                if (!NodeParentsAllEnabled(node.Parent)) return false;
            }

            return true;
        }

        public int id => Node.Hash;
        public ITreeViewItem parent { get; internal set; }
        public IEnumerable<ITreeViewItem> children => m_CachedChildren;
        bool ITreeViewItem.hasChildren => HasChildren;

        public void AddChild(ITreeViewItem child)
        {
            throw new NotImplementedException();
        }

        public void AddChildren(IList<ITreeViewItem> children)
        {
            throw new NotImplementedException();
        }

        public void RemoveChild(ITreeViewItem child)
        {
            throw new NotImplementedException();
        }

        public void PopulateChildren()
        {
            m_CachedChildren.Clear();

            foreach (var child in Node.Children)
            {
                if (!child.ShowForWorld(World))
                    continue;

                var item = Acquire(Graph, child, this, World);
                m_CachedChildren.Add(item);
            }
        }

        public void Release()
        {
            World = null;
            Graph = null;
            Node = null;
            parent = null;
            foreach (var child in m_CachedChildren.OfType<SystemTreeViewItem>())
            {
                child.Release();
            }

            m_CachedChildren.Clear();

            Pool.Release(this);
        }
    }
}
