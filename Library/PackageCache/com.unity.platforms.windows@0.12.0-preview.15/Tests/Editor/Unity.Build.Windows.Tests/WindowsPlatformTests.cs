using NUnit.Framework;
using Unity.Build.Editor;
using Unity.Serialization.Json;

namespace Unity.Build.Windows.Tests
{
    class WindowsPlatformTests
    {
        [Test]
        public void WindowsPlatform_Equals()
        {
            var platform = new WindowsPlatform();
            Assert.That(platform, Is.EqualTo(Platform.Windows));
        }

        [Test]
        public void WindowsPlatform_GetIcon()
        {
            Assert.That(Platform.Windows.GetIcon(), Is.Not.Null);
        }

        [Test]
        public void WindowsPlatform_Serialization()
        {
            var serialized = JsonSerialization.ToJson(Platform.Windows);
            var deserialized = JsonSerialization.FromJson<Platform>(serialized);
            Assert.That(deserialized, Is.EqualTo(Platform.Windows));
        }
    }
}
