using System.IO;
using Unity.Build.Common;
using Unity.Build.Classic;
using Unity.Build.Classic.Private;
using UnityEditor;

namespace Unity.Build.Windows.Classic
{
    sealed class WindowsClassicNonIncrementalPipeline : ClassicNonIncrementalPipelineBase
    {
        public override Platform Platform { get; } = Platform.Windows;

        public override BuildStepCollection BuildSteps { get; } = new[]
        {
            typeof(SaveScenesAndAssetsStep),
            typeof(ApplyUnitySettingsStep),
            typeof(BuildPlayerStep),
            typeof(CopyAdditionallyProvidedFilesStep),
            typeof(WindowsProduceArtifactStep)
        };

        protected override ResultBase OnCanRun(RunContext context)
        {
            var artifact = context.GetBuildArtifact<WindowsArtifact>();
            if (artifact == null)
            {
                return Result.Failure($"Could not retrieve build artifact '{nameof(WindowsArtifact)}'.");
            }

            if (artifact.OutputTargetFile == null)
            {
                return Result.Failure($"{nameof(WindowsArtifact.OutputTargetFile)} is null.");
            }

            if (!File.Exists(artifact.OutputTargetFile.FullName))
            {
                return Result.Failure($"Output target file '{artifact.OutputTargetFile.FullName}' not found.");
            }

            return Result.Success();
        }

        protected override RunResult OnRun(RunContext context)
        {
            return WindowsRunInstance.Create(context);
        }

        private string GetInstallInBuildFolder(BuildType buildType, ScriptingImplementation scriptingImplementation)
        {
            var buildTypeName = (buildType == BuildType.Debug || buildType == BuildType.Develop) ? "development" : "nondevelopment";
            var scriptingBackend = scriptingImplementation == ScriptingImplementation.IL2CPP ? "il2cpp" : "mono";

            var path = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.StandaloneWindows64, BuildOptions.None);
            return Path.Combine(path, "Variations", $"win64_{buildTypeName}_{scriptingBackend}");
        }

        protected override void PrepareContext(BuildContext context)
        {
            base.PrepareContext(context);
            var classicData = context.GetValue<ClassicSharedData>();
            if (context.HasComponent<InstallInBuildFolder>())
            {
                var targetFolder = GetInstallInBuildFolder(context.GetComponentOrDefault<ClassicBuildProfile>().Configuration, context.GetComponentOrDefault<ClassicScriptingSettings>().ScriptingBackend);
                classicData.StreamingAssetsDirectory = Path.Combine(targetFolder, "DataSource", "StreamingAssets");
            }
            else
            {
                classicData.StreamingAssetsDirectory = $"{context.GetOutputBuildDirectory()}/{context.GetComponentOrDefault<GeneralSettings>().ProductName}_Data/StreamingAssets";
            }
        }

        public override DirectoryInfo GetOutputBuildDirectory(BuildConfiguration config)
        {
            if (config.HasComponent<InstallInBuildFolder>())
            {
                return new DirectoryInfo(
                    GetInstallInBuildFolder(
                        config.GetComponentOrDefault<ClassicBuildProfile>().Configuration,
                        config.GetComponentOrDefault<ClassicScriptingSettings>().ScriptingBackend));
            }
            else
            {
                return base.GetOutputBuildDirectory(config);
            }
        }
    }
}
