using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Scenes.Editor
{
    internal static class SubSceneInspectorUtility
    {
        public static Transform GetUncleanHierarchyObject(SubScene[] subscenes)
        {
            foreach (var scene in subscenes)
            {
                var res = GetUncleanHierarchyObject(scene.transform);
                if (res != null)
                    return res;
            }

            return null;
        }

        public static Transform GetUncleanHierarchyObject(Transform child)
        {
            while (child)
            {
                if (child.localPosition != Vector3.zero)
                    return child;
                if (child.localRotation != Quaternion.identity)
                    return child;
                if (child.localScale != Vector3.one)
                    return child;

                child = child.parent;
            }

            return null;
        }

        public static bool HasChildren(SubScene[] scenes)
        {
            foreach (var scene in scenes)
            {
                if (scene.transform.childCount != 0)
                    return true;
            }

            return false;
        }

        public static void CloseSceneWithoutSaving(params SubScene[] scenes)
        {
            foreach (var scene in scenes)
                EditorSceneManager.CloseScene(scene.EditingScene, true);
        }

        public struct LoadableScene
        {
            public Entity Scene;
            public string Name;
            public SubScene SubScene;
            public int SectionIndex;
            public bool IsLoaded;
            public bool Section0IsLoaded;
            public int NumSubSceneSectionsLoaded;
        }

        static NativeArray<Entity> GetActiveWorldSections(World world, Hash128 sceneGUID)
        {
            if (world == null || !world.IsCreated) return default;

            var sceneSystem = world.GetExistingSystem<SceneSystem>();
            if (sceneSystem == null)
                return default;

            var entities = world.EntityManager;

            var sceneEntity = sceneSystem.GetSceneEntity(sceneGUID);

            if (!entities.HasComponent<ResolvedSectionEntity>(sceneEntity))
                return default;

            return entities.GetBuffer<ResolvedSectionEntity>(sceneEntity).Reinterpret<Entity>().AsNativeArray();
        }

        public static LoadableScene[] GetLoadableScenes(SubScene[] scenes)
        {
            var loadables = new List<LoadableScene>();
            DefaultWorldInitialization.DefaultLazyEditModeInitialize(); // workaround for occasional null World at this point
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            foreach (var scene in scenes)
            {
                bool section0IsLoaded = false;
                var numSections = 0;
                var numSectionsLoaded = 0;
                foreach (var section in GetActiveWorldSections(world, scene.SceneGUID))
                {
                    if (entityManager.HasComponent<SceneSectionData>(section))
                    {
                        var name = scene.SceneAsset != null ? scene.SceneAsset.name : "Missing Scene Asset";
                        var sectionIndex = entityManager.GetComponentData<SceneSectionData>(section).SubSectionIndex;
                        if (sectionIndex != 0)
                            name += $" Section: {sectionIndex}";

                        numSections += 1;
                        var isLoaded = entityManager.HasComponent<RequestSceneLoaded>(section);
                        if (isLoaded)
                            numSectionsLoaded += 1;
                        if (sectionIndex == 0)
                            section0IsLoaded = isLoaded;

                        loadables.Add(new LoadableScene
                        {
                            Scene = section,
                            Name = name,
                            SubScene = scene,
                            SectionIndex = sectionIndex,
                            IsLoaded = isLoaded,
                            Section0IsLoaded = section0IsLoaded,
                        });
                    }
                }

                // Go over all sections of this subscene and set the number of sections that are loaded.
                // This is needed to decide whether are able to unload section 0.
                for (int i = 0; i < numSections; i++)
                {
                    var idx = numSections - 1 - i;
                    var l = loadables[idx];
                    l.NumSubSceneSectionsLoaded = numSectionsLoaded;
                    loadables[idx] = l;
                }
            }

            return loadables.ToArray();
        }

        public static void ForceReimport(params SubScene[] scenes)
        {
            bool needRefresh = false;
            foreach (var world in World.All)
            {
                var sceneSystem = world.GetExistingSystem<SceneSystem>();
                if (sceneSystem != null)
                {
                    foreach (var scene in scenes)
                        needRefresh |= SceneWithBuildConfigurationGUIDs.Dirty(scene.SceneGUID, sceneSystem.BuildConfigurationGUID);
                }
            }
            if (needRefresh)
                AssetDatabase.Refresh();
        }

        public static bool CanEditScene(SubScene subScene)
        {
            if (!subScene.CanBeLoaded())
                return false;

            return !subScene.IsLoaded;
        }

        public static void EditScene(params SubScene[] scenes)
        {
            foreach (var subScene in scenes)
            {
                if (CanEditScene(subScene))
                {
                    Scene scene;
                    if (Application.isPlaying)
                        scene = EditorSceneManager.LoadSceneInPlayMode(subScene.EditableScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
                    else
                        scene = EditorSceneManager.OpenScene(subScene.EditableScenePath, OpenSceneMode.Additive);
                    scene.isSubScene = true;
                }
            }
        }

        public static void CloseAndAskSaveIfUserWantsTo(params SubScene[] subScenes)
        {
            if (!Application.isPlaying)
            {
                var dirtyScenes = new List<Scene>();
                foreach (var scene in subScenes)
                {
                    if (scene.EditingScene.isLoaded && scene.EditingScene.isDirty)
                    {
                        dirtyScenes.Add(scene.EditingScene);
                    }
                }

                if (dirtyScenes.Count != 0)
                {
                    if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(dirtyScenes.ToArray()))
                        return;
                }

                CloseSceneWithoutSaving(subScenes);
            }
            else
            {
                foreach (var scene in subScenes)
                {
                    if (scene.EditingScene.isLoaded)
                        EditorSceneManager.UnloadSceneAsync(scene.EditingScene);
                }
            }
        }

        public static void SaveScene(SubScene scene)
        {
            if (scene.EditingScene.isLoaded && scene.EditingScene.isDirty)
            {
                EditorSceneManager.SaveScene(scene.EditingScene);
            }
        }

        public static MinMaxAABB GetActiveWorldMinMax(World world, UnityEngine.Object[] targets)
        {
            MinMaxAABB bounds = MinMaxAABB.Empty;

            if (world == null)
                return bounds;

            var entities = world.EntityManager;
            foreach (SubScene subScene in targets)
            {
                foreach (var section in GetActiveWorldSections(World.DefaultGameObjectInjectionWorld, subScene.SceneGUID))
                {
                    if (entities.HasComponent<SceneBoundingVolume>(section))
                        bounds.Encapsulate(entities.GetComponentData<SceneBoundingVolume>(section).Value);
                }
            }

            return bounds;
        }

        // Visualize SubScene using bounding volume when it is selected.
        public static void DrawSubsceneBounds(SubScene scene)
        {
            var isEditing = scene.IsLoaded;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var entities = world.EntityManager;
            foreach (var section in GetActiveWorldSections(World.DefaultGameObjectInjectionWorld, scene.SceneGUID))
            {
                if (!entities.HasComponent<SceneBoundingVolume>(section))
                    continue;

                if (isEditing)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.gray;

                AABB aabb = entities.GetComponentData<SceneBoundingVolume>(section).Value;
                Gizmos.DrawWireCube(aabb.Center, aabb.Size);
            }
        }

        public static LiveConversionMode LiveConversionMode
        {
            get
            {
                if (EditorApplication.isPlaying || LiveConversionEnabled)
                    return LiveConversionSceneViewShowRuntime ? LiveConversionMode.SceneViewShowsRuntime : LiveConversionMode.SceneViewShowsAuthoring;
                else
                    return LiveConversionMode.Disabled;
            }
        }

        public static bool LiveConversionEnabled
        {
            get
            {
                // DEPRECATE 2021-04-25
                if (EditorPrefs.HasKey("Unity.Entities.Streaming.SubScene.LiveLinkEnabledInEditMode"))
                {
                    var oldValue = EditorPrefs.GetBool("Unity.Entities.Streaming.SubScene.LiveLinkEnabledInEditMode", false);
                    EditorPrefs.DeleteKey("Unity.Entities.Streaming.SubScene.LiveLinkEnabledInEditMode");
                    LiveConversionEnabled = oldValue;
                    return oldValue;
                }

                return EditorPrefs.GetBool("Unity.Entities.Streaming.SubScene.LiveConversionEnabled", false);
            }
            set
            {
                if (LiveConversionEnabled == value)
                    return;

                EditorPrefs.SetBool("Unity.Entities.Streaming.SubScene.LiveConversionEnabled", value);
                LiveLinkConnection.GlobalDirtyLiveLink();
                LiveLinkModeChanged();
            }
        }

        public static bool LiveConversionSceneViewShowRuntime
        {
            get
            {
                // DEPRECATE 2021-04-25
                if (EditorPrefs.HasKey("Unity.Entities.Streaming.SubScene.LiveLinkShowGameStateInSceneView"))
                {
                    var oldValue = EditorPrefs.GetBool("Unity.Entities.Streaming.SubScene.LiveLinkShowGameStateInSceneView", false);
                    EditorPrefs.DeleteKey("Unity.Entities.Streaming.SubScene.LiveLinkShowGameStateInSceneView");
                    LiveConversionEnabled = oldValue;
                    return oldValue;
                }

                return EditorPrefs.GetBool("Unity.Entities.Streaming.SubScene.LiveConversionSceneViewShowRuntime", false);
            }
            set
            {
                if (LiveConversionSceneViewShowRuntime == value)
                    return;

                EditorPrefs.SetBool("Unity.Entities.Streaming.SubScene.LiveConversionSceneViewShowRuntime", value);
                LiveLinkConnection.GlobalDirtyLiveLink();
                LiveLinkModeChanged();
            }
        }

        [Obsolete("LiveLinkModeChanged will no longer be public. Removed after 2021-04-25")]
        public static event Action LiveLinkModeChanged = delegate {};
    }
}
