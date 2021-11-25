using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Entities.Editor
{
    class QueryWithEntitiesViewData
    {
        const int k_MaxEntityDisplayCount = 5;

        public readonly World World;
        public readonly string SystemName;
        public readonly EntityQuery Query;
        public readonly int QueryOrder;

        int m_LastVersion;

        public QueryWithEntitiesViewData(World world, EntityQuery query, string systemName = null, int queryOrder = 0)
        {
            World = world;
            SystemName = systemName;
            Query = query;
            this.QueryOrder = queryOrder;
        }

        public int TotalEntityCount { get; private set; }
        public List<EntityViewData> Entities { get; } = new List<EntityViewData>();

        public bool Update()
        {
            if (!World.IsCreated)
            {
                var count = Entities.Count;
                TotalEntityCount = 0;
                Entities.Clear();
                return count != 0;
            }

            if (!World.EntityManager.IsQueryValid(Query))
                return false;

            var query = Query;
            var currentVersion = query.GetCombinedComponentOrderVersion();
            if (m_LastVersion == currentVersion)
                return false;

            m_LastVersion = currentVersion;
            Entities.Clear();

            using var entities = query.ToEntityArray(Allocator.Temp);
            TotalEntityCount = entities.Length;
            for (var i = 0; i < Math.Min(entities.Length, k_MaxEntityDisplayCount); i++)
            {
                Entities.Add(new EntityViewData(World, entities[i]));
            }

            return true;
        }
    }
}
