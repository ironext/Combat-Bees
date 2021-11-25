using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Editor.Bridge;
using Unity.Jobs;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Entities.Editor
{
    readonly struct HierarchyGameObjectChanges : IDisposable
    {
        public readonly NativeList<Scene> LoadedScenes;
        public readonly NativeList<Scene> UnloadedScenes;
        public readonly NativeList<GameObjectChangeTrackerEvent> GameObjectChangeTrackerEvents;

        public bool HasChanges() => !(LoadedScenes.Length == 0 && UnloadedScenes.Length == 0);
        
        public HierarchyGameObjectChanges(Allocator allocator)
        {
            LoadedScenes = new NativeList<Scene>(allocator);
            UnloadedScenes = new NativeList<Scene>(allocator);
            GameObjectChangeTrackerEvents = new NativeList<GameObjectChangeTrackerEvent>(allocator);
        }

        public void Clear()
        {
            LoadedScenes.Clear();
            UnloadedScenes.Clear();
            GameObjectChangeTrackerEvents.Clear();
        }
        
        public void Dispose()
        {
            LoadedScenes.Dispose();
            UnloadedScenes.Dispose();
            GameObjectChangeTrackerEvents.Dispose();
        }
    }

    readonly struct GameObjectChangeTrackerEvent
    {
        public readonly EventType Type;
        public readonly int InstanceId;
        public readonly int OptionalNewParent;

        public GameObjectChangeTrackerEvent(EventType eventType, int instanceId, int optionalNewParent = 0)
        {
            InstanceId = instanceId;
            Type = eventType;
            OptionalNewParent = optionalNewParent;
        }

        internal static GameObjectChangeTrackerEvent CreatedOrChanged(GameObject obj)
            => new GameObjectChangeTrackerEvent(EventType.CreatedOrChanged, obj.GetInstanceID());

        internal static GameObjectChangeTrackerEvent Destroyed(GameObject obj)
            => new GameObjectChangeTrackerEvent(EventType.Destroyed, obj.GetInstanceID());

        internal static GameObjectChangeTrackerEvent Moved(GameObject obj, Transform newParent)
            => new GameObjectChangeTrackerEvent(EventType.Moved, obj.GetInstanceID(), newParent ? newParent.gameObject.GetInstanceID() : 0);

        internal GameObjectChangeTrackerEvent Merge(ref GameObjectChangeTrackerEvent eventToApply)
            => new GameObjectChangeTrackerEvent(Type | eventToApply.Type, InstanceId, eventToApply.OptionalNewParent);

        [Flags]
        public enum EventType : byte
        {
            CreatedOrChanged = 1 << 0,
            Moved = 1 << 1,
            Destroyed = 1 << 2
        }
    }

    class HierarchyGameObjectChangeTracker : IDisposable
    {
        readonly HashSet<Scene> m_Scenes = new HashSet<Scene>();
        readonly HashSet<Scene> m_UnloadedScenes = new HashSet<Scene>();

        bool m_SceneManagerChanged;

        public HierarchyGameObjectChangeTracker(Allocator allocator)
        {
            m_SceneManagerChanged = true;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Dispose()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            m_SceneManagerChanged = true;
        }
        
        void OnSceneClosed(Scene scene)
        {
            m_SceneManagerChanged = true;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            m_SceneManagerChanged = true;
        }
        
        void OnSceneUnloaded(Scene scene)
        {
            m_SceneManagerChanged = true;
        }

        public void GetChanges(HierarchyGameObjectChanges changes)
        {
            changes.Clear();
            
            if (m_SceneManagerChanged)
            {
                m_SceneManagerChanged = false;
                
                for (var i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);

                    if (scene.isSubScene)
                        continue;
                    
                    if (m_Scenes.Contains(scene)) 
                        continue;
                    
                    changes.LoadedScenes.Add(scene);
                    m_Scenes.Add(scene);
                }

                var scenes = m_Scenes.ToArray();

                foreach (var scene in scenes)
                {
                    if (scene.isLoaded) 
                        continue;
                    
                    changes.UnloadedScenes.Add(scene);
                    m_Scenes.Remove(scene);
                }
            }
        }

        [BurstCompile]
        internal struct RecordEventJob : IJob
        {
            public NativeList<GameObjectChangeTrackerEvent> Events;
            public GameObjectChangeTrackerEvent Event;

            public void Execute()
            {
                for (var i = 0; i < Events.Length; i++)
                {
                    ref var existingEvent = ref Events.ElementAt(i);
                    if (existingEvent.InstanceId != Event.InstanceId || existingEvent.Type > Event.Type)
                        continue;

                    var e = Events[i].Merge(ref Event);
                    Events[i] = e;
                    return;
                }

                Events.Add(Event);
            }
        }
    }
}
