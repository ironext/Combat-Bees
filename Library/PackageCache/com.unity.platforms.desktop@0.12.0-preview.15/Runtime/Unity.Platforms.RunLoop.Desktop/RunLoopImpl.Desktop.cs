#if (UNITY_WINDOWS || UNITY_MACOSX || UNITY_LINUX)
using System.Runtime.InteropServices;
using Unity.Baselib.LowLevel;

namespace Unity.Platforms
{
    public class RunLoopImpl
    {
        public static void EnterMainLoop(RunLoop.RunLoopDelegate runLoopDelegate)
        {
            while (true)
            {
                double timestampInSeconds = Binding.Baselib_Timer_GetTimeSinceStartupInSeconds();

                if (runLoopDelegate(timestampInSeconds) == false)
                    break;
            }

            PlatformEvents.SendQuitEvent(null, new QuitEvent());
        }
    }
}
#endif