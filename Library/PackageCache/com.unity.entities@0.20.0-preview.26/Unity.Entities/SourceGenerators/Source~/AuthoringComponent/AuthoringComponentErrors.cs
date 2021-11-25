using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.AuthoringComponent
{
    static class AuthoringComponentErrors
    {
        const string ErrorDescription = "Authoring Component Generator Error";

        public static void DC0030(GeneratorExecutionContext context, Location location, string componentTypeName)
        {
            context.LogError(
                nameof(DC0030),
                ErrorDescription,
                $"GenerateAuthoringComponentAttribute is used on managed IComponentData {componentTypeName} without a default constructor. " +
                $"This is not supported. Add a default constructor to {componentTypeName} to enable this to work: public {componentTypeName}() {{}}",
                location);
        }

        public static void DC0040(GeneratorExecutionContext context, Location location, string fieldName, string bufferElementDataTypeName)
        {
            context.LogError(
                nameof(DC0040),
                ErrorDescription,
                "Structs implementing IBufferElementData may only contain fields of either primitive or blittable types. " +
                $"However, '{bufferElementDataTypeName}' contains a field named {fieldName}, which is NOT of a primitive or blittable type.",
                location);
        }

        public static void DC0041(GeneratorExecutionContext context, Location location, string bufferElementDataTypeName)
        {
            context.LogError(
                nameof(DC0041),
                ErrorDescription,
                $"IBufferElementData can only be implemented by structs. '{bufferElementDataTypeName}' is a class." +
                $"Please change {bufferElementDataTypeName} to a struct.",
                location);
        }

        public static void DC0042(GeneratorExecutionContext context, Location location, string bufferElementDataTypeName)
        {
            context.LogError(nameof(DC0042),
                ErrorDescription,
                "Structs implementing IBufferElementData and marked with a GenerateAuthoringComponentAttribute attribute " +
                $"cannot have an explicit layout. {bufferElementDataTypeName} has an explicit layout. " +
                "Please implement your own authoring component.",
                location);
        }

        public static void DC3003(GeneratorExecutionContext context, Location location, string typeWithGenerateAuthoringComponentAttributeName)
        {
            context.LogError(
                nameof(DC3003),
                ErrorDescription,
                $"{typeWithGenerateAuthoringComponentAttributeName} has a GenerateAuthoringComponentAttribute, " +
                "and must therefore implement either the IComponentData interface or the IBufferElementData interface.",
                location);
        }

        public static void DC0060(GeneratorExecutionContext context, Location location, string structContainingManagedType)
        {
            context.LogError(nameof(DC0060),
                ErrorDescription,
                $"Invalid use of Entity[] in the {structContainingManagedType} struct: IComponentData structs cannot contain managed types." +
                "Either use an array that works in IComponentData structs (DynamicBuffer) or a IComponentData class.",
                location);
        }

        public static void DC0061(GeneratorExecutionContext context, Location location, string assemblyName)
        {
            context.LogError(nameof(DC0061),
                ErrorDescription,
                $"Assembly {assemblyName} contains use of GenerateAuthoringComponentAttribute but does not have a reference to `Unity.Entities.Hybrid`." +
                $" Please add an assembly reference to `Unity.Entities.Hybrid` in the asmdef for {assemblyName}." +
                " This will get ignored when compiling a mixed assembly for DOTS Runtime.",
                location);
        }
    }
}
