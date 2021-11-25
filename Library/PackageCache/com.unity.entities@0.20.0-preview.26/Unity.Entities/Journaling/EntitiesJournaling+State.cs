#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel.Unsafe;

namespace Unity.Entities
{
    partial class EntitiesJournaling
    {
        /// <summary>
        /// Journaling state data container.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct JournalingState : IDisposable
        {
            bool m_Enabled;
            int m_EntityTypeIndex;
            ulong m_RecordIndex;
            UnsafeCircularBuffer<Record> m_Records;
            UnsafeCircularBuffer<byte> m_Buffer;
            SpinLock m_Lock;

            internal bool Enabled
            {
                get => m_Enabled;
                set => m_Enabled = value;
            }

            internal JournalingState(bool enabled, int capacityInBytes)
            {
                m_Enabled = enabled;
                m_EntityTypeIndex = TypeManager.GetTypeIndex<Entity>();
                m_RecordIndex = 0;
                m_Records = new UnsafeCircularBuffer<Record>(capacityInBytes / 2 / sizeof(Record), Allocator.Persistent);
                m_Buffer = new UnsafeCircularBuffer<byte>(capacityInBytes / 2, Allocator.Persistent);
                m_Lock = new SpinLock();
            }

            public void Dispose()
            {
                m_Records.Dispose();
                m_Buffer.Dispose();
                m_RecordIndex = 0;
            }

            [NotBurstCompatible]
            internal IEnumerable<RecordView> GetRecords(bool blocking)
            {
                if (!Initialized)
                    yield break;

                var locked = blocking ? m_Lock.Acquire() : m_Lock.TryAcquire();
                try
                {
                    if (!locked)
                        throw new InvalidOperationException("Record buffer is currently locked for write.");

                    using (var buffer = m_Buffer.ToNativeArray(Allocator.Temp))
                    {
                        for (var i = 0; i < m_Records.Count; ++i)
                        {
                            var record = m_Records.ElementAt(i);
                            yield return GetRecordView(in record, in buffer);
                        }
                    }
                }
                finally
                {
                    if (locked)
                        m_Lock.Release();
                }
            }

            [NotBurstCompatible]
            RecordView GetRecordView(in Record record, in NativeArray<byte> buffer)
            {
                var recordOffset = record.Position - m_Buffer.FrontIndex;
                if (recordOffset < 0)
                    recordOffset = m_Buffer.Capacity + recordOffset;

                // Get header
                var bufferPtr = (byte*)buffer.GetUnsafeReadOnlyPtr();
                var headerOffset = bufferPtr + recordOffset;
                var header = UnsafeUtility.AsRef<Header>(headerOffset);

                // Get entities array
                var entitiesOffset = headerOffset + sizeof(Header);
                var entitiesLength = sizeof(Entity) * record.EntityCount;
                var entities = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>(entitiesOffset, record.EntityCount, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref entities, AtomicSafetyHandle.GetTempMemoryHandle());
#endif

                // Get type index array
                var typesOffset = entitiesOffset + entitiesLength;
                var typesLength = sizeof(int) * record.TypeCount;
                var types = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(typesOffset, record.TypeCount, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref types, AtomicSafetyHandle.GetTempMemoryHandle());
#endif

                // Get component data
                var dataOffset = typesOffset + typesLength;
                var dataLength = record.DataLength;
                var data = default(object);
                switch (record.RecordType)
                {
                    case RecordType.SetComponentData:
                    {
                        var type = TypeManager.GetType(types[0]);
                        if (type != null)
                        {
                            data = Activator.CreateInstance(type);
                            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                            var addr = handle.AddrOfPinnedObject();
                            UnsafeUtility.MemCpy(addr.ToPointer(), dataOffset, dataLength);
                            handle.Free();
                        }
                        break;
                    }
                    case RecordType.SetSharedComponentData:
                        //TODO
                        break;
                    case RecordType.SetComponentObject:
                        //TODO
                        break;
                    case RecordType.SystemAdded:
                    case RecordType.SystemRemoved:
                    {
                        data = new SystemView(UnsafeUtility.AsRef<SystemHandleUntyped>(dataOffset));
                        break;
                    }
                    default:
                        break;
                }

                return new RecordView(record.Index, record.RecordType, record.FrameIndex, header.WorldSequenceNumber, header.ExecutingSystem, entities.ToArray(), types.Select(ComponentType.FromTypeIndex).ToArray(), data);
            }

            internal void PushBack(RecordType recordType, in WorldUnmanaged world, Entity* entities, int entityCount, int* types, int typeCount, void* data, int dataLength)
            {
                if (!Initialized || !m_Enabled || m_Records.Capacity <= 0 || m_Buffer.Capacity <= 0)
                    return;

                if (entities == null && entityCount != 0)
                    entityCount = 0;
                if (types == null && typeCount != 0)
                    typeCount = 0;
                if (data == null && dataLength != 0)
                    dataLength = 0;

                // Skip Entity type index
                if (typeCount > 0 && types[0] == m_EntityTypeIndex)
                {
                    types++;
                    typeCount--;
                }

                m_Lock.Acquire();
                try
                {
                    if (!PushBackRecord(recordType, entityCount, typeCount, dataLength))
                        return;

                    PushBackHeader(in world);
                    PushBackEntities(entities, entityCount);
                    PushBackTypes(types, typeCount);
                    PushBackData(data, dataLength);
                }
                finally
                {
                    m_Lock.Release();
                }
            }

            internal void PushBack(RecordType recordType, in WorldUnmanaged world, ArchetypeChunk* chunks, int chunkCount, int* types, int typeCount, void* data, int dataLength)
            {
                if (!Initialized || !m_Enabled || m_Records.Capacity <= 0 || m_Buffer.Capacity <= 0)
                    return;

                if (chunks == null && chunkCount != 0)
                    chunkCount = 0;
                if (types == null && typeCount != 0)
                    typeCount = 0;
                if (data == null && dataLength != 0)
                    dataLength = 0;

                var entityCount = GetEntityCount(chunks, chunkCount);

                // Skip Entity type index
                if (typeCount > 0 && types[0] == m_EntityTypeIndex)
                {
                    types++;
                    typeCount--;
                }

                m_Lock.Acquire();
                try
                {
                    if (!PushBackRecord(recordType, entityCount, typeCount, dataLength))
                        return;

                    PushBackHeader(in world);
                    PushBackEntities(chunks, chunkCount);
                    PushBackTypes(types, typeCount);
                    PushBackData(data, dataLength);
                }
                finally
                {
                    m_Lock.Release();
                }
            }

            internal void Clear()
            {
                if (!Initialized)
                    return;

                try
                {
                    m_Lock.Acquire();
                    m_Records.Clear();
                    m_Buffer.Clear();
                    m_RecordIndex = 0;
                }
                finally
                {
                    m_Lock.Release();
                }
            }

            bool PushBackRecord(RecordType recordType, int entityCount, int typeCount, int dataLength)
            {
                // Verify payload size can fit in buffer
                var length = sizeof(Header) + (sizeof(Entity) * entityCount) + (sizeof(int) * typeCount) + dataLength;
                if (length > m_Buffer.Capacity)
                {
                    UnityEngine.Debug.LogError($"EntitiesJournaling: Cannot store {recordType} event, buffer not large enough. Increase entities journaling total memory in preferences.");
                    return false;
                }
                else if (length == m_Buffer.Capacity)
                {
                    m_Records.Clear();
                    m_Buffer.Clear();
                }

                // Verify we can fit the new record and its payload in buffer
                var record = new Record(m_Buffer.BackIndex, length, m_RecordIndex++, recordType, FrameCountSystem.FrameCount, entityCount, typeCount, dataLength);
                if (m_Records.IsFull || (m_Buffer.Capacity - m_Buffer.Count < record.Length))
                {
                    // Doesn't fit, remove old records
                    var recordCount = 0;
                    var bytesCount = 0;
                    for (var i = 0; i < m_Records.Count; ++i)
                    {
                        recordCount++;
                        bytesCount += m_Records[i].Length;
                        if (m_Buffer.Capacity - (m_Buffer.Count - bytesCount) >= record.Length)
                            break;
                    }

                    if (!m_Records.PopFront(recordCount))
                    {
                        UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to pop front {recordCount} records.");
                        return false;
                    }

                    if (!m_Buffer.PopFront(bytesCount))
                    {
                        UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to pop front {bytesCount} bytes in buffer.");
                        return false;
                    }
                }

                // Push back the new record (it should never fail)
                if (!m_Records.PushBack(record))
                    UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to push back record.");

                return true;
            }

            void PushBackHeader(in WorldUnmanaged world)
            {
                var header = new Header(world.SequenceNumber, world.ExecutingSystem);
                if (!m_Buffer.PushBack((byte*)&header, sizeof(Header)))
                    UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to push back header in buffer.");
            }

            void PushBackEntities(Entity* entities, int entityCount)
            {
                if (entities == null || entityCount <= 0)
                    return;

                if (!m_Buffer.PushBack((byte*)entities, sizeof(Entity) * entityCount))
                    UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to push back entities in buffer.");
            }

            void PushBackEntities(ArchetypeChunk* chunks, int chunkCount)
            {
                if (chunks == null || chunkCount <= 0)
                    return;

                for (var i = 0; i < chunkCount; ++i)
                {
                    var archetypeChunk = chunks[i];
                    var chunk = archetypeChunk.m_Chunk;
                    var archetype = chunk->Archetype;
                    var buffer = chunk->Buffer;
                    var length = chunk->Count;
                    var startOffset = archetype->Offsets[0] + archetypeChunk.m_BatchStartEntityIndex * archetype->SizeOfs[0];
                    if (!m_Buffer.PushBack(buffer + startOffset, sizeof(Entity) * length))
                        UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to push back chunk entities in buffer.");
                }
            }

            void PushBackTypes(int* types, int typeCount)
            {
                if (types == null || typeCount <= 0)
                    return;

                if (!m_Buffer.PushBack((byte*)types, sizeof(int) * typeCount))
                    UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to push back component types in buffer.");
            }

            void PushBackData(void* data, int dataLength)
            {
                if (data == null || dataLength <= 0)
                    return;

                if (!m_Buffer.PushBack((byte*)data, dataLength))
                    UnityEngine.Debug.LogError($"EntitiesJournaling: Failed to push back data in buffer.");
            }

            static int GetEntityCount(ArchetypeChunk* chunks, int chunkCount)
            {
                if (chunks == null || chunkCount <= 0)
                    return 0;

                var entityCount = 0;
                for (var i = 0; i < chunkCount; ++i)
                    entityCount += chunks[i].Count;

                return entityCount;
            }
        }
    }
}
#endif
