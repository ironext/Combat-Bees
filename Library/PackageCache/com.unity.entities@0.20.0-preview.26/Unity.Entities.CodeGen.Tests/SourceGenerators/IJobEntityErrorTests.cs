#if !SYSTEM_SOURCEGEN_DISABLED && DOTS_EXPERIMENTAL

using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities.CodeGen.Tests;

namespace Unity.Entities.CodeGen.SourceGenerators.Tests
{
    [TestFixture]
    public class IJobEntityWarningAndErrorTests  : SourceGenTests
    {
        protected override Type[] DefaultCompilationReferenceTypes { get; } =
        {
            typeof(Entity),
            typeof(Translation),
            typeof(NativeArray<>)
        };

        protected override string[] DefaultUsings { get; } =
        {
            "System", "Unity.Entities", "Unity.Collections", "Unity.Entities.CodeGen.Tests"
        };

        [Test]
        public void IJE_DC0001_InvalidValueTypesInExecuteMethod()
        {
            const string source =
                @"public partial struct WithInvalidValueTypeParameters : IJobEntity
                {
                    void Execute(Entity entity, float invalidFloat)
                    {
                    }
                }

                public partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        Dependency = new WithInvalidValueTypeParameters().Schedule(Dependency);
                    }
                }";

            AssertProducesWarning(source, "IJE_DC0001", "WithInvalidValueTypeParameters");
        }

        [Test]
        public void IJE_DC0002_NonPartialType()
        {
            const string source =
                @"public struct NonPartialJobEntity : IJobEntity
                {
                    void Execute(Entity entity)
                    {
                    }
                }

                public partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        Dependency = new NonPartialJobEntity().Schedule(Dependency);
                    }
                }";

            AssertProducesError(source, "IJE_DC0002", "NonPartialJobEntity");
        }

        [Test]
        public void IJE_DC0003_NoExecuteMethod()
        {
            const string source =
                @"public partial struct NoExecuteMethod : IJobEntity
                {
                    void NotExecuting(Entity entity)
                    {
                    }
                }

                public partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var instance = new NoExecuteMethod();
                        Dependency = instance.Schedule(Dependency);
                    }
                }";

            AssertProducesError(source, "IJE_DC0003", "NoExecuteMethod");
        }

        [Test]
        public void IJE_DC0003_TooManyExecuteMethods()
        {
            const string source =
                @"public partial struct TooManyExecuteMethods : IJobEntity
                {
                    void Execute(Entity entity)
                    {
                    }

                    void Execute([EntityInQueryIndex] int index)
                    {
                    }

                    void Execute()
                    {
                    }
                }

                public partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var instance = new TooManyExecuteMethods();
                        Dependency = instance.Schedule(Dependency);
                    }
                }";

            AssertProducesError(source, "IJE_DC0003", "TooManyExecuteMethods");
        }

        [Test]
        public void IJE_DC0004_NonIntegerEntityInQueryParameter()
        {
            const string source =
                @"public partial struct NonIntegerEntityInQueryParameter : IJobEntity
                {
                    void Execute(Entity entity, [EntityInQueryIndex] bool notInteger)
                    {
                    }
                }

                public partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        Dependency = new NonIntegerEntityInQueryParameter().Schedule(Dependency);
                    }
                }";

            AssertProducesError(source, "IJE_DC0004", "NonIntegerEntityInQueryParameter");
        }

        [Test]
        public void IJE_DC0005_TooManyIntegerEntityInQueryParameters()
        {
            const string source =
                @"public partial struct TooManyIntegerEntityInQueryParameters : IJobEntity
                {
                    void Execute(Entity entity, [EntityInQueryIndex] int first, [EntityInQueryIndex] int second)
                    {
                    }
                }

                public partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        Dependency = new TooManyIntegerEntityInQueryParameters().Schedule(Dependency);
                    }
                }";

            AssertProducesError(source, "IJE_DC0005", "TooManyIntegerEntityInQueryParameters");
        }

        [Test]
        public void IJE_DC0006_MoreThanOneUserDefinedExecuteMethods()
        {
            const string source = @"
                public partial class TooManyUserDefinedExecuteMethods : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var job = new ThrustJob();
                        job.ScheduleParallel(Dependency);
                    }

                    struct NonIJobEntityStruct
                    {
                        public void Execute() {}
                        public void Execute(int someVal) {}
                    }

                    partial struct ThrustJob : IJobEntity
                    {
                        public void Execute(ref Translation translation) {}
                        public void Execute(int someVal) {}
                    }
                }";

            AssertProducesError(source, "IJE_DC0006", "TooManyUserDefinedExecuteMethods");
        }
    }
}

#endif
