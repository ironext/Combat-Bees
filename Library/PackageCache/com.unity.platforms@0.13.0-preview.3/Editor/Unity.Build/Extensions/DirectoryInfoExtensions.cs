using System.IO;
using System.Linq;
using UnityEditor;

#if !UNITY_2021_2_OR_NEWER
using Unity.Build.Bridge;
#endif

namespace Unity.Build
{
    internal static class DirectoryInfoExtensions
    {
        const string k_Directory = "directory";

        [InitializeOnLoadMethod]
        static void Register()
        {
#if UNITY_2021_2_OR_NEWER
            EditorGUI.hyperLinkClicked += (window, args) =>
            {
                if (args.hyperLinkData.TryGetValue(k_Directory, out var outputFolder))
                {
                    EditorUtility.RevealInFinder(outputFolder);
                }
            };
#else
            EditorGUIBridge.HyperLinkClicked += (args) =>
            {
                if (args.TryGetValue(k_Directory, out var outputFolder))
                {
                    EditorUtility.RevealInFinder(outputFolder);
                }
            };
#endif
        }


        public static DirectoryInfo Combine(this DirectoryInfo directoryInfo, params string[] paths)
        {
            return new DirectoryInfo(Path.Combine(new[] { directoryInfo.FullName }.Concat(paths).ToArray()));
        }

        public static FileInfo GetFile(this DirectoryInfo directoryInfo, string fileName)
        {
            return new FileInfo(Path.Combine(directoryInfo.FullName, fileName));
        }

        public static FileInfo GetFile(this DirectoryInfo directoryInfo, FileInfo file)
        {
            return new FileInfo(Path.Combine(directoryInfo.FullName, file.Name));
        }

        public static void EnsureExists(this DirectoryInfo directoryInfo)
        {
            if (!Directory.Exists(directoryInfo.FullName))
            {
                directoryInfo.Create();
            }
        }

        public static void CopyTo(this DirectoryInfo directoryInfo, DirectoryInfo destination, bool recursive)
        {
            if (!Directory.Exists(directoryInfo.FullName))
            {
                throw new DirectoryNotFoundException($"Directory '{directoryInfo.FullName}' not found.");
            }

            destination.EnsureExists();

            // Copy files
            foreach (var file in directoryInfo.GetFiles())
            {
                file.CopyTo(Path.Combine(destination.FullName, file.Name), true);
            }

            // Copy subdirs
            if (recursive)
            {
                foreach (var subdir in directoryInfo.GetDirectories())
                {
                    CopyTo(subdir, destination.Combine(subdir.Name), recursive);
                }
            }
        }

        public static string GetRelativePath(this DirectoryInfo directoryInfo)
        {
            var path = directoryInfo.FullName.ToForwardSlash();
            var relativePath = new DirectoryInfo(".").FullName.ToForwardSlash();
            return path.StartsWith(relativePath) ? path.Substring(relativePath.Length).TrimStart('/') : path;
        }

        public static string GetRelativePath(this DirectoryInfo directoryInfo, DirectoryInfo relativeTo)
        {
            var path = directoryInfo.FullName;
            var relativePath = relativeTo.FullName;
            return path.StartsWith(relativePath) ? path.Remove(0, relativePath.Length).TrimStart('\\', '/') : path;
        }

        public static string ToHyperLink(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null || !directoryInfo.Exists)
            {
                return string.Empty;
            }

            return $"<a {k_Directory}=\"{directoryInfo.FullName}\">{directoryInfo.FullName}</a>";
        }
    }
}
