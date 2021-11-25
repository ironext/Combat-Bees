using Unity.Build.Classic;
using Unity.Build.Editor;
using UnityEditor;

namespace Unity.Build.Windows.Classic
{
    static class WindowsMenuItem
    {
        [MenuItem(BuildConfigurationMenuItem.k_MenuPathName + KnownPlatforms.Windows.DisplayName + ClassicBuildConfigurationMenuItem.k_ItemNameSuffix)]
        static void CreateClassicBuildConfigurationAsset()
        {
            ClassicBuildConfigurationMenuItem.CreateAssetInActiveDirectory(Platform.Windows);
        }
    }
}
