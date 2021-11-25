using System;
using System.Diagnostics;
using System.IO;
using Unity.Build.Desktop.DotsRuntime;
using Unity.Build.DotsRuntime;
using Unity.Build.Internals;

namespace Unity.Build.Windows.DotsRuntime
{
    abstract class WindowsBuildTarget : DesktopBuildTarget
    {
        public override Platform Platform => Platform.Windows;
        public override string PlatformDisplayName => "Windows";
        public override bool CanBuild => UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor;
        public override string ExecutableExtension => ".exe";

        public override BuildTarget CreateBuildTargetFromType(TargetType type = TargetType.DotNet)
        {
            switch (type)
            {
                case TargetType.DotNet:
                    return new DotNetTinyWindowsBuildTarget();
                case TargetType.DotNetStandard_2_0:
                    return new DotNetStandard20WindowsBuildTarget();
                case TargetType.Il2cpp:
                    return new IL2CPPWindowsBuildTarget();
                default:
                    return new UnknownBuildTarget();
            }
        }

        public override bool Run(FileInfo buildTarget, Type[] usedComponents)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = buildTarget.FullName;
            startInfo.WorkingDirectory = buildTarget.Directory.FullName;
            return new DesktopRun().RunOnThread(startInfo, usedComponents);
        }

        internal override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, string[] args, int timeout)
        {
            var shellArgs = new ShellProcessArguments
            {
                Executable = Path.Combine(workingDirPath, $"{exeName}.exe"),
                Arguments = args,
            };

            return DesktopRun.RunTestMode(shellArgs, workingDirPath, timeout);
        }
    }

    sealed class DotNetTinyWindowsBuildTarget : WindowsBuildTarget
    {
#if UNITY_EDITOR_WIN
        protected override bool IsDefaultBuildTarget => true;
#endif

        public override TargetType Type => TargetType.DotNet;
        public override string DisplayName => base.DisplayName + " - Tiny";
        public override string DefaultAssetFileName => "Win";
        public override bool ShouldCreateBuildTargetByDefault => true;
    }

    sealed class DotNetStandard20WindowsBuildTarget : WindowsBuildTarget
    {
        public override TargetType Type => TargetType.DotNetStandard_2_0;

        public override bool Run(FileInfo buildTarget, Type[] usedComponents)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.Arguments = $"\"{buildTarget.FullName.Trim('\"')}\"";
            startInfo.FileName =  Path.Combine(buildTarget.Directory.FullName, "netcorerun.exe");
            startInfo.WorkingDirectory = buildTarget.Directory.FullName;
            return new DesktopRun().RunOnThread(startInfo, usedComponents);
        }

        internal override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, string[] args, int timeout)
        {
            var arguments = new string[1 + args.Length];
            arguments[0] = $"\"{workingDirPath}/{exeName}{ExecutableExtension}\"";
            Array.Copy(args, 0, arguments, 1, args.Length);

            var shellArgs = new ShellProcessArguments
            {
                Executable = Path.Combine(workingDirPath, "netcorerun.exe"),
                Arguments = arguments,
            };

            return DesktopRun.RunTestMode(shellArgs, workingDirPath, timeout);
        }
    }

    sealed class IL2CPPWindowsBuildTarget : WindowsBuildTarget
    {
        public override TargetType Type => TargetType.Il2cpp;
        public override string DisplayName => base.DisplayName + " - Tiny";
    }
}
