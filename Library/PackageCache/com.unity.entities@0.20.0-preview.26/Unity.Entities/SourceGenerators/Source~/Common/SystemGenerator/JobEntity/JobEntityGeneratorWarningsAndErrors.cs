using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.SystemGeneratorCommon;

namespace Unity.Entities.SourceGen.SystemGeneratorCommon
{
    public static class JobEntityGeneratorWarningsAndErrors
    {
        const string WarningTitle = "IJobEntity Warning";
        const string ErrorTitle = "IJobEntity Error";

        public static void IJE_DC0001(
            SystemGeneratorContext context,
            Location location,
            string iJobEntityTypeName,
            string methodSignature,
            string parameterName,
            string parameterType)
        {
            context.LogWarning(
                nameof(IJE_DC0001),
                WarningTitle,
                $"The parameter '{parameterName}' of type {parameterType} in {iJobEntityTypeName}.{methodSignature} will be ignored.",
                location);
        }

        public static void IJE_DC0002(SystemGeneratorContext context, Location location, string nonPartialJobEntityStructName)
        {
            context.LogError(
                nameof(IJE_DC0002),
                ErrorTitle,
                $"{nonPartialJobEntityStructName} is an IJobEntity job struct, but is not defined with partial. " +
                "IJobEntity job structs are source generated. Please add the `partial` keyword as part of the struct definition.",
                location);
        }

        public static void IJE_DC0003(SystemGeneratorContext context, Location location, string jobEntityTypeName, int numMethodsFoundWithGenerateExecuteMethodAttribute)
        {
            context.LogError(
                nameof(IJE_DC0003),
                ErrorTitle,
                $"{numMethodsFoundWithGenerateExecuteMethodAttribute} Execute() method(s) -- including source-generated ones -- found in {jobEntityTypeName}. " +
                "Please ensure that each IJobEntity type contains one or two Execute() methods.",
                location);
        }

        public static void IJE_DC0004(SystemGeneratorContext context, Location location, string jobEntityTypeName, string methodSignature, string nonIntegerEntityQueryParameter)
        {
            context.LogError(
                nameof(IJE_DC0004),
                ErrorTitle,
                $"{jobEntityTypeName}.{methodSignature} accepts a non-integer parameter ('{nonIntegerEntityQueryParameter}') with the [EntityInQueryIndex] attribute. " +
                "This is not allowed. The [EntityInQueryIndex] attribute may only be applied on integer parameters.",
                location);
        }

        public static void IJE_DC0005(SystemGeneratorContext context, Location location, string jobEntityTypeName, string methodSignature)
        {
            context.LogError(
                nameof(IJE_DC0005),
                ErrorTitle,
                $"{jobEntityTypeName}.{methodSignature} accepts more than one integer parameters with the [EntityInQueryIndex] attribute. " +
                $"This is not allowed. The [EntityInQueryIndex] attribute can only be applied EXACTLY ONCE on an integer parameter in {jobEntityTypeName}.{methodSignature}.",
                location);
        }

        public static void IJE_DC0006(SystemGeneratorContext context, Location location, string jobEntityTypeName, int numUserDefinedExecuteMethods)
        {
            context.LogError(
                nameof(IJE_DC0006),
                ErrorTitle,
                $"You have defined {numUserDefinedExecuteMethods} Execute() method(s) in {jobEntityTypeName}. " +
                "Please define exactly one Execute() method in each IJobEntity type.",
                location);
        }
    }
}
