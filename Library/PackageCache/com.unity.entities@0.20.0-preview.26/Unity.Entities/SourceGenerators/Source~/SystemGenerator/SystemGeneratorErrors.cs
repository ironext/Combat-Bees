using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator
{
    static class SystemGeneratorErrors
    {
        public static void DC0058(GeneratorExecutionContext context, Location location, string nonPartialSystemBaseDerivedClassName)
        {
            context.LogError(nameof(DC0058), "System Error",
                $"All SystemBase-derived classes must be defined with the `partial` keyword, so that source generators can emit additional code into these classes. Please add the `partial` keyword to {nonPartialSystemBaseDerivedClassName}, as well as all the classes it is nested within.",
                location);
        }

        public static void DC0060(GeneratorExecutionContext context, Location location, string assemblyName)
        {
            context.LogError(nameof(DC0060), "System Error",
                $"Assembly {assemblyName} contains Entities.ForEach or Entities.OnUpdate invocations that use burst but does not have a reference to Unity.Burst.  Please add an assembly reference to `Unity.Burst` in the asmdef for {assemblyName}.", location);
        }
    }
}
