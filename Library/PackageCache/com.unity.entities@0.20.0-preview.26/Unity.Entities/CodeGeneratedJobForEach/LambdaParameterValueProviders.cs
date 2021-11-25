using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.CodeGeneratedJobForEach
{
    public struct LambdaParameterValueProvider_Entity
    {
        private EntityTypeHandle _typeHandle;

        public struct Runtime
        {
            [NativeDisableUnsafePtrRestriction]
            public unsafe Entity* arrayPtr;

            public unsafe ref Entity For(int i)
            {
                return ref *(arrayPtr + i);
            }
        }

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
            _typeHandle = jobComponentSystem.GetEntityTypeHandle();
        }

        public unsafe Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk)
        {
            var ptr = (Entity*)chunk.GetNativeArray(_typeHandle).GetUnsafeReadOnlyPtr();
            return new Runtime()
            {
                arrayPtr = ptr
            };
        }

        public struct StructuralChangeRuntime
        {
            public Entity For(Entity e)
            {
                return e;
            }
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime();
        }
    }

    public struct LambdaParameterValueProvider_DynamicBuffer<T> where T : struct, IBufferElementData
    {
        BufferTypeHandle<T> _typeHandle;

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
            _typeHandle = jobComponentSystem.GetBufferTypeHandle<T>(isReadOnly);
        }

        public struct Runtime
        {
            public BufferAccessor<T> bufferAccessor;

            public DynamicBuffer<T> For(int i)
            {
                return bufferAccessor[i];
            }
        }

        public Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk)
        {
            return new Runtime()
            {
                bufferAccessor = chunk.GetBufferAccessor(_typeHandle)
            };
        }

        public struct StructuralChangeRuntime
        {
            public EntityManager _entityManager;

            public DynamicBuffer<T> For(Entity e)
            {
                return _entityManager.GetBuffer<T>(e);
            }
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime() { _entityManager = componentSystem.EntityManager };
        }
    }

    public struct LambdaParameterValueProvider_IComponentData<T>
        where T : struct, IComponentData
    {
        ComponentTypeHandle<T> _typeHandle;

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
            _typeHandle = jobComponentSystem.GetComponentTypeHandle<T>(isReadOnly);
        }

        public struct Runtime
        {
            public unsafe byte* ptr;

            public unsafe ref T For(int i)
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(ptr, i);
            }
        }

        public unsafe Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk)
        {
            var componentDatas = chunk.GetNativeArray(_typeHandle);
            return new Runtime()
            {
                ptr = (byte*)(_typeHandle.IsReadOnly
                    ? componentDatas.GetUnsafeReadOnlyPtr()
                    : componentDatas.GetUnsafePtr()),
            };
        }

        public struct StructuralChangeRuntime
        {
            public EntityManager _manager;
            public int _typeIndex;

            public unsafe T For(Entity entity, out T originalComponent)
            {
                var access = _manager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                UnsafeUtility.CopyPtrToStructure(ecs->GetComponentDataWithTypeRO(entity, _typeIndex), out originalComponent);
                return originalComponent;
            }

            public unsafe void WriteBack(Entity entity, ref T lambdaComponent, ref T originalComponent)
            {
                var access = _manager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                // MemCmp check is necessary to ensure we only write-back the value if we changed it in the lambda (or a called function)
                if (UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref lambdaComponent), UnsafeUtility.AddressOf(ref originalComponent), UnsafeUtility.SizeOf<T>()) != 0 &&
                    ecs->HasComponent(entity, _typeIndex))
                {
                    UnsafeUtility.CopyStructureToPtr(ref lambdaComponent, ecs->GetComponentDataWithTypeRW(entity, _typeIndex, ecs->GlobalSystemVersion));
                }
            }
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime() { _manager = componentSystem.EntityManager, _typeIndex = TypeManager.GetTypeIndex<T>() };
        }
    }

    // Most of this is unused but it makes the symmetry of codegen easier (from a codegen perspective we can treat tag components the same as normal ones)
    public struct LambdaParameterValueProvider_IComponentData_Tag<T>
        where T : struct, IComponentData
    {
        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly) {}

        public struct Runtime
        {
            public T For(int i) => default;
        }

        public unsafe Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk) { return new Runtime() {}; }
        public struct StructuralChangeRuntime
        {
            public unsafe T For(Entity entity, out T originalComponent)
            {
                originalComponent = default;
                return default;
            }

            public unsafe void WriteBack(Entity entity, ref T lambdaComponent, ref T originalComponent)
            {}
        }
        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime();
        }
    }

    public struct LambdaParameterValueProvider_ManagedComponentData<T> where T : class
    {
        private EntityManager _entityManager;
        private ComponentTypeHandle<T> _typeHandle;

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
            _entityManager = jobComponentSystem.EntityManager;
            _typeHandle = _entityManager.GetComponentTypeHandle<T>(isReadOnly);
        }

        public struct Runtime
        {
            internal ManagedComponentAccessor<T> Accessor;
            public T For(int i) => Accessor[i];
        }

        public Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk)
        {
            return new Runtime()
            {
                Accessor = chunk.GetManagedComponentAccessor(_typeHandle, _entityManager)
            };
        }

        public struct StructuralChangeRuntime
        {
            public EntityManager _entityManager;

            public T For(Entity entity)
            {
                return _entityManager.GetComponentObject<T>(entity);
            }
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime() { _entityManager = componentSystem.EntityManager };
        }
    }

    public struct LambdaParameterValueProvider_ISharedComponentData<T> where T : struct, ISharedComponentData
    {
        SharedComponentTypeHandle<T> _typeHandle;
        EntityManager _entityManager;

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
            _typeHandle = jobComponentSystem.GetSharedComponentTypeHandle<T>();
            _entityManager = jobComponentSystem.EntityManager;
        }

        public struct Runtime
        {
            public T _data;
            public T For(int i) => _data;
        }

        public Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk) =>
            new Runtime()
        {
            _data = chunk.GetSharedComponentData(_typeHandle, _entityManager)
        };

        public struct StructuralChangeRuntime
        {
            public EntityManager _manager;

            public unsafe T For(Entity entity)
            {
                return _manager.GetSharedComponentData<T>(entity);
            }
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime() { _manager = componentSystem.EntityManager };
        }
    }

    public struct LambdaParameterValueProvider_EntityInQueryIndex
    {
        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
        }

        public struct Runtime
        {
            internal int entityInQueryIndexOfFirstEntityInChunk;
            public int For(int i) => entityInQueryIndexOfFirstEntityInChunk + i;
        }

        public Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk, int chunkIndex, int entityInQueryIndexOfFirstEntity)
        {
            return new Runtime() {entityInQueryIndexOfFirstEntityInChunk = entityInQueryIndexOfFirstEntity};
        }

        public struct StructuralChangeRuntime
        {
            internal int entityInQueryIndexOfFirstEntityInChunk;
            public int For(Entity entity) => entityInQueryIndexOfFirstEntityInChunk++;
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime() {entityInQueryIndexOfFirstEntityInChunk = 0};
        }
    }

    public struct LambdaParameterValueProvider_NativeThreadIndex
    {
        [NativeSetThreadIndexAttribute] internal int _nativeThreadIndex;

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
        }

        public struct Runtime
        {
            internal int _nativeThreadIndex;
            public int For(int i) => _nativeThreadIndex;
        }

        public Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk)
        {
            return new Runtime() {_nativeThreadIndex = _nativeThreadIndex};
        }

        public Runtime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new Runtime() {_nativeThreadIndex = 0};
        }
    }

    public struct StructuralChangeEntityProvider
    {
        EntityQuery _query;
        EntityQuery.GatherEntitiesResult _gatherEntitiesResult;
        EntityQueryMask _Mask;

        public int EntityCount => _gatherEntitiesResult.EntityCount;
        public unsafe Entity For(int i)
        {
            var entity = _gatherEntitiesResult.EntityBuffer[i];
            if (_Mask.Matches(entity))
                return entity;
            else
                return Entity.Null;
        }

        public void PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            _query = query;
            _Mask = componentSystem.EntityManager.GetEntityQueryMask(query);
            _query.GatherEntitiesToArray(out _gatherEntitiesResult);
        }

        public unsafe delegate void PerformLambdaDelegate(void* jobStruct, void* runtimes, Entity entity);

        public unsafe void IterateEntities(void* jobStruct, void* runtimes, PerformLambdaDelegate action)
        {
            try
            {
                int entityCount = this.EntityCount;
                for (int i = 0; i != entityCount; i++)
                {
                    Entity entity = this.For(i);
                    if (entity != Entity.Null)
                        action(jobStruct, runtimes, entity);
                }
            }
            finally
            {
                _query.ReleaseGatheredEntities(ref _gatherEntitiesResult);
            }
        }
    }
}
