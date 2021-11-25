using UnityEditor;
using UnityEngine;

namespace Unity.Editor.Bridge
{
    static class EditorWindowBridge
    {
        public static void ClearPersistentViewData(EditorWindow window) => window.ClearPersistentViewData();

        public static T[] GetEditorWindowInstances<T>() where T : EditorWindow => Resources.FindObjectsOfTypeAll<T>();
    }
}
