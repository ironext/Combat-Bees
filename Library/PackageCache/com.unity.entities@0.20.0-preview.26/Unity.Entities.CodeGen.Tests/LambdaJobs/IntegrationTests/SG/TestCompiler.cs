using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExternalCSharpCompiler;
using Unity.CompilationPipeline.Common;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.Entities.CodeGen.Tests.SourceGenerationTests
{
    public static class TestCompiler
    {
        internal static string DirectoryForTestDll { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "IntegrationTestDlls");
        internal const string OutputDllName = "SourceGenerationIntegrationTests.dll";

        static ExternalCompiler Compile(AssemblyInfo assemblyInfo)
        {
            var compiler = new ExternalCompiler();

            // Handle special-cased defines (these can come through response files and won't get picked up for test compilation)
            var defines = new List<string>(assemblyInfo.Defines);

#if DOTS_EXPERIMENTAL
            void IncludeDefineIfNotPresent(string define)
            {
                if (defines.All(def => def != define))
                    defines.Add(define);
            }
            IncludeDefineIfNotPresent("DOTS_EXPERIMENTAL");
#endif

            assemblyInfo.Defines = defines.ToArray();

            compiler.BeginCompiling(assemblyInfo, new string[0], SystemInfo.operatingSystemFamily, new string[0]);
            return compiler;
        }

        static AssemblyInfo CreateAssemblyInfo(string cSharpCode, IEnumerable<string> referencedTypesDllPaths, bool allowUnsafe = false)
        {
            var scriptDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').Where(str => !string.IsNullOrEmpty(str)).ToArray();
            return new AssemblyInfo
            {
                Files = new[] { SaveFileAndReturnPath(cSharpCode) },
                References = ExternalCompiler.GetReferencedSystemDllFullPaths().Concat(referencedTypesDllPaths).ToArray(),
                OutputDirectory = DirectoryForTestDll,
                AllowUnsafeCode = allowUnsafe,
                Name = OutputDllName,
                Defines = scriptDefines
            };
        }

        public static void CleanUp()
        {
            Directory.Delete(DirectoryForTestDll, true);
        }

        public static (bool IsSuccess, CompilerMessage[] CompilerMessages) Compile(string cSharpCode, IEnumerable<Type> referencedTypes, bool allowUnsafe = false)
        {
            var assemblyInfo = CreateAssemblyInfo(cSharpCode, referencedTypes.Select(r => r.Assembly.Location), allowUnsafe);
            var compiler = Compile(assemblyInfo);

            while (!compiler.Poll())
            {
                Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
            }

            var compilerMessages = compiler.GetCompilerMessages();
            return (IsSuccess: !compilerMessages.Any(), CompilerMessages: compilerMessages);
        }

        static string SaveFileAndReturnPath(string cSharpCode)
        {
            Directory.CreateDirectory(DirectoryForTestDll);

            var randomFilePath = Path.Combine(DirectoryForTestDll, $"{Path.GetRandomFileName()}.cs");
            File.WriteAllText(randomFilePath, cSharpCode);

            return randomFilePath;
        }
    }
}
