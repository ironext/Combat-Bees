#if DEVELOPMENT_BUILD || UNITY_EDITOR
namespace Unity.Entities
{
    partial class EntitiesJournaling
    {
        /// <summary>
        /// Record type enumeration.
        /// </summary>
        public enum RecordType : int
        {
            WorldCreated,
            WorldDestroyed,
            SystemAdded,
            SystemRemoved,
            CreateEntity,
            DestroyEntity,
            AddComponent,
            RemoveComponent,
            SetComponentData,
            SetSharedComponentData,
            SetComponentObject,
            SetBuffer
        }
    }
}
#endif
