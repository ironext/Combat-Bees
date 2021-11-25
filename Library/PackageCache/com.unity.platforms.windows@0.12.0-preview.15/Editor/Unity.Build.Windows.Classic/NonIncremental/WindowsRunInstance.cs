using System.Diagnostics;
using Unity.Build.Desktop.Classic;

namespace Unity.Build.Windows.Classic
{
    sealed class WindowsRunInstance : ClassicDesktopRunInstance
    {
        protected override ProcessStartInfo GetStartInfo(RunContext context)
        {
            var startInfo = base.GetStartInfo(context);
            var artifact = context.GetBuildArtifact<WindowsArtifact>();

            startInfo.FileName = artifact.OutputTargetFile.FullName;
            startInfo.WorkingDirectory = artifact.OutputTargetFile.Directory?.FullName ?? string.Empty;
            return startInfo;
        }

        public WindowsRunInstance(RunContext context) : base(context)
        {
        }

        public static RunResult Create(RunContext context)
        {
            return new WindowsRunInstance(context).Start(context);
        }
    }
}
