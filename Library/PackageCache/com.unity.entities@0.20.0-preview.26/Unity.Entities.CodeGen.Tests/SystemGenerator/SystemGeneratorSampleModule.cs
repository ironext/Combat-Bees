#if DOTS_EXPERIMENTAL

using NUnit.Framework;

namespace Unity.Entities.CodeGen.Tests.SourceGenerationTests
{
    public class SystemGeneratorSampleModuleTest : SystemGeneratorTests
    {
        private const string Code =
            @"
                using System;
                using Unity.Entities;
                using Unity.Mathematics;

                public unsafe partial class SampleSystemModuleTest : SystemBase
                {
                    public struct SampleTranslation : IComponentData
                    {
                        public float3 Value;
                    }

                    public static unsafe class SampleHelper
                    {
                        public static T* GetComponentDataPtrOfFirstChunk<T>() where T : unmanaged, IComponentData
                        {
                            throw new Exception(""Replaced with SourceGen"");
                        }
                    }

                    protected override void OnCreate()
                    {
                        var entity = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(entity, new SampleTranslation{Value = new float3(42)});
                    }

                    protected override void OnUpdate()
                    {
                        SampleTranslation* ptr = SampleHelper.GetComponentDataPtrOfFirstChunk<SampleTranslation>();
                        SampleTranslation translation = ptr[0];
                    }
                }
            ";

        [Test]
        public void SystemGenerator_SampleModuleTest()
        {
            RunTest(
                Code,
                new GeneratedType { Name = "SampleSystemModuleTest" });
        }
    }
}
#endif
