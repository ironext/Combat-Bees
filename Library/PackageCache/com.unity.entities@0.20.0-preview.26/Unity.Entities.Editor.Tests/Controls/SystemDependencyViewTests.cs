using NUnit.Framework;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor.Tests
{
    class SystemDependencyViewTests : ControlsTestFixture
    {
        [Test]
        public void SystemDependencyView_GeneratesCorrectVisualHierarchy()
        {
            var data = new SystemDependencyViewData(new SystemProxy(m_SystemA, m_WorldProxy), "System A", "Before");
            var el = new SystemDependencyView(data);

            Assert.That(el.Q<Label>(className: UssClasses.SystemDependencyView.Name).text, Is.EqualTo("System A"));
            Assert.That(el.Q<Label>(className: UssClasses.SystemDependencyView.ContentElement).text, Is.EqualTo("Before"));
        }

        [Test]
        public void SystemDependencyView_UpdatesCorrectly()
        {
            var el = new SystemDependencyView(new SystemDependencyViewData(new SystemProxy(m_SystemA, m_WorldProxy),"System A", "Before"));

            var name = el.Q<Label>(className: UssClasses.SystemDependencyView.Name);
            var content = el.Q<Label>(className: UssClasses.SystemDependencyView.ContentElement);
            Assert.That(name.text, Is.EqualTo("System A"));
            Assert.That(content.text, Is.EqualTo("Before"));

            el.Update(new SystemDependencyViewData(new SystemProxy(m_SystemB, m_WorldProxy),"System B", "After"));
            Assert.That(name.text, Is.EqualTo("System B"));
            Assert.That(content.text, Is.EqualTo("After"));
        }
    }
}
