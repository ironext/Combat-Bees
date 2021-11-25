#if DOTS_EXPERIMENTAL
using NUnit.Framework;

namespace Unity.Entities.CodeGen.Tests.SourceGenerationTests
{
    // NOT SUPPORTED UNTIL E.FE PORTED TO NEW SYSTEM GENERATOR
    // TODO: https://unity3d.atlassian.net/browse/DOTS-4716
    /*
    public class JobEntityAndLambdaJobsInSameOnUpdateMethod : JobEntitySourceGenerationTests
    {
        const string Code =
            @"using Unity.Entities;
            using Unity.Mathematics;
            using Unity.Transforms;
            using Unity.Jobs;

            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    public class MyFirstClass
                    {
                        public struct MyEntityJob : IJobEntity
                        {
                            public float MyDeltaTime;

                            public void Execute(ref Rotation rotation, in RotationSpeed_ForEach speed)
                            {
                                rotation.Value =
                                    math.mul(
                                        math.normalize(rotation.Value),
                                        quaternion.AxisAngle(math.up(), speed.RadiansPerSecond * MyDeltaTime));
                            }
                        }

                        public struct Rotation : IComponentData
                        {
	                        public quaternion Value;
                        }

                        public struct Translation : IComponentData
                        {
                            public float Value;
                        }

                        public struct RotationSpeed_ForEach : IComponentData
                        {
	                        public float RadiansPerSecond;
                        }
                    }

                    public partial class TwoForEachTypes
                    {
                        public partial class Child : JobComponentSystem
                        {
                            protected override JobHandle OnUpdate(JobHandle inputDeps)
                            {
                                var myEntityJob = new MyFirstClass.MyEntityJob { MyDeltaTime = Time.DeltaTime };
                                JobHandle myJobHandle = Entities.ForEach(myEntityJob).ScheduleParallel(inputDeps);

                                return Entities.ForEach((ref MyFirstClass.Translation translation) => { translation.Value *= 1.2345f; }).Schedule(myJobHandle);
                            }
                        }
                    }
                }
            }";

        [Test]
        public void JobEntity_AndLambdaJobs_InSameOnUpdateMethodTest()
        {
            RunTest(
                Code,
                new GeneratedType
                {
                    Name = "OuterNamespace.InnerNamespace.JobEntityAndForEach"
                });
        }
    }
    */
}
#endif
