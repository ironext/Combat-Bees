using System;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class EntityView : VisualElement
    {
        public EntityView(EntityViewData data)
        {
            Resources.Templates.EntityView.Clone(this);

            this.Q<Label>(className: UssClasses.EntityView.EntityName).text = data.EntityName;
            this.Q<VisualElement>(className: UssClasses.EntityView.GoTo)
                .RegisterCallback<MouseDownEvent, EntityView>((_, @this) => EntitySelectionProxy.SelectEntity(data.World, data.Entity), this);
        }
    }
}
