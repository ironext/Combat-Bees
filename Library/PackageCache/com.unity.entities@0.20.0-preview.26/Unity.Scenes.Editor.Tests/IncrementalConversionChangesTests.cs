using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Conversion;
using Unity.Entities.Hybrid.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Scenes.Editor.Tests
{
    [Serializable]
    [TestFixture]
    class IncrementalConversionChangesTests
    {
        [SerializeField] TestWithEditorLiveLink m_editorLiveLinkTest;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!m_editorLiveLinkTest.OneTimeSetUp())
                return;
            SceneManager.SetActiveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene));
            LiveConversionSettings.AdditionalConversionSystems.Clear();
            LiveConversionSettings.AdditionalConversionSystems.Add(typeof(IncrementalConversionTestSystem));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_editorLiveLinkTest.OneTimeTearDown();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            LiveConversionSettings.AdditionalConversionSystems.Clear();
        }

        [SetUp]
        public void SetUp() => m_editorLiveLinkTest.SetUp();

        [UnityTest]
        public IEnumerator IncrementalConversion_WithSingleObject_IncomingChangesAreCorrect()
        {
            var subScene = m_editorLiveLinkTest.CreateEmptySubScene("TestSubScene", true);
            SceneManager.SetActiveScene(subScene.EditingScene);

            var w = m_editorLiveLinkTest.GetLiveLinkWorldForEditMode();
            yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);
            {
                var go = new GameObject("TestGameObject");
                int goId = go.GetInstanceID();

                Undo.RegisterCreatedObjectUndo(go, "Test Create");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.AreEqual(new[] {go}, IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.AreEqual(new[] {goId}, IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);

                Undo.RegisterCompleteObjectUndo(go, "Test Change GameObject");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.AreEqual(new[] {go}, IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.AreEqual(new[] {goId}, IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);

                // Changing a component should not mark the game object itself as dirty.
                Undo.RegisterCompleteObjectUndo(go.transform, "Test Change Transform");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.AreEqual(new[] {go.transform}, IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);

                Undo.DestroyObjectImmediate(go);
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.AreEqual(new[] {goId}, IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);
            }
        }

        [UnityTest]
        public IEnumerator IncrementalConversion_WithObjectHierarchy_IncomingChangesContainChildren()
        {
            var subScene = m_editorLiveLinkTest.CreateEmptySubScene("TestSubScene", true);
            SceneManager.SetActiveScene(subScene.EditingScene);

            var w = m_editorLiveLinkTest.GetLiveLinkWorldForEditMode();
            yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);
            {
                var root = new GameObject("Root");
                int rootId = root.GetInstanceID();
                var child = new GameObject("Child");
                int childId = child.GetInstanceID();
                child.transform.SetParent(root.transform);

                Undo.RegisterCreatedObjectUndo(root, "Test Create");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.AreEquivalent(new[] {root, child}, IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.AreEquivalent(new[] {rootId, childId}, IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);

                Undo.DestroyObjectImmediate(root);
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.AreEquivalent(new[] {rootId, childId}, IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);
            }
        }

        [UnityTest]
        public IEnumerator IncrementalConversion_WithCreateThenDelete_IncomingChangesAreEmpty()
        {
            var subScene = m_editorLiveLinkTest.CreateEmptySubScene("TestSubScene", true);
            SceneManager.SetActiveScene(subScene.EditingScene);

            var w = m_editorLiveLinkTest.GetLiveLinkWorldForEditMode();
            yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);
            {
                var go = new GameObject();

                Undo.RegisterCreatedObjectUndo(go, "Test Create");
                Undo.IncrementCurrentGroup();
                Undo.DestroyObjectImmediate(go);
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);
            }
        }

        [UnityTest]
        public IEnumerator IncrementalConversion_WithDeleteThenCreate_IncomingChangesMarkGameObjectChanged()
        {
            var subScene = m_editorLiveLinkTest.CreateEmptySubScene("TestSubScene", true);
            SceneManager.SetActiveScene(subScene.EditingScene);
            var go = new GameObject("Go");
            var goId = go.GetInstanceID();

            var w = m_editorLiveLinkTest.GetLiveLinkWorldForEditMode();
            yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);
            {
                Undo.DestroyObjectImmediate(go);
                Undo.PerformUndo();
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.AreEqual(new[] {go}, IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.AreEqual(new[] {goId}, IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);
            }
        }

        [UnityTest]
        public IEnumerator IncrementalConversion_WithDuplicateChanges_IncomingChangesAreDeduplicated()
        {
            var subScene = m_editorLiveLinkTest.CreateEmptySubScene("TestSubScene", true);
            SceneManager.SetActiveScene(subScene.EditingScene);
            var go = new GameObject();
            var goId = go.GetInstanceID();

            var w = m_editorLiveLinkTest.GetLiveLinkWorldForEditMode();
            yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);
            {
                Undo.RegisterCompleteObjectUndo(go, "Test Change");
                Undo.RegisterCompleteObjectUndo(go, "Test Change");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.AreEqual(new[] {go}, IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.AreEqual(new[] {goId}, IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);

                Undo.RegisterCompleteObjectUndo(go.transform, "Test Component Change");
                Undo.RegisterCompleteObjectUndo(go.transform, "Test Component Change");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.AreEqual(new []{go.transform}, IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);
            }
        }

        [UnityTest]
        public IEnumerator IncrementalConversion_WithParentChange_IncomingChangesOnlyContainTransformChange()
        {
            var subScene = m_editorLiveLinkTest.CreateEmptySubScene("TestSubScene", true);
            SceneManager.SetActiveScene(subScene.EditingScene);
            var root = new GameObject("Root");
            var child = new GameObject("Child");

            var w = m_editorLiveLinkTest.GetLiveLinkWorldForEditMode();
            yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);
            {
                Undo.SetTransformParent(child.transform, root.transform, "Test Transform change");
                IncrementalConversionTestSystem.CaptureNext(subScene.SceneGUID);
                yield return m_editorLiveLinkTest.UpdateEditorAndWorld(w);

                CollectionAssert.AreEqual(new[] {child.transform}, IncrementalConversionTestSystem.ChangedComponents);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjects);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.ChangedGameObjectsInstanceIds);
                CollectionAssert.IsEmpty(IncrementalConversionTestSystem.DeletedGameObjectInstanceIds);
            }
        }

        [UpdateInGroup(typeof(ConversionSetupGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.GameObjectConversion)]
        [DisableAutoCreation]
        partial class IncrementalConversionTestSystem : SystemBase
        {
            public static readonly List<GameObject> ChangedGameObjects = new List<GameObject>();
            public static readonly List<int> ChangedGameObjectsInstanceIds = new List<int>();
            public static readonly List<Component> ChangedComponents = new List<Component>();
            public static readonly List<int> DeletedGameObjectInstanceIds = new List<int>();
            static Hash128 _sceneGuid;

            public static void CaptureNext(Hash128 sceneGuid)
            {
                ChangedComponents.Clear();
                ChangedGameObjects.Clear();
                ChangedGameObjectsInstanceIds.Clear();
                DeletedGameObjectInstanceIds.Clear();
                _sceneGuid = sceneGuid;
            }

            protected override void OnUpdate()
            {
                var ics = World.GetExistingSystem<IncrementalChangesSystem>();
                if (ics.SceneGUID != _sceneGuid)
                    return;
                _sceneGuid = default;
                ChangedGameObjects.AddRange(ics.IncomingChanges.ChangedGameObjects);
                Fill(ChangedGameObjectsInstanceIds, ics.IncomingChanges.ChangedGameObjectsInstanceIds);
                Fill(DeletedGameObjectInstanceIds, ics.IncomingChanges.RemovedGameObjectInstanceIds);
                ChangedComponents.AddRange(ics.IncomingChanges.ChangedComponents);

                void Fill(List<int> list, NativeArray<int>.ReadOnly data)
                {
                    for (int i = 0; i < data.Length; i++)
                        list.Add(data[i]);
                }
            }
        }
    }
}
