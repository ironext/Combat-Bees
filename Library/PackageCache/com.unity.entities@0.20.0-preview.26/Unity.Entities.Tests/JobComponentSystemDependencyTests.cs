using System;
#if !UNITY_DOTSRUNTIME  // IJobForEeach is deprecated
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

#pragma warning disable 649
#pragma warning disable 618

namespace Unity.Entities.Tests
{
    partial class JobComponentSystemDependencyTests : ECSTestsFixture
    {
        public partial class ReadSystem1 : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle input)
            {
                return Entities.ForEach((in EcsTestData _) => {}).Schedule(input);
            }
        }

        public partial class ReadSystem2 : JobComponentSystem
        {
            public bool returnWrongJob = false;
            public bool ignoreInputDeps = false;

            protected override JobHandle OnUpdate(JobHandle input)
            {
                JobHandle h;

                if (ignoreInputDeps)
                {
                    h = Entities.ForEach((in EcsTestData _) =>{}).Schedule(default);
                }
                else
                {
                    h = Entities.ForEach((in EcsTestData _) =>{}).Schedule(input);
                }

                return returnWrongJob ? input : h;
            }
        }

        public partial class ReadSystem3 : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle input)
            {
                return input;
            }
        }

        public partial class WriteSystem : JobComponentSystem
        {
            public bool SkipJob = false;

            protected override JobHandle OnUpdate(JobHandle input)
            {
                return !SkipJob ? Entities.ForEach((ref EcsTestData _) => { }).Schedule(input) : input;
            }
        }

        public partial class AlwaySynchronizeDependenciesSystem1 : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                return Entities.ForEach((ref EcsTestData _) => { }).Schedule(inputDeps);
            }
        }

        [AlwaysSynchronizeSystem]
        public partial class AlwaySynchronizeDependenciesSystem2 : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                if (!inputDeps.Equals(new JobHandle()))//after completing all jobs an empty jobhandle is returned.
                {
                    throw new Exception("InputDeps were not forced to completion earlier in frame.");
                }

                return Entities.ForEach((ref EcsTestData _) => { }).Schedule(inputDeps);
            }
        }

        [Test]
        public void ReturningWrongJobReportsCorrectSystemUpdate()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            ReadSystem2 a = World.GetOrCreateSystem<ReadSystem2>();
            WriteSystem b = World.GetOrCreateSystem<WriteSystem>();

            a.returnWrongJob = true;

			LogAssert.Expect(LogType.Error, "The system Unity.Entities.Tests.JobComponentSystemDependencyTests+ReadSystem2 reads Unity.Entities.Tests.EcsTestData via ReadSystem2:ReadSystem2_LambdaJob_1_Job but that type was not returned as a job dependency. To ensure correct behavior of other systems, the job or a dependency of it must be returned from the OnUpdate method.");

            a.Update();
            b.Update();
        }

        [Test]
        public void IgnoredInputDepsThrowsInCorrectSystemUpdate()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            WriteSystem ws1 = World.GetOrCreateSystem<WriteSystem>();
            ReadSystem2 rs2 = World.GetOrCreateSystem<ReadSystem2>();

            rs2.ignoreInputDeps = true;

            ws1.Update();
            Assert.Throws<InvalidOperationException>(() => { rs2.Update(); });
        }

        [Test]
        public void NotSchedulingWriteJobIsHarmless()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            WriteSystem ws1 = World.GetOrCreateSystem<WriteSystem>();

            ws1.Update();
            ws1.SkipJob = true;
            ws1.Update();
        }

        [Test]
        public void NotUsingDataIsHarmless()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            ReadSystem1 rs1 = World.GetOrCreateSystem<ReadSystem1>();
            ReadSystem3 rs3 = World.GetOrCreateSystem<ReadSystem3>();

            rs1.Update();
            rs3.Update();
        }

        [Test]
        public void ReadAfterWrite_JobForEachGroup_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            var ws = World.GetOrCreateSystem<WriteSystem>();
            var rs = World.GetOrCreateSystem<ReadSystem2>();

            ws.Update();
            rs.Update();
        }

        partial class UseEcsTestDataFromEntity : JobComponentSystem
        {
            public struct MutateEcsTestDataJob : IJob
            {
                public ComponentDataFromEntity<EcsTestData> data;

                public void Execute()
                {
                }
            }

            protected override JobHandle OnUpdate(JobHandle dep)
            {
                var job = new MutateEcsTestDataJob { data = GetComponentDataFromEntity<EcsTestData>() };
                return job.Schedule(dep);
            }
        }

        // The writer dependency on EcsTestData is not predeclared during
        // OnCreate, but we still expect the code to work correctly.
        // This should result in a sync point when adding the dependency for the first time.
        [Test]
        public void AddingDependencyTypeDuringOnUpdateSyncsDependency()
        {
            var systemA = World.CreateSystem<UseEcsTestDataFromEntity>();
            var systemB = World.CreateSystem<UseEcsTestDataFromEntity>();

            systemA.Update();
            systemB.Update();
        }

        class EmptyJobComponentSystem : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle dep)
            {
                return dep;
            }
        }

        partial class JobComponentSystemWithJobChunkJob : JobComponentSystem
        {
#if DOTS_EXPERIMENTAL
            public partial struct EmptyJob : IJobEntity
            {
                public ComponentTypeHandle<EcsTestData> TestDataTypeHandle;
                public void Execute()
                {
                }
            }
#else
            public struct EmptyJob : IJobChunk
            {
                public ComponentTypeHandle<EcsTestData> TestDataTypeHandle;
                public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
                {
                }
            }
#endif
            protected override JobHandle OnUpdate(JobHandle dep)
            {
                var query = EntityManager.UniversalQuery;

                var job = new EmptyJob
                {
                    TestDataTypeHandle = GetComponentTypeHandle<EcsTestData>()
                };
                var handle = job.Schedule(query, dep);
                return handle;
            }
        }

        [Test]
        public void EmptySystemAfterNonEmptySystemDoesntThrow()
        {
            m_Manager.CreateEntity(typeof(EcsTestData));

            var systemA = World.CreateSystem<JobComponentSystemWithJobChunkJob>();
            var systemB = World.CreateSystem<EmptyJobComponentSystem>();

            systemA.Update();
            systemB.Update();
        }

        [Test]
        public void AlwaysSynchronizeSystemForcesSynchronization()
        {
            m_Manager.CreateEntity(typeof(EcsTestData));

            var systemA = World.CreateSystem<AlwaySynchronizeDependenciesSystem1>();
            var systemB = World.CreateSystem<AlwaySynchronizeDependenciesSystem2>();

            systemA.Update();
            Assert.DoesNotThrow(() =>
            {
                systemB.Update();
            });
        }
    }
}
#endif
