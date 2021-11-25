#if DEVELOPMENT_BUILD || UNITY_EDITOR
using NUnit.Framework;
using System;
using System.Linq;
using Unity.Collections;
using static Unity.Entities.EntitiesJournaling;

namespace Unity.Entities.Tests
{
    [TestFixture]
    unsafe class EntitiesJournalingTests : ECSTestsFixture
    {
        static RecordView MakeRecord(RecordType recordType, World world, Entity entity, object data) =>
            new RecordView(0, recordType, 0, world.SequenceNumber, world.Unmanaged.ExecutingSystem, new[] { entity }, Array.Empty<ComponentType>(), data);

        static RecordView MakeRecord(RecordType recordType, World world, Entity entity, ComponentType type, object data) =>
            new RecordView(0, recordType, 0, world.SequenceNumber, world.Unmanaged.ExecutingSystem, new[] { entity }, new[] { type }, data);

        static RecordView MakeRecord(RecordType recordType, World world, Entity entity, ComponentType[] types, object data) =>
            new RecordView(0, recordType, 0, world.SequenceNumber, world.Unmanaged.ExecutingSystem, new[] { entity }, types, data);

        static RecordView MakeRecord(RecordType recordType, World world, NativeArray<Entity> entities, object data) =>
            new RecordView(0, recordType, 0, world.SequenceNumber, world.Unmanaged.ExecutingSystem, entities.ToArray(), Array.Empty<ComponentType>(), data);

        static RecordView MakeRecord(RecordType recordType, World world, NativeArray<Entity> entities, ComponentType type, object data) =>
            new RecordView(0, recordType, 0, world.SequenceNumber, world.Unmanaged.ExecutingSystem, entities.ToArray(), new[] { type }, data);

        static RecordView MakeRecord(RecordType recordType, World world, NativeArray<Entity> entities, ComponentType[] types, object data) =>
            new RecordView(0, recordType, 0, world.SequenceNumber, world.Unmanaged.ExecutingSystem, entities.ToArray(), types, data);

        static void CheckRecords(RecordView[] expectedRecords)
        {
            var actualRecords = Records.All;
            Assert.AreEqual(expectedRecords.Length, actualRecords.Length);
            for (var i = 0; i < expectedRecords.Length; ++i)
                AssertAreEquals(expectedRecords[i], actualRecords[i]);
        }

        static void AssertAreEquals(RecordView expected, RecordView actual)
        {
            Assert.AreEqual(expected.RecordType, actual.RecordType);
            Assert.AreEqual(expected.FrameIndex, actual.FrameIndex);
            Assert.AreEqual(expected.World, actual.World);
            Assert.AreEqual(expected.ExecutingSystem, actual.ExecutingSystem);
            Assert.IsTrue(expected.Entities.SequenceEqual(actual.Entities));
            Assert.IsTrue(expected.ComponentTypes.SequenceEqual(actual.ComponentTypes));
            Assert.IsTrue(Equals(expected.Data, actual.Data));
        }

        bool m_LastEnabled;
        int m_LastTotalMemoryMB;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_LastEnabled = Enabled;
            m_LastTotalMemoryMB = TotalMemoryMB;
            Shutdown();
            Enabled = true;
            TotalMemoryMB = 4;
            Initialize();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Shutdown();
            Enabled = m_LastEnabled;
            TotalMemoryMB = m_LastTotalMemoryMB;
            Initialize();
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Clear();
        }

        [TearDown]
        public override void TearDown()
        {
            Clear();
            base.TearDown();
        }

        [Test]
        public void CreateEntity()
        {
            var entity = m_Manager.CreateEntity();

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
            });
        }

        [Test]
        public void CreateEntity_WithArchetype()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype);

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
            });
        }

        [Test]
        public void CreateEntity_WithComponentTypes()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }, null),
            });
        }

        [Test]
        public void CreateEntity_WithArchetypeAndEntityArray()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            using (var entities = new NativeArray<Entity>(10, Allocator.Temp))
            {
                m_Manager.CreateEntity(archetype, entities);

                CheckRecords(new[]
                {
                    MakeRecord(RecordType.CreateEntity, World, entities, typeof(EcsTestData), null),
                });
            }
        }

        [Test]
        public void CreateEntity_WithArchetypeAndEntityCount()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            using (var entities = m_Manager.CreateEntity(archetype, 10, Allocator.Temp))
            {
                CheckRecords(new[]
                {
                    MakeRecord(RecordType.CreateEntity, World, entities, typeof(EcsTestData), null),
                });
            }
        }

        [Test]
        public void DestroyEntity()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(entity);

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.DestroyEntity, World, entity, null),
            });
        }

        [Test]
        public void DestroyEntity_WithQuery()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData)))
            {
                m_Manager.DestroyEntity(query);
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.DestroyEntity, World, entity, null),
            });
        }

        [Test]
        public void DestroyEntity_WithEntityArray()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            using (var entities = m_Manager.CreateEntity(archetype, 10, Allocator.Temp))
            {
                m_Manager.DestroyEntity(entities);

                CheckRecords(new[]
                {
                    MakeRecord(RecordType.CreateEntity, World, entities, typeof(EcsTestData), null),
                    MakeRecord(RecordType.DestroyEntity, World, entities, null),
                });
            }
        }

        [Test]
        public void AddComponent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, typeof(EcsTestData));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestData), null),
            });
        }

        [Test]
        public void AddComponents()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponents(entity, new ComponentTypes(new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }, null),
            });
        }

        [Test]
        public void AddComponent_WithQuery()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData)))
            {
                m_Manager.AddComponent(query, typeof(EcsTestData2));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestData2), null),
            });
        }

        [Test]
        public void AddComponent_WithEntityArray()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            using (var entities = m_Manager.CreateEntity(archetype, 10, Allocator.Temp))
            {
                m_Manager.AddComponent(entities, typeof(EcsTestData2));

                CheckRecords(new[]
                {
                    MakeRecord(RecordType.CreateEntity, World, entities, typeof(EcsTestData), null),
                    MakeRecord(RecordType.AddComponent, World, entities, typeof(EcsTestData2), null),
                });
            }
        }

        [Test]
        public void AddComponentData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestData(42));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.SetComponentData, World, entity, typeof(EcsTestData), new EcsTestData(42)),
            });
        }

        [Test]
        public void AddSharedComponent()
        {
            var entity = m_Manager.CreateEntity();
            using (var chunkArray = new NativeArray<ArchetypeChunk>(new[] { m_Manager.GetChunk(entity) }, Allocator.Temp))
            {
                m_Manager.AddSharedComponent(chunkArray, new EcsTestSharedComp(42));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.SetSharedComponentData, World, entity, typeof(EcsTestSharedComp), null),
            });
        }

        [Test]
        public void AddSharedComponentData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddSharedComponentData(entity, new EcsTestSharedComp(42));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.SetSharedComponentData, World, entity, typeof(EcsTestSharedComp), null),
            });
        }

        [Test]
        public void AddSharedComponentData_WithQuery()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData)))
            {
                m_Manager.AddSharedComponentData(query, new EcsTestSharedComp(42));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.SetSharedComponentData, World, entity, typeof(EcsTestSharedComp), null),
            });
        }

        [Test]
        public void EntityJournalErrorAdded_DoesNotExist_EntityManager()
        {
            var e = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(e);
            Assert.That(() => m_Manager.AddComponent<EcsTestData>(e),
                Throws.InvalidOperationException.With.Message.Contains("(" + e.Index + ":" + e.Version + ")"));
            Clear();

            var arch = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entities = m_Manager.CreateEntity(arch, 5, Allocator.TempJob);
            var destroyedEntity = entities[0];
            m_Manager.DestroyEntity(entities[0]);
            Assert.That(() => m_Manager.AddComponent<EcsTestData>(entities),
                Throws.ArgumentException.With.Message.Contains("(" + destroyedEntity.Index + ":" + destroyedEntity.Version + ")"));
            entities.Dispose();
        }

        [Test]
        public void EntityJournalErrorAdded_DoesNotHaveComponent_EntityManager()
        {
            var e = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.RemoveComponent<EcsTestData>(e);
            Assert.That(() => m_Manager.SetComponentData<EcsTestData>(e, new EcsTestData {value = 10}),
                Throws.ArgumentException.With.Message.Contains("(" + e.Index + ":" + e.Version + ")"));
        }

        [Test]
        public void EntityJournalErrorAdded_DoesNotExist_EntityCommandBuffer()
        {
            // NOTE: ECB playback being bursted does not add to the error message, so we are disabling it for the test
            var e = m_Manager.CreateEntity();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            ecb.m_Data->m_MainThreadChain.m_CanBurstPlayback = false;
            ecb.AddComponent<EcsTestData>(e);
            m_Manager.DestroyEntity(e);
            Assert.That(() => ecb.Playback(m_Manager),
                Throws.InvalidOperationException.With.Message.Contains("(" + e.Index + ":" + e.Version + ")"));
            ecb.Dispose();

            var arch = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entities = m_Manager.CreateEntity(arch, 5, Allocator.TempJob);
            var destroyedEntity = entities[0];

            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            ecb2.m_Data->m_MainThreadChain.m_CanBurstPlayback = false;
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData)))
            {
                ecb2.AddComponentForEntityQuery(query, typeof(EcsTestData));
            }

            m_Manager.DestroyEntity(entities[0]);
            Assert.That(() => ecb2.Playback(m_Manager),
                Throws.InvalidOperationException.With.Message.Contains("(" + destroyedEntity.Index + ":" + destroyedEntity.Version + ")"));
            entities.Dispose();
            ecb2.Dispose();
        }

        [Test]
        public void EntityJournalErrorAdded_DoesNotHaveComponent_EntityCommandBuffer()
        {
            // NOTE: ECB playback being bursted does not add to the error message, so we are disabling it for the test
            var e = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.RemoveComponent<EcsTestData>(e);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            ecb.m_Data->m_MainThreadChain.m_CanBurstPlayback = false;
            ecb.SetComponent(e, new EcsTestData {value = 10});
            Assert.That(() => ecb.Playback(m_Manager),
                Throws.ArgumentException.With.Message.Contains("(" + e.Index + ":" + e.Version + ")"));
            ecb.Dispose();

            var arch = m_Manager.CreateArchetype(typeof(EcsTestSharedComp));
            var entities = m_Manager.CreateEntity(arch, 5, Allocator.TempJob);
            var removedEntity = entities[0];

            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            ecb2.m_Data->m_MainThreadChain.m_CanBurstPlayback = false;
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp)))
            {
                ecb2.SetSharedComponentForEntityQuery(query, new EcsTestSharedComp {value = 10});
            }

            m_Manager.RemoveComponent<EcsTestSharedComp>(entities[0]);
            Assert.That(() => ecb2.Playback(m_Manager),
                Throws.ArgumentException.With.Message.Contains("(" + removedEntity.Index + ":" + removedEntity.Version + ")"));
            entities.Dispose();
            ecb2.Dispose();
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [Test]
        public void AddComponentObject()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentObject(entity, new EcsTestManagedComponent { value = "hi" });

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestManagedComponent), null),
                MakeRecord(RecordType.SetComponentObject, World, entity, typeof(EcsTestManagedComponent), null),
            });
        }
#endif

        [Test]
        public void AddChunkComponentData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddChunkComponentData<EcsTestData>(entity);

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, ComponentType.ChunkComponent<EcsTestData>(), null),
            });
        }

        [Test]
        public void AddChunkComponentData_WithQuery()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData)))
            {
                m_Manager.AddChunkComponentData(query, new EcsTestData2(42));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.AddComponent, World, entity, ComponentType.ChunkComponent<EcsTestData2>(), null),
                MakeRecord(RecordType.SetComponentData, World, entity, ComponentType.ChunkComponent<EcsTestData2>(), new EcsTestData2(42)),
            });
        }

        [Test]
        public void AddBuffer()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<EcsIntElement>(entity);

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsIntElement), null),
            });
        }

        [Test]
        public void RemoveComponent()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.RemoveComponent(entity, typeof(EcsTestData));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.RemoveComponent, World, entity, typeof(EcsTestData), null),
            });
        }

        [Test]
        public void RemoveComponents()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            m_Manager.RemoveComponent(entity, new ComponentTypes(new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }, null),
                MakeRecord(RecordType.RemoveComponent, World, entity, new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }, null),
            });
        }

        [Test]
        public void RemoveComponent_WithQuery()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData)))
            {
                m_Manager.RemoveComponent(query, typeof(EcsTestData));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.RemoveComponent, World, entity, typeof(EcsTestData), null),
            });
        }

        [Test]
        public void RemoveComponents_WithQuery()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestData2)))
            {
                m_Manager.RemoveComponent(query, new ComponentTypes(new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }, null),
                MakeRecord(RecordType.RemoveComponent, World, entity, new ComponentType[] { typeof(EcsTestData), typeof(EcsTestData2) }, null),
            });
        }

        [Test]
        public void RemoveComponent_WithEntityArray()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            using (var entities = m_Manager.CreateEntity(archetype, 10, Allocator.Temp))
            {
                m_Manager.RemoveComponent(entities, typeof(EcsTestData));

                CheckRecords(new[]
                {
                    MakeRecord(RecordType.CreateEntity, World, entities, typeof(EcsTestData), null),
                    MakeRecord(RecordType.RemoveComponent, World, entities, typeof(EcsTestData), null),
                });
            }
        }

        [Test]
        public void SetComponentData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestData), null),
                MakeRecord(RecordType.SetComponentData, World, entity, typeof(EcsTestData), new EcsTestData(42)),
            });
        }

        [Test]
        public void SetSharedComponentData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, typeof(EcsTestSharedComp));
            m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp(42));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.SetSharedComponentData, World, entity, typeof(EcsTestSharedComp), null),
            });
        }

        [Test]
        public void SetSharedComponentData_WithQuery()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, typeof(EcsTestSharedComp));
            using (var query = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp)))
            {
                m_Manager.SetSharedComponentData(query, new EcsTestSharedComp(42));
            }

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.RemoveComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.AddComponent, World, entity, typeof(EcsTestSharedComp), null),
                MakeRecord(RecordType.SetSharedComponentData, World, entity, typeof(EcsTestSharedComp), null),
            });
        }

        [Test]
        public void SetChunkComponentData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddChunkComponentData<EcsTestData>(entity);

            var chunk = m_Manager.GetChunk(entity);
            m_Manager.SetChunkComponentData(chunk, new EcsTestData(42));

            CheckRecords(new[]
            {
                MakeRecord(RecordType.CreateEntity, World, entity, null),
                MakeRecord(RecordType.AddComponent, World, entity, ComponentType.ChunkComponent<EcsTestData>(), null),
                MakeRecord(RecordType.SetComponentData, World, chunk.m_Chunk->metaChunkEntity, typeof(EcsTestData), new EcsTestData(42)),
            });
        }
    }
}
#endif
