using System;
using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGeneratorCommon;

namespace Unity.Entities.SourceGen.LambdaJobs
{
    static class LambdaJobsErrors
    {
        // Don't have a good way to detect fields in reference types yet (containing type is captured instead of field)
        /*
        public static void DC0001(SystemGeneratorContext context, Location location, string fieldName, string containingTypeName)
        {
            context.LogError(nameof(DC0001), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression uses field '{fieldName}` in reference type `{containingTypeName}'. Either assign the field to a local outside of the lambda expression and use that instead, or use .WithoutBurst() and .Run()", location);
        }

        public static DiagnosticMessage DC0002(MethodDefinition method, Instruction instruction, MethodReference mr, TypeReference argument)
        {
            return MakeError(nameof(DC0002), $"Entities.ForEach Lambda expression invokes '{mr.Name}' on a {argument.Name} which is a reference type. This is only allowed with .WithoutBurst() and .Run().", method, instruction);
        }
        */

        public static void DC0003(SystemGeneratorContext context, Location location, string name)
        {
            context.LogError(nameof(DC0003), "Lambda Jobs Error",
                $"The name '{name}' is already used in this system.", location);
        }

        public static void DC0004(SystemGeneratorContext context, Location location, string variableName)
        {
            context.LogError(nameof(DC0004), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression captures a non-value type '{variableName}'. This is only allowed with .WithoutBurst() and .Run()", location);
        }

        public static void DC0005(SystemGeneratorContext context, Location location, string parameterName, string parameterTypeName)
        {
            context.LogError(nameof(DC0005), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression parameter '{parameterName}' with type {parameterTypeName} is not supported", location);
        }

        /*
        We now no longer assume Entities references need to be Entities.ForEach
        (since they can also be Entities.OnUpdate, and potentially other in the future and we can't resolve symbols while walking syntax)
        public static void DC0006(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0006), "Lambda Jobs Error",
                $"Scheduling an Entities query requires a .ForEach (for Entities.ForEach), .WithCode (for Job.WithCode) or .OnUpdate (for IJobEntity) invocation", location);
        }
        */

        public static void DC0008(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0008), "Lambda Jobs Error",
                $"The argument to {methodName} needs to be a literal value.", location);
        }

        public static void DC0009(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0009), "Lambda Jobs Error",
                $"{methodName} is only allowed to be called once.", location);
        }

        public static void DC0010(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0010), "Lambda Jobs Error",
                $"The Entities.ForEach statement contains dynamic code in {methodName} that cannot be statically analyzed.", location);
        }

        public static void DC0011(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0011), "Lambda Jobs Error",
                $"Every Entities.ForEach statement needs to end with a .Schedule(), .ScheduleParallel() or .Run() invocation.", location);
        }

        public static void DC0012(SystemGeneratorContext context, Location location, string argumentName, string constructionMethodName)
        {
            context.LogError(nameof(DC0012), "Lambda Jobs Error",
                $"Entities.{constructionMethodName} is called with an invalid argument {argumentName}. You can only use Entities.{constructionMethodName} on local variables that are captured inside the lambda. Please assign the field to a local variable and use that instead.", location);
        }

        public static void DC0013(SystemGeneratorContext context, Location location, string capturedVariableName)
        {
            context.LogError(nameof(DC0013), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression writes to captured variable '{capturedVariableName}' that is then read outside. This is only supported when you use .Run().", location);
        }

        public static void DC0014(SystemGeneratorContext context, Location location, string parameterName, string[] supportedParameters)
        {
            context.LogError(nameof(DC0014), "Lambda Jobs Error",
                $"Execute() parameter '{parameterName}' is not a supported parameter in an IJobEntitiesForEach type. Supported `int` parameter names are {supportedParameters.SeparateByComma()}.", location);
        }

        /* Neither of these are valid any longer
        public static void DC0017(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0017), "Lambda Jobs Error",
                $"Scheduling an Lambda job requires a `.WithCode` invocation", location);
        }
        public static void DC0018(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0018), "Lambda Jobs Error",
                $"Scheduling an Chunk job requires a `.ForEach` invocation", location);
        }*/

        public static void DC0019(SystemGeneratorContext context, Location location, string sharedComponentTypeName, bool descriptionIsInSystemBase)
        {
            var message = descriptionIsInSystemBase
                ? $"Entities.ForEach uses ISharedComponentData type {sharedComponentTypeName}. This is not supported in ISystem systems."
                : $"Entities.ForEach uses ISharedComponentData type {sharedComponentTypeName}. This is only supported when using .WithoutBurst() and .Run()";
            context.LogError(nameof(DC0019), "Lambda Jobs Error", message, location);
        }

        public static void DC0020(SystemGeneratorContext context, Location location, string sharedComponentTypeName)
        {
            context.LogError(nameof(DC0020), "Lambda Jobs Error",
                $"ISharedComponentData type {sharedComponentTypeName} can not be received by ref. Use by value or in.", location);
        }

        public static void DC0021(SystemGeneratorContext context, Location location, string parameterName, string unsupportedTypeName)
        {
            context.LogError(nameof(DC0021), "Lambda Jobs Error",
                $"parameter '{parameterName}' has type {unsupportedTypeName}. This type is not a IComponentData / ISharedComponentData and is therefore not a supported parameter type for Entities.ForEach.", location);
        }

        public static void DC0023(SystemGeneratorContext context, Location location, string componentTypeName, bool descriptionIsInSystemBase)
        {
            var message = descriptionIsInSystemBase
                ? $"Entities.ForEach uses managed IComponentData `{componentTypeName}`. This is not supported in ISystem systems."
                : $"Entities.ForEach uses managed IComponentData `{componentTypeName}`. This is only supported when using .WithoutBurst() and .Run().";
            context.LogError(nameof(DC0023), "Lambda Jobs Error", message, location);
        }

        public static void DC0024(SystemGeneratorContext context, Location location, string componentTypeName)
        {
            context.LogError(nameof(DC0024), "Lambda Jobs Error",
                $"Entities.ForEach uses managed IComponentData `{componentTypeName}` by ref. To get write access, receive it without the ref modifier.", location);
        }

        public static void DC0025(SystemGeneratorContext context, Location location, string typeName)
        {
            context.LogError(nameof(DC0025), "Lambda Jobs Error",
                $"Type `{typeName}` is not allowed to implement method `OnCreateForCompiler'.  This method is supplied by code generation.", location);
        }

        public static void DC0026(SystemGeneratorContext context, Location location, string allTypeName)
        {
            context.LogError(nameof(DC0026), "Lambda Jobs Error",
                $"Entities.ForEach lists has .WithAll<{allTypeName}>() and a .WithSharedComponentFilter method with a parameter of that type.  Remove the redundant WithAll method.", location);
        }

        public static void DC0027(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0027), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression makes a structural change. Use an EntityCommandBuffer to make structural changes or add a .WithStructuralChanges invocation to the Entities.ForEach to allow for structural changes.  Note: WithStructuralChanges is runs without burst and is only allowed with .Run().", location);
        }

        /* DC0027 is a better error
        public static void DC0028(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0028), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression makes a structural change with a Schedule or ScheduleParallel call. Structural changes are only supported with .Run() and .WithoutStructuralChanges().", location);
        }
        */

        public static void DC0029(SystemGeneratorContext context, Location location, SyntaxExtensions.ForEachType forEachType)
        {
            string errorTitle = String.Empty;
            string errorMessage = String.Empty;
            switch (forEachType)
        {
                case SyntaxExtensions.ForEachType.LambdaJobs:
                    errorTitle = "Lambda Jobs Error";
                    errorMessage = "Entities.ForEach Lambda expression has a nested Entities.ForEach Lambda expression. Only a single Entities.ForEach Lambda expression is currently supported.";
                    break;
                case SyntaxExtensions.ForEachType.JobEntitiesForEach:
                    errorTitle = "IJobEntitiesForEach Error";
                    errorMessage = "Entities.ForEach Lambda expression has a nested Entities.ForEach(IJobEntitiesForEach job) invocation. Only a single Entities.ForEach Lambda expression is currently supported.";
                    break;
            }
            context.LogError(nameof(DC0029), errorTitle, errorMessage, location);
        }

        public static void DC0031(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0031), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression stores the EntityQuery with a .WithStoreEntityQueryInField invocation but does not store it in a valid field.  Entity Queries can only be stored in fields of the containing JobComponentSystem.", location);
        }

        /* This is now allowed in 2020.2+.  Source generators don't have any idea about Unity version currently, but they shouldn't ship until 2020.2 is the default version of Unity for DOTS.
        public static void DC0032(SystemGeneratorContext context, Location location, string jobComponentSystemTypeName)
        {
            context.LogError(nameof(DC0032), "Lambda Jobs Error",
                $"Entities.ForEach Lambda expression exists in JobComponentSystem {jobComponentSystemTypeName} marked with ExecuteAlways.  This will result in a temporary exception being thrown during compilation, using it is not supported yet.  Please move this code out to a non-jobified ComponentSystem. This will be fixed in an upcoming 2020.2 release.", location);
        }
        */

        public static void DC0033(SystemGeneratorContext context, Location location, string parameterName, string unsupportedTypeName)
        {
            context.LogError(nameof(DC0033), "Lambda Jobs Error",
                $"{unsupportedTypeName} implements IBufferElementData and must be used as DynamicBuffer<{unsupportedTypeName}>. Parameter '{parameterName}' is not a IComponentData / ISharedComponentData and is therefore not a supported parameter type for Entities.ForEach.", location);
        }

        public static void DC0034(SystemGeneratorContext context, Location location, string argumentName, string unsupportedTypeName)
        {
            context.LogError(nameof(DC0034), "Lambda Jobs Error",
                $"Entities.WithReadOnly is called with an argument {argumentName} of unsupported type {unsupportedTypeName}. It can only be called with an argument that is marked with [NativeContainerAttribute] or a type that has a field marked with [NativeContainerAttribute].", location);
        }

        public static void DC0036(SystemGeneratorContext context, Location location, string argumentName, string unsupportedTypeName)
        {
            context.LogError(nameof(DC0036), "Lambda Jobs Error",
                $"Entities.WithNativeDisableContainerSafetyRestriction is called with an invalid argument {argumentName} of unsupported type {unsupportedTypeName}. It can only be called with an argument that is marked with [NativeContainerAttribute] or a type that has a field marked with [NativeContainerAttribute].", location);
        }

        public static void DC0037(SystemGeneratorContext context, Location location, string argumentName, string unsupportedTypeName)
        {
            context.LogError(nameof(DC0037), "Lambda Jobs Error",
                $"Entities.WithNativeDisableParallelForRestriction is called with an invalid argument {argumentName} of unsupported type {unsupportedTypeName}. It can only be called with an argument that is marked with [NativeContainerAttribute] or a type that has a field marked with [NativeContainerAttribute].", location);
        }

        /* This is now covered by DC0012
        public static void DC0038(SystemGeneratorContext context, Location location, string argumentName, string constructionMethodName)
        {
            context.LogError(nameof(DC0038), "Lambda Jobs Error",
                $"Entities.{constructionMethodName} is called with an invalid argument {argumentName}. You cannot use Entities.{constructionMethodName} with fields of user-defined types as the argument. Please assign the field to a local variable and use that instead.", location);
        }
        */


        public static void DC0043(SystemGeneratorContext context, Location location, string jobName)
        {
            context.LogError(nameof(DC0043), "Lambda Jobs Error",
                $"Entities.WithName cannot be used with name '{jobName}'. The given name must consist of letters, digits, and underscores only, and may not contain two consecutive underscores.", location);
        }

        public static void DC0044(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0044), "Lambda Jobs Error",
                $"Entities.ForEach can only be used with an inline lambda.  Calling it with a delegate stored in a variable, field, or returned from a method is not supported.", location);
        }

        /* This is now permitted with source generated Entities.ForEach
        public static void DC0045(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0045), "Lambda Jobs Error",
                $"Entities.ForEach cannot use {methodName} with branches in the method invocation.", location);
        }
        */

        public static void DC0046(SystemGeneratorContext context, Location location, string methodName, string typeName)
        {
            context.LogError(nameof(DC0046), "Lambda Jobs Error",
                $"Entities.ForEach cannot use component access method {methodName} that needs write access with the same type {typeName} that is used in lambda parameters.", location);
        }

        public static void DC0047(SystemGeneratorContext context, Location location, string methodName, string typeName)
        {
            context.LogError(nameof(DC0047), "Lambda Jobs Error",
                $"Entities.ForEach cannot use component access method {methodName} with the same type {typeName} that is used in lambda parameters with write access (as ref).", location);
        }

        /* Replaced with DC0059
        public static void DC0048(SystemGeneratorContext context, Location location, string methodName, string methodToAnalyzeName)
        {
            context.LogError(nameof(DC0048), "Lambda Jobs Error",
                $"{methodName} in {methodToAnalyzeName} has a dynamic parameter passed in by a method call.  This method can only be invoked with a boolean literal as a parameter in Entities.ForEach.", location);
        }

        public static void DC0049(SystemGeneratorContext context, Location location, string methodName, string methodToAnalyzeName)
        {
            context.LogError(nameof(DC0049), "Lambda Jobs Error",
                $"{methodName} in {methodToAnalyzeName} has a dynamic parameter passed in with a variable as a parameter.  This method can only be invoked with a boolean literal as a parameter in Entities.ForEach.", location);
        }
        */

        public static void DC0050(SystemGeneratorContext context, Location location, string parameterTypeFullName)
        {
            context.LogError(nameof(DC0050), "Lambda Jobs Error",
                $"Type {parameterTypeFullName} cannot be used as an Entities.ForEach parameter as generic types and generic parameters are not currently supported in Entities.ForEach", location);
        }


        public static void DC0051(SystemGeneratorContext context, Location location, string argumentTypeName, string invokedMethodName)
        {
            context.LogError(nameof(DC0051), "Lambda Jobs Error",
                $"Type {argumentTypeName} cannot be used with {invokedMethodName} as generic types and parameters are not allowed", location);
        }


        public static void DC0052(SystemGeneratorContext context, Location location, string argumentTypeName, string invokedMethodName)
        {
            context.LogError(nameof(DC0052), "Lambda Jobs Error",
                $"Type {argumentTypeName} cannot be used with {invokedMethodName} as it is not a supported component type", location);
        }

        public static void DC0053(SystemGeneratorContext context, Location location, string systemTypeName)
        {
            context.LogError(nameof(DC0053), "Lambda Jobs Error",
                $"Entities.ForEach cannot be used in system {systemTypeName} as Entities.ForEach in generic system types are not supported.", location);
        }

        public static void DC0054(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0054), "Lambda Jobs Error",
                $"Entities.ForEach is used in generic method {methodName}.  This is not currently supported.", location);
        }

        public static void DC0055(SystemGeneratorContext context, Location location, string lambdaParameterComponentTypeName)
        {
            context.LogWarning(nameof(DC0055), "Lambda Jobs Warning",
                $"Entities.ForEach passes {lambdaParameterComponentTypeName} by value.  Any changes made will not be stored to the underlying component.  Please specify the access you require. Use 'in' for read-only access or `ref` for read-write access.", location);
        }

        public static void DC0056(SystemGeneratorContext context, Location location, string typeGroup1Name, string typeGroup2Name, string componentTypeName)
        {
            context.LogError(nameof(DC0056), "Lambda Jobs Error",
                $"Entities.ForEach has component {componentTypeName} in both {typeGroup1Name} and {typeGroup2Name}.  This is not permitted.", location);
        }

        public static void DC0057(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0057), "Lambda Jobs Error",
                $"WithStructuralChanges cannot be used with Job.WithCode.  WithStructuralChanges should instead be used with Entities.ForEach.", location);
        }

        public static void DC0059(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0059), "Lambda Jobs Error",
                $"The argument indicating read only access to {methodName} cannot be dynamic and must to be a boolean literal `true` or `false` value when used inside of an Entities.ForEach.", location);
        }

        public static void DC0048(SystemGeneratorContext context, Location location,
            string jobEntitiesForEachTypeName)
        {
            context.LogError(
                nameof(DC0048),
                "IJobEntitiesForEach Error",
                $"IJobEntitiesForEach types may only contain value-type fields, but {jobEntitiesForEachTypeName} contains non-value type fields.",
                location);
        }

        public static void DC0059(SystemGeneratorContext context, Location location)
        {
            context.LogError(
                nameof(DC0059),
                "IJobEntitiesForEach Error",
                "IJobEntitiesForEach types may not have the [Unity.Burst.BurstCompile] attribute. IJobEntityBatch types generated from IJobEntitiesForEach types" +
                "will be bursted (or not) depending on whether WithoutBurst() is invoked when calling Entities.ForEach(IJobEntitiesForEach job).",
                location);
        }

        public static void DC0062(SystemGeneratorContext context, Location location)
        {
            context.LogError(
                nameof(DC0062),
                "IJobEntitiesForEach Error",
                "Using WithStructuralChanges(), WithReadOnly(), WithDisposeOnCompletion(), WithNativeDisableContainerSafetyRestriction(), " +
                "WithNativeDisableParallelForRestriction() or WithNativeDisableUnsafePtrRestriction() together with " +
                "Entities.ForEach(IJobEntitiesForEach instance) is not supported.",
                location);
        }

		public static void DC0063(SystemGeneratorContext context, Location location, string methodName, string componentDataName)
        {
            context.LogError(nameof(DC0063), "System Error",
                $"Method {methodName} is giving write access to component data {componentDataName} in an Entities.ForEach.  The job system cannot guarantee the safety of that invocation.  Either change the scheduling from ScheduleParallel to Schedule or access through a captured ComponentDataFromEntity and mark it with WithNativeDisableParallelForRestriction if you are certain that this is safe.", location);
        }

        public static void DC0064(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0064), "System Error", $"WithEntityQueryOptions must be used with a EntityQueryOption value as the argument.", location);
        }

        public static void DC0068(SystemGeneratorContext context, Location location, string jobEntitiesForEachTypeName)
        {
            context.LogError(
                nameof(DC0068),
                "IJobEntitiesForEach Error",
                $"No '{jobEntitiesForEachTypeName}' type found that both 1) implements the IJobEntitiesForEach interface and 2) contains an Execute() method.",
                location);
        }

        public static void DC0069(
            SystemGeneratorContext context,
            Location location,
            string unsupportedParameterType,
            string unsupportedParameterName)
        {
            context.LogError(
                nameof(DC0069),
                "IJobEntitiesForEach Error",
                $"IJobEntitiesForEach.Execute() parameter '{unsupportedParameterName}' of type {unsupportedParameterType} is not supported.",
                location);
        }

        public static void DC0070(SystemGeneratorContext context, Location location, ITypeSymbol duplicateType)
        {
            context.LogError(
                nameof(DC0070),
                "LambdaJobs Error",
                $"{duplicateType.Name} is used multiple times as a lambda parameter. Each IComponentData, ISharedComponentData, DynamicBuffer<T> type may only be used once in Entities.ForEach().",
                location);
        }

        public static void DC0071(SystemGeneratorContext context, Location location, string methodName)
        {
            context.LogError(nameof(DC0071), "Lambda Jobs Error",
                $"Invocation {methodName} cannot be used in an Entities.ForEach in system implementing ISystem.", location);
        }

        public static void DC0072(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0072), "Lambda Jobs Error",
                $"Entities.ForEach in ISystem systems must be accessed through the SystemState argument passed into the containing method (state.Entities.ForEach(...).", location);
        }

        public static void DC0073(SystemGeneratorContext context, Location location)
        {
            context.LogError(nameof(DC0073), "Lambda Jobs Error",
                $"WithScheduleGranularity cannot be used with Schedule or Run as it controls how parallel job scheduling occurs. Use ScheduleParallel with this feature.", location);
        }
    }
}
