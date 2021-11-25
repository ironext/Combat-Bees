using NUnit.Framework;

namespace Unity.Entities.Editor.Tests
{
    class LiveLinkConfigHelperTests
    {
        [Test]
        public void EnsureLiveLinkConfigHelperIsProperlyInitialized()
        {
            Assert.That(LiveLinkConfigHelper.IsProperlyInitialized, Is.True);
        }
    }
}
