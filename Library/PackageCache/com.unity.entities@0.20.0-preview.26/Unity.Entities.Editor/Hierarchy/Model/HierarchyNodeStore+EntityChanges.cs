using System;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Entities.Editor
{
    partial struct HierarchyNodeStore
    {
        /// <summary>
        /// Represents a batch of changes to be integrated in to the dynamic hierarchy model.
        /// </summary>
        unsafe struct HierarchyEntityChangesBatch
        {
            /// <summary>
            /// !!IMPORTANT!! This is the order we will process elements for the batch.
            /// </summary>
            enum ChangeType
            {
                RemovedSceneReferences = 0,
                RemovedSceneTags,
                RemovedParents,
                DestroyedEntities,
                CreatedEntities,
                AddedSceneReferences,
                AddedSceneTags,
                AddedParents,
            }

            readonly struct Range
            {
                public readonly int Start;
                public readonly int Count;

                public Range(int start, int count)
                {
                    Start = start;
                    Count = count;
                }
            }

            readonly HierarchyEntityChanges m_Changes;
            readonly int m_BatchStart;
            readonly int m_BatchCount;
            
#pragma warning disable 649
            fixed int m_Offset[8];
            fixed int m_Length[8];
#pragma warning restore 649

            public NativeArray<Entity> CreatedEntities => SubArray(m_Changes.CreatedEntities, GetRange(ChangeType.CreatedEntities));
            public NativeArray<Entity> DestroyedEntities => SubArray(m_Changes.DestroyedEntities, GetRange(ChangeType.DestroyedEntities));
            public NativeArray<Entity> AddedSceneReferencesEntities=> SubArray(m_Changes.AddedSceneReferencesEntities, GetRange(ChangeType.AddedSceneReferences));
            public NativeArray<Entity> RemovedSceneReferencesEntities=> SubArray(m_Changes.RemovedSceneReferencesEntities, GetRange(ChangeType.RemovedSceneReferences));
            public NativeArray<Entity> AddedParentEntities=> SubArray(m_Changes.AddedParentEntities, GetRange(ChangeType.AddedParents));
            public NativeArray<Entity> RemovedParentEntities=> SubArray(m_Changes.RemovedParentEntities, GetRange(ChangeType.RemovedParents));
            public NativeArray<Entity> AddedSceneTagEntities=> SubArray(m_Changes.AddedSceneTagEntities, GetRange(ChangeType.AddedSceneTags));
            public NativeArray<Entity> RemovedSceneTagEntities=> SubArray(m_Changes.RemovedSceneTagEntities, GetRange(ChangeType.RemovedSceneTags));
            public NativeArray<Parent> AddedParentComponents=> SubArray(m_Changes.AddedParentComponents, GetRange(ChangeType.AddedParents));
            public NativeArray<SceneTag> AddedSceneTagComponents=> SubArray(m_Changes.AddedSceneTagComponents, GetRange(ChangeType.AddedSceneTags));

            public HierarchyEntityChangesBatch(HierarchyEntityChanges changes, int batchStart, int batchCount)
            {
                m_Changes = changes;
                m_BatchStart = batchStart;
                m_BatchCount = batchCount;
                
                // !!IMPORTANT!! This order MUST match the 'ChangeType' enum.
                m_Length[0] = m_Changes.RemovedSceneReferencesEntities.Length;
                m_Length[1] = m_Changes.RemovedSceneTagEntities.Length;
                m_Length[2] = m_Changes.RemovedParentEntities.Length;
                m_Length[3] = m_Changes.DestroyedEntities.Length;
                m_Length[4] = m_Changes.CreatedEntities.Length;
                m_Length[5] = m_Changes.AddedSceneReferencesEntities.Length;
                m_Length[6] = m_Changes.AddedSceneTagEntities.Length;
                m_Length[7] = m_Changes.AddedParentEntities.Length;

                var offset = 0;
                for (var i = 0; i < 8; i++)
                {
                    m_Offset[i] = offset;
                    offset += m_Length[i];
                }
            }

            Range GetRange(ChangeType type)
            {
                var offset = m_Offset[(int) type];
                var length = m_Length[(int) type];
                var start = math.clamp(m_BatchStart - offset, 0, length);
                var count = math.clamp(m_BatchStart - offset + m_BatchCount - start, 0, length - start);
                return new Range(start, count);
            }

            static NativeArray<T> SubArray<T>(NativeList<T> list, Range range) where T : unmanaged
            {
                if (range.Count == 0) return default;
                return list.AsArray().GetSubArray(range.Start, range.Count);
            }
        }

        public struct IntegrateEntityChangesEnumerator : IEnumerator
        {
            readonly HierarchyNodeStore m_Hierarchy;
            readonly EntityManager m_EntityManager;
            readonly HierarchyEntityChanges m_Changes;

            int m_TotalCount;
            int m_BatchStart;
            int m_BatchCount;
            
            bool m_IsDone;
            
            public object Current => null;

            public void Reset() 
                => throw new InvalidOperationException($"{nameof(IntegrateEntityChangesEnumerator)} can not be reset. Instead a new instance should be used with a new change set.");

            public float Progress => m_TotalCount > 0 ? m_BatchStart / (float) m_TotalCount : 0;

            public IntegrateEntityChangesEnumerator(HierarchyNodeStore hierarchy, EntityManager entityManager, HierarchyEntityChanges changes, int batchSize)
            {
                m_Hierarchy = hierarchy;
                m_EntityManager = entityManager;
                m_Changes = changes;
                m_TotalCount = changes.GetChangeCount();
                m_BatchStart = 0;
                m_BatchCount = batchSize > 0 ? batchSize : m_TotalCount;
                m_IsDone = false;
                
                m_Hierarchy.m_Nodes.ResizeEntityCapacity(entityManager.EntityCapacity);
                
                for (var i = 0; i < changes.AddedSceneReferencesEntities.Length; i++)
                    m_Hierarchy.m_EntityScenes.Add(changes.AddedSceneReferencesEntities[i], entityManager.GetComponentObject<Scenes.SubScene>(changes.AddedSceneReferencesEntities[i]).gameObject.scene);

                for (var i = 0; i < changes.RemovedSceneReferencesEntities.Length; i++)
                    m_Hierarchy.m_EntityScenes.Remove(changes.RemovedSceneReferencesEntities[i]);
            }
            
            public bool MoveNext()
            {
                unsafe
                {
                    if (m_IsDone)
                        return false;
                    
                    new IntegrateEntityChangesJob
                    {
                        EntityDataAccess = m_EntityManager.GetCheckedEntityDataAccess(),
                        Batch = new HierarchyEntityChangesBatch(m_Changes, m_BatchStart, m_BatchCount),
                        Hierarchy = m_Hierarchy
                    }.Run();

                    m_BatchStart += m_BatchCount;

                    if (m_BatchStart < m_TotalCount) 
                        return true;
                    
                    m_IsDone = true;
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Integrates the given <see cref="HierarchyEntityChanges"/> set in to this hierarchy.
        /// </summary>
        /// <param name="entityManager">The entity  manager being operated on.</param>
        /// <param name="changes">The entity changes to apply.</param>
        /// <returns>The scheduled job handle.</returns>
        public void IntegrateEntityChanges(EntityManager entityManager, HierarchyEntityChanges changes)
        {
            var enumerator = CreateIntegrateEntityChangesEnumerator(entityManager, changes, changes.GetChangeCount());
            while (enumerator.MoveNext()) { }
        }

        /// <summary>
        /// Creates an enumerator which will integrate the given entity changes over several ticks.
        /// </summary>
        /// <param name="entityManager">The entity manager being operated on.</param>
        /// <param name="changes">The entity changes to apply.</param>
        /// <param name="batchSize">The number of changes to integrate per tick.</param>
        /// <returns>An enumerator which can be ticked.</returns>
        public IntegrateEntityChangesEnumerator CreateIntegrateEntityChangesEnumerator(EntityManager entityManager, HierarchyEntityChanges changes, int batchSize)
        {
            return new IntegrateEntityChangesEnumerator(this, entityManager, changes, batchSize);
        }

        [BurstCompile]
        unsafe struct IntegrateEntityChangesJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityDataAccess* EntityDataAccess;
            
            public HierarchyEntityChangesBatch Batch;
            public HierarchyNodeStore Hierarchy;

            public void Execute()
            {
                var removedSceneReferencesEntities = Batch.RemovedSceneReferencesEntities;
                var removedSceneTagEntities = Batch.RemovedSceneTagEntities;
                var removedParentEntities = Batch.RemovedParentEntities;
                var destroyedEntities = Batch.DestroyedEntities;
                var createdEntities = Batch.CreatedEntities;
                var addedSceneReferencesEntities = Batch.AddedSceneReferencesEntities;
                var addedSceneTagEntities = Batch.AddedSceneTagEntities;
                var addedSceneTagComponents = Batch.AddedSceneTagComponents;
                var addedParentEntities = Batch.AddedParentEntities;
                var addedParentComponents = Batch.AddedParentComponents;
                
                for (var i = 0; i < removedSceneReferencesEntities.Length; i++)
                    Hierarchy.RemoveNode(HierarchyNodeHandle.FromSubScene(removedSceneReferencesEntities[i]));

                for (var i = 0; i < removedSceneTagEntities.Length; i++)
                    Hierarchy.SetParent(HierarchyNodeHandle.FromEntity(removedSceneTagEntities[i]), HierarchyNodeHandle.Root);
                
                for (var i = 0; i < removedParentEntities.Length; i++)
                    Hierarchy.SetParent(HierarchyNodeHandle.FromEntity(removedParentEntities[i]), HierarchyNodeHandle.Root);

                for (var i = 0; i < destroyedEntities.Length; i++)
                    Hierarchy.RemoveNode(HierarchyNodeHandle.FromEntity(destroyedEntities[i]));

                for (var i = 0; i < createdEntities.Length; i++)
                    Hierarchy.m_Nodes.ValueByEntity.SetValueDefaultUnchecked(createdEntities[i]);

                for (var i = 0; i < addedSceneReferencesEntities.Length; i++)
                    Hierarchy.AddNode(HierarchyNodeHandle.FromSubScene(addedSceneReferencesEntities[i]), HierarchyNodeHandle.FromScene(Hierarchy.m_EntityScenes[addedSceneReferencesEntities[i]]));

                for (var i = 0; i < addedSceneTagEntities.Length; i++)
                    if (addedSceneTagComponents[i].SceneEntity != Entity.Null)
                        Hierarchy.SetParent(HierarchyNodeHandle.FromEntity(addedSceneTagEntities[i]), HierarchyNodeHandle.FromSubScene(EntityDataAccess, addedSceneTagComponents[i]));

                for (var i = 0; i < addedParentEntities.Length; i++)
                    Hierarchy.SetParent(HierarchyNodeHandle.FromEntity(addedParentEntities[i]), HierarchyNodeHandle.FromEntity(addedParentComponents[i].Value));
                
                // @FIXME This should be done once at the end and not during each batch.
                Hierarchy.UpdateChangeVersion(HierarchyNodeHandle.Root);
            }
        }
    }
}