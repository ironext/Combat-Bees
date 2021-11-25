using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Transforms;

namespace Unity.Entities.Editor
{
    [BurstCompatible]
    struct HierarchyEntityChanges : IDisposable
    {
        public NativeList<Entity> CreatedEntities;
        public NativeList<Entity> DestroyedEntities;
        public NativeList<Entity> AddedSceneReferencesEntities;
        public NativeList<Entity> RemovedSceneReferencesEntities;
        public NativeList<Entity> AddedParentEntities;
        public NativeList<Entity> RemovedParentEntities;
        public NativeList<Entity> AddedSceneTagEntities;
        public NativeList<Entity> RemovedSceneTagEntities;
        public NativeList<Parent> AddedParentComponents;
        public NativeList<SceneTag> AddedSceneTagComponents;

        public bool HasChanges()
        {
            return !(CreatedEntities.Length == 0
                     && DestroyedEntities.Length == 0
                     && AddedSceneReferencesEntities.Length == 0
                     && RemovedSceneReferencesEntities.Length == 0
                     && AddedParentEntities.Length == 0
                     && RemovedParentEntities.Length == 0
                     && AddedSceneTagEntities.Length == 0
                     && RemovedSceneTagEntities.Length == 0
                     && AddedParentComponents.Length == 0
                     && AddedSceneTagComponents.Length == 0);
        }

        public int GetChangeCount()
        {
            var count = 0;

            count += CreatedEntities.Length;
            count += DestroyedEntities.Length;
            count += AddedSceneReferencesEntities.Length;
            count += RemovedSceneReferencesEntities.Length;
            count += AddedParentEntities.Length;
            count += RemovedParentEntities.Length;
            count += AddedSceneTagEntities.Length;
            count += RemovedSceneTagEntities.Length;

            return count;
        }

        public void Clear()
        {
            CreatedEntities.Clear();
            DestroyedEntities.Clear();
            AddedSceneReferencesEntities.Clear();
            RemovedSceneReferencesEntities.Clear();
            AddedParentEntities.Clear();
            RemovedParentEntities.Clear();
            AddedSceneTagEntities.Clear();
            RemovedSceneTagEntities.Clear();
            AddedParentComponents.Clear();
            AddedSceneTagComponents.Clear();
        }

        public HierarchyEntityChanges(Allocator allocator)
        {
            CreatedEntities = new NativeList<Entity>(allocator);
            DestroyedEntities = new NativeList<Entity>(allocator);
            AddedSceneReferencesEntities = new NativeList<Entity>(allocator);
            RemovedSceneReferencesEntities = new NativeList<Entity>(allocator);
            AddedParentEntities = new NativeList<Entity>(allocator);
            RemovedParentEntities = new NativeList<Entity>(allocator);
            AddedSceneTagEntities = new NativeList<Entity>(allocator);
            RemovedSceneTagEntities = new NativeList<Entity>(allocator);
            AddedParentComponents = new NativeList<Parent>(allocator);
            AddedSceneTagComponents = new NativeList<SceneTag>(allocator);
        }

        public void Dispose()
        {
            CreatedEntities.Dispose();
            DestroyedEntities.Dispose();
            AddedSceneReferencesEntities.Dispose();
            RemovedSceneReferencesEntities.Dispose();
            AddedParentEntities.Dispose();
            RemovedParentEntities.Dispose();
            AddedSceneTagEntities.Dispose();
            RemovedSceneTagEntities.Dispose();
            AddedParentComponents.Dispose();
            AddedSceneTagComponents.Dispose();
        }
    }

    /// <summary>
    /// The <see cref="HierarchyEntityChangeTracker"/> is responsible for tracking hierarchy changes over time from the underlying data model (entity or gameObject).
    /// </summary>
    class HierarchyEntityChangeTracker : IDisposable
    {
        static readonly EntityQueryDesc k_EntityQueryDesc = new EntityQueryDesc();
        static readonly EntityQueryDesc k_ParentQueryDesc = new EntityQueryDesc {All = new ComponentType[] {typeof(Parent)}};
        static readonly EntityQueryDesc k_SceneReferenceQueryDesc = new EntityQueryDesc {All = new ComponentType[] {typeof(SceneReference)}};
        static readonly EntityQueryDesc k_SceneTagQueryDesc = new EntityQueryDesc {All = new ComponentType[] {typeof(SceneTag)}, None = new ComponentType[] {typeof(Parent)}};

        readonly World m_World;

        /// <summary>
        /// Change trackers handle the low level entities APIs and gather a set of potentially changed data.
        /// </summary>
        readonly EntityDiffer m_EntityChangeTracker;

        readonly ComponentDataDiffer m_ParentChangeTracker;
        readonly ComponentDataDiffer m_SceneReferenceChangeTracker;
        readonly SharedComponentDataDiffer m_SceneTagChangeTracker;
        readonly EntityQuery m_EmptyQuery;

        EntityQueryDesc m_EntityQueryDesc;

        EntityQuery m_EntityQuery;
        EntityQuery m_ParentQuery;
        EntityQuery m_SceneReferenceQuery;
        EntityQuery m_SceneTagQuery;

        NativeList<int> m_DistinctBuffer;

        /// <summary>
        /// Gets or sets the entity query used by the change tracker. 
        /// </summary>
        /// <remarks>
        /// A value of 'null' indicates the 'UniversalQuery' should be used.
        /// </remarks>
        public EntityQueryDesc EntityQueryDesc
        {
            get => m_EntityQueryDesc;
            set
            {
                if (m_EntityQueryDesc == value)
                    return;

                m_EntityQueryDesc = value;
                RebuildQueryCache(value);
            }
        }

        void RebuildQueryCache(EntityQueryDesc value)
        {
            var desc = null == value ? m_World.EntityManager.UniversalQuery.GetEntityQueryDesc() : value;

            m_EntityQuery = CreateEntityQuery(desc, k_EntityQueryDesc);
            m_ParentQuery = CreateEntityQuery(desc, k_ParentQueryDesc);
            m_SceneReferenceQuery = CreateEntityQuery(desc, k_SceneReferenceQueryDesc);

            if (null == value)
            {
                m_SceneTagQuery = CreateEntityQuery(desc, k_SceneTagQueryDesc);
            }
            else
            {
                m_SceneTagQuery = m_EmptyQuery;
            }
        }

        public HierarchyEntityChangeTracker(World world, Allocator allocator)
        {
            m_World = world;
            m_EntityChangeTracker = new EntityDiffer(world);
            m_ParentChangeTracker = new ComponentDataDiffer(ComponentType.ReadOnly<Parent>());
            m_SceneReferenceChangeTracker = new ComponentDataDiffer(ComponentType.ReadOnly<SceneReference>());
            m_SceneTagChangeTracker = new SharedComponentDataDiffer(ComponentType.ReadOnly<SceneTag>());
            m_DistinctBuffer = new NativeList<int>(16, allocator);
            m_EmptyQuery = m_World.EntityManager.CreateEntityQuery(new EntityQueryDesc{None = new ComponentType[] {typeof(Entity)}});

            RebuildQueryCache(null);
        }

        public void Dispose()
        {
            m_EntityChangeTracker.Dispose();
            m_ParentChangeTracker.Dispose();
            m_SceneReferenceChangeTracker.Dispose();
            m_SceneTagChangeTracker.Dispose();
            m_DistinctBuffer.Dispose();
        }

        public HierarchyEntityChanges GetChanges(Allocator allocator)
        {
            var changes = new HierarchyEntityChanges(allocator);
            GetChanges(changes);
            return changes;
        }

        public void GetChanges(HierarchyEntityChanges changes)
        {
            changes.Clear();

            var entityChangesJobHandle = m_EntityChangeTracker.GetEntityQueryMatchDiffAsync(m_EntityQuery, changes.CreatedEntities, changes.DestroyedEntities);
            var parentChanges = m_ParentChangeTracker.GatherComponentChangesAsync(m_ParentQuery, Allocator.TempJob, out var parentComponentChangesJobHandle);
            var sceneReferenceChanges = m_SceneReferenceChangeTracker.GatherComponentChangesAsync(m_SceneReferenceQuery, Allocator.TempJob, out var sceneReferenceChangesJobHandle);
            var sceneTagChanges = m_SceneTagChangeTracker.GatherComponentChanges(m_World.EntityManager, m_SceneTagQuery, Allocator.TempJob);

            JobHandle.CombineDependencies(entityChangesJobHandle, parentComponentChangesJobHandle, sceneReferenceChangesJobHandle).Complete();

            if (changes.HasChanges())
            {
                // @TODO burst this
                parentChanges.GetAddedComponentEntities(changes.AddedParentEntities);
                parentChanges.GetRemovedComponentEntities(changes.RemovedParentEntities);
                parentChanges.GetAddedComponentData(changes.AddedParentComponents);
                sceneReferenceChanges.GetAddedComponentEntities(changes.AddedSceneReferencesEntities);
                sceneReferenceChanges.GetRemovedComponentEntities(changes.RemovedSceneReferencesEntities);
                sceneTagChanges.GetAddedComponentEntities(changes.AddedSceneTagEntities);
                sceneTagChanges.GetRemovedComponentEntities(changes.RemovedSceneTagEntities);
                sceneTagChanges.GetAddedComponentData(changes.AddedSceneTagComponents);

                new DistinctJob
                {
                    EntityCapacity = m_World.EntityManager.EntityCapacity,
                    Changes = changes,
                    DistinctBuffer = m_DistinctBuffer
                }.Run();
            }

            parentChanges.Dispose();
            sceneReferenceChanges.Dispose();
            sceneTagChanges.Dispose();
        }

        [BurstCompile]
        struct DistinctJob : IJob
        {
            public int EntityCapacity;
            public HierarchyEntityChanges Changes;
            public NativeList<int> DistinctBuffer;

            public void Execute()
            {
                DistinctBuffer.ResizeUninitialized(EntityCapacity);

                if (Changes.CreatedEntities.Length > 0 && Changes.DestroyedEntities.Length > 0)
                    RemoveDuplicate(DistinctBuffer, Changes.CreatedEntities, Changes.DestroyedEntities);

                if (Changes.AddedParentEntities.Length > 0 && Changes.RemovedParentEntities.Length > 0)
                    RemoveDuplicate(DistinctBuffer, Changes.AddedParentEntities, Changes.RemovedParentEntities, Changes.AddedParentComponents);

                if (Changes.AddedSceneReferencesEntities.Length > 0 && Changes.RemovedSceneReferencesEntities.Length > 0)
                    RemoveDuplicate(DistinctBuffer, Changes.AddedSceneReferencesEntities, Changes.RemovedSceneReferencesEntities);

                if (Changes.AddedSceneTagEntities.Length > 0 && Changes.RemovedSceneTagEntities.Length > 0)
                    RemoveDuplicate(DistinctBuffer, Changes.AddedSceneTagEntities, Changes.RemovedSceneTagEntities);
            }

            static unsafe void RemoveDuplicate(NativeList<int> index, NativeList<Entity> added, NativeList<Entity> removed)
            {
                UnsafeUtility.MemClear(index.GetUnsafePtr(), index.Length * sizeof(int));

                var addedLength = added.Length;
                var removedLength = removed.Length;

                for (var i = 0; i < addedLength; i++)
                    index[added[i].Index] = i + 1;

                for (var i = 0; i < removedLength; i++)
                {
                    var addIndex = index[removed[i].Index] - 1;

                    if (addIndex < 0)
                        continue;

                    // An entity was recorded as added AND removed with the same index.
                    var addedEntity = added[addIndex];
                    var removedEntity = removed[i];

                    if (addedEntity.Version != removedEntity.Version)
                        continue;

                    // Swap back
                    added[addIndex] = added[addedLength - 1];
                    removed[i] = removed[removedLength - 1];

                    index[added[addIndex].Index] = addIndex + 1;

                    addedLength--;
                    removedLength--;
                    i--;
                }

                added.ResizeUninitialized(addedLength);
                removed.ResizeUninitialized(removedLength);
            }

            unsafe void RemoveDuplicate<TData>(NativeArray<int> index, NativeList<Entity> added, NativeList<Entity> removed, NativeList<TData> data) where TData : unmanaged
            {
                UnsafeUtility.MemClear(index.GetUnsafePtr(), index.Length * sizeof(int));

                var addedLength = added.Length;
                var removedLength = removed.Length;

                for (var i = 0; i < addedLength; i++)
                    index[added[i].Index] = i + 1;

                for (var i = 0; i < removedLength; i++)
                {
                    var addIndex = index[removed[i].Index] - 1;

                    if (addIndex < 0)
                        continue;

                    // An entity was recorded as added AND removed with the same index.
                    var addedEntity = added[addIndex];
                    var removedEntity = removed[i];

                    if (addedEntity.Version != removedEntity.Version)
                        continue;

                    // Swap back
                    added[addIndex] = added[addedLength - 1];
                    data[addIndex] = data[addedLength - 1];
                    removed[i] = removed[removedLength - 1];

                    index[added[addIndex].Index] = addIndex + 1;

                    addedLength--;
                    removedLength--;
                }

                added.ResizeUninitialized(addedLength);
                data.ResizeUninitialized(addedLength);
                removed.ResizeUninitialized(removedLength);
            }
        }
        
        EntityQuery CreateEntityQuery(params EntityQueryDesc[] queriesDesc)
        {
            try
            {
                ValidateEntityQueryDesc(queriesDesc);
            }
            catch
            {
                return m_EmptyQuery;
            }
            
            using var builder = new EntityQueryDescBuilder(Allocator.Temp);
            
            for (var q = 0; q != queriesDesc.Length; q++)
            {
                foreach (var type in queriesDesc[q].All)
                    builder.AddAll(type);

                foreach (var type in queriesDesc[q].Any)
                    builder.AddAny(type);

                foreach (var type in queriesDesc[q].None)
                    builder.AddNone(type);
            }
            
            builder.Options(queriesDesc[0].Options);
            builder.FinalizeQuery();
            
            return m_World.EntityManager.CreateEntityQuery(builder);
        }

        static void ValidateEntityQueryDesc(params EntityQueryDesc[] queriesDesc)
        {
            var count = 0;
            
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var entityQueryDesc in queriesDesc)
                count += entityQueryDesc.None.Length + entityQueryDesc.All.Length + entityQueryDesc.Any.Length;

            var componentTypeIds = new NativeArray<int>(count, Allocator.Temp);
            
            var componentIndex = 0;

            foreach (var entityQueryDesc in queriesDesc)
            {
                ValidateComponentTypes(entityQueryDesc.None, ref componentTypeIds, ref componentIndex);
                ValidateComponentTypes(entityQueryDesc.All, ref componentTypeIds, ref componentIndex);
                ValidateComponentTypes(entityQueryDesc.Any, ref componentTypeIds, ref componentIndex);
            }

            // Check for duplicate, only if necessary
            if (count > 1)
            {
                // Sort the Ids to have identical value adjacent
                componentTypeIds.Sort();

                // Check for identical values
                var refId = componentTypeIds[0];
                
                for (var i = 1; i < componentTypeIds.Length; i++)
                {
                    var curId = componentTypeIds[i];
                    if (curId == refId)
                    {
                        var compType = TypeManager.GetType(curId);
                        throw new EntityQueryDescValidationException(
                            $"EntityQuery contains a filter with duplicate component type name {compType.Name}.  Queries can only contain a single component of a given type in a filter.");
                    }

                    refId = curId;
                }
            }

            componentTypeIds.Dispose();
        }
        
        static void ValidateComponentTypes(ComponentType[] componentTypes, ref NativeArray<int> allComponentTypeIds, ref int curComponentTypeIndex)
        {
            foreach (var componentType in componentTypes)
            {
                allComponentTypeIds[curComponentTypeIndex++] = componentType.TypeIndex;
                
                if (componentType.AccessModeType == ComponentType.AccessMode.Exclude)
                    throw new ArgumentException("EntityQueryDesc cannot contain Exclude Component types");
            }
        }
    }
}