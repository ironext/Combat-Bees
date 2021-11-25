
namespace Unity.Entities.Editor
{
    class EntityQueryContent
    {
        public World World { get; }
        public EntityQuery Query { get; }
        public string SystemName { get; }
        public int QueryOrder { get;  }

        public EntityQueryContent(World world, EntityQuery query, string systemName, int queryOrder)
        {
            World = world;
            Query = query;
            SystemName = systemName;
            QueryOrder = queryOrder;
        }
    }
}
