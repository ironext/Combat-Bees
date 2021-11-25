using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Entities
{
    public class EntityQueryDescValidationException : Exception
    {
        public EntityQueryDescValidationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Describes a query to find archetypes in terms of required, optional, and excluded
    /// components.
    /// </summary>
    /// <remarks>
    /// Define an EntityQueryDesc object to describe complex queries. Inside a system,
    /// pass an EntityQueryDesc object to <see cref="ComponentSystemBase.GetEntityQuery(EntityQueryDesc[])"/>
    /// to create the <see cref="EntityQuery"/>.
    ///
    /// A query description combines the component types you specify in `All`, `Any`, and `None` sets according to the
    /// following rules:
    ///
    /// * All - Includes archetypes that have every component in this set
    /// * Any - Includes archetypes that have at least one component in this set
    /// * None - Excludes archetypes that have any component in this set
    ///
    /// For example, given entities with the following components:
    ///
    /// * Player has components: Position, Rotation, Player
    /// * Enemy1 has components: Position, Rotation, Melee
    /// * Enemy2 has components: Position, Rotation, Ranger
    ///
    /// The query description below matches all of the archetypes that:
    /// have any of [Melee or Ranger], AND have none of [Player], AND have all of [Position and Rotation]
    ///
    /// <example>
    /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="query-description" title="Query Description"/>
    /// </example>
    ///
    /// In other words, the query created from this description selects the Enemy1 and Enemy2 entities, but not the Player entity.
    /// </remarks>
    public class EntityQueryDesc : IEquatable<EntityQueryDesc>
    {
        /// <summary>
        /// Include archetypes that contain at least one (but possibly more) of the
        /// component types in the Any list.
        /// </summary>
        public ComponentType[] Any = Array.Empty<ComponentType>();
        /// <summary>
        /// Exclude archetypes that contain any of the
        /// component types in the None list.
        /// </summary>
        public ComponentType[] None = Array.Empty<ComponentType>();
        /// <summary>
        /// Include archetypes that contain all of the
        /// component types in the All list.
        /// </summary>
        public ComponentType[] All = Array.Empty<ComponentType>();
        /// <summary>
        /// Specialized query options.
        /// </summary>
        /// <remarks>
        /// You should not need to set these options for most queries.
        ///
        /// Options is a bit mask; use the bitwise OR operator to combine multiple options.
        /// </remarks>
        public EntityQueryOptions Options = EntityQueryOptions.Default;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void ValidateComponentTypes(ComponentType[] componentTypes, ref NativeArray<int> allComponentTypeIds, ref int curComponentTypeIndex)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var componentType = componentTypes[i];
                allComponentTypeIds[curComponentTypeIndex++] = componentType.TypeIndex;
                if (componentType.AccessModeType == ComponentType.AccessMode.Exclude)
                    throw new ArgumentException("EntityQueryDesc cannot contain Exclude Component types");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void Validate()
        {
            // Determine the number of ComponentTypes contained in the filters
            var itemCount = None.Length + All.Length + Any.Length;

            // Project all the ComponentType Ids of None, All, Any queryDesc filters into the same array to identify duplicated later on
            // Also, check that queryDesc doesn't contain any ExcludeComponent...

            var allComponentTypeIds = new NativeArray<int>(itemCount, Allocator.Temp);
            var curComponentTypeIndex = 0;
            ValidateComponentTypes(None, ref allComponentTypeIds, ref curComponentTypeIndex);
            ValidateComponentTypes(All, ref allComponentTypeIds, ref curComponentTypeIndex);
            ValidateComponentTypes(Any, ref allComponentTypeIds, ref curComponentTypeIndex);

            // Check for duplicate, only if necessary
            if (itemCount > 1)
            {
                // Sort the Ids to have identical value adjacent
                allComponentTypeIds.Sort();

                // Check for identical values
                var refId = allComponentTypeIds[0];
                for (int i = 1; i < allComponentTypeIds.Length; i++)
                {
                    var curId = allComponentTypeIds[i];
                    if (curId == refId)
                    {
#if NET_DOTS
                        throw new EntityQueryDescValidationException(
                            $"EntityQuery contains a filter with duplicate component type index {curId}.  Queries can only contain a single component of a given type in a filter.");
#else
                        var compType = TypeManager.GetType(curId);
                        throw new EntityQueryDescValidationException(
                            $"EntityQuery contains a filter with duplicate component type name {compType.Name}.  Queries can only contain a single component of a given type in a filter.");
#endif
                    }

                    refId = curId;
                }
            }

            allComponentTypeIds.Dispose();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntityQueryDesc);
        }

        public bool Equals(EntityQueryDesc other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (ReferenceEquals(null, other))
                return false;

            if (!Options.Equals(other.Options))
                return false;

            if (!ArraysEqual(All, other.All))
                return false;

            if (!ArraysEqual(Any, other.Any))
                return false;

            if (!ArraysEqual(None, other.None))
                return false;

            return true;
        }

        public static bool operator ==(EntityQueryDesc lhs, EntityQueryDesc rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);

            return lhs.Equals(rhs);
        }

        public static bool operator !=(EntityQueryDesc lhs, EntityQueryDesc rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            int result = 17;
            result = (result * 397) ^ Options.GetHashCode();
            result = (result * 397) ^ (All ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (Any ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (None ?? Array.Empty<ComponentType>()).GetHashCode();
            return result;
        }

        static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; ++i)
            {
                if (!comparer.Equals(a1[i], a2[i]))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// The bit flags to use for the <see cref="EntityQueryDesc.Options"/> field.
    /// </summary>
    [Flags]
    public enum EntityQueryOptions
    {
        /// <summary>
        /// No options specified.
        /// </summary>
        Default = 0,
        /// <summary>
        /// The query does not exclude the special <see cref="Prefab"/> component.
        /// </summary>
        IncludePrefab = 1,
        /// <summary>
        /// The query does not exclude the special <see cref="Disabled"/> component.
        /// </summary>
        IncludeDisabled = 2,
        /// <summary>
        /// The query filters selected entities based on the
        /// <see cref="WriteGroupAttribute"/> settings of the components specified in the query description.
        /// </summary>
        FilterWriteGroup = 4,
    }

    /// <summary>
    /// Provides an efficient test of whether a specific entity would be selected by an EntityQuery.
    /// </summary>
    /// <remarks>
    /// Use a mask to quickly identify whether an entity would be selected by an EntityQuery.
    ///
    /// <example>
    /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="entity-query-mask" title="Query Mask"/>
    /// </example>
    ///
    /// You can create up to 1024 unique EntityQueryMasks in an application.
    /// Note that EntityQueryMask only filters by Archetype, it doesn't support EntityQuery shared component or change filtering.
    /// </remarks>
    /// <seealso cref="EntityManager.GetEntityQueryMask"/>
    public unsafe struct EntityQueryMask
    {
        internal byte Index;
        internal byte Mask;

        [NativeDisableUnsafePtrRestriction]
        internal readonly EntityComponentStore* EntityComponentStore;

        internal EntityQueryMask(byte index, byte mask, EntityComponentStore* entityComponentStore)
        {
            Index = index;
            Mask = mask;
            EntityComponentStore = entityComponentStore;
        }

        internal bool IsCreated()
        {
            return EntityComponentStore != null;
        }

        /// <summary>
        /// Reports whether an entity would be selected by the EntityQuery instance used to create this entity query mask.
        /// </summary>
        /// <remarks>
        /// The match does not consider any filter settings of the EntityQuery.
        /// </remarks>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity would be returned by the EntityQuery, false if it would not.</returns>
        public bool Matches(Entity entity)
        {
            return EntityComponentStore->Exists(entity) && EntityComponentStore->GetArchetype(entity)->CompareMask(this);
        }
    };

    [GenerateBurstMonoInterop("EntityQuery")]
    internal unsafe partial struct EntityQueryImpl
    {
        // limit before branch off for certain functions that can either be implemented
        // as immediate main-thread operations or parallel jobs
        private  const int kImmediateMemoryThreshold = 128 * 1024;

        internal EntityDataAccess*              _Access;
        internal EntityQueryData* _QueryData;
        internal EntityQueryFilter          _Filter;
        internal ulong _SeqNo;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal bool                    _DisallowDisposing;
#endif

        internal GCHandle                _CachedState;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal ComponentSafetyHandles* SafetyHandles => &_Access->DependencyManager->Safety;
#endif

        internal void Construct(EntityQueryData* queryData, EntityDataAccess* access, ulong seqno)
        {
            _Access = access;
            _QueryData = queryData;
            _Filter = default(EntityQueryFilter);
            _SeqNo = seqno;
            fixed(EntityQueryImpl* self = &this)
            {
                access->AliveEntityQueries.Add((ulong)(IntPtr)self, default);
            }
        }

        public bool IsEmpty
        {
            get
            {
                var queryRequiresBatching = _QueryData->DoesQueryRequireBatching;
                if (!_Filter.RequiresMatchesFilter && !queryRequiresBatching)
                    return IsEmptyIgnoreFilter;

                SyncFilterTypes();
                int archetypeCount = _QueryData->MatchingArchetypes.Length;
                var ptrs = _QueryData->MatchingArchetypes.Ptr;
                if (queryRequiresBatching)
                {
                    var batches = stackalloc ArchetypeChunk[ChunkIterationUtility.kMaxBatchesPerChunk];
                    for (var m = 0; m < archetypeCount; ++m)
                    {
                        var match = ptrs[m];
                        var archetype = match->Archetype;
                        if (archetype->EntityCount > 0)
                        {
                            for (var chunkIndex = 0; chunkIndex < archetype->Chunks.Count; ++chunkIndex)
                            {
                                var chunk = archetype->Chunks[chunkIndex];
                                var chunkMatchesFilter = match->ChunkMatchesFilter(chunkIndex, ref _Filter);
                                if (!chunkMatchesFilter)
                                    continue;

                                var chunkRequiresBatching = ChunkIterationUtility.DoesChunkRequireBatching(chunk, match, out var skipChunk);
                                if (skipChunk)
                                    continue;

                                if (chunkRequiresBatching)
                                {
                                    ChunkIterationUtility.FindBatchesForChunk(chunk, match, _QueryData->MatchingArchetypes.entityComponentStore, batches, out var batchCount);
                                    if (batchCount > 0)
                                        return false;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (var m = 0; m < archetypeCount; ++m)
                    {
                        var match = ptrs[m];
                        var archetype = match->Archetype;
                        if (archetype->EntityCount > 0)
                        {
                            for (var c = 0; c < archetype->Chunks.Count; ++c)
                            {
                                if (match->ChunkMatchesFilter(c, ref _Filter) && archetype->Chunks[c]->Count > 0)
                                    return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        public bool IsEmptyIgnoreFilter => _QueryData->GetMatchingChunkCache().Length == 0;

        [NotBurstCompatible]
        internal ComponentType[] GetQueryTypes()
        {
            using (var types = new NativeHashSet<ComponentType>(128, Allocator.Temp))
            {

                for (var i = 0; i < _QueryData->ArchetypeQueryCount; ++i)
                {
                    for (var j = 0; j < _QueryData->ArchetypeQuery[i].AnyCount; ++j)
                    {
                        types.Add(TypeManager.GetType(_QueryData->ArchetypeQuery[i].Any[j]));
                    }
                    for (var j = 0; j < _QueryData->ArchetypeQuery[i].AllCount; ++j)
                    {
                        types.Add(TypeManager.GetType(_QueryData->ArchetypeQuery[i].All[j]));
                    }
                    for (var j = 0; j < _QueryData->ArchetypeQuery[i].NoneCount; ++j)
                    {
                        types.Add(ComponentType.Exclude(TypeManager.GetType(_QueryData->ArchetypeQuery[i].None[j])));
                    }
                }

                using (var typesArray = types.ToNativeArray(Allocator.Temp))
                {
                    return typesArray.ToArray();
                }
            }
        }

        [NotBurstCompatible]
        internal ComponentType[] GetReadAndWriteTypes()
        {
            var types = new ComponentType[_QueryData->ReaderTypesCount + _QueryData->WriterTypesCount];
            var typeArrayIndex = 0;
            for (var i = 0; i < _QueryData->ReaderTypesCount; ++i)
            {
                types[typeArrayIndex++] = ComponentType.ReadOnly(TypeManager.GetType(_QueryData->ReaderTypes[i]));
            }
            for (var i = 0; i < _QueryData->WriterTypesCount; ++i)
            {
                types[typeArrayIndex++] = TypeManager.GetType(_QueryData->WriterTypes[i]);
            }

            return types;
        }

        public void Dispose()
        {
            fixed (EntityQueryImpl* self = &this)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (_DisallowDisposing)
                    throw new InvalidOperationException("EntityQuery cannot be disposed. Note that queries created with GetEntityQuery() should not be manually disposed; they are owned by the system, and will be destroyed along with the system itself.");
#endif
                _Access->AliveEntityQueries.Remove((ulong)(IntPtr)self);

                if (_CachedState.IsAllocated)
                {
                    FreeCachedState(self);
                    _CachedState = default;
                }

                if (_QueryData != null)
                    ResetFilter();

                _Access = null;
                _QueryData = null;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        ///     Gets safety handle to a ComponentType required by this EntityQuery.
        /// </summary>
        /// <param name="indexInEntityQuery">Index of a ComponentType in this EntityQuery's RequiredComponents list./param>
        /// <returns>AtomicSafetyHandle for a ComponentType</returns>
        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        internal AtomicSafetyHandle GetSafetyHandle(int indexInEntityQuery)
        {
            var type = _QueryData->RequiredComponents + indexInEntityQuery;
            var isReadOnly = type->AccessModeType == ComponentType.AccessMode.ReadOnly;
            return SafetyHandles->GetSafetyHandle(type->TypeIndex, isReadOnly);
        }

        /// <summary>
        ///     Gets buffer safety handle to a ComponentType required by this EntityQuery.
        /// </summary>
        /// <param name="indexInEntityQuery">Index of a ComponentType in this EntityQuery's RequiredComponents list./param>
        /// <returns>AtomicSafetyHandle for a buffer</returns>
        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        internal AtomicSafetyHandle GetBufferSafetyHandle(int indexInEntityQuery)
        {
            var type = _QueryData->RequiredComponents + indexInEntityQuery;
            return SafetyHandles->GetBufferSafetyHandle(type->TypeIndex);
        }

#endif

        bool GetIsReadOnly(int indexInEntityQuery)
        {
            var type = _QueryData->RequiredComponents + indexInEntityQuery;
            var isReadOnly = type->AccessModeType == ComponentType.AccessMode.ReadOnly;
            return isReadOnly;
        }

        public int CalculateEntityCount()
        {
            SyncFilterTypes();
            return ChunkIterationUtility.CalculateEntityCount(ref _QueryData->MatchingArchetypes, ref _Filter, _QueryData->DoesQueryRequireBatching ? 1 : 0);
        }

        public int CalculateEntityCount(NativeArray<Entity> entityArray)
        {
            SyncFilterTypes();
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return ChunkIterationUtility.CalculateEntityCountInEntityArray((Entity*)entityArray.GetUnsafeReadOnlyPtr(), entityArray.Length, _QueryData, ecs, ref mask, ref _Filter);
        }

        public int CalculateEntityCountWithoutFiltering()
        {
            var dummyFilter = default(EntityQueryFilter);
            return ChunkIterationUtility.CalculateEntityCount(ref _QueryData->MatchingArchetypes, ref dummyFilter, _QueryData->DoesQueryRequireBatching ? 1 : 0);
        }

        public int CalculateEntityCountWithoutFiltering(NativeArray<Entity> entityArray)
        {
            var dummyFilter = default(EntityQueryFilter);
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return ChunkIterationUtility.CalculateEntityCountInEntityArray((Entity*) entityArray.GetUnsafeReadOnlyPtr(), entityArray.Length, _QueryData, ecs, ref mask, ref dummyFilter);
        }

        public int CalculateChunkCount()
        {
            SyncFilterTypes();
            return ChunkIterationUtility.CalculateChunkCount(ref _QueryData->MatchingArchetypes, ref _Filter);
        }

        public int CalculateChunkCountWithoutFiltering()
        {
            var dummyFilter = default(EntityQueryFilter);
            return ChunkIterationUtility.CalculateChunkCount(ref _QueryData->MatchingArchetypes, ref dummyFilter);
        }

        void CalculateChunkAndEntityCount(out int entityCount, out int chunkCount)
        {
            SyncFilterTypes();
            entityCount = ChunkIterationUtility.CalculateChunkAndEntityCount(ref _QueryData->MatchingArchetypes, ref _Filter, out chunkCount);
        }

        public bool MatchesInEntityArray(NativeArray<Entity> entityArray)
        {
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return ChunkIterationUtility.MatchesAnyInEntityArray((Entity*) entityArray.GetUnsafeReadOnlyPtr(), entityArray.Length, _QueryData, ecs, ref mask, ref _Filter);
        }

        public bool MatchesInEntityArrayIgnoreFilter(NativeArray<Entity> entityArray)
        {
            var dummyFilter = default(EntityQueryFilter);
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return ChunkIterationUtility.MatchesAnyInEntityArray((Entity*) entityArray.GetUnsafeReadOnlyPtr(), entityArray.Length, _QueryData, ecs, ref mask, ref dummyFilter);
        }

        public ArchetypeChunkIterator GetArchetypeChunkIterator()
        {
            return new ArchetypeChunkIterator(_QueryData->MatchingArchetypes, _Access->DependencyManager, _Access->EntityComponentStore->GlobalSystemVersion, ref _Filter, _Access->m_WorldUnmanaged.UpdateAllocator.ToAllocator);
        }

        internal int GetIndexInEntityQuery(int componentType)
        {
            var componentIndex = 0;
            while (componentIndex < _QueryData->RequiredComponentsCount && _QueryData->RequiredComponents[componentIndex].TypeIndex != componentType)
                ++componentIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentIndex >= _QueryData->RequiredComponentsCount || _QueryData->RequiredComponents[componentIndex].AccessModeType == ComponentType.AccessMode.Exclude)
                throw new InvalidOperationException($"Trying to get iterator for {TypeManager.GetType(componentType)} but the required component type was not declared in the EntityQuery.");
#endif
            return componentIndex;
        }

        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArrayAsync(Allocator allocator, out JobHandle jobhandle)
        {
            JobHandle dependency = default;

            var filterCount = _Filter.Changed.Count;
            if (filterCount > 0)
            {
                var readerTypes = stackalloc int[filterCount];
                for (int i = 0; i < filterCount; ++i)
                    readerTypes[i] = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]].TypeIndex;

                dependency = _Access->DependencyManager->GetDependency(readerTypes, filterCount, null, 0);
            }

            return ChunkIterationUtility.CreateArchetypeChunkArrayAsync(_QueryData->MatchingArchetypes, allocator, out jobhandle, ref _Filter, dependency);
        }

        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(Allocator allocator)
        {
            SyncFilterTypes();
            var res = ChunkIterationUtility.CreateArchetypeChunkArray(_QueryData->GetMatchingChunkCache(), _QueryData->MatchingArchetypes, allocator, ref _Filter);
            return res;
        }


        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<Entity> ToEntityArrayAsync(Allocator allocator, out JobHandle jobhandle, EntityQuery outer)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
#else
            var entityType = new EntityTypeHandle();
#endif

            return ChunkIterationUtility.CreateEntityArrayAsync(_QueryData->MatchingArchetypes, allocator, entityType,
                outer,CalculateEntityCount(), out jobhandle, GetDependency());
        }

        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<Entity> ToEntityArray(Allocator allocator, EntityQuery outer)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
#else
            var entityType = new EntityTypeHandle();
#endif
            CalculateChunkAndEntityCount(out int entityCount, out int chunkCount);

            NativeArray<Entity> res;

            /*in cases of sparse entities spread over many archetypes, the cache lines read from chunks will exceed
             the actual memory of the entities read. In cases like these, a jobified path is the better approach */
            if (math.max(chunkCount * 64,entityCount * sizeof(Entity)) <= kImmediateMemoryThreshold)
            {
                 res = ChunkIterationUtility.CreateEntityArray(
                    _QueryData->MatchingArchetypes, allocator, entityType,outer,entityCount);
            }
            else
            {
                res = ChunkIterationUtility.CreateEntityArrayAsyncComplete(_QueryData->MatchingArchetypes, allocator, entityType,
                    outer, entityCount, GetDependency());
            }
            return res;
        }

        //internal function meant for debugging capabilities only, where scheduling jobs can result in undefined behavior
        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<Entity> ToEntityArrayImmediate(Allocator allocator, EntityQuery outer)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
#else
            var entityType = new EntityTypeHandle();
#endif
            CalculateChunkAndEntityCount(out int entityCount, out int chunkCount);

            NativeArray<Entity> res;

            res = ChunkIterationUtility.CreateEntityArray(
                    _QueryData->MatchingArchetypes, allocator, entityType,outer,entityCount);

            return res;
        }

        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<Entity> ToEntityArray(NativeArray<Entity> entityArray, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
#else
            var entityType = new EntityTypeHandle();
#endif
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);

            var arrayPtr = ChunkIterationUtility.CreateEntityArrayFromEntityArray((Entity*)entityArray.GetUnsafeReadOnlyPtr(),
                                                                                    entityArray.Length,
                                                                                    allocator,
                                                                                    _QueryData,
                                                                                    ecs,
                                                                                    ref mask,
                                                                                    ref entityType,
                                                                                    ref _Filter,
                                                                                    out var arrayLength);

            var res = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>(arrayPtr, arrayLength, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref res, AtomicSafetyHandle.Create());
#endif

            return res;
        }


        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        internal void GatherEntitiesToArray(out EntityQuery.GatherEntitiesResult result, EntityQuery outer)
        {
            ChunkIterationUtility.GatherEntitiesToArray(_QueryData, ref _Filter, out result);

            if (result.EntityBuffer == null)
            {
                var entityCount = CalculateEntityCount();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
#else
                var entityType = new EntityTypeHandle();
#endif
                var entities = new NativeArray<Entity>(entityCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);

                var job = new GatherEntitiesJob
                {
                    EntityTypeHandle = entityType,
                    Entities = (byte*)entities.GetUnsafePtr()
                };
                job.Run(outer);
                result.EntityArray = entities;
                result.EntityBuffer = (Entity*)result.EntityArray.GetUnsafeReadOnlyPtr();
                result.EntityCount = result.EntityArray.Length;
            }
        }

        internal void ReleaseGatheredEntities(ref EntityQuery.GatherEntitiesResult result)
        {
            ChunkIterationUtility.currentOffsetInResultBuffer = result.StartingOffset;
            if (result.EntityArray.IsCreated)
            {
                result.EntityArray.Dispose();
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<T> ToComponentDataArrayAsync<T>(Allocator allocator, out JobHandle jobhandle, EntityQuery outer)
            where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), true), true, _Access->EntityComponentStore->GlobalSystemVersion);
#else
            var componentType = new ComponentTypeHandle<T>(true, _Access->EntityComponentStore->GlobalSystemVersion);
#endif


#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int typeIndex = TypeManager.GetTypeIndex<T>();
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
#endif
            return ChunkIterationUtility.CreateComponentDataArrayAsync(allocator, componentType,CalculateEntityCount(), outer, out jobhandle, GetDependency());
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<T> ToComponentDataArray<T>(NativeArray<Entity> entityArray, Allocator allocator)
            where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), true), true, _Access->EntityComponentStore->GlobalSystemVersion);
#else
            var componentType = new ComponentTypeHandle<T>(true, _Access->EntityComponentStore->GlobalSystemVersion);
#endif

            var typeIndex = TypeManager.GetTypeIndex<T>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArray of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
#endif

            var ecs = _Access->EntityComponentStore;
            var typeInfo = ecs->GetTypeInfo(typeIndex);
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);

            var arrayPtr = ChunkIterationUtility.CreateComponentDataArrayFromEntityArray(
                (Entity*)entityArray.GetUnsafeReadOnlyPtr(),
                entityArray.Length,
                allocator,_QueryData,
                ecs,
                typeIndex,
                typeInfo.SizeInChunk,
                typeInfo.AlignmentInBytes,
                ref mask,
                ref _Filter,
                out var arrayLength);
            var res = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(arrayPtr, arrayLength, allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref res, AtomicSafetyHandle.Create());
#endif

            return res;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<T> ToComponentDataArray<T>(Allocator allocator, EntityQuery outer)
            where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), true), true, _Access->EntityComponentStore->GlobalSystemVersion);
#else
            var componentType = new ComponentTypeHandle<T>(true, _Access->EntityComponentStore->GlobalSystemVersion);
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int typeIndex = TypeManager.GetTypeIndex<T>();
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArray of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
#endif

            CalculateChunkAndEntityCount(out int entityCount, out int chunkCount);

            NativeArray<T> res;

            /*in cases of sparse entities spread over many archetypes, the cache lines read from chunks will exceed
             the actual memory of the entities read. In cases like these, a jobified path is the better approach */
            if (math.max(chunkCount * 64,entityCount * UnsafeUtility.SizeOf<T>()) <= kImmediateMemoryThreshold)
            {
                res = ChunkIterationUtility.CreateComponentDataArray(allocator, componentType,entityCount,outer);
            }
            else
            {
                res = ChunkIterationUtility.CreateComponentDataArrayAsyncComplete(allocator, componentType, entityCount,outer, GetDependency());
            }
            return res;
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [NotBurstCompatible]
        public T[] ToComponentDataArray<T>() where T : class, IComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(typeIndex, true), true, _Access->EntityComponentStore->GlobalSystemVersion);
#else
            var componentType = new ComponentTypeHandle<T>(true, _Access->EntityComponentStore->GlobalSystemVersion);
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArray of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
#endif

            var mcs = _Access->ManagedComponentStore;
            var matches = _QueryData->MatchingArchetypes;
            var entityCount = ChunkIterationUtility.CalculateChunkAndEntityCount(ref matches, ref _Filter, out int dummyChunkCount);
            T[] res = new T[entityCount];
            int i = 0;
            int archetypeCount = matches.Length;
            var ptrs = _QueryData->MatchingArchetypes.Ptr;
            for (int mi = 0; mi < archetypeCount; ++mi)
            {
                var match = ptrs[mi];
                var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(match->Archetype, typeIndex);
                var chunks = match->Archetype->Chunks;

                for (int ci = 0; ci < chunks.Count; ++ci)
                {
                    var chunk = chunks[ci];

                    if (_Filter.RequiresMatchesFilter && !chunk->MatchesFilter(match, ref _Filter))
                        continue;

                    var managedComponentArray = (int*)ChunkDataUtility.GetComponentDataRW(chunk, 0, indexInTypeArray, _Access->EntityComponentStore->GlobalSystemVersion);
                    for (int entityIndex = 0; entityIndex < chunk->Count; ++entityIndex)
                    {
                        res[i++] = (T)mcs.GetManagedComponent(managedComponentArray[entityIndex]);
                    }
                }
            }

            return res;
        }

#endif

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public void CopyFromComponentDataArray<T>(NativeArray<T> componentDataArray, EntityQuery outer)
            where T : struct, IComponentData
        {
            CalculateChunkAndEntityCount(out var entityCount, out var chunkCount);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (entityCount != componentDataArray.Length)
                throw new ArgumentException($"Length of input array ({componentDataArray.Length}) does not match length of EntityQuery ({entityCount})");

            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), false), false, _Access->EntityComponentStore->GlobalSystemVersion);
#else
            var componentType = new ComponentTypeHandle<T>(false, _Access->EntityComponentStore->GlobalSystemVersion);
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int typeIndex = TypeManager.GetTypeIndex<T>();
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying CopyFromComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
#endif
            /*in cases of sparse entities spread over many archetypes, the cache lines read from chunks will exceed
             the actual memory of the entities read. In cases like these, a jobified path is the better approach */
            if (math.max(chunkCount * 64,entityCount * UnsafeUtility.SizeOf<T>()) <= kImmediateMemoryThreshold)
            {
                ChunkIterationUtility.CopyFromComponentDataArray(componentDataArray, componentType, outer);
            }
            else
            {
                ChunkIterationUtility.CopyFromComponentDataArrayAsyncComplete(_QueryData->MatchingArchetypes,
                    componentDataArray, componentType, outer, ref _Filter, GetDependency());
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = BurstCompatibleAttribute.BurstCompatibleCompileTarget.Editor)]
        public void CopyFromComponentDataArrayAsync<T>(NativeArray<T> componentDataArray, out JobHandle jobhandle, EntityQuery outer)
            where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentDataArray.m_AllocatorLabel == Allocator.Temp)
            {
                throw new ArgumentException(
                    $"The NativeContainer is allocated with Allocator.Temp." +
                    $", use TempJob instead.",nameof (componentDataArray));
            }

            var entityCount = CalculateEntityCount();
            if (entityCount != componentDataArray.Length)
                throw new ArgumentException($"Length of input array ({componentDataArray.Length}) does not match length of EntityQuery ({entityCount})");
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), false), false, _Access->EntityComponentStore->GlobalSystemVersion);
#else
            var componentType = new ComponentTypeHandle<T>(false, _Access->EntityComponentStore->GlobalSystemVersion);
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int typeIndex = TypeManager.GetTypeIndex<T>();
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying CopyFromComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
#endif

            ChunkIterationUtility.CopyFromComponentDataArrayAsync(_QueryData->MatchingArchetypes, componentDataArray, componentType, outer, ref _Filter, out jobhandle, GetDependency());
        }


        public Entity GetSingletonEntity()
        {
            if (!_Filter.RequiresMatchesFilter)
            {
                // Fast path with no filter
                var matchingChunkCache = _QueryData->GetMatchingChunkCache();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (matchingChunkCache.Length != 1 || matchingChunkCache.Ptr[0]->Count != 1)
                    throw new InvalidOperationException($"GetSingletonEntity() requires that exactly one entity exist that match this query, but there are {CalculateEntityCountWithoutFiltering()}.");
#endif
                var chunk = matchingChunkCache.Ptr[0];
                // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                return UnsafeUtility.AsRef<Entity>(ChunkIterationUtility.GetChunkComponentDataROPtr(chunk, 0));
            }
            else
            {
                // Slow path with filter, can't just use first matching archetype/chunk
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                var queryEntityCount = CalculateEntityCount();
                if (queryEntityCount != 1)
                    throw new InvalidOperationException(
                        $"GetSingletonEntity() requires that exactly one entity exists that matches this query, but there are {queryEntityCount}.");
#endif
                var matchingChunkCache = _QueryData->GetMatchingChunkCache();
                var chunkList = *matchingChunkCache.MatchingChunks;
                var matchingArchetypeIndices = *matchingChunkCache.PerChunkMatchingArchetypeIndex;
                var matchingArchetypes = _QueryData->MatchingArchetypes.Ptr;
                int chunkCount = chunkList.Length;
                for (int i = 0; i < chunkCount; ++i)
                {
                    var chunk = chunkList[i];
                    var matchIndex = matchingArchetypeIndices[i];
                    var match = matchingArchetypes[matchIndex];
                    if (match->ChunkMatchesFilter(chunk->ListIndex, ref _Filter))
                    {
                        // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                        return UnsafeUtility.AsRef<Entity>(ChunkIterationUtility.GetChunkComponentDataROPtr(chunk, 0));
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                throw new InvalidOperationException(
                    "Bug in GetSingleton(): found no chunk that matches the provided filter, but CalculateEntityCount() == 1");
#else
                return default;
#endif
            }
        }

        internal int GetFirstArchetypeIndexWithEntity(out int entityCount)
        {
            entityCount = 0;
            int archeTypeIndex = -1;
            int archetypeCount = _QueryData->MatchingArchetypes.Length;
            var ptrs = _QueryData->MatchingArchetypes.Ptr;
            for (int i = 0; i < archetypeCount; i++)
            {
                var entityCountInArchetype = ptrs[i]->Archetype->EntityCount;
                if (archeTypeIndex == -1 && entityCountInArchetype > 0)
                    archeTypeIndex = i;
                entityCount += entityCountInArchetype;
            }

            return archeTypeIndex;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public T GetSingleton<T>() where T : struct, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (TypeManager.IsZeroSized(typeIndex))
                throw new InvalidOperationException($"Can't call GetSingleton<{typeof(T)}>() with zero-size type {typeof(T)}.");
#endif
            _Access->DependencyManager->CompleteWriteDependencyNoChecks(typeIndex);

            // Fast path with no filter
            if (!_Filter.RequiresMatchesFilter && _QueryData->RequiredComponentsCount <= 2 && _QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                var matchingChunkCache = _QueryData->GetMatchingChunkCache();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (matchingChunkCache.Length != 1 || matchingChunkCache.Ptr[0]->Count != 1)
                    throw new InvalidOperationException($"GetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {CalculateEntityCountWithoutFiltering()}.");
#endif
                var chunk = matchingChunkCache.Ptr[0]; // only one matching chunk
                var matchIndex = matchingChunkCache.PerChunkMatchingArchetypeIndex->Ptr[0];
                var match = _QueryData->MatchingArchetypes.Ptr[matchIndex];
                // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                return UnsafeUtility.AsRef<T>(ChunkIterationUtility.GetChunkComponentDataROPtr(chunk, match->IndexInArchetype[1]));
            }
            else
            {
                // Slow path with filter, can't just use first matching archetype/chunk
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                var queryEntityCount = CalculateEntityCount();
                if (queryEntityCount != 1)
                    throw new InvalidOperationException(
                        $"GetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {queryEntityCount}.");
#endif
                var matchingChunkCache = _QueryData->GetMatchingChunkCache();
                var chunkList = *matchingChunkCache.MatchingChunks;
                var matchingArchetypeIndices = *matchingChunkCache.PerChunkMatchingArchetypeIndex;
                var matchingArchetypes = _QueryData->MatchingArchetypes.Ptr;
                int chunkCount = chunkList.Length;
                for (int i = 0; i < chunkCount; ++i)
                {
                    var chunk = chunkList[i];
                    var matchIndex = matchingArchetypeIndices[i];
                    var match = matchingArchetypes[matchIndex];
                    if (match->ChunkMatchesFilter(chunk->ListIndex, ref _Filter))
                    {
                        // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                        return UnsafeUtility.AsRef<T>(
                            ChunkIterationUtility.GetChunkComponentDataROPtr(chunk, match->IndexInArchetype[1]));
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                throw new InvalidOperationException(
                    "Bug in GetSingleton(): found no chunk that matches the provided filter, but CalculateEntityCount() == 1");
#else
                return default;
#endif
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void SetSingleton<T>(T value) where T : struct, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (GetIsReadOnly(GetIndexInEntityQuery(typeIndex)))
                throw new InvalidOperationException($"Can't call SetSingleton<{typeof(T)}>() on query where access to {typeof(T)} is read-only.");
            if (TypeManager.IsZeroSized(typeIndex))
                throw new InvalidOperationException($"Can't call SetSingleton<{typeof(T)}>() with zero-size type {typeof(T)}.");
#endif
            _Access->DependencyManager->CompleteWriteDependencyNoChecks(typeIndex);

            if (!_Filter.RequiresMatchesFilter && _QueryData->RequiredComponentsCount <= 2 && _QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                // Fast path with no filter & assuming this is a simple query with just one singleton component
                var matchingChunkCache = _QueryData->GetMatchingChunkCache();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (matchingChunkCache.Length != 1 || matchingChunkCache.Ptr[0]->Count != 1)
                    throw new InvalidOperationException($"SetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {CalculateEntityCountWithoutFiltering()}.");
#endif
                var chunk = matchingChunkCache.Ptr[0]; // only one matching chunk
                var matchIndex = matchingChunkCache.PerChunkMatchingArchetypeIndex->Ptr[0];
                var match = _QueryData->MatchingArchetypes.Ptr[matchIndex];
                // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                UnsafeUtility.CopyStructureToPtr(ref value, ChunkIterationUtility.GetChunkComponentDataPtr(chunk, true,
                    match->IndexInArchetype[1], _Access->EntityComponentStore->GlobalSystemVersion));
            }
            else
            {
                // Slower path w/filtering and/or a multiple-component query
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var queryEntityCount = CalculateEntityCount();
                if (queryEntityCount != 1)
                    throw new InvalidOperationException(
                        $"SetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {queryEntityCount}.");
#endif
                var matchingChunkCache = _QueryData->GetMatchingChunkCache();
                var chunkList = *matchingChunkCache.MatchingChunks;
                var matchingArchetypeIndices = *matchingChunkCache.PerChunkMatchingArchetypeIndex;
                var matchingArchetypes = _QueryData->MatchingArchetypes.Ptr;
                int chunkCount = chunkList.Length;
                for (int i = 0; i < chunkCount; ++i)
                {
                    var chunk = chunkList[i];
                    var matchIndex = matchingArchetypeIndices[i];
                    var match = matchingArchetypes[matchIndex];
                    if (match->ChunkMatchesFilter(chunk->ListIndex, ref _Filter))
                    {
                        // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                        UnsafeUtility.CopyStructureToPtr(ref value, ChunkIterationUtility.GetChunkComponentDataPtr(
                            chunk, true,
                            match->IndexInArchetype[1], _Access->EntityComponentStore->GlobalSystemVersion));
                        return;
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                throw new InvalidOperationException(
                    "Bug in SetSingleton(): found no chunk that matches the provided filter, but CalculateEntityCount() == 1");
#endif
            }
        }

        internal bool CompareComponents(ComponentType* componentTypes, int count)
        {
            return EntityQueryManager.CompareComponents(componentTypes, count, _QueryData);
        }

        public bool CompareComponents(ComponentType[] componentTypes)
        {
            fixed(ComponentType* componentTypesPtr = componentTypes)
            {
                return EntityQueryManager.CompareComponents(componentTypesPtr, componentTypes.Length, _QueryData);
            }
        }

        public bool CompareComponents(NativeArray<ComponentType> componentTypes)
        {
            return EntityQueryManager.CompareComponents((ComponentType*)componentTypes.GetUnsafeReadOnlyPtr(), componentTypes.Length, _QueryData);
        }

        public bool CompareQuery(EntityQueryDesc[] queryDesc)
        {
            var builder = new EntityQueryDescBuilder(Allocator.Temp);
            EntityQueryManager.ConvertToEntityQueryDescBuilder(ref builder, queryDesc);
            bool result = CompareQuery(builder);
            builder.Dispose();
            return result;
        }

        public bool CompareQuery(in EntityQueryDescBuilder queryDesc)
        {
            return EntityQueryManager.CompareQuery(queryDesc, _QueryData);
        }

        [BurstDiscard]
        [BurstMonoInteropMethod]
        internal static void _ResetFilter(EntityQueryImpl* self)
        {
            var sharedCount = self->_Filter.Shared.Count;
            for (var i = 0; i < sharedCount; ++i)
                self->_Access->RemoveSharedComponentReference(self->_Filter.Shared.SharedComponentIndex[i]);

            self->_Filter.Changed.Count = 0;
            self->_Filter.Shared.Count = 0;
        }

        [BurstDiscard]
        [BurstMonoInteropMethod]
        internal static void _FreeCachedState(EntityQueryImpl* self)
        {
            if (self->_CachedState.Target is IDisposable obj)
            {
                obj.Dispose();
            }
            self->_CachedState.Free();
        }

        public void SetSharedComponentFilter<SharedComponent1>(SharedComponent1 sharedComponent1)
            where SharedComponent1 : struct, ISharedComponentData
        {
            ResetFilter();
            AddSharedComponentFilter(sharedComponent1);
        }

        public void ResetFilter()
        {
            fixed (EntityQueryImpl* self = &this)
            {
                ResetFilter(self);
            }
        }

        public void SetSharedComponentFilter<SharedComponent1, SharedComponent2>(SharedComponent1 sharedComponent1,
            SharedComponent2 sharedComponent2)
            where SharedComponent1 : struct, ISharedComponentData
            where SharedComponent2 : struct, ISharedComponentData
        {
            ResetFilter();
            AddSharedComponentFilter(sharedComponent1);
            AddSharedComponentFilter(sharedComponent2);
        }

        public void SetChangedVersionFilter(ComponentType componentType)
        {
            ResetFilter();
            AddChangedVersionFilter(componentType);
        }

        public void SetOrderVersionFilter()
        {
            ResetFilter();
            AddOrderVersionFilter();
        }

        internal void SetChangedFilterRequiredVersion(uint requiredVersion)
        {
            _Filter.RequiredChangeVersion = requiredVersion;
        }

        public void SetChangedVersionFilter(ComponentType[] componentType)
        {
            if (componentType.Length > EntityQueryFilter.ChangedFilter.Capacity)
                throw new ArgumentException(
                    $"EntityQuery.SetFilterChanged accepts a maximum of {EntityQueryFilter.ChangedFilter.Capacity} component array length");
            if (componentType.Length <= 0)
                throw new ArgumentException(
                    $"EntityQuery.SetFilterChanged component array length must be larger than 0");

            ResetFilter();
            for (var i = 0; i != componentType.Length; i++)
                AddChangedVersionFilter(componentType[i]);
        }

        public void AddChangedVersionFilter(ComponentType componentType)
        {
            var newFilterIndex = _Filter.Changed.Count;
            if (newFilterIndex >= EntityQueryFilter.ChangedFilter.Capacity)
                throw new ArgumentException($"EntityQuery accepts a maximum of {EntityQueryFilter.ChangedFilter.Capacity} changed filters.");

            _Filter.Changed.Count = newFilterIndex + 1;
            _Filter.Changed.IndexInEntityQuery[newFilterIndex] = GetIndexInEntityQuery(componentType.TypeIndex);

            _Filter.AssertValid();
        }

        public void AddSharedComponentFilter<SharedComponent>(SharedComponent sharedComponent)
            where SharedComponent : struct, ISharedComponentData
        {
            var newFilterIndex = _Filter.Shared.Count;
            if (newFilterIndex >= EntityQueryFilter.SharedComponentData.Capacity)
                throw new ArgumentException($"EntityQuery accepts a maximum of {EntityQueryFilter.SharedComponentData.Capacity} shared component filters.");

            _Filter.Shared.Count = newFilterIndex + 1;
            _Filter.Shared.IndexInEntityQuery[newFilterIndex] = GetIndexInEntityQuery(TypeManager.GetTypeIndex<SharedComponent>());
            _Filter.Shared.SharedComponentIndex[newFilterIndex] = _Access->InsertSharedComponent(sharedComponent);

            _Filter.AssertValid();
        }

        public void AddOrderVersionFilter()
        {
            _Filter.UseOrderFiltering = true;

            _Filter.AssertValid();
        }

        public void CompleteDependency()
        {
            _Access->DependencyManager->CompleteDependenciesNoChecks(_QueryData->ReaderTypes, _QueryData->ReaderTypesCount,
                _QueryData->WriterTypes, _QueryData->WriterTypesCount);
        }

        public JobHandle GetDependency()
        {
            return _Access->DependencyManager->GetDependency(_QueryData->ReaderTypes, _QueryData->ReaderTypesCount,
                _QueryData->WriterTypes, _QueryData->WriterTypesCount);
        }

        public JobHandle AddDependency(JobHandle job)
        {
            return _Access->DependencyManager->AddDependency(_QueryData->ReaderTypes, _QueryData->ReaderTypesCount,
                _QueryData->WriterTypes, _QueryData->WriterTypesCount, job);
        }

        public int GetCombinedComponentOrderVersion()
        {
            var version = 0;

            for (var i = 0; i < _QueryData->RequiredComponentsCount; ++i)
                version += _Access->EntityComponentStore->GetComponentTypeOrderVersion(_QueryData->RequiredComponents[i].TypeIndex);

            return version;
        }

        internal bool AddReaderWritersToLists(ref UnsafeList<int> reading, ref UnsafeList<int> writing)
        {
            bool anyAdded = false;
            for (int i = 0; i < _QueryData->ReaderTypesCount; ++i)
                anyAdded |= CalculateReaderWriterDependency.AddReaderTypeIndex(_QueryData->ReaderTypes[i], ref reading, ref writing);

            for (int i = 0; i < _QueryData->WriterTypesCount; ++i)
                anyAdded |= CalculateReaderWriterDependency.AddWriterTypeIndex(_QueryData->WriterTypes[i], ref reading, ref writing);
            return anyAdded;
        }

        internal void SyncFilterTypes()
        {
            for (int i = 0; i < _Filter.Changed.Count; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                _Access->DependencyManager->CompleteWriteDependency(type.TypeIndex);
            }
        }

        internal static void SyncFilterTypes(ref UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, ComponentDependencyManager* safetyManager)
        {
            filter.AssertValid();
            if (matchingArchetypes.Length < 1)
                return;

            var match = *matchingArchetypes.Ptr;
            for (int i = 0; i < filter.Changed.Count; ++i)
            {
                var indexInEntityQuery = filter.Changed.IndexInEntityQuery[i];
                var componentIndexInChunk = match->IndexInArchetype[indexInEntityQuery];
                var type = match->Archetype->Types[componentIndexInChunk];
                safetyManager->CompleteWriteDependency(type.TypeIndex);
            }
        }

        public bool HasFilter()
        {
            return _Filter.RequiresMatchesFilter;
        }


        public EntityQueryMask GetEntityQueryMask()
        {
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return mask;
        }

        public bool Matches(Entity e)
        {
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            if (mask.Matches(e))
            {
                var chunk = ecs->GetChunk(e);
                var match = _QueryData->MatchingArchetypes.Ptr[
                    EntityQueryManager.FindMatchingArchetypeIndexForArchetype(ref _QueryData->MatchingArchetypes, chunk->Archetype)];
                return chunk->MatchesFilter(match, ref _Filter);
            }

            return false;
        }

        public bool MatchesNoFilter(Entity e)
        {
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return mask.Matches(e);
        }

        public EntityQueryDesc GetEntityQueryDesc()
        {
            var archetypeQuery = _QueryData->ArchetypeQuery;

            var allComponentTypes = new ComponentType[archetypeQuery->AllCount];
            for (var i = 0; i < archetypeQuery->AllCount; ++i)
            {
                allComponentTypes[i] = new ComponentType
                {
                    TypeIndex = archetypeQuery->All[i],
                    AccessModeType = (ComponentType.AccessMode)archetypeQuery->AllAccessMode[i]
                };
            }

            var anyComponentTypes = new ComponentType[archetypeQuery->AnyCount];
            for (var i = 0; i < archetypeQuery->AnyCount; ++i)
            {
                anyComponentTypes[i] = new ComponentType
                {
                    TypeIndex = archetypeQuery->Any[i],
                    AccessModeType = (ComponentType.AccessMode)archetypeQuery->AnyAccessMode[i]
                };
            }

            var noneComponentTypes = new ComponentType[archetypeQuery->NoneCount];
            for (var i = 0; i < archetypeQuery->NoneCount; ++i)
            {
                noneComponentTypes[i] = new ComponentType
                {
                    TypeIndex = archetypeQuery->None[i],
                    AccessModeType = (ComponentType.AccessMode)archetypeQuery->NoneAccessMode[i]
                };
            }

            return new EntityQueryDesc
            {
                All = allComponentTypes,
                Any = anyComponentTypes,
                None = noneComponentTypes,
                Options = archetypeQuery->Options
            };
        }

        internal bool CheckChunkListCacheConsistency()
        {
            return UnsafeCachedChunkList.CheckCacheConsistency(ref _QueryData->MatchingChunkCache, _QueryData);
        }

        internal static EntityQueryImpl* Allocate()
        {
            void* ptr = Memory.Unmanaged.Allocate(sizeof(EntityQueryImpl), 8, Allocator.Persistent);
            UnsafeUtility.MemClear(ptr, sizeof(EntityQueryImpl));
            return (EntityQueryImpl*)ptr;
        }

        internal static void Free(EntityQueryImpl* impl)
        {
            Memory.Unmanaged.Free(impl, Allocator.Persistent);
        }
    }

    /// <summary>
    /// Use an EntityQuery object to select entities with components that meet specific requirements.
    /// </summary>
    /// <remarks>
    /// An entity query defines the set of component types that an [archetype] must contain
    /// in order for its chunks and entities to be selected and specifies whether the components accessed
    /// through the query are read-only or read-write.
    ///
    /// For simple queries, you can create an EntityQuery based on an array of
    /// component types. The following example defines a EntityQuery that finds all entities
    /// with both Rotation and RotationSpeed components.
    ///
    /// <example>
    /// <code source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="query-from-list" title="EntityQuery Example"/>
    /// </example>
    ///
    /// The query uses [ComponentType.ReadOnly] instead of the simpler `typeof` expression
    /// to designate that the system does not write to RotationSpeed. Always specify read-only
    /// when possible, since there are fewer constraints on read-only access to data, which can help
    /// the Job scheduler execute your Jobs more efficiently.
    ///
    /// For more complex queries, you can use an <see cref="EntityQueryDesc"/> object to create the entity query.
    /// A query description provides a flexible query mechanism to specify which archetypes to select
    /// based on the following sets of components:
    ///
    /// * `All` = All component types in this array must exist in the archetype
    /// * `Any` = At least one of the component types in this array must exist in the archetype
    /// * `None` = None of the component types in this array can exist in the archetype
    ///
    /// For example, the following query includes archetypes containing Rotation and
    /// RotationSpeed components, but excludes any archetypes containing a Static component:
    ///
    /// <example>
    /// <code source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="query-from-description" title="EntityQuery Example"/>
    /// </example>
    ///
    /// **Note:** Do not include completely optional components in the query description. To handle optional
    /// components, use <see cref="IJobChunk"/> and the [ArchetypeChunk.Has()] method to determine whether a chunk contains the
    /// optional component or not. Since all entities within the same chunk have the same components, you
    /// only need to check whether an optional component exists once per chunk -- not once per entity.
    ///
    /// Within a system class, use the [ComponentSystemBase.GetEntityQuery()] function
    /// to get a EntityQuery instance.
    ///
    /// You can filter entities based on
    /// whether they have [changed] or whether they have a specific value for a [shared component].
    /// Once you have created an EntityQuery object, you can
    /// [reset] and change the filter settings, but you cannot modify the base query.
    ///
    /// Use an EntityQuery for the following purposes:
    ///
    /// * To get a [native array] of a the values for a specific <see cref="IComponentData"/> type for all entities matching the query
    /// * To get an [native array] of the <see cref="ArchetypeChunk"/> objects matching the query
    /// * To schedule an <see cref="IJobChunk"/> job
    /// * To control whether a system updates using [ComponentSystemBase.RequireForUpdate(query)]
    ///
    /// Note that [Entities.ForEach] defines an entity query implicitly based on the methods you call. You can
    /// access this implicit EntityQuery object using [Entities.WithStoreEntityQueryInField]. However, you cannot
    /// create an [Entities.ForEach] construction based on an existing EntityQuery object.
    ///
    /// [Entities.ForEach]: xref:Unity.Entities.SystemBase.Entities
    /// [Entities.WithStoreEntityQueryInField]: xref:Unity.Entities.SystemBase.Entities
    /// [ComponentSystemBase.GetEntityQuery()]: xref:Unity.Entities.ComponentSystemBase.GetEntityQuery*
    /// [ComponentType.ReadOnly]: xref:Unity.Entities.ComponentType.ReadOnly``1
    /// [ComponentSystemBase.RequireForUpdate()]: xref:Unity.Entities.ComponentSystemBase.RequireForUpdate(Unity.Entities.EntityQuery)
    /// [ArchetypeChunk.Has()]: xref:Unity.Entities.ArchetypeChunk.Has``1(Unity.Entities.ComponentTypeHandle{``0})
    /// [archetype]: xref:Unity.Entities.EntityArchetype
    /// [changed]: xref:Unity.Entities.EntityQuery.SetChangedVersionFilter*
    /// [shared component]: xref:Unity.Entities.EntityQuery.SetSharedComponentFilter*
    /// [reset]: xref:Unity.Entities.EntityQuery.ResetFilter*
    /// [native array]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html
    /// </remarks>
    [BurstCompatible]
    [DebuggerTypeProxy(typeof(EntityQueryDebugView))]
    unsafe public struct EntityQuery : IDisposable
    {
        public bool Equals(EntityQuery other)
        {
            return __impl == other.__impl;
        }

        [NotBurstCompatible]
        public override bool Equals(object obj)
        {
            return obj is EntityQuery other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)(long)__impl);
        }

        static internal unsafe EntityQuery Construct(EntityQueryData* queryData, EntityDataAccess* access)
        {
            EntityQuery _result = default;
            var _ptr = EntityQueryImpl.Allocate();
            _result.__seqno = WorldUnmanaged.ms_NextSequenceNumber.Data++;
            _ptr->Construct(queryData, access, _result.__seqno);
            _result.__impl = _ptr;
            _CreateSafetyHandle(ref _result);
            return _result;
        }

        /// <summary>
        /// Reports whether this query would currently select zero entities.
        /// </summary>
        /// <returns>True, if this EntityQuery matches zero existing entities. False, if it matches one or more entities.</returns>
        public bool IsEmpty => _GetImpl()->IsEmpty;

        /// <summary>
        /// Reports whether this query would currently select zero entities. This will ignore any filters set on the EntityQuery.
        /// </summary>
        /// <returns>True, if this EntityQuery matches zero existing entities. False, if it matches one or more entities.</returns>
        public bool IsEmptyIgnoreFilter => _GetImpl()->IsEmptyIgnoreFilter;

        /// <summary>
        /// Gets the array of <see cref="ComponentType"/> objects included in this EntityQuery.
        /// </summary>
        /// <returns>An array of ComponentType objects</returns>
        [NotBurstCompatible]
        internal ComponentType[] GetQueryTypes() => _GetImpl()->GetQueryTypes();

        /// <summary>
        ///     Packed array of this EntityQuery's ReadOnly and writable ComponentTypes.
        ///     ReadOnly ComponentTypes come before writable types in this array.
        /// </summary>
        /// <returns>Array of ComponentTypes</returns>
        [NotBurstCompatible]
        internal ComponentType[] GetReadAndWriteTypes() => _GetImpl()->GetReadAndWriteTypes();

        /// <summary>
        /// Disposes this EntityQuery instance.
        /// </summary>
        /// <remarks>Do not dispose EntityQuery instances accessed using
        /// <see cref="ComponentSystemBase.GetEntityQuery(ComponentType[])"/>. Systems automatically dispose of
        /// their own entity queries.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if you attempt to dispose an EntityQuery
        /// belonging to a system.</exception>
        public void Dispose()
        {
            var self = _GetImpl();
            self->Dispose();

            EntityQueryImpl.Free(self);

            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(__safety);
            #endif

            __impl = null;
        }

        [NotBurstCompatible]
        internal IDisposable _CachedState
        {
            [NotBurstCompatible]
            get
            {
                var impl = _GetImpl();
                if (!impl->_CachedState.IsAllocated)
                    return null;
                return (IDisposable)impl->_CachedState.Target;
            }
            [NotBurstCompatible]
            set
            {
                var impl = _GetImpl();
                if (!impl->_CachedState.IsAllocated)
                {
                    impl->_CachedState = GCHandle.Alloc(value);
                }
                else
                {
                    impl->_CachedState.Target = value;
                }
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        ///     Gets safety handle to a ComponentType required by this EntityQuery.
        /// </summary>
        /// <param name="indexInEntityQuery">Index of a ComponentType in this EntityQuery's RequiredComponents list./param>
        /// <returns>AtomicSafetyHandle for a ComponentType</returns>
        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal AtomicSafetyHandle GetSafetyHandle(int indexInEntityQuery) => _GetImpl()->GetSafetyHandle(indexInEntityQuery);

        /// <summary>
        ///     Gets buffer safety handle to a ComponentType required by this EntityQuery.
        /// </summary>
        /// <param name="indexInEntityQuery">Index of a ComponentType in this EntityQuery's RequiredComponents list./param>
        /// <returns>AtomicSafetyHandle for a buffer</returns>
        [BurstCompatible(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal AtomicSafetyHandle GetBufferSafetyHandle(int indexInEntityQuery) => _GetImpl()->GetBufferSafetyHandle(indexInEntityQuery);

#endif

        /// <summary>
        /// Calculates the number of entities selected by this EntityQuery.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute and apply any filters to calculate the entity count. If you are checking for whether the entity count equals zero, consider the more efficient IsEmpty property.
        /// </remarks>
        /// <returns>The number of entities based on the current EntityQuery properties.</returns>
        public int CalculateEntityCount() => _GetImpl()->CalculateEntityCount();
        /// <summary>
        /// Calculates the number of entities in the input entity list which match this EntityQuery.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute and apply any filters to calculate the entity count. If you are checking for whether the entity count equals zero, consider the more efficient IsEmpty property.
        /// </remarks>
        /// <param name="entityArray">A list of entities to limit execution to. Only entities in the list will be considered.</param>
        /// <returns>The number of entities based on the current EntityQuery properties.</returns>
        public int CalculateEntityCount(NativeArray<Entity> entityArray) => _GetImpl()->CalculateEntityCount(entityArray);
        /// <summary>
        /// Calculates the number of entities selected by this EntityQuery, ignoring any set filters.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute to calculate the entity count. If you are checking for whether the entity count equals zero, consider the more efficient IsEmptyIgnoreFilter property.
        /// </remarks>
        /// <returns>The number of entities based on the current EntityQuery properties.</returns>
        public int CalculateEntityCountWithoutFiltering() => _GetImpl()->CalculateEntityCountWithoutFiltering();
        /// <summary>
        /// Calculates the number of entities in the input entity list which match this EntityQuery, ignoring any filters.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute and apply any filters to calculate the entity count. If you are checking for whether the entity count equals zero, consider the more efficient IsEmpty property.
        /// </remarks>
        /// <param name="entityArray">A list of entities to limit execution to. Only entities in the list will be considered.</param>
        /// <returns>The number of entities based on the current EntityQuery properties.</returns>
        public int CalculateEntityCountWithoutFiltering(NativeArray<Entity> entityArray) => _GetImpl()->CalculateEntityCountWithoutFiltering(entityArray);
        /// <summary>
        /// Calculates the number of chunks that match this EntityQuery.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute and apply any filters to calculate the chunk count.
        /// </remarks>
        /// <returns>The number of chunks based on the current EntityQuery properties.</returns>
        public int CalculateChunkCount() => _GetImpl()->CalculateChunkCount();
        /// <summary>
        /// Calculates the number of chunks that match this EntityQuery, ignoring any set filters.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute to calculate the chunk count.
        /// </remarks>
        /// <returns>The number of chunks based on the current EntityQuery properties.</returns>
        public int CalculateChunkCountWithoutFiltering() => _GetImpl()->CalculateChunkCountWithoutFiltering();
        /// <summary>
        /// Fast path to determine if any entities in the input entity list match this EntityQuery.
        /// </summary>
        /// <param name="entityArray">A list of entities to limit execution to. Only entities in the list will be considered.</param>
        /// <returns>True if any entity in the list matches the query, false if no entities match the query</returns>
        public bool MatchesAny(NativeArray<Entity> entityArray) => _GetImpl()->MatchesInEntityArray(entityArray);
        /// <summary>
        /// Fast path to determine if any entities in the input entity list match this EntityQuery, ignoring any filters.
        /// </summary>
        /// <param name="entityArray">A list of entities to limit execution to. Only entities in the list will be considered.</param>
        /// <returns>True if any entity in the list matches the query, false if no entities match the query</returns>
        public bool MatchesAnyIgnoreFilter(NativeArray<Entity> entityArray) => _GetImpl()->MatchesInEntityArrayIgnoreFilter(entityArray);
        /// <summary>
        /// Gets an ArchetypeChunkIterator which can be used to iterate over every chunk returned by this EntityQuery.
        /// </summary>
        /// <returns>ArchetypeChunkIterator for this EntityQuery</returns>
        public ArchetypeChunkIterator GetArchetypeChunkIterator() => _GetImpl()->GetArchetypeChunkIterator();
        /// <summary>
        ///     Index of a ComponentType in this EntityQuery's RequiredComponents list.
        ///     For example, you have a EntityQuery that requires these ComponentTypes: Position, Velocity, and Color.
        ///
        ///     These are their type indices (according to the TypeManager):
        ///         Position.TypeIndex == 3
        ///         Velocity.TypeIndex == 5
        ///            Color.TypeIndex == 17
        ///
        ///     RequiredComponents: [Position -> Velocity -> Color] (a linked list)
        ///     Given Velocity's TypeIndex (5), the return value would be 1, since Velocity is in slot 1 of RequiredComponents.
        /// </summary>
        /// <param name="componentType">Index of a ComponentType in the TypeManager</param>
        /// <returns>An index into RequiredComponents.</returns>
        internal int GetIndexInEntityQuery(int componentType) => _GetImpl()->GetIndexInEntityQuery(componentType);
        /// <summary>
        /// Asynchronously creates an array of the chunks containing entities matching this EntityQuery.
        /// </summary>
        /// <remarks>
        /// Use <paramref name="jobhandle"/> as a dependency for jobs that use the returned chunk array.
        /// <seealso cref="CreateArchetypeChunkArray(Unity.Collections.Allocator)"/>.</remarks>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="jobhandle">An `out` parameter assigned the handle to the internal job
        /// that gathers the chunks matching this EntityQuery.
        /// </param>
        /// <returns>NativeArray of all the chunks containing entities matching this query.</returns>
        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArrayAsync(Allocator allocator, out JobHandle jobhandle) => _GetImpl()->CreateArchetypeChunkArrayAsync(allocator, out jobhandle);

        /// <summary>
        /// Synchronously creates an array of the chunks containing entities matching this EntityQuery.
        /// </summary>
        /// <remarks>This method blocks until the internal job that performs the query completes.
        /// <seealso cref="CreateArchetypeChunkArrayAsync(Allocator, out JobHandle)"/>
        /// </remarks>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <returns>NativeArray of all the chunks in this ComponentChunkIterator.</returns>
        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(Allocator allocator) => _GetImpl()->CreateArchetypeChunkArray(allocator);

        /// <summary>
        /// Creates a NativeArray containing the selected entities.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="jobhandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the NativeArray.</param>
        /// <returns>An array containing all the entities selected by the EntityQuery.</returns>
        public NativeArray<Entity> ToEntityArrayAsync(Allocator allocator, out JobHandle jobhandle) => _GetImpl()->ToEntityArrayAsync(allocator, out jobhandle, this);


        /// <summary>
        /// Creates a NativeArray containing the selected entities.
        /// </summary>
        /// <remarks>This version of the function blocks until the Job used to fill the array is complete.</remarks>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <returns>An array containing all the entities selected by the EntityQuery.</returns>
        public NativeArray<Entity> ToEntityArray(Allocator allocator) => _GetImpl()->ToEntityArray(allocator, this);

        /// <summary>
        /// Creates a NativeArray containing the selected entities, given an input entity list to limit the search.
        /// </summary>
        /// <remarks>This version of the function blocks until the Job used to fill the array is complete.</remarks>
        /// <param name="entityArray">The list of entities to be considered. Only entities in this list will be considered as output. </param>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <returns>An array containing all the entities selected by the EntityQuery.</returns>
        public NativeArray<Entity> ToEntityArray(NativeArray<Entity>entityArray, Allocator allocator) => _GetImpl()->ToEntityArray(entityArray, allocator);

        public struct GatherEntitiesResult
        {
            public int StartingOffset;
            public int EntityCount;
            public Entity* EntityBuffer;
            public NativeArray<Entity> EntityArray;
        }

        internal void GatherEntitiesToArray(out GatherEntitiesResult result) => _GetImpl()->GatherEntitiesToArray(out result, this);
        internal void ReleaseGatheredEntities(ref GatherEntitiesResult result) => _GetImpl()->ReleaseGatheredEntities(ref result);
        /// <summary>
        /// Creates a NativeArray containing the components of type T for the selected entities.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="jobhandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the NativeArray.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>An array containing the specified component for all the entities selected
        /// by the EntityQuery.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeArray<T> ToComponentDataArrayAsync<T>(Allocator allocator, out JobHandle jobhandle)            where T : struct, IComponentData
            => _GetImpl()->ToComponentDataArrayAsync<T>(allocator, out jobhandle, this);

        /// <summary>
        /// Creates a NativeArray containing the components of type T for the selected entities.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>An array containing the specified component for all the entities selected
        /// by the EntityQuery.</returns>
        /// <exception cref="InvalidOperationException">Thrown if you ask for a component that is not part of
        /// the group.</exception>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeArray<T> ToComponentDataArray<T>(Allocator allocator)            where T : struct, IComponentData
            => _GetImpl()->ToComponentDataArray<T>(allocator, this);
        /// <summary>
        /// Creates a NativeArray containing the components of type T for the selected entities, given an input entity list to limit the search.
        /// </summary>
        /// <remarks>This version of the function blocks until the Job used to fill the array is complete.</remarks>
        /// <param name="entityArray">The list of entities to be considered. Only entities in this list will be considered as output. </param>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <returns>An array containing all the entities selected by the EntityQuery.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeArray<T> ToComponentDataArray<T>(NativeArray<Entity>entityArray, Allocator allocator) where T : struct, IComponentData
            => _GetImpl()->ToComponentDataArray<T>(entityArray, allocator);
#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [NotBurstCompatible]
        public T[] ToComponentDataArray<T>() where T : class, IComponentData
            => _GetImpl()->ToComponentDataArray<T>();
#endif

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void CopyFromComponentDataArray<T>(NativeArray<T> componentDataArray)            where T : struct, IComponentData
            => _GetImpl()->CopyFromComponentDataArray<T>(componentDataArray, this);

        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void CopyFromComponentDataArrayAsync<T>(NativeArray<T> componentDataArray, out JobHandle jobhandle)            where T : struct, IComponentData
            => _GetImpl()->CopyFromComponentDataArrayAsync<T>(componentDataArray, out jobhandle, this);

        public Entity GetSingletonEntity() => _GetImpl()->GetSingletonEntity();

        /// <summary>
        /// Gets the value of a singleton component.
        /// </summary>
        /// <remarks>A singleton component is a component of which only one instance exists that satisfies this query.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>A copy of the singleton component.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <seealso cref="SetSingleton{T}(T)"/>
        /// <seealso cref="GetSingletonEntity"/>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public T GetSingleton<T>() where T : struct, IComponentData
            => _GetImpl()->GetSingleton<T>();
        /// <summary>
        /// Sets the value of a singleton component.
        /// </summary>
        /// <remarks>
        /// For a component to be a singleton, there can be only one instance of that component
        /// that satisfies this query.
        ///
        /// **Note:** singletons are otherwise normal entities. The EntityQuery and <see cref="ComponentSystemBase"/>
        /// singleton functions add checks that you have not created two instances of a
        /// type that can be accessed by this singleton query, but other APIs do not prevent such accidental creation.
        ///
        /// To create a singleton, create an entity with the singleton component.
        ///
        /// For example, if you had a component defined as:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="singleton-type-example" title="Singleton"/>
        /// </example>
        ///
        /// You could create a singleton as follows:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="create-singleton" title="Create Singleton"/>
        /// </example>
        ///
        /// To update the singleton component after creation, you can use an EntityQuery object that
        /// selects the singleton entity and call this `SetSingleton()` function:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="set-singleton" title="Set Singleton"/>
        /// </example>
        ///
        /// You can set and get the singleton value from a system: see <seealso cref="ComponentSystemBase.SetSingleton{T}(T)"/>
        /// and <seealso cref="ComponentSystemBase.GetSingleton{T}"/>.
        /// </remarks>
        /// <param name="value">An instance of type T containing the values to set.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if more than one instance of this component type
        /// exists in the world or the component type appears in more than one archetype.</exception>
        /// <seealso cref="GetSingleton{T}"/>
        /// <seealso cref="GetSingletonEntity"/>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void SetSingleton<T>(T value) where T : struct, IComponentData
            => _GetImpl()->SetSingleton<T>(value);
        internal bool CompareComponents(ComponentType* componentTypes, int count) => _GetImpl()->CompareComponents(componentTypes, count);
        /// <summary>
        /// Compares a list of component types to the types defining this EntityQuery.
        /// </summary>
        /// <remarks>Only required types in the query are used as the basis for the comparison.
        /// If you include types that the query excludes or only includes as optional,
        /// the comparison returns false.</remarks>
        /// <param name="componentTypes">An array of ComponentType objects.</param>
        /// <returns>True, if the list of types, including any read/write access specifiers,
        /// matches the list of required component types of this EntityQuery.</returns>
        [NotBurstCompatible]
        public bool CompareComponents(ComponentType[] componentTypes) => _GetImpl()->CompareComponents(componentTypes);
        /// <summary>
        /// Compares a list of component types to the types defining this EntityQuery.
        /// </summary>
        /// <remarks>Only required types in the query are used as the basis for the comparison.
        /// If you include types that the query excludes or only includes as optional,
        /// the comparison returns false. Do not include the <see cref="Entity"/> type, which
        /// is included implicitly.</remarks>
        /// <param name="componentTypes">An array of ComponentType objects.</param>
        /// <returns>True, if the list of types, including any read/write access specifiers,
        /// matches the list of required component types of this EntityQuery.</returns>
        public bool CompareComponents(NativeArray<ComponentType> componentTypes) => _GetImpl()->CompareComponents(componentTypes);
        /// <summary>
        /// Compares a query description to the description defining this EntityQuery.
        /// </summary>
        /// <remarks>The `All`, `Any`, and `None` components in the query description are
        /// compared to the corresponding list in this EntityQuery.</remarks>
        /// <param name="queryDesc">The query description to compare.</param>
        /// <returns>True, if the query description contains the same components with the same
        /// read/write access modifiers as this EntityQuery.</returns>
        [Obsolete("Use unmanaged CompareQuery() equivalent instead. (RemovedAfter 2021-05-22)")]
        public bool CompareQuery(EntityQueryDesc[] queryDesc) => _GetImpl()->CompareQuery(queryDesc);
        /// <summary>
        /// Compares a query description to the description defining this EntityQuery.
        /// </summary>
        /// <remarks>The `All`, `Any`, and `None` components in the query description are
        /// compared to the corresponding list in this EntityQuery.</remarks>
        /// <param name="queryDesc">The query description to compare.</param>
        /// <returns>True, if the query description contains the same components with the same
        /// read/write access modifiers as this EntityQuery.</returns>
        public bool CompareQuery(in EntityQueryDescBuilder queryDesc) => _GetImpl()->CompareQuery(queryDesc);
        /// <summary>
        /// Resets this EntityQuery's filter.
        /// </summary>
        /// <remarks>
        /// Removes references to shared component data, if applicable, then resets the filter type to None.
        /// </remarks>
        [NotBurstCompatible]
        public void ResetFilter() => _GetImpl()->ResetFilter();
        /// <summary>
        /// Filters this EntityQuery so that it only selects entities with shared component values
        /// matching the values specified by the `sharedComponent1` parameter.
        /// </summary>
        /// <param name="sharedComponent1">The shared component values on which to filter.</param>
        /// <typeparam name="SharedComponent1">The type of shared component. (The type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        [NotBurstCompatible]
        public void SetSharedComponentFilter<SharedComponent1>(SharedComponent1 sharedComponent1)            where SharedComponent1 : struct, ISharedComponentData
            => _GetImpl()->SetSharedComponentFilter<SharedComponent1>(sharedComponent1);
        /// <summary>
        /// Filters this EntityQuery based on the values of two separate shared components.
        /// </summary>
        /// <remarks>
        /// The filter only selects entities for which both shared component values
        /// specified by the `sharedComponent1` and `sharedComponent2` parameters match.
        /// </remarks>
        /// <param name="sharedComponent1">Shared component values on which to filter.</param>
        /// <param name="sharedComponent2">Shared component values on which to filter.</param>
        /// <typeparam name="SharedComponent1">The type of shared component. (The type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        /// <typeparam name="SharedComponent2">The type of shared component. (The type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        [NotBurstCompatible]
        public void SetSharedComponentFilter<SharedComponent1, SharedComponent2>(SharedComponent1 sharedComponent1,
            SharedComponent2 sharedComponent2)            where SharedComponent1 : struct, ISharedComponentData
            where SharedComponent2 : struct, ISharedComponentData
            => _GetImpl()->SetSharedComponentFilter<SharedComponent1, SharedComponent2>(sharedComponent1, sharedComponent2);
        /// <summary>
        /// Filters out entities in chunks for which the specified component has not changed.
        /// </summary>
        /// <remarks>
        ///     Saves a given ComponentType's index in RequiredComponents in this group's Changed filter.
        /// </remarks>
        /// <param name="componentType">ComponentType to mark as changed on this EntityQuery's filter.</param>
        [NotBurstCompatible] // Due to talking to managed component store
        public void SetChangedVersionFilter(ComponentType componentType) => _GetImpl()->SetChangedVersionFilter(componentType);
        internal void SetChangedFilterRequiredVersion(uint requiredVersion) => _GetImpl()->SetChangedFilterRequiredVersion(requiredVersion);

        /// <summary>
        /// Filters out entities in chunks for which the specified component has not changed.
        /// </summary>
        /// <remarks>
        ///     Saves a given ComponentType's index in RequiredComponents in this group's Changed filter.
        /// </remarks>
        /// <param name="componentType">ComponentTypes to mark as changed on this EntityQuery's filter.</param>
        [NotBurstCompatible]
        public void SetChangedVersionFilter(ComponentType[] componentType) => _GetImpl()->SetChangedVersionFilter(componentType);

        /// <summary>
        /// Filters out entities in chunks for which the specified component has not changed. Additive with other filter functions.
        /// </summary>
        /// <remarks>
        ///     Saves a given ComponentType's index in RequiredComponents in this group's Changed filter.
        /// </remarks>
        /// <param name="componentType">ComponentType to mark as changed on this EntityQuery's filter.</param>
        public void AddChangedVersionFilter(ComponentType componentType) => _GetImpl()->AddChangedVersionFilter(componentType);

        /// <summary>
        /// Filters this EntityQuery so that it only selects entities with shared component values
        /// matching the values specified by the `sharedComponent1` parameter. Additive with other filter functions.
        /// </summary>
        /// <param name="sharedComponent1">The shared component values on which to filter.</param>
        /// <typeparam name="SharedComponent1">The type of shared component. (The type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        [NotBurstCompatible]
        public void AddSharedComponentFilter<SharedComponent>(SharedComponent sharedComponent)            where SharedComponent : struct, ISharedComponentData
            => _GetImpl()->AddSharedComponentFilter<SharedComponent>(sharedComponent);

        /// <summary>
        /// Filters out entities in chunks for which no structural changes have occurred.
        /// </summary>
        [NotBurstCompatible]
        public void SetOrderVersionFilter() => _GetImpl()->SetOrderVersionFilter();

        /// <summary>
        /// Filters out entities in chunks for which no structural changes have occurred. Additive with other filter functions.
        /// </summary>
        public void AddOrderVersionFilter() => _GetImpl()->AddOrderVersionFilter();
        /// <summary>
        /// Ensures all jobs running on this EntityQuery complete.
        /// </summary>
        /// <remarks>An entity query uses jobs internally when required to create arrays of
        /// entities and chunks. This function completes those jobs and returns when they are finished.
        /// </remarks>
        public void CompleteDependency() => _GetImpl()->CompleteDependency();
        /// <summary>
        /// Combines all dependencies in this EntityQuery into a single JobHandle.
        /// </summary>
        /// <remarks>An entity query uses jobs internally when required to create arrays of
        /// entities and chunks.</remarks>
        /// <returns>JobHandle that represents the combined dependencies of this EntityQuery</returns>
        public JobHandle GetDependency() => _GetImpl()->GetDependency();
        /// <summary>
        /// Adds another job handle to this EntityQuery's dependencies.
        /// </summary>
        /// <remarks>An entity query uses jobs internally when required to create arrays of
        /// entities and chunks. This junction adds an external job as a dependency for those
        /// internal jobs.</remarks>
        public JobHandle AddDependency(JobHandle job) => _GetImpl()->AddDependency(job);
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public int GetCombinedComponentOrderVersion() => _GetImpl()->GetCombinedComponentOrderVersion();
        internal bool AddReaderWritersToLists(ref UnsafeList<int> reading, ref UnsafeList<int> writing) => _GetImpl()->AddReaderWritersToLists(ref reading, ref writing);
        /// <summary>
        /// Syncs the needed types for the filter.
        /// For every type that is change filtered we need to CompleteWriteDependency to avoid race conditions on the
        /// change version of those types
        /// </summary>
        internal void SyncFilterTypes() => _GetImpl()->SyncFilterTypes();
        /// <summary>
        /// Syncs the needed types for the filter using the types in UnsafeMatchingArchetypePtrList
        /// This version is used when the EntityQuery is not known
        /// </summary>
        internal static void SyncFilterTypes(ref UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, ComponentDependencyManager* safetyManager) => EntityQueryImpl.SyncFilterTypes(ref matchingArchetypes, ref filter, safetyManager);
        /// <summary>
        /// Reports whether this entity query has a filter applied to it.
        /// </summary>
        /// <returns>Returns true if the query has a filter, returns false if the query does not have a filter.</returns>
        public bool HasFilter() => _GetImpl()->HasFilter();
        /// <summary>
        /// Returns an EntityQueryMask, which can be used to quickly determine if an entity matches the query.
        /// </summary>
        /// <remarks>A maximum of 1024 EntityQueryMasks can be allocated per World.</remarks>
        public EntityQueryMask GetEntityQueryMask() => _GetImpl()->GetEntityQueryMask();
        /// <summary>
        /// Returns true if the entity matches the query, false if it does not. Applies any filters
        /// </summary>
        /// <param name="e">The entity to check for match</param>
        /// <remarks>This function creates an EntityQueryMask, if one does not exist for this query already. A maximum of 1024 EntityQueryMasks can be allocated per World.</remarks>
        public bool Matches(Entity e) => _GetImpl()->Matches(e);
        /// <summary>
        /// Returns true if the entity matches the query, false if it does not. Applies any filters
        /// </summary>
        /// <param name="e">The entity to check for match</param>
        /// <remarks>This function creates an EntityQueryMask, if one does not exist for this query already. A maximum of 1024 EntityQueryMasks can be allocated per World.</remarks>
        public bool MatchesNoFilter(Entity e) => _GetImpl()->MatchesNoFilter(e);

        /// <summary>
        /// Returns an EntityQueryDesc, which can be used to re-create the EntityQuery.
        /// </summary>
        [NotBurstCompatible]
        public EntityQueryDesc GetEntityQueryDesc() => _GetImpl()->GetEntityQueryDesc();

        internal void InvalidateCache() => _GetImpl()->_QueryData->MatchingChunkCache.InvalidateCache();
        internal void UpdateCache() => ChunkIterationUtility.RebuildChunkListCache(_GetImpl()->_QueryData);
        internal bool CheckChunkListCacheConsistency() => _GetImpl()->CheckChunkListCacheConsistency();
        internal bool IsCacheValid => _GetImpl()->_QueryData->MatchingChunkCache.IsCacheValid;

        /// <summary>
        ///  Internal gen impl
        /// </summary>
        /// <returns></returns>
        internal EntityQueryImpl* _GetImpl()
        {
            _CheckSafetyHandle();
            return __impl;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void _CheckSafetyHandle()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(__safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void _CreateSafetyHandle(ref EntityQuery _s)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _s.__safety = AtomicSafetyHandle.Create();
#endif
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle __safety;
#endif

        internal EntityQueryImpl* __impl;
        internal ulong __seqno;

        public static bool operator==(EntityQuery lhs, EntityQuery rhs)
        {
            return lhs.__seqno == rhs.__seqno;
        }

        public static bool operator!=(EntityQuery lhs, EntityQuery rhs)
        {
            return !(lhs == rhs);
        }
    }


#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public static unsafe class EntityQueryManagedComponentExtensions
    {
        /// <summary>
        /// Gets the value of a singleton component.
        /// </summary>
        /// <remarks>A singleton component is a component of which only one instance exists that satisfies this query.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>A copy of the singleton component.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <seealso cref="SetSingleton{T}(EntityQuery, T)"/>
        /// <seealso cref="GetSingleton{T}(EntityQuery)"/>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        public static T GetSingleton<T>(this EntityQuery query) where T : class, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var impl = query._GetImpl();
            impl->_Access->DependencyManager->CompleteWriteDependencyNoChecks(typeIndex);
            int managedComponentIndex;

            if (!impl->_Filter.RequiresMatchesFilter && impl->_QueryData->RequiredComponentsCount <= 2 && impl->_QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                // Fast path with no filter & assuming this is a simple query with just one singleton component
                var archetypeIndex = impl->GetFirstArchetypeIndexWithEntity(out var archetypeEntityCount);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (archetypeEntityCount != 1)
                    throw new InvalidOperationException($"GetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {archetypeEntityCount}.");
#endif
                var match = impl->_QueryData->MatchingArchetypes.Ptr[archetypeIndex];
                // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                managedComponentIndex = *(int*)ChunkIterationUtility.GetChunkComponentDataPtr(match->Archetype->Chunks[0], true,
                    match->IndexInArchetype[1], impl->_Access->EntityComponentStore->GlobalSystemVersion);
                return (T)impl->_Access->ManagedComponentStore.GetManagedComponent(managedComponentIndex);
            }
            else
            {
                // Slower path w/filtering and/or multi-component queries
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var queryEntityCount = query.CalculateEntityCount();
                if (queryEntityCount != 1)
                    throw new InvalidOperationException($"GetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {queryEntityCount}.");
#endif

                var queryData = impl->_QueryData;
                var matchingChunkCache = queryData->GetMatchingChunkCache();
                var chunkList = *matchingChunkCache.MatchingChunks;
                var matchingArchetypeIndices = *matchingChunkCache.PerChunkMatchingArchetypeIndex;
                var matchingArchetypes = queryData->MatchingArchetypes.Ptr;
                int chunkCount = chunkList.Length;
                for (int i = 0; i < chunkCount; ++i)
                {
                    var chunk = chunkList[i];
                    var matchIndex = matchingArchetypeIndices[i];
                    var match = matchingArchetypes[matchIndex];
                    if (match->ChunkMatchesFilter(chunk->ListIndex, ref impl->_Filter))
                    {
                        // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                        managedComponentIndex = *(int*)ChunkIterationUtility.GetChunkComponentDataPtr(match->Archetype->Chunks[0], true,
                            match->IndexInArchetype[1], impl->_Access->EntityComponentStore->GlobalSystemVersion);
                        return (T)impl->_Access->ManagedComponentStore.GetManagedComponent(managedComponentIndex);
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                throw new InvalidOperationException(
                    "Bug in GetSingleton(): found no chunk that matches the provided filter, but CalculateEntityCount() == 1");
#else
                return default;
#endif
            }
        }

        /// <summary>
        /// Sets the value of a singleton component.
        /// </summary>
        /// <remarks>
        /// For a component to be a singleton, there can be only one instance of that component
        /// that satisfies this query.
        ///
        /// **Note:** singletons are otherwise normal entities. The EntityQuery and <see cref="ComponentSystemBase"/>
        /// singleton functions add checks that you have not created two instances of a
        /// type that can be accessed by this singleton query, but other APIs do not prevent such accidental creation.
        ///
        /// To create a singleton, create an entity with the singleton component.
        ///
        /// For example, if you had a component defined as:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="singleton-type-example" title="Singleton"/>
        /// </example>
        ///
        /// You could create a singleton as follows:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="create-singleton" title="Create Singleton"/>
        /// </example>
        ///
        /// To update the singleton component after creation, you can use an EntityQuery object that
        /// selects the singleton entity and call this `SetSingleton()` function:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="set-singleton" title="Set Singleton"/>
        /// </example>
        ///
        /// You can set and get the singleton value from a system: see <seealso cref="ComponentSystemBase.SetSingleton{T}(T)"/>
        /// and <seealso cref="ComponentSystemBase.GetSingleton{T}"/>.
        /// </remarks>
        /// <param name="value">An instance of type T containing the values to set.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if more than one instance of this component type
        /// exists in the world or the component type appears in more than one archetype.</exception>
        /// <seealso cref="GetSingleton{T}"/>
        /// <seealso cref="EntityQuery.GetSingletonEntity"/>
        public static void SetSingleton<T>(this EntityQuery query, T value) where T : class, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var impl = query._GetImpl();
            var access = impl->_Access;

            access->DependencyManager->CompleteWriteDependencyNoChecks(typeIndex);
            int* managedComponentIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (value != null && value.GetType() != typeof(T))
                throw new ArgumentException($"Assigning component value is of type: {value.GetType()} but the expected component type is: {typeof(T)}");
#endif

            if (!impl->_Filter.RequiresMatchesFilter && impl->_QueryData->RequiredComponentsCount <= 2 && impl->_QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                // Fast path with no filter & assuming this is a simple query with just one singleton component
                var archetypeIndex = impl->GetFirstArchetypeIndexWithEntity(out var entityCount);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (entityCount != 1)
                    throw new InvalidOperationException($"SetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {entityCount}.");
#endif
                var match = impl->_QueryData->MatchingArchetypes.Ptr[archetypeIndex];
                // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                managedComponentIndex = (int*)ChunkIterationUtility.GetChunkComponentDataPtr(match->Archetype->Chunks[0], true,
                    match->IndexInArchetype[1], access->EntityComponentStore->GlobalSystemVersion);
                access->ManagedComponentStore.UpdateManagedComponentValue(managedComponentIndex, value, ref *access->EntityComponentStore);
            }
            else
            {
                // Slower path w/filtering and/or multi-component query
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var queryEntityCount = query.CalculateEntityCount();
                if (queryEntityCount != 1)
                    throw new InvalidOperationException(
                        $"SetSingleton<{typeof(T)}>() requires that exactly one {typeof(T)} exist that match this query, but there are {queryEntityCount}.");
#endif
                var queryData = impl->_QueryData;
                var matchingChunkCache = queryData->GetMatchingChunkCache();
                var chunkList = *matchingChunkCache.MatchingChunks;
                var matchingArchetypeIndices = *matchingChunkCache.PerChunkMatchingArchetypeIndex;
                var matchingArchetypes = queryData->MatchingArchetypes.Ptr;
                int chunkCount = chunkList.Length;
                for (int i = 0; i < chunkCount; ++i)
                {
                    var chunk = chunkList[i];
                    var matchIndex = matchingArchetypeIndices[i];
                    var match = matchingArchetypes[matchIndex];
                    if (match->ChunkMatchesFilter(chunk->ListIndex, ref impl->_Filter))
                    {
                        // TODO(https://unity3d.atlassian.net/browse/DOTS-4625): Can't just grab the first entity here, it may be disabled
                        managedComponentIndex = (int*)ChunkIterationUtility.GetChunkComponentDataPtr(match->Archetype->Chunks[0], true,
                            match->IndexInArchetype[1], access->EntityComponentStore->GlobalSystemVersion);
                        access->ManagedComponentStore.UpdateManagedComponentValue(managedComponentIndex, value, ref *access->EntityComponentStore);
                        return;
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                throw new InvalidOperationException(
                    "Bug in SetSingleton(): found no chunk that matches the provided filter, but CalculateEntityCount() == 1");
#endif
            }
        }
    }
#endif
}
