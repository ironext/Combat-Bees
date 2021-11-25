using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Unity.Build;
using Unity.Build.Internals;
using Unity.Build.DotsRuntime;
using Debug = UnityEngine.Debug;

namespace Unity.Build.Desktop.DotsRuntime
{
    public abstract class DesktopBuildTarget : BuildTarget
    {
        public enum TargetType
        {
            DotNet,
            DotNetStandard_2_0,
            Il2cpp,
        }

        public abstract TargetType Type { get; }
        public override bool UsesIL2CPP => Type == TargetType.Il2cpp;

        public abstract string PlatformDisplayName { get; }
        public virtual string PlatformBeeTargetName => PlatformDisplayName.ToLower();

        public override string DisplayName => $"{PlatformDisplayName} {TargetTypeDisplayName(Type)}";
        public override string BeeTargetName => $"{PlatformBeeTargetName}-{TargetTypeBeeTargetName(Type)}";

        // Create the right desktop build target (Windows, MacOS, Linux) depending on the TargetType
        public virtual BuildTarget CreateBuildTargetFromType(TargetType type = TargetType.DotNet)
        {
            return new UnknownBuildTarget();
        }

        public static string TargetTypeDisplayName(TargetType type)
        {
            switch (type)
            {
                case TargetType.DotNet:
                    return ".NET";
                case TargetType.DotNetStandard_2_0:
                    return ".NET Standard 2.0";
                case TargetType.Il2cpp:
                    return "Il2CPP";
                default:
                    throw new System.InvalidOperationException();
            }
        }

        public static string TargetTypeBeeTargetName(TargetType type)
        {
            switch (type)
            {
                case TargetType.DotNet:
                    return "dotnet";
                case TargetType.DotNetStandard_2_0:
                    return "dotnet-ns20";
                case TargetType.Il2cpp:
                    return "il2cpp";
                default:
                    throw new System.InvalidOperationException();
            }
        }
    }

    public class DesktopRun
    {
        // This will almost certainly be a single string built from an unhandled exception,
        // but the default handling will emit an event PER LINE. So we will concatenate the
        // lines to a single, usable, actual error message.
        private string StandardErrorString;

        public DesktopRun()
        {
        }

        private void WaitForProcess(Process process)
        {
            // This has to be called to start async. redirection of standard error
            process.BeginErrorReadLine();

            process.WaitForExit();

            if (!string.IsNullOrEmpty(StandardErrorString))
            {
                Debug.LogFormat(UnityEngine.LogType.Error, UnityEngine.LogOption.NoStacktrace, null, StandardErrorString);
            }
        }

        public bool RunOnThread(ProcessStartInfo startInfo, Type[] usedComponents)
        {

            startInfo.CreateNoWindow = false;

            for (int i = 0; i < usedComponents.Length; i++)
            {
                if (usedComponents[i].Name == "TinyRenderingSettings")
                    startInfo.CreateNoWindow = true;
            }

            // This should always remain false because the means of redirection
            // only reads one line at a time and there isn't any consistent and generic way
            // to detect the start AND end of a log.
            startInfo.RedirectStandardOutput = false;

            // This should be true because otherwise we won't catch unhandled exceptions.
            // However, the same issue exists as with RedirectStandardOutput, though
            // an unhandled exception is a crash anyway, so we can build the string from
            // and emit it finally when the process exits.
            startInfo.RedirectStandardError = true;

            // This should remain false in order to support RedirectStandardError.
            startInfo.UseShellExecute = false;

            // Add the editor GUID in the command line so Run will also try to establish player connection to the calling editor
            var editorConnectionType = Type.GetType($"UnityEditor.EditorConnectionInternal,UnityEditor");
            var methodGetLocalGuid = editorConnectionType?.GetMethod("GetLocalGuid");
            if (methodGetLocalGuid != null)
            {
                uint guid32 = (uint)methodGetLocalGuid.Invoke(null, null);
                startInfo.Arguments += $" -player-connection-guid={guid32}";
            }

            var process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null)
                {
                    if (!string.IsNullOrEmpty(StandardErrorString))
                        StandardErrorString += "\n";
                    StandardErrorString += args.Data;
                }
            };

            var success = process.Start();
            if (!success)
                return false;

            new Thread(() => { WaitForProcess(process); }).Start();

            return true;
        }

        internal static ShellProcessOutput RunTestMode(ShellProcessArguments shellArgs, string workingDirPath, int timeout)
        {
            shellArgs.WorkingDirectory = new DirectoryInfo(workingDirPath);
            shellArgs.ThrowOnError = false;

            // samples should be killed on timeout
            if (timeout > 0)
            {
                shellArgs.MaxIdleTimeInMilliseconds = timeout;
                shellArgs.MaxIdleKillIsAnError = false;
            }

            return ShellProcess.Run(shellArgs);
        }
    }
}
