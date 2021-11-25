#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Unity.Entities
{
    partial class EntitiesJournaling
    {
        /// <summary>
        /// Data header written in buffer.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        readonly struct Header
        {
            public readonly ulong WorldSequenceNumber;
            public readonly SystemHandleUntyped ExecutingSystem;

            public Header(ulong worldSeqNumber, in SystemHandleUntyped executingSystem)
            {
                WorldSequenceNumber = worldSeqNumber;
                ExecutingSystem = executingSystem;
            }
        }
    }
}
#endif
