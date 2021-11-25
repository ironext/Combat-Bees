using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class TabContent : VisualElement, ITabContent
    {
        static readonly string s_UssClassName = "tab-element";

        public string TabName { get; set; }

        public virtual void OnTabVisibilityChanged(bool isVisible) { }

        public TabContent()
        {
            AddToClassList(s_UssClassName);
        }
    }
}
