using NUnit.Framework;

namespace Unity.Entities.CodeGen.Tests.SourceGenerationTests
{
    public class LambdaJobsInJobComponentSystem : LambdaJobsSourceGenerationIntegrationTest
    {
        readonly string _testSource = $@"
using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities.Tests;
using Unity.Entities.CodeGen.Tests;

partial class LambdaJobsInJobComponentSystem : JobComponentSystem
{{
    EntityQuery m_Query;

    protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
    {{
        var innerCapturedFloats = new NativeArray<float>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        innerCapturedFloats[0] = 456;
        byte* innerRawPtr = (byte*)IntPtr.Zero;
        float innerScopeFloat = 2.0f;

        Job
               .WithCode(
                    () =>
                    {{
                        innerCapturedFloats[0] = 3;
                    }}).Run();
        Entities
                .ForEach(
                    (int entityInQueryIndex,
                        Entity myEntity,
                        ref Translation translation, in Acceleration acceleration, in DynamicBuffer<MyBufferFloat> myBufferFloat) =>
                    {{
                    }}).Run();

        var newDependency = Job
               .WithCode(
                    () =>
                    {{
                        innerCapturedFloats[0] = 5;
                    }}).Schedule(inputDeps);

        return Entities
                .WithBurst(FloatMode.Deterministic, FloatPrecision.High, true)
                .WithEntityQueryOptions(EntityQueryOptions.IncludeDisabled)
                .WithChangeFilter<Translation>()
                .WithNone<Boid>()
                .WithAll<Velocity>()
                .WithReadOnly(innerCapturedFloats)
                .WithDisposeOnCompletion(innerCapturedFloats)
                .WithNativeDisableContainerSafetyRestriction(innerCapturedFloats)
                .WithNativeDisableUnsafePtrRestriction(innerRawPtr)
                .WithStoreEntityQueryInField(ref m_Query)
                .ForEach(
                    (int entityInQueryIndex,
                        Entity myEntity,
                        DynamicBuffer<MyBufferInt> myBufferInts,
                        ref Translation translation, in Acceleration acceleration, in DynamicBuffer<MyBufferFloat> myBufferFloat) =>
                    {{
                        EcsTestData LocalMethodThatReturnsValue()
                        {{
                            return default;
                        }}

                        LocalMethodThatReturnsValue();
                        translation.Value += (innerCapturedFloats[2] + acceleration.Value + entityInQueryIndex + myEntity.Version + myBufferInts[2].Value + innerScopeFloat + myBufferFloat[0].Value);
                        Console.Write(innerRawPtr->ToString());
                    }})
                .Schedule(newDependency);
        }}
}}";

        [Test]
        public void LambdaJobsInJobComponentSystemTest()
        {
            RunTest(_testSource, new GeneratedType {Name = "LambdaJobsInJobComponentSystem"});
        }
    }
}
