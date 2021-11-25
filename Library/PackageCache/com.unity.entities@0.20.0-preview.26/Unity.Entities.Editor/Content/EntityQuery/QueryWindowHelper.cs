using System;
using Unity.Properties.UI;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Editor
{
    static class QueryWindowHelper
    {
        static EditorWindow s_LastQueryWindow;

        public static void OpenNewWindow(World world, EntityQuery query, string systemName, int queryOrder)
        {
            var windowName = L10n.Tr("Query");

            SelectionUtility.ShowInWindow(new EntityQueryContentProvider
            {
                World = world,
                Query = query,
                SystemName = systemName,
                QueryOrder = queryOrder
            }, new ContentWindowParameters
            {
                AddScrollView = false,
                ApplyInspectorStyling = false
            });

            var currentWindow = EditorWindow.GetWindow<EditorWindow>(windowName);
            currentWindow.titleContent = EditorGUIUtility.TrTextContent(windowName, EditorIcons.Query);

            if (s_LastQueryWindow)
            {
                var pos = s_LastQueryWindow.position;
                currentWindow.position = new Rect(pos.x + 50, pos.y  + 50, currentWindow.position.width, currentWindow.position.height);
            }
            s_LastQueryWindow = currentWindow;
        }
    }
}
