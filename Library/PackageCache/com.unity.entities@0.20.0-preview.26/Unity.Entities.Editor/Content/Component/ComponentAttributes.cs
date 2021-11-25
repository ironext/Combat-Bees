using System;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Properties.Editor;
using Unity.Properties.Internal;
using Unity.Properties.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class ComponentAttributes : ITabContent
    {
        public string TabName { get; } = L10n.Tr("Attributes");
        public readonly Type ComponentType;

        public ComponentAttributes(Type componentType)
        {
            ComponentType = componentType;
        }
        
        public void OnTabVisibilityChanged(bool isVisible) { }
    }

    [UsedImplicitly]
    class ComponentAttributesInspector : Inspector<ComponentAttributes>
    {
        public override VisualElement Build()
        {
            var root = new VisualElement();

            var memberSection = new FoldoutWithoutActionButton {HeaderName = {text = L10n.Tr("Members")}};
            var propertyBag = PropertyBagStore.GetPropertyBag(Target.ComponentType);

            // TODO: @sean how do we avoid this ?
            var method = typeof(ComponentAttributesInspector)
                .GetMethod(nameof(Visit), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(Target.ComponentType);
            method.Invoke(this, new object[] { propertyBag, memberSection });

            var typeInfo = TypeManager.GetTypeInfo(TypeManager.GetTypeIndex(Target.ComponentType));
            var typeAttributesSection = new FoldoutWithoutActionButton() { HeaderName = {text = L10n.Tr("Type Attributes") }};
            typeAttributesSection.Add(new ComponentAttributeView("Namespace", Target.ComponentType.Namespace));

            var ecsAttributesSection = new FoldoutWithoutActionButton() { HeaderName = {text = L10n.Tr("ECS-Related Attributes") }};
            ecsAttributesSection.Add(new ComponentAttributeView("Type Index", typeInfo.TypeIndex.ToString()));
            ecsAttributesSection.Add(new ComponentAttributeView("Stable Type Hash", typeInfo.StableTypeHash.ToString()));
            ecsAttributesSection.Add(new ComponentAttributeView("Category", typeInfo.Category.ToString()));
            ecsAttributesSection.Add(new ComponentAttributeView("Size in chunk", typeInfo.SizeInChunk.ToString()));
            ecsAttributesSection.Add(new ComponentAttributeView("Type size", typeInfo.TypeSize.ToString()));
            ecsAttributesSection.Add(new ComponentAttributeView("Alignment", $"{typeInfo.AlignmentInBytes} B"));
            ecsAttributesSection.Add(new ComponentAttributeView("Alignment in chunk", $"{typeInfo.AlignmentInChunkInBytes} B"));

            root.Add(memberSection);
            root.Add(typeAttributesSection);
            root.Add(ecsAttributesSection);
            return root;
        }

        void Visit<TContainer>(IPropertyBag<TContainer> propertyBag, VisualElement root)
        {
            if (propertyBag is IPropertyList<TContainer> propertyList)
            {
                TContainer container = default;
                foreach (var property in propertyList.GetProperties(ref container))
                {
                    root.Add(new ComponentAttributeView(property.Name, TypeUtility.GetTypeDisplayName(property.DeclaredValueType())));
                }
            }
        }
    }

    class ComponentAttributeView : VisualElement
    {
        public ComponentAttributeView(string label, string value)
        {
            Resources.Templates.ContentProvider.ComponentAttribute.Clone(this);
            this.Q<Label>(className: UssClasses.ComponentAttribute.Name).text = label;
            this.Q<Label>(className: UssClasses.ComponentAttribute.Value).text = value;
        }
    }
}
