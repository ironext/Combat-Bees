#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Threading;

namespace Unity.Entities
{
    partial class EntitiesJournaling
    {
        struct SpinLock
        {
            int m_Locked;

            public bool Acquire()
            {
#if !NET_DOTS
                while (Interlocked.CompareExchange(ref m_Locked, 1, 0) != 0)
                    continue;
#endif
                return true;
            }

            public bool TryAcquire()
            {
#if !NET_DOTS
                return Interlocked.CompareExchange(ref m_Locked, 1, 0) == 0;
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
