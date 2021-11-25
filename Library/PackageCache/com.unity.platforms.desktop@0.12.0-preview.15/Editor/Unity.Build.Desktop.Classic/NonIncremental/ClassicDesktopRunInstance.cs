using System.Diagnostics;
using Unity.Build.Common;
using Unity.Build.Desktop;

namespace Unity.Build.Desktop.Classic
{
    public abstract class ClassicDesktopRunInstance : DesktopRunInstance
    {
        protected override ProcessStartInfo GetStartInfo(RunContext context)
        {
            var startInfo = base.GetStartInfo(context);

            if (m_RedirectOutput)
            {
                startInfo.Arguments += " -logfile -"; // Skip log file
            }

            return startInfo;
        }

        public ClassicDesktopRunInstance(RunContext context) : base(context)
        {
        }
    }
}
