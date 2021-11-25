#if DOTS_EXPERIMENTAL
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Entities.Tests.ForEachCodegen
{
    [TestFixture]
    public partial class JobEntityCodeGenTests : ECSTestsFixture
    {
        const int EcsTestDataValue = 3;
        const int EcsTestData2Value = 4;
        const int EcsTestSharedCompValue = 5;
        const int DynamicBufferFirstItem = 18;
        const int DynamicBufferSecondItem = 19;

        [InternalBufferCapacity(8)]
        public struct TestBufferElement : IBufferElementData
        {
            public static implicit operator int(TestBufferElement e) { return e.Value; }
            public static implicit operator TestBufferElement(int e) { return new TestBufferElement { Value = e }; }
            public int Value;
        }

        MyTestSystem TestSystem;
        Entity TestEntity;

        [SetUp]
        public void SetUp()
        {
            TestSystem = World.GetOrCreateSystem<MyTestSystem>();

            var entityArchetype = m_Manager.CreateArchetype(
                ComponentType.ReadWrite<EcsTestData>(),
                ComponentType.ReadWrite<EcsTestData2>(),
                ComponentType.ReadWrite<EcsTestSharedComp>(),
                ComponentType.ReadWrite<TestBufferElement>(),
                ComponentType.ReadWrite<EcsTestTag>());

            TestEntity = m_Manager.CreateEntity(entityArchetype);
            m_Manager.SetComponentData(TestEntity, new EcsTestData { value = EcsTestDataValue });
            m_Manager.SetComponentData(TestEntity, new EcsTestData2 { value0 = EcsTestData2Value });

            var buffer = m_Manager.GetBuffer<TestBufferElement>(TestEntity);
            buffer.Add(new TestBufferElement {Value = DynamicBufferFirstItem});
            buffer.Add(new TestBufferElement {Value = DynamicBufferSecondItem});

            m_Manager.SetSharedComponentData(TestEntity, new EcsTestSharedComp { value = EcsTestSharedCompValue });
        }

        [Test]
        public void SimplestCase()
        {
            TestSystem.AddTwoComponents();
            Assert.AreEqual(EcsTestDataValue + EcsTestData2Value, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithConstant()
        {
            const int valueToAssign = 7;
            TestSystem.AssignUniformValue(valueToAssign);

            Assert.AreEqual(valueToAssign, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithTagComponent()
        {
            const int valueToAssign = 5;
            TestSystem.WithTagParam(valueToAssign);

            Assert.AreEqual(valueToAssign, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithAll()
        {
            const int valueToAssign = 5;
            TestSystem.WithAll_ExistingTag(valueToAssign);

            Assert.AreEqual(valueToAssign, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithAllSharedComponent()
        {
            const int incrementBy = 1;
            TestSystem.WithAllSharedComponentData_IncrementComponentValue(incrementBy);

            Assert.AreEqual(EcsTestDataValue + incrementBy, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithSharedComponentFilter()
        {
            const int incrementBy = 1;
            TestSystem.WithSharedComponentFilter(incrementBy);

            Assert.AreEqual(EcsTestDataValue + incrementBy, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithChangeFilter()
        {
            const int incrementBy = 1;
            TestSystem.WithChangeFilter(incrementBy);

            Assert.AreEqual(EcsTestDataValue + incrementBy, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithJobAndThenEntitiesForEach()
        {
            const int increment = 3;
            TestSystem.WithJobAndThenJobEntity(1, increment);

            Assert.AreEqual(EcsTestDataValue + increment, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void AddToDynamicBuffer()
        {
            const int newValue = 4;
            TestSystem.AddToDynamicBuffer(newValue);

            var buffer = m_Manager.GetBuffer<TestBufferElement>(TestEntity);

            Assert.AreEqual(3, buffer.Length);
            CollectionAssert.AreEqual(new[] {DynamicBufferFirstItem, DynamicBufferSecondItem, newValue}, buffer.Reinterpret<int>().AsNativeArray());
        }

        [Test]
        public void ModifyDynamicBuffer()
        {
            const int multiplier = 2;

            TestSystem.MultiplyAllDynamicBufferValues(multiplier);
            var buffer = m_Manager.GetBuffer<TestBufferElement>(TestEntity);

            CollectionAssert.AreEqual(new[] {DynamicBufferFirstItem * multiplier, DynamicBufferSecondItem * multiplier}, buffer.Reinterpret<int>().AsNativeArray());
        }

        [Test]
        public void IterateExistingDynamicBufferReadOnly()
        {
            TestSystem.SumAllDynamicBufferValues_AssignToComponent();
            Assert.AreEqual(DynamicBufferFirstItem + DynamicBufferSecondItem, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithNone()
        {
            TestSystem.WithNone();
            Assert.AreEqual(EcsTestDataValue, m_Manager.GetComponentData<EcsTestData>(TestEntity).value); // Nothing changed
        }

        [Test]
        public void WithAny_DoesntExecute_OnEntityWithoutThatComponent()
        {
            TestSystem.WithAny_DoesntExecute_OnEntityWithoutThatComponent();
            Assert.AreEqual(EcsTestDataValue, m_Manager.GetComponentData<EcsTestData>(TestEntity).value); // Nothing changed
        }

        public struct EntityInQueryValue : IComponentData { public int Value; }

        [Test]
        public void UseEntityInQueryIndex()
        {
            var entityArchetype =
                m_Manager.CreateArchetype(
                    ComponentType.ReadWrite<EntityInQueryValue>(),
                    ComponentType.ReadWrite<EcsTestSharedComp>());

            using (var entities = TestSystem.EntityManager.CreateEntity(entityArchetype, 10, Allocator.Temp))
            {
                int value = 0;
                foreach (var entity in entities)
                {
                    TestSystem.EntityManager.SetComponentData(entity, new EntityInQueryValue {Value = value});
                    TestSystem.EntityManager.SetSharedComponentData(entity, new EcsTestSharedComp {value = value});
                    value++;
                }
            }
            Assert.IsTrue(TestSystem.UseEntityInQueryIndex());
        }

        public struct MySharedComponentData : ISharedComponentData
        {
            public int Value;
        }

        [Test]
        public void MultipleCapturingEntitiesForEachInNestedUsingStatementsTest()
        {
            const int valueToAssign = 5;
            const int incrementBy = 5;

            TestSystem.MultipleForEachesInNestedUsingStatements_FirstAssign_ThenIncrement(valueToAssign, incrementBy);

            Assert.AreEqual(valueToAssign + incrementBy, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [Test]
        public void ManyManagedComponents()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent { value = "SomeString" });
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent2 { value2 = "SomeString2" });
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent3 { value3 = "SomeString3" });
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent4 { value4 = "SomeString4" });
            TestSystem.Many_ManagedComponents();
        }
#endif

        [Test]
        public void JobDebuggerSafetyThrowsInRun()
        {
            var jobHandle = TestSystem.ScheduleEcsTestData();
            Assert.Throws<InvalidOperationException>(() => { TestSystem.RunEcsTestData(); });
            jobHandle.Complete();
        }

        [Test]
        public void JobDebuggerSafetyThrowsInSchedule()
        {
            var jobHandle = TestSystem.ScheduleEcsTestData();
            Assert.Throws<InvalidOperationException>(() => { TestSystem.ScheduleEcsTestData(); });
            jobHandle.Complete();
        }

        [Test]
        public void RunWithFilterButNotQueryDoesNotThrow()
        {
            Assert.DoesNotThrow(TestSystem.RunWithFilterButNotQuery);
        }

        partial struct AddTwoComponentsJob : IJobEntity
        {
            public void Execute(ref EcsTestData e1, in EcsTestData2 e2)
            {
                e1.value += e2.value0;
            }
        }

        partial struct AssignUniformValueJob : IJobEntity
        {
            public int Value;
            public void Execute(ref EcsTestData e1, in EcsTestData2 e2)
            {
                e1.value = Value;
            }
        }

        partial struct AssignUniformValue_WithTagParamJob : IJobEntity
        {
            public int Value;
            public void Execute(ref EcsTestData e1, in EcsTestTag testTag)
            {
                e1.value = Value;
            }
        }

        partial struct IncrementComponentValueJob : IJobEntity
        {
            public int IncrementBy;
            public void Execute(ref EcsTestData e1)
            {
                e1.value += IncrementBy;
            }
        }

        partial struct AddItemToDynamicBuffer : IJobEntity
        {
            public int AddThisToBuffer;

            public void Execute(ref EcsTestData e1, DynamicBuffer<TestBufferElement> dynamicBuffer)
            {
                dynamicBuffer.Add(AddThisToBuffer);
            }
        }

        partial struct MultiplyAllDynamicBufferValuesJob : IJobEntity
        {
            public int Multiplier;
            public void Execute(ref EcsTestData e1, DynamicBuffer<TestBufferElement> dynamicBuffer)
            {
                for (int i = 0; i < dynamicBuffer.Length; ++i)
                {
                    dynamicBuffer[i] = dynamicBuffer[i].Value * Multiplier;
                }
            }
        }

        static int SumBufferElements(DynamicBuffer<TestBufferElement> buf)
        {
            int total = 0;
            for (int i = 0; i != buf.Length; i++)
            {
                total += buf[i].Value;
            }
            return total;
        }

        partial struct SumAllDynamicBufferValues : IJobEntity
        {
            public void Execute(ref EcsTestData e1, DynamicBuffer<TestBufferElement> dynamicBuffer)
            {
                e1.value = SumBufferElements(dynamicBuffer);
            }
        }

        partial struct CompareEntityQueryIndex : IJobEntity
        {
            public static bool Success = true;

            public void Execute([EntityInQueryIndex]int entityInQueryIndex, in EntityInQueryValue value)
            {
                if (entityInQueryIndex != value.Value)
                {
                    Success = false;
                }
            }
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public partial struct ManyManagedComponentsJob : IJobEntity
        {
            public static int Count;

            public void Execute(
                EcsTestManagedComponent t0,
                EcsTestManagedComponent2 t1,
                EcsTestManagedComponent3 t2,
                EcsTestManagedComponent4 t3)
            {
                Assert.AreEqual("SomeString", t0.value);
                Assert.AreEqual("SomeString2", t1.value2);
                Assert.AreEqual("SomeString3", t2.value3);
                Assert.AreEqual("SomeString4", t3.value4);

                Count++;
            }
        }
#endif

        partial class MyTestSystem : SystemBase
        {
            protected override void OnCreate()
            {
            }

            public void AddTwoComponents()
            {
                var job = new AddTwoComponentsJob();
                job.Schedule(Dependency).Complete();
            }

            public void AssignUniformValue(int valueToAssign)
            {
                var job = new AssignUniformValueJob { Value = valueToAssign };
                job.Schedule(Dependency).Complete();
            }

            public void WithTagParam(int valueToAssign)
            {
                var job = new AssignUniformValue_WithTagParamJob { Value = valueToAssign };
                job.Schedule(Dependency).Complete();
            }

            public void WithAll_ExistingTag(int valueToAssign)
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(EcsTestTag), typeof(EcsTestData), typeof(EcsTestData2)}
                });
                var job = new AssignUniformValueJob { Value = valueToAssign };
                job.Schedule(query, Dependency).Complete();
            }

            public void WithNone()
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(EcsTestData)},
                    None = new ComponentType[]{typeof(EcsTestData2)}
                });
                var job = new IncrementComponentValueJob { IncrementBy = 1 };
                job.Schedule(query, Dependency).Complete();
            }

            public void WithAny_DoesntExecute_OnEntityWithoutThatComponent()
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(EcsTestData)},
                    Any = new ComponentType[]{typeof(EcsTestData3)}
                });
                var job = new IncrementComponentValueJob { IncrementBy = 1 };
                job.Schedule(query, Dependency).Complete();
            }

            public void WithAllSharedComponentData_IncrementComponentValue(int incrementBy)
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(EcsTestData), typeof(EcsTestSharedComp)},
                });
                var job = new IncrementComponentValueJob { IncrementBy = incrementBy };
                job.Schedule(query, Dependency).Complete();
            }

            public void WithSharedComponentFilter(int incrementBy)
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(EcsTestData), typeof(EcsTestSharedComp)},
                });
                query.SetSharedComponentFilter(new EcsTestSharedComp { value = 5 });
                var job = new IncrementComponentValueJob { IncrementBy = incrementBy };
                job.Schedule(query, Dependency).Complete();
            }

            public void WithChangeFilter(int incrementBy)
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(EcsTestData)},
                });
                query.SetChangedVersionFilter(typeof(EcsTestData));
                var job = new IncrementComponentValueJob { IncrementBy = incrementBy };
                job.Run(query);

                AfterUpdateVersioning();
                BeforeUpdateVersioning();

                // Should not run
                query.SetChangedVersionFilter(typeof(EcsTestData));
                var job2 = new IncrementComponentValueJob { IncrementBy = incrementBy };
                job2.Run(query);
            }

            public void WithJobAndThenJobEntity(int initialIncrement, int overwrittenIncrement)
            {
                int incrementBy = initialIncrement;

                Job.WithCode(() => { incrementBy = overwrittenIncrement; }).Run();

                new IncrementComponentValueJob { IncrementBy = incrementBy }.Schedule(Dependency).Complete();
            }

            public void AddToDynamicBuffer(int value)
            {
                var buffer = new AddItemToDynamicBuffer { AddThisToBuffer = value };
                buffer.Schedule(Dependency).Complete();
            }

            public void MultiplyAllDynamicBufferValues(int multiplier)
            {
                var job = new MultiplyAllDynamicBufferValuesJob { Multiplier = multiplier };
                job.Schedule(Dependency).Complete();
            }

            public void SumAllDynamicBufferValues_AssignToComponent()
            {
                var job = new SumAllDynamicBufferValues();
                job.Schedule(Dependency).Complete();
            }

            public bool UseEntityInQueryIndex()
            {
                var job = new CompareEntityQueryIndex();
                job.Run();
                return CompareEntityQueryIndex.Success;
            }

            public JobHandle ScheduleEcsTestData()
            {
                var job = new IncrementComponentValueJob{IncrementBy = 1};
                var handle = job.Schedule(default);
                return handle;
            }

            public void RunEcsTestData()
            {
                var job = new IncrementComponentValueJob{IncrementBy = 1};
                job.Run();
            }

            public void MultipleForEachesInNestedUsingStatements_FirstAssign_ThenIncrement(int valueToAssign, int incrementBy)
            {
                JobHandle jobHandle = default;
                using (var refStartPos = new NativeArray<int>(10, Allocator.TempJob))
                {
                    using (var refEndPos = new NativeArray<int>(10, Allocator.TempJob))
                    {
                        var job = new AssignUniformValueJob { Value = refEndPos[0] + refStartPos[0] + valueToAssign };
                        jobHandle = job.Schedule(jobHandle);

                        var valueJob = new IncrementComponentValueJob { IncrementBy = incrementBy };
                        jobHandle = valueJob.Schedule(jobHandle);

                        jobHandle.Complete();
                    }
                }
            }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
            public void Many_ManagedComponents()
            {
                var job = new ManyManagedComponentsJob();
                job.Run();
                Assert.AreEqual(1, ManyManagedComponentsJob.Count);
            }
#endif

            partial struct FindEcsTestDataJob : IJobEntity
            {
                public void Execute(ref EcsTestData _)
                {
                }
            }

            partial struct FindEntityJob : IJobEntity
            {
                public void Execute(Entity _)
                {
                }
            }

            // Not invoked, only used to store query in field with WithStoreEntityQueryInField
            public void ResolveDuplicateFieldsInEntityQuery()
            {
                var job = new FindEcsTestDataJob();
                job.Run();
            }

            public void RunWithFilterButNotQuery()
            {
                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[]{typeof(Translation)},
                });
                query.SetChangedVersionFilter(typeof(Translation));
                var job = new FindEntityJob();
                job.Run(query);
            }

            protected override void OnUpdate() { }
        }
    }
}
#endif
