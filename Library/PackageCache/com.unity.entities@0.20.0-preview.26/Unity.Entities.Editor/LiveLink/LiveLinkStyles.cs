using UnityEngine;

namespace Unity.Entities.Editor
{
    static class LiveLinkStyles
    {
#if UNITY_2021_1_OR_NEWER
        public static readonly GUIStyle Dropdown = new GUIStyle("Dropdown") { fixedHeight = 20 };
#else
        public static readonly GUIStyle Dropdown = new GUIStyle("Dropdown")
        {
            fixedWidth = 40,
            margin = { top = 4, bottom = 4, left = 0, right = 0 }
        };
#endif
        public static readonly GUIStyle CommandLeft = "AppCommandLeft";
        public static readonly GUIStyle CommandLeftOn = "AppCommandLeftOn";
        public static readonly GUIStyle CommandMid = "AppCommandMid";
        public static readonly GUIStyle CommandRight = "AppCommandRight";
    }
}
