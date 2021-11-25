using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor.Tests
{
    class QueryWithEntitiesViewTests
    {
        World m_World;
        EntityQuery m_Query;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_World = new World("QueryWithEntitiesTestWorld");
            var archetype = m_World.EntityManager.CreateArchetype(typeof(EntityGuid), typeof(EcsTestSharedComp));
            using var entities = m_World.EntityManager.CreateEntity(archetype, 2, Allocator.TempJob);
            for (var i = 0; i < entities.Length; i++)
            {
                m_World.EntityManager.SetSharedComponentData(entities[i], new EcsTestSharedComp{ value = i == 0 ? 123 : 345});
#if !DOTS_DISABLE_DEBUG_NAMES
                m_World.EntityManager.SetName(entities[i], $"QueryWithEntitiesView_Entity{i}");
#endif
            }

            m_Query = m_World.EntityManager.CreateEntityQuery(typeof(EntityGuid), typeof(EcsTestSharedComp));
            m_Query.SetSharedComponentFilter(new EcsTestSharedComp{value = 123});
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_World.Dispose();
        }

        [Test]
        public void QueryWithEntitiesView_GeneratesCorrectVisualHierarchy()
        {
            var data = new QueryWithEntitiesViewData(m_World, m_Query, "System A", 2);
            var el = new QueryWithEntitiesView(data);

            var headerTitleLabel = el.HeaderName;
            Assert.That(headerTitleLabel, Is.Not.Null);
            Assert.That(headerTitleLabel.text, Is.EqualTo("Query #2"));
            Assert.That(el.Q(className: UssClasses.QueryWithEntities.OpenQueryWindowButton), Is.Not.Null);
            Assert.That(el.Q(className: UssClasses.QueryWithEntities.SeeAllContainer), Is.Not.Null);
        }

        [Test]
        public void QueryWithEntitiesView_UpdatesCorrectly()
        {
            var data = new QueryWithEntitiesViewData(m_World, m_Query);
            var el = new QueryWithEntitiesView(data);
            el.Update();

            Assert.That(el.HeaderName.text, Is.EqualTo("Query #0"));
            Assert.That(el.Q(className: UssClasses.QueryWithEntities.SeeAllContainer).style.display.value, Is.EqualTo(DisplayStyle.None));

            var entityViews = el.Query<EntityView>().ToList();
            Assert.That(entityViews.Count, Is.EqualTo(1));
            var entityView = entityViews.FirstOrDefault();
            Assert.That(entityView, Is.Not.Null);
#if !DOTS_DISABLE_DEBUG_NAMES
            Assert.That(entityView.Q<Label>(className: UssClasses.EntityView.EntityName).text, Is.EqualTo("QueryWithEntitiesView_Entity0"));
#endif
        }
    }
}
