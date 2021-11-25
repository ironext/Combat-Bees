#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Entities
{
    /// <summary>
    /// Entities journaling provides detailed information about past ECS events.
    /// </summary>
    public static unsafe partial class EntitiesJournaling
    {
        const bool k_EnabledDefault = false;
        const int k_TotalMemoryMBDefault = 4;

#if !UNITY_DOTSRUNTIME
        const string k_EnabledKey = nameof(EntitiesJournaling) + "." + nameof(Enabled);
        const string k_TotalMemoryMBKey = nameof(EntitiesJournaling) + "." + nameof(TotalMemoryMB);
#else
        static bool s_Enabled = k_EnabledDefault;
        static int s_TotalMemoryMB = k_TotalMemoryMBDefault;
#endif

        static Dictionary<ulong, string> s_WorldMap;
        static Dictionary<SystemHandleUntyped, Type> s_SystemMap;

        sealed class SharedInit { internal static readonly SharedStatic<bool> Ref = SharedStatic<bool>.GetOrCreate<SharedInit>(); }
        static ref bool Initialized => ref SharedInit.Ref.Data;

        sealed class SharedState { internal static readonly SharedStatic<JournalingState> Ref = SharedStatic<JournalingState>.GetOrCreate<SharedState>(); }
        static ref JournalingState State => ref SharedState.Ref.Data;

        /// <summary>
        /// Whether or not entities journaling events are recorded.
        /// </summary>
        /// <remarks>
        /// Setting this to <see langword="false"/> does not clear or deallocate the jouraling state.
        /// </remarks>
        [NotBurstCompatible]
        public static bool Enabled
        {
#if !UNITY_DOTSRUNTIME
            get => PlayerPrefs.GetInt(k_EnabledKey, k_EnabledDefault ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt(k_EnabledKey, value ? 1 : 0);
#else
            get => s_Enabled;
            set => s_Enabled = value;
#endif
        }

        /// <summary>
        /// Total memory allocated for entities journaling recording, in megabytes.
        /// </summary>
        /// <remarks>
        /// New value will only take effect on next initialization.
        /// </remarks>
        [NotBurstCompatible]
        public static int TotalMemoryMB
        {
#if !UNITY_DOTSRUNTIME
            get => PlayerPrefs.GetInt(k_TotalMemoryMBKey, k_TotalMemoryMBDefault);
            set => PlayerPrefs.SetInt(k_TotalMemoryMBKey, value);
#else
            get => s_TotalMemoryMB;
            set => s_TotalMemoryMB = value;
#endif
        }

        /// <summary>
        /// Retrieve records currently in buffer, as an enumerable.
        /// </summary>
        /// <remarks>
        /// <b>Call will be blocking if records are currently locked for write.</b>
        /// <para>For this reason, it is not recommended to call this in a debugger watch window, otherwise a deadlock might occur.</para>
        /// </remarks>
        /// <returns><see cref="IEnumerable"/> of <see cref="RecordView"/>.</returns>
        [NotBurstCompatible]
        public static IEnumerable<RecordView> GetRecords() => State.GetRecords(blocking: true);

        /// <summary>
        /// Try to retrieve records currently in buffer, as an enumerable.
        /// </summary>
        /// <remarks>
        /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
        /// </remarks>
        /// <returns><see cref="IEnumerable"/> of <see cref="RecordView"/>.</returns>
        [NotBurstCompatible]
        public static IEnumerable<RecordView> TryGetRecords() => State.GetRecords(blocking: false);

        /// <summary>
        /// Non-blocking utility methods to retrieve records.
        /// </summary>
        public static class Records
        {
            /// <summary>
            /// Get all records currently in buffer.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] All => TryGetRecords().ToArray();

            /// <summary>
            /// Get a number records starting from index.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="index">The start index.</param>
            /// <param name="count">The count of records.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] Range(int index, int count) => TryGetRecords().Skip(index).Take(count).ToArray();

            /// <summary>
            /// Get the record matching a record index.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The record index.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithRecordIndex(ulong index) => TryGetRecords().WithRecordIndex(index).ToArray();

            /// <summary>
            /// Get all records matching a record type.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The record type.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithRecordType(RecordType type) => TryGetRecords().WithRecordType(type).ToArray();

            /// <summary>
            /// Get all records matching a frame index.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The frame index.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithFrameIndex(int index) => TryGetRecords().WithFrameIndex(index).ToArray();

            /// <summary>
            /// Get all records matching a world name.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The world name.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithWorld(string name) => TryGetRecords().WithWorld(name).ToArray();

            /// <summary>
            /// Get all records matching a world sequence number.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The world sequence number.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithWorld(ulong sequenceNumber) => TryGetRecords().WithWorld(sequenceNumber).ToArray();

            /// <summary>
            /// Get all records matching an existing world.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The world.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithWorld(World world) => TryGetRecords().WithWorld(world).ToArray();

            /// <summary>
            /// Get all records matching a system type name.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The system type name.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithSystem(string name) => TryGetRecords().WithSystem(name).ToArray();

            /// <summary>
            /// Get all records matching a system handle untyped.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The system handle untyped.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithSystem(SystemHandleUntyped handle) => TryGetRecords().WithSystem(handle).ToArray();

            /// <summary>
            /// Get all records matching an existing system.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The system.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithSystem(ComponentSystemBase system) => TryGetRecords().WithSystem(system).ToArray();

            /// <summary>
            /// Get all records matching a component type name.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The component type name.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithComponentType(string name) => TryGetRecords().WithComponentType(name).ToArray();

            /// <summary>
            /// Get all records matching a component type.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="typeIndex">The component type.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithComponentType(ComponentType componentType) => GetRecords().WithComponentType(componentType).ToArray();

            /// <summary>
            /// Get all records matching a component type index.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="typeIndex">The component type index.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithComponentType(int typeIndex) => GetRecords().WithComponentType(typeIndex).ToArray();

            /// <summary>
            /// Get all records matching an entity index.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="index">The entity index.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithEntity(int index) => GetRecords().WithEntity(index).ToArray();

            /// <summary>
            /// Get all records matching an entity index and version.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="index">The entity index.</param>
            /// <param name="version">The entity version.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithEntity(int index, int version) => GetRecords().WithEntity(index, version).ToArray();

            /// <summary>
            /// Get all records matching an entity.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="entity">The entity.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithEntity(Entity entity) => GetRecords().WithEntity(entity).ToArray();

#if !DOTS_DISABLE_DEBUG_NAMES
            /// <summary>
            /// Get all records matching an existing entity name.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="InvalidOperationException"/> if records are currently locked for write.
            /// </remarks>
            /// <param name="name">The entity name.</param>
            /// <returns>Array of <see cref="RecordView"/>.</returns>
            [NotBurstCompatible]
            public static RecordView[] WithEntity(string name) => GetRecords().WithEntity(name).ToArray();
#endif
        }

        /// <summary>
        /// Clear all the records.
        /// </summary>
        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        public static void Clear() => State.Clear();

        [NotBurstCompatible]
        internal static void Initialize()
        {
            if (Initialized)
                return;

            State = new JournalingState(Enabled, TotalMemoryMB * 1024 * 1024);
            s_WorldMap = new Dictionary<ulong, string>();
            s_SystemMap = new Dictionary<SystemHandleUntyped, Type>();
            World.WorldCreated += OnWorldCreated;
            World.SystemCreated += OnSystemCreated;

            Initialized = true;
        }

        [NotBurstCompatible]
        internal static void Shutdown()
        {
            if (!Initialized)
                return;

            World.SystemCreated -= OnSystemCreated;
            World.WorldCreated -= OnWorldCreated;
            s_WorldMap = null;
            s_SystemMap = null;
            State.Dispose();

            Initialized = false;
        }

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordWorldCreated(in WorldUnmanaged world) =>
            State.PushBack(RecordType.WorldCreated, in world, (Entity*)null, 0, null, 0, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordWorldDestroyed(in WorldUnmanaged world) =>
            State.PushBack(RecordType.WorldDestroyed, in world, (Entity*)null, 0, null, 0, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSystemAdded(in WorldUnmanaged world, SystemHandleUntyped* system) =>
            State.PushBack(RecordType.SystemAdded, in world, (Entity*)null, 0, null, 0, system, system != null ? UnsafeUtility.SizeOf<SystemHandleUntyped>() : 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSystemRemoved(in WorldUnmanaged world, SystemHandleUntyped* system) =>
            State.PushBack(RecordType.SystemRemoved, in world, (Entity*)null, 0, null, 0, system, system != null ? UnsafeUtility.SizeOf<SystemHandleUntyped>() : 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordCreateEntity(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount) =>
            State.PushBack(RecordType.CreateEntity, in world, entities, entityCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordCreateEntity(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount) =>
            State.PushBack(RecordType.CreateEntity, in world, chunks, chunkCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordDestroyEntity(in WorldUnmanaged world, Entity* entities, int entityCount) =>
            State.PushBack(RecordType.DestroyEntity, in world, entities, entityCount, null, 0, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordDestroyEntity(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount) =>
            State.PushBack(RecordType.DestroyEntity, in world, chunks, chunkCount, null, 0, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordAddComponent(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount) =>
            State.PushBack(RecordType.AddComponent, in world, entities, entityCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordAddComponent(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount) =>
            State.PushBack(RecordType.AddComponent, in world, chunks, chunkCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordRemoveComponent(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount) =>
            State.PushBack(RecordType.RemoveComponent, in world, entities, entityCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordRemoveComponent(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount) =>
            State.PushBack(RecordType.RemoveComponent, in world, chunks, chunkCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetComponentData(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount, void* data, int dataLength) =>
            State.PushBack(RecordType.SetComponentData, in world, entities, entityCount, types, typeCount, data, dataLength);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetComponentData(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount, void* data, int dataLength) =>
            State.PushBack(RecordType.SetComponentData, in world, chunks, chunkCount, types, typeCount, data, dataLength);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetSharedComponentData(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount) =>
            State.PushBack(RecordType.SetSharedComponentData, in world, entities, entityCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetSharedComponentData(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount) =>
            State.PushBack(RecordType.SetSharedComponentData, in world, chunks, chunkCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetComponentObject(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount) =>
            State.PushBack(RecordType.SetComponentObject, in world, entities, entityCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetComponentObject(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount) =>
            State.PushBack(RecordType.SetComponentObject, in world, chunks, chunkCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetBuffer(in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount) =>
            State.PushBack(RecordType.SetBuffer, in world, entities, entityCount, types, typeCount, null, 0);

        [BurstCompatible(RequiredUnityDefine = "DEVELOPMENT_BUILD || UNITY_EDITOR")]
        internal static void RecordSetBuffer(in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount) =>
            State.PushBack(RecordType.SetBuffer, in world, chunks, chunkCount, types, typeCount, null, 0);

        static void OnWorldCreated(World world)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (world == null)
                throw new ArgumentNullException(nameof(world));
#endif
            s_WorldMap.Add(world.SequenceNumber, world.Name);
        }

        static void OnSystemCreated(World world, ComponentSystemBase system)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (system == null)
                throw new ArgumentNullException(nameof(system));
            if (system.m_StatePtr == null)
                throw new ArgumentException(nameof(system.m_StatePtr));
#endif
            s_SystemMap.Add(system.m_StatePtr->m_Handle, system.GetType());
        }

        static World GetWorld(ulong worldSeqNumber)
        {
            for (var i = 0; i < World.All.Count; ++i)
            {
                var world = World.All[i];
                if (!world.IsCreated)
                    continue;

                if (world.SequenceNumber == worldSeqNumber)
                    return world;
            }
            return null;
        }

        static ComponentSystemBase GetSystem(SystemHandleUntyped systemHandle)
        {
            var world = GetWorld(systemHandle.m_WorldSeqNo);
            if (world == null)
                return null;

            for (var i = 0; i < world.Systems.Count; ++i)
            {
                var system = world.Systems[i];
                var statePtr = system.m_StatePtr;
                if (statePtr == null)
                    continue;

                if (statePtr->m_Handle == systemHandle)
                    return system;
            }
            return null;
        }

        static Entity GetEntity(int index, int version, ulong worldSeqNumber)
        {
            var world = GetWorld(worldSeqNumber);
            if (world == null)
                return Entity.Null;

            var entity = new Entity { Index = index, Version = version };
            return world.EntityManager.Exists(entity) ? entity : Entity.Null;
        }
    }
}
#endif
