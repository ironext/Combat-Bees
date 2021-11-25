#if ENABLE_PLAYMODE_EXTENSION
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Build.Classic;
using Unity.Build.Common;
using UnityEditor;
using UnityEditor.TestRunner.TestLaunchers;
using UnityEditor.TestTools;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.TestTools.TestRunner.TestRun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestRunner.Utils;
using UnityEngine.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.Callbacks;


namespace Unity.Build.Playmode.TestRunner
{
    [Serializable]
    internal class BuildConfigurationPlayerLauncher : RuntimeTestLauncherBase
    {
        private readonly PlaymodeTestsControllerSettings m_Settings;
        private readonly TestJobData m_JobData;
        private readonly BuildTarget m_TargetPlatform;
        private readonly Platform m_BuildConfigurationPlatform;
        private ExecutionSettings ExecutionSettings => m_JobData.executionSettings;

        public BuildConfigurationPlayerLauncher(PlaymodeTestsControllerSettings settings, TestJobData jobData)
        {
            m_Settings = settings;
            m_JobData = jobData;
            m_TargetPlatform = ExecutionSettings.targetPlatform ?? EditorUserBuildSettings.activeBuildTarget;
            m_BuildConfigurationPlatform = m_TargetPlatform.GetPlatform() ?? throw new Exception($"Cannot resolve platform for {m_TargetPlatform}");
        }

        private static SceneList.SceneInfo GetSceneInfo(string path)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            // Note: Don't ever set AutoLoad to true, the tests are responsible for loading scenes
            return new SceneList.SceneInfo() { AutoLoad = false, Scene = GlobalObjectId.GetGlobalObjectIdSlow(sceneAsset) };
        }

        private string GetBuildConfiguratioName()
        {
            var name = m_BuildConfigurationPlatform.Name;
            return name;
        }

        // Borrowed from com.unity.test-framework@1.2.1-preview.4\UnityEditor.TestRunner\TestLaunchers\PlayerLauncher.cs
        private BuildPlayerOptions ModifyBuildOptions(BuildPlayerOptions buildOptions)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetReferencedAssemblies().Any(z => z.Name == "UnityEditor.TestRunner")).ToArray();
            var attributes = allAssemblies.SelectMany(assembly => assembly.GetCustomAttributes(typeof(TestPlayerBuildModifierAttribute), true).OfType<TestPlayerBuildModifierAttribute>()).ToArray();
            var modifiers = attributes.Select(attribute => attribute.ConstructModifier()).ToArray();

            foreach (var modifier in modifiers)
            {
                buildOptions = modifier.ModifyOptions(buildOptions);
            }

            return buildOptions;
        }

        /// <summary>
        /// Simulates com.unity.test-framework@1.2.1-preview.4\UnityEditor.TestRunner\TestLaunchers\PlayerLauncher.cs ModifyBuildOptions behavior
        /// Where running tests can be split into build and run separate operations.
        /// See com.unity.test-framework.build@0.0.1-preview.12\SplitBuildAndRun.cs for more details, depending on specific command line arguments, that file:
        /// - change target location path
        /// - change application's product name
        /// - determine if player needs to run
        /// </summary>
        private void SetupSplitBuildAndRun(BuildConfiguration config, out bool performPlayerLaunch)
        {
            performPlayerLaunch = true;

            var tempOptions = new BuildPlayerOptions()
            {
                locationPathName = Path.Combine("Temp", config.name + DateTime.Now.Ticks.ToString()),
                options = BuildOptions.AutoRunPlayer,
                target = m_TargetPlatform
            };
            tempOptions = ModifyBuildOptions(tempOptions);

            // If BuildOptions.AutoRunPlayer was not unset, that means user still intends to run player right after build 
            performPlayerLaunch = (tempOptions.options & BuildOptions.AutoRunPlayer) != 0;

            config.SetComponent<OutputBuildDirectory>(new OutputBuildDirectory()
            {
                OutputDirectory = tempOptions.locationPathName
            });

            config.SetComponent(new GeneralSettings()
            {
                CompanyName = PlayerSettings.companyName,
                ProductName = PlayerSettings.productName
            });
        }

        private BuildConfiguration CreateBuildConfiguration(string name, string firstScenePath, out bool performPlayerLaunch)
        {
            var config = BuildConfiguration.CreateInstance();

            config.name = name;
            
            var scenes = new List<string>() { firstScenePath };
            scenes.AddRange(EditorBuildSettings.scenes.Select(x => x.path));

            config.SetComponent(new SceneList
            {
                SceneInfos = new List<SceneList.SceneInfo>(scenes.Select(s => GetSceneInfo(s)))
            });

            var profile = new ClassicBuildProfile()
            {
                Configuration = BuildType.Develop,
                Platform = m_BuildConfigurationPlatform
            };
            
            config.SetComponent(profile);
            config.SetComponent(new PlaymodeTestRunnerComponent());

            SetupSplitBuildAndRun(config, out performPlayerLaunch);

            return config;
        }

        // A copy paste from com.unity.test-framework@1.2.1-preview.4\UnityEditor.TestRunner\TestLaunchers\PlayerLauncher.cs PrepareScene
        // Would be nice to share one day
        private Scene PrepareScene(Scene scene, string scenePath)
        {
            var runner = GameObject.Find(PlaymodeTestsController.kPlaymodeTestControllerName).GetComponent<PlaymodeTestsController>();
            runner.AddEventHandlerMonoBehaviour<PlayModeRunnerCallback>();
            runner.settings = m_Settings;
            var commandLineArgs = Environment.GetCommandLineArgs();
            if (!commandLineArgs.Contains("-doNotReportTestResultsBackToEditor"))
            {
                runner.AddEventHandlerMonoBehaviour<RemoteTestResultSender>();
            }
            runner.AddEventHandlerMonoBehaviour<PlayerQuitHandler>();
            runner.includedObjects = new ScriptableObject[]
                {ScriptableObject.CreateInstance<RuntimeTestRunCallbackListener>()};
            SaveScene(scene, scenePath);
            return scene;
        }

        public virtual void Run()
        {
            var editorConnectionTestCollector = RemoteTestRunController.instance;
            editorConnectionTestCollector.hideFlags = HideFlags.HideAndDontSave;
            editorConnectionTestCollector.Init(m_TargetPlatform, ExecutionSettings.playerHeartbeatTimeout);

            var remotePlayerLogController = RemotePlayerLogController.instance;
            remotePlayerLogController.hideFlags = HideFlags.HideAndDontSave;

            using (var settings = new BuildConfigurationPlayerLauncherContextSettings(ExecutionSettings.overloadTestRunSettings))
            {
                PrepareScene(m_JobData.InitTestScene, m_JobData.InitTestScenePath);

                var filter = m_Settings.BuildNUnitFilter();
                var runner = LoadTests(filter);
                var exceptionThrown = ExecutePreBuildSetupMethods(runner.LoadedTest, filter);
                if (exceptionThrown)
                {
                    CallbacksDelegator.instance.RunFailed("Run Failed: One or more errors in a prebuild setup. See the editor log for details.");
                    return;
                }

                var name = GetBuildConfiguratioName();
                var path = $"Assets/{m_BuildConfigurationPlatform.Name}.buildConfiguration";
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Creating build configuration at path {path}");
                var config = CreateBuildConfiguration(name, m_JobData.InitTestScenePath, out var performPlayerLaunch);

                // In basic scenarios you can build without saving build configuration to disk
                // But dots related systems, require build configuration to be present on disk
                config.SerializeToPath(path);
                AssetDatabase.Refresh();

                var buildResult = config.Build();
                AssetDatabase.DeleteAsset(path);
                buildResult.LogResult();
                
                editorConnectionTestCollector.PostBuildAction();
                ExecutePostBuildCleanupMethods(runner.LoadedTest, filter);


                if (buildResult.Failed)
                {
                    ScriptableObject.DestroyImmediate(editorConnectionTestCollector);
                    Debug.LogError("Player build failed");
                    throw new TestLaunchFailedException("Player build failed");
                }

                editorConnectionTestCollector.PostSuccessfulBuildAction();

                if (!performPlayerLaunch)
                    return;

                var runResult = config.Run();
                runResult.LogResult();
                if (runResult.Failed)
                    throw new TestLaunchFailedException("Player run failed");

                editorConnectionTestCollector.PostSuccessfulLaunchAction();
            }
        }
    }
}
#endif
