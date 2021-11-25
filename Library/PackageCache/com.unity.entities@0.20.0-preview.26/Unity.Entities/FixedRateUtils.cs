using System;
using System.ComponentModel;
using Unity.Core;
using Unity.Mathematics;

namespace Unity.Entities
{

    public interface IFixedRateManager
    {
        bool ShouldGroupUpdate(ComponentSystemGroup group);
        float Timestep { get; set; }
    }

    public static class FixedRateUtils
    {
        internal const float MinFixedDeltaTime = 0.0001f;
        internal const float MaxFixedDeltaTime = 10.0f;

        public class FixedRateSimpleManager : IFixedRateManager
        {
            float m_FixedTimestep;
            public float Timestep
            {
                get => m_FixedTimestep;
                set => m_FixedTimestep = math.clamp(value, MinFixedDeltaTime, MaxFixedDeltaTime);
            }
            double m_LastFixedUpdateTime;
            bool m_DidPushTime;

            public FixedRateSimpleManager(float fixedDeltaTime)
            {
                Timestep = fixedDeltaTime;
            }

            public bool ShouldGroupUpdate(ComponentSystemGroup group)
            {
                // if this is true, means we're being called a second or later time in a loop
                if (m_DidPushTime)
                {
                    group.World.PopTime();
                    m_DidPushTime = false;
                    return false;
                }

                group.World.PushTime(new TimeData(
                    elapsedTime: m_LastFixedUpdateTime,
                    deltaTime: m_FixedTimestep));

                m_LastFixedUpdateTime += m_FixedTimestep;

                m_DidPushTime = true;
                return true;
            }
        }

        public class FixedRateCatchUpManager : IFixedRateManager
        {
            // TODO: move this to World
            float m_MaximumDeltaTime;
            public float MaximumDeltaTime
            {
                get => m_MaximumDeltaTime;
                set => m_MaximumDeltaTime = math.max(value, m_FixedTimestep);
            }

            float m_FixedTimestep;
            public float Timestep
            {
                get => m_FixedTimestep;
                set
                {
                    m_FixedTimestep = math.clamp(value, MinFixedDeltaTime, MaxFixedDeltaTime);
                }
            }

            double m_LastFixedUpdateTime;
            long m_FixedUpdateCount;
            bool m_DidPushTime;
            double m_MaxFinalElapsedTime;

            public FixedRateCatchUpManager(float fixedDeltaTime)
            {
                Timestep = fixedDeltaTime;
            }

            public bool ShouldGroupUpdate(ComponentSystemGroup group)
            {
                float worldMaximumDeltaTime = group.World.MaximumDeltaTime;
                float maximumDeltaTime = math.max(worldMaximumDeltaTime, m_FixedTimestep);

                // if this is true, means we're being called a second or later time in a loop
                if (m_DidPushTime)
                {
                    group.World.PopTime();
                }
                else
                {
                    m_MaxFinalElapsedTime = m_LastFixedUpdateTime + maximumDeltaTime;
                }

                var finalElapsedTime = math.min(m_MaxFinalElapsedTime, group.World.Time.ElapsedTime);
                if (m_FixedUpdateCount == 0)
                {
                    // First update should always occur at t=0
                }
                else if (finalElapsedTime - m_LastFixedUpdateTime >= m_FixedTimestep)
                {
                    // Advance the timestep and update the system group
                    m_LastFixedUpdateTime += m_FixedTimestep;
                }
                else
                {
                    // No update is necessary at this time.
                    m_DidPushTime = false;
                    return false;
                }

                m_FixedUpdateCount++;

                group.World.PushTime(new TimeData(
                    elapsedTime: m_LastFixedUpdateTime,
                    deltaTime: m_FixedTimestep));

                m_DidPushTime = true;
                return true;
            }
        }
    }
}
