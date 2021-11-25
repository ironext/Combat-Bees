using System.Collections.Generic;
using System.IO;

namespace Unity.Build.Classic.Private
{
    sealed class CopyAdditionallyProvidedFilesStepBeforeBuild : BuildStepBase
    {
        class ProviderInfo
        {
            public List<string> Paths = new List<string>();
        }

        public override BuildResult Run(BuildContext context)
        {
            var info = new ProviderInfo();

            var classicSharedData = context.GetValue<ClassicSharedData>();

            var oldStreamingAssetsDirectory = classicSharedData.StreamingAssetsDirectory;
            classicSharedData.StreamingAssetsDirectory = "Assets/StreamingAssets";

            foreach (var customizer in classicSharedData.Customizers)
                customizer.OnBeforeRegisterAdditionalFilesToDeploy();

            foreach (var customizer in classicSharedData.Customizers)
            {
                customizer.RegisterAdditionalFilesToDeploy((from, to) =>
                {
                    var parent = Path.GetDirectoryName(to);
                    Directory.CreateDirectory(parent);
                    File.Copy(from, to, true);
                    info.Paths.Add(to);
                });
            }
            classicSharedData.StreamingAssetsDirectory = oldStreamingAssetsDirectory;
            context.SetValue(info);
            return context.Success();
        }

        public override BuildResult Cleanup(BuildContext context)
        {
            var info = context.GetValue<ProviderInfo>();
            foreach (var f in info.Paths)
            {
                if (!File.Exists(f))
                    continue;
                File.Delete(f);
            }
            return context.Success();
        }
    }
}
