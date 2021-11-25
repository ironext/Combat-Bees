using NUnit.Framework;
using Unity.Build.Classic.Private;

namespace Unity.Build.Windows.Classic.Tests
{
    [TestFixture]
    class WindowsClassicBuildPipelineTests
    {
        [Test]
        public void BuildPipelineSelectorTests()
        {
            var selector = new BuildPipelineSelector();
            Assert.That(selector.SelectFor(Platform.Windows).GetType(), Is.EqualTo(typeof(WindowsClassicNonIncrementalPipeline)));
        }
    }
}
