using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.PerformanceTesting;
using Unity.Entities.Tests;
using Unity.Jobs;
using static Unity.Entities.PerformanceTests.PerformanceTestHelpers;

namespace Unity.Entities.PerformanceTests
{
    [TestFixture]
    [Category("Performance")]
    public sealed class EntityQueryIncrementalCachingPerformanceTests : EntityPerformanceTestFixture
    {
        NativeArray<EntityQuery> CreateUniqueQueries(int size)
        {
            var queries = new NativeArray<EntityQuery>(size, Allocator.TempJob);

            for (int i = 0; i < size; i++)
            {
                var typeCount = CollectionHelper.Log2Ceil(i);
                var typeList = new List<ComponentType>();
                for (int typeIndex = 0; typeIndex < typeCount; typeIndex++)
                {
                    if ((i & (1 << typeIndex)) != 0)
                        typeList.Add(TestTags.TagTypes[typeIndex]);
                }

                typeList.Add(typeof(EcsTestData));
                typeList.Add(typeof(EcsTestSharedComp));

                var types = typeList.ToArray();
                queries[i] = m_Manager.CreateEntityQuery(types);
            }

            return queries;
        }

        [Test, Performance]
        public void CreateDestroyEntity_Scaling([Values(10, 100)] int archetypeCount, [Values(10, 100)] int queryCount)
        {
            const int kInitialEntityCount = 5000000;
            const int kCreateDestroyEntityCount = 200000;

            using(var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount, Allocator.TempJob,typeof(EcsTestData), typeof(EcsTestSharedComp)))
            using(var queries = CreateUniqueQueries(queryCount))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], kInitialEntityCount / archetypeCount);
                }

                var basicArchetype = m_Manager.CreateArchetype(typeof(EcsTestData));

                var createEntities = default(NativeArray<Entity>);
                Measure.Method(() =>
                    {
                        createEntities = m_Manager.CreateEntity(basicArchetype, kCreateDestroyEntityCount,
                            Allocator.TempJob);
                    })
                    .CleanUp(() =>
                    {
                        m_Manager.DestroyEntity(createEntities);
                        createEntities.Dispose();
                    })
                    .MeasurementCount(100)
                    .WarmupCount(1)
                    .SampleGroup("CreateEntities")
                    .Run();

                var destroyEntities = default(NativeArray<Entity>);
                Measure.Method(() => { m_Manager.DestroyEntity(destroyEntities); })
                    .SetUp(() =>
                    {
                        destroyEntities = m_Manager.CreateEntity(basicArchetype, kCreateDestroyEntityCount,
                            Allocator.TempJob);
                    })
                    .CleanUp(() => { destroyEntities.Dispose(); })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .SampleGroup("DestroyEntities")
                    .Run();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestChunkJob : IJobChunk
        {
            public ComponentTypeHandle<EcsTestData> EcsTestDataRW;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var data = chunk.GetNativeArray(EcsTestDataRW);
                data[0] = new EcsTestData {value = 10};
            }
        }

        void IJobChunk_Performance_Scheduling(int entityCount, int archetypeCount, bool enableQueryFiltering,
            bool enableQueryChunkCache)
        {
            using (var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount,
                Allocator.TempJob, typeof(EcsTestData), typeof(EcsTestSharedComp)))
            using (var basicQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData),
                typeof(EcsTestSharedComp)))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], entityCount / archetypeCount);
                }
                if (enableQueryFiltering)
                    basicQuery.SetSharedComponentFilter(default(EcsTestSharedComp));
                var handle = default(JobHandle);
                Measure.Method(() =>
                    {
                        handle = new TestChunkJob
                        {
                            EcsTestDataRW = m_Manager.GetComponentTypeHandle<EcsTestData>(false)
                        }.Schedule(basicQuery, handle);
                    })
                    .CleanUp(() =>
                    {
                        handle.Complete();
                        if (!enableQueryChunkCache)
                        {
                            basicQuery.InvalidateCache();
                        }
                    })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void IJobChunk_Performance_Scheduling_WithFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Scheduling(entityCount, archetypeCount, true, true);
        }
        [Test, Performance]
        public void IJobChunk_Performance_Scheduling_WithoutFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Scheduling(entityCount, archetypeCount, false, true);
        }
        [Test, Performance]
        public void IJobChunk_Performance_Scheduling_WithFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Scheduling(entityCount, archetypeCount, true, false);
        }
        [Test, Performance]
        public void IJobChunk_Performance_Scheduling_WithoutFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Scheduling(entityCount, archetypeCount, false, false);
        }

        void IJobChunk_Performance_Executing(int entityCount, int archetypeCount, bool enableQueryFiltering,
            bool enableQueryChunkCache)
        {
            using (var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount, Allocator.TempJob, typeof(EcsTestData), typeof(EcsTestSharedComp)))
            using (var basicQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestSharedComp)))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], entityCount / archetypeCount);
                }

                if (enableQueryFiltering)
                    basicQuery.SetSharedComponentFilter(default(EcsTestSharedComp));

                Measure.Method(() =>
                    {
                        new TestChunkJob
                        {
                            EcsTestDataRW = m_Manager.GetComponentTypeHandle<EcsTestData>(false)
                        }.Run(basicQuery);
                    })
                    .CleanUp(() =>
                    {
                        if (!enableQueryChunkCache)
                        {
                            basicQuery.InvalidateCache();
                        }
                    })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void IJobChunk_Performance_Executing_WithFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Executing(entityCount, archetypeCount, true, true);
        }
        [Test, Performance]
        public void IJobChunk_Performance_Executing_WithoutFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Executing(entityCount, archetypeCount, false, true);
        }
        [Test, Performance]
        public void IJobChunk_Performance_Executing_WithFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Executing(entityCount, archetypeCount, true, false);
        }
        [Test, Performance]
        public void IJobChunk_Performance_Executing_WithoutFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobChunk_Performance_Executing(entityCount, archetypeCount, false, false);
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestEntityBatchJob : IJobEntityBatch
        {
            public ComponentTypeHandle<EcsTestData> EcsTestDataRW;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var data = batchInChunk.GetNativeArray(EcsTestDataRW);
                data[0] = new EcsTestData {value = 10};
            }
        }

        void IJobEntityBatch_Performance_Scheduling(int entityCount, int archetypeCount, bool enableQueryFiltering,
            bool enableQueryChunkCache)
        {
            using (var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount,  Allocator.TempJob, typeof(EcsTestData), typeof(EcsTestSharedComp)))
            using (var basicQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestSharedComp)))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], entityCount / archetypeCount);
                }

                if (enableQueryFiltering)
                    basicQuery.SetSharedComponentFilter(default(EcsTestSharedComp));

                var handle = default(JobHandle);
                Measure.Method(() =>
                    {
                        handle = new TestEntityBatchJob
                        {
                            EcsTestDataRW = m_Manager.GetComponentTypeHandle<EcsTestData>(false)
                        }.ScheduleParallel(basicQuery, handle);
                    })
                    .CleanUp(() =>
                    {
                        handle.Complete();
                        if (!enableQueryChunkCache)
                        {
                            basicQuery.InvalidateCache();
                        }
                    })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void IJobEntityBatch_Performance_Scheduling_WithFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Scheduling(entityCount, archetypeCount, true, true);
        }
        [Test, Performance]
        public void IJobEntityBatch_Performance_Scheduling_WithoutFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Scheduling(entityCount, archetypeCount, false, true);
        }
        [Test, Performance]
        public void IJobEntityBatch_Performance_Scheduling_WithFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Scheduling(entityCount, archetypeCount, true, false);
        }
        [Test, Performance]
        public void IJobEntityBatch_Performance_Scheduling_WithoutFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Scheduling(entityCount, archetypeCount, false, false);
        }

        void IJobEntityBatch_Performance_Executing(int entityCount, int archetypeCount, bool enableQueryFiltering,
            bool enableQueryChunkCache)
        {
            using (var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount,  Allocator.TempJob, typeof(EcsTestData), typeof(EcsTestSharedComp)))
            using (var basicQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestSharedComp)))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], entityCount / archetypeCount);
                }

                if (enableQueryFiltering)
                    basicQuery.SetSharedComponentFilter(default(EcsTestSharedComp));

                Measure.Method(() =>
                    {
                        new TestEntityBatchJob
                        {
                            EcsTestDataRW = m_Manager.GetComponentTypeHandle<EcsTestData>(false)
                        }.Run(basicQuery);
                    })
                    .CleanUp(() =>
                    {
                        if (!enableQueryChunkCache)
                        {
                            basicQuery.InvalidateCache();
                        }
                    })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void IJobEntityBatch_Performance_Executing_WithFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Executing(entityCount, archetypeCount, true, true);
        }
        [Test, Performance]
        public void IJobEntityBatch_Performance_Executing_WithoutFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Executing(entityCount, archetypeCount, false, true);
        }
        [Test, Performance]
        public void IJobEntityBatch_Performance_Executing_WithFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Executing(entityCount, archetypeCount, true, false);
        }
        [Test, Performance]
        public void IJobEntityBatch_Performance_Executing_WithoutFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatch_Performance_Executing(entityCount, archetypeCount, false, false);
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestEntityBatchWithIndexJob : IJobEntityBatchWithIndex
        {
            public ComponentTypeHandle<EcsTestData> EcsTestDataRW;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex, int indexOfFirstEntityInQuery)
            {
                var data = batchInChunk.GetNativeArray(EcsTestDataRW);
                data[0] = new EcsTestData {value = 10};
            }
        }

        public void IJobEntityBatchWithIndex_Performance_Scheduling(int entityCount, int archetypeCount,
            bool enableQueryFiltering, bool enableQueryChunkCache)
        {
            using (var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount,
                Allocator.TempJob, typeof(EcsTestData),
                typeof(EcsTestSharedComp)))
            using (var basicQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData),
                typeof(EcsTestSharedComp)))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], entityCount / archetypeCount);
                }
                if (enableQueryFiltering)
                    basicQuery.SetSharedComponentFilter(default(EcsTestSharedComp));
                var handle = default(JobHandle);
                Measure.Method(() =>
                    {
                        handle = new TestEntityBatchWithIndexJob
                        {
                            EcsTestDataRW = m_Manager.GetComponentTypeHandle<EcsTestData>(false)
                        }.ScheduleParallel(basicQuery, handle);
                    })
                    .CleanUp(() =>
                    {
                        handle.Complete();
                        if (!enableQueryChunkCache)
                        {
                            basicQuery.InvalidateCache();
                        }
                    })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Scheduling_WithFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Scheduling(entityCount, archetypeCount, true, true);
        }
        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Scheduling_WithoutFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Scheduling(entityCount, archetypeCount, false, true);
        }
        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Scheduling_WithFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Scheduling(entityCount, archetypeCount, true, false);
        }
        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Scheduling_WithoutFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Scheduling(entityCount, archetypeCount, false, false);
        }

        void IJobEntityBatchWithIndex_Performance_Executing(int entityCount, int archetypeCount,
            bool enableQueryFiltering, bool enableQueryChunkCache)
        {
            using (var archetypes = CreateUniqueArchetypes(m_Manager, archetypeCount,  Allocator.TempJob, typeof(EcsTestData), typeof(EcsTestSharedComp)))
            using (var basicQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestSharedComp)))
            {
                for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
                {
                    m_Manager.CreateEntity(archetypes[archetypeIndex], entityCount / archetypeCount);
                }
                if (enableQueryFiltering)
                    basicQuery.SetSharedComponentFilter(default(EcsTestSharedComp));

                Measure.Method(() =>
                    {
                        new TestEntityBatchWithIndexJob
                        {
                            EcsTestDataRW = m_Manager.GetComponentTypeHandle<EcsTestData>(false)
                        }.Run(basicQuery);
                    })
                    .CleanUp(() =>
                    {
                        if (!enableQueryChunkCache)
                        {
                            basicQuery.InvalidateCache();
                        }
                    })
                    .WarmupCount(1)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Executing_WithFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Executing(entityCount, archetypeCount, true, true);
        }
        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Executing_WithoutFilter_WithCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Executing(entityCount, archetypeCount, false, true);
        }
        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Executing_WithFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Executing(entityCount, archetypeCount, true, false);
        }
        [Test, Performance]
        public void IJobEntityBatchWithIndex_Performance_Executing_WithoutFilter_WithoutCache([Values(100, 10000, 5000000)] int entityCount,
            [Values(10, 100)] int archetypeCount)
        {
            IJobEntityBatchWithIndex_Performance_Executing(entityCount, archetypeCount, false, false);
        }
    }
}
