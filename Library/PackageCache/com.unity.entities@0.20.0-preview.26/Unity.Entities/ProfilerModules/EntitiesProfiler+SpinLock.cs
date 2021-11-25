#if ENABLE_PROFILER
using System.Threading;

namespace Unity.Entities
{
    partial class EntitiesProfiler
    {
        struct SpinLock
        {
#if !NET_DOTS
            int m_Locked;
#endif

            public void Acquire()
            {
#if !NET_DOTS
                while (Interlocked.CompareExchange(ref m_Locked, 1, 0) != 0)
                    continue;
#endif
            }

            public void Release()
            {
#if !NET_DOTS
                Volatile.Write(ref m_Locked, 0);
#endif
            }
        }
    }
}
#endif
