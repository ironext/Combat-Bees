using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGeneratorCommon;

/*
    JobEntityGenerator and JobEntityModule (A System Generator Module) work together to provide the IJobEntity feature.

    For an explanation of the feature, see: https://unity.slack.com/archives/CQ811KGJJ/p1620212365031500

    -- JobEntityGenerator --
    A user writes an IJobEntity with `partial`:

    public partial struct MyJob : IJobEntity
    {
        public void Execute(ref Translation translation, in Velocity velocity)
        {
            ...
        }
    }

    The **JobEntityGenerator** will generate code that extends MyJob into a working IJobEntityBatch:

    //Generated
    public partial struct MyJob : IJobEntity, IJobEntityBatch
    {
        ComponentTypeHandle<Translation> __TranslationTypeHandle;
        [ReadOnly]
        ComponentTypeHandle<Velocity> __VelocityTypeHandle;

        public void Execute(ArchetypeChunk batch, int batchIndex)
        {
            var translationData = UnsafeGetChunkNativeArrayIntPtr<Rotation>(batch, __TranslationTypeHandle);
            var velocityData  = UnsafeGetChunkNativeArrayIntPtr<Rotation>(batch, __VelocityTypeHandle);
            int count = batch.Count;
            for (int i = 0; i < count; ++i)
            {
                ref var translationData__ref = ref UnsafeGetRefToNativeArrayPtrElement<Translation>(translationData, i);
                ref var velocityData__ref = ref UnsafeGetRefToNativeArrayPtrElement<Velocity>(velocityData, i);
                Execute(ref translationData__ref, in velocityData__ref);
            }
        }
    }

    -- JobEntityModule --
    A user wants to create and schedule an IJobEntity, so after writing the above struct they write this in a System:
    public partial MySystem : SystemBase
    {
        public void OnUpdate()
        {
            var myJob = new MyJob();
            Dependency = myJob.Schedule(Dependency);
        }
    }

    In this case, **JobEntityModule** will generate changes to the System to allow this generated job to be scheduled normally:

    // Generated
    public partial class MySystem : SystemBase
    {
        protected void __OnUpdate_2C361387()
        {
            var myJob = new MyJob();
            Dependency = __ScheduleViaJobEntityBatchExtension_0(myJob, __query_0, 1, Dependency);
        }

        public JobHandle __ScheduleViaJobEntityBatchExtension_0(MyJob job, EntityQuery entityQuery, int batchesPerChunk, JobHandle dependency)
        {
            Unity_Transforms_Translation_RW_ComponentTypeHandle.Update(this);
            Velocity_RO_ComponentTypeHandle.Update(this);
            job.__TranslationTypeHandle = Unity_Transforms_Translation_RW_ComponentTypeHandle;
            job.__VelocityTypeHandle = Velocity_RO_ComponentTypeHandle;
            return JobEntityBatchExtensions.Schedule(job, entityQuery, dependency);
        }
    }

    This is why we have two different generators. One is about generating the extension to your job struct and the other is generating the new callsite in a system.
    JobEntityGenerator- extending Job struct (once per struct)
    JobEntityModule- extending callsite in System (once per Job invocation in a system)
*/

namespace Unity.Entities.SourceGen.JobEntity
{
    public class JobEntityModule : ISystemModule
    {
        List<(SyntaxNode SyntaxNode, TypeDeclarationSyntax SystemType)> m_Candidates = new List<(SyntaxNode SyntaxNode, TypeDeclarationSyntax SystemType)>();
        Dictionary<TypeDeclarationSyntax, List<MemberAccessExpressionSyntax>> m_JobEntityInvocationCandidates = new Dictionary<TypeDeclarationSyntax, List<MemberAccessExpressionSyntax>>();
        List<TypeDeclarationSyntax> nonPartialJobEntityTypes = new List<TypeDeclarationSyntax>();

        public IEnumerable<(SyntaxNode SyntaxNode, TypeDeclarationSyntax SystemType)> Candidates => m_Candidates;
        public bool RequiresReferenceToBurst => false;

        enum ScheduleMode
        {
            Schedule,
            ScheduleByRef,
            ScheduleParallel,
            ScheduleParallelByRef,
            Run,
            RunByRef
        }

        enum ExtensionType
        {
            Batch,
            BatchIndex
        }

        public void OnReceiveSyntaxNode(SyntaxNode node)
        {
            if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccessExpressionSyntax }
                && memberAccessExpressionSyntax.Kind() == SyntaxKind.SimpleMemberAccessExpression
                && (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax || memberAccessExpressionSyntax.Expression is ObjectCreationExpressionSyntax))
            {
                var schedulingMethodName = memberAccessExpressionSyntax.Name.Identifier.ValueText;

                if (Enum.GetNames(typeof(ScheduleMode)).Contains(schedulingMethodName))
                {
                    TypeDeclarationSyntax containingType = node.Ancestors().OfType<TypeDeclarationSyntax>().First();

                    // Discard if no base type, meaning it can't possible inherit from SystemBase or JobComponentSystem
                    if (containingType.BaseList == null || containingType.BaseList.Types.Count == 0)
                        return;
                    m_JobEntityInvocationCandidates.Add(containingType, memberAccessExpressionSyntax);
                    m_Candidates.Add((node, containingType));
                }
            }
        }

        static (bool IsCandidate, bool IsExtensionMethodUsed) IsIJobEntityCandidate(TypeInfo typeInfo)
        {
            if (typeInfo.Type.InheritsFromInterface("Unity.Entities.IJobEntity"))
            {
                return (IsCandidate: typeInfo.Type.InheritsFromInterface("Unity.Entities.IJobEntity"), IsExtensionMethodUsed: false);
            }

            // IsExtensionMethodUsed is ignored if IsCandidate is false, so we don't need to test the same thing twice
            return (IsCandidate: typeInfo.Type.ToFullName() == "Unity.Entities.IJobEntityExtensions", IsExtensionMethodUsed: true);
        }

        static (ExpressionSyntax Argument, INamedTypeSymbol Symbol)
            GetJobEntitySymbolPassedToExtensionMethod(MemberAccessExpressionSyntax candidate, SemanticModel semanticModel)
        {
            var arguments = candidate.Ancestors().OfType<InvocationExpressionSyntax>().First().ChildNodes().OfType<ArgumentListSyntax>().SelectMany(a => a.Arguments).ToArray();
            var jobEntityArgument = GetJobEntityArgumentPassedToExtensionMethod(arguments);

            switch (jobEntityArgument.Expression)
            {
                case ObjectCreationExpressionSyntax objectCreationExpressionSyntax
                    when ((IMethodSymbol)semanticModel.GetSymbolInfo(objectCreationExpressionSyntax).Symbol).ReceiverType is INamedTypeSymbol namedTypeSymbol:
                {
                    return (Argument: jobEntityArgument.Expression, Symbol: namedTypeSymbol);
                }
                case IdentifierNameSyntax identifierNameSyntax
                    when ((ILocalSymbol)semanticModel.GetSymbolInfo(identifierNameSyntax).Symbol).Type is INamedTypeSymbol namedTypeSymbol:
                {
                    return (Argument: jobEntityArgument.Expression, Symbol: namedTypeSymbol);
                }
            }
            return default;
        }

        static (INamedTypeSymbol jobSymbol, SyntaxNode PassedArgument) GetIJobEntityTypeDeclarationAndArgument(
            MemberAccessExpressionSyntax candidate, SemanticModel semanticModel, TypeInfo typeInfo, bool isExtensionMethodUsed)
        {
            if (isExtensionMethodUsed)
            {
                var (argument, symbol) = GetJobEntitySymbolPassedToExtensionMethod(candidate, semanticModel);
                return (jobSymbol: symbol, PassedArgument: argument);
            }

            return (jobSymbol: (INamedTypeSymbol)typeInfo.Type, PassedArgument: candidate.Expression);
        }

        static ArgumentSyntax GetJobEntityArgumentPassedToExtensionMethod(IReadOnlyList<ArgumentSyntax> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                var currentArgument = arguments[i];
                if (currentArgument.NameColon != null)
                {
                    if (currentArgument.NameColon.Name.Identifier.ValueText == "jobData")
                    {
                        return currentArgument;
                    }

                    continue;
                }

                if (i == 0)
                {
                    return currentArgument;
                }
            }
            throw new ArgumentException("No IJobEntity argument found.");
        }

        public bool GenerateSystemType(SystemGeneratorContext context)
        {
            foreach (var nonPartialType in nonPartialJobEntityTypes)
            {
                JobEntityGeneratorWarningsAndErrors.IJE_DC0002(context, nonPartialType.GetLocation(), nonPartialType.Identifier.ValueText);
            }

            var candidates = m_JobEntityInvocationCandidates[context.SystemType];
            var success = true;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                ExpressionSyntax jobEntityInstance = GetExpressionSyntax(candidate);

                var typeInfo = context.SemanticModel.GetTypeInfo(jobEntityInstance);

                var (isCandidate, isExtensionMethodUsed) = IsIJobEntityCandidate(typeInfo);

                if (!isCandidate)
                {
                    continue;
                }

                if (typeInfo.Type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    bool isPartial = typeDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                    if (!isPartial)
                    {
                        JobEntityGeneratorWarningsAndErrors.IJE_DC0002(context, typeDeclarationSyntax.GetLocation(), typeDeclarationSyntax.Identifier.ValueText);
                        continue;
                    }
                }

                var (jobEntityType, jobArgumentUsedInSchedulingMethod) =
                    GetIJobEntityTypeDeclarationAndArgument(candidate, context.SemanticModel, typeInfo, isExtensionMethodUsed);

                var jobEntityDesc = new JobEntityDescription(jobEntityType, context.SemanticModel);

                if (!jobEntityDesc.Valid)
                {
                    jobEntityDesc.OutputErrors(context);
                    continue;
                }

                var queryTypes = new List<(INamedTypeSymbol, bool)>();
                var jobEntityAssignments = new List<(JobEntityParam, string)>();

                foreach (var param in jobEntityDesc.UserExecuteMethodParams)
                {
                    if(param.IsQueryableType)
                        queryTypes.Add(((INamedTypeSymbol)param.TypeSymbol, param.IsReadOnly));

                    // Managed types do not use
                    if (!param.RequiresTypeHandleFieldInSystemBase)
                    {
                        if (String.IsNullOrEmpty(param.JobEntityFieldAssignment))
                        {
                            continue;
                        }
                        jobEntityAssignments.Add((param, param.JobEntityFieldAssignment));
                        continue;
                    }

                    var componentTypeField = context.GetOrCreateComponentTypeField(param.TypeSymbol, param.IsReadOnly);
                    jobEntityAssignments.Add((param, componentTypeField));
                }

                var generatedQueryField = context.GetOrCreateQueryField(new EntityQueryDescription() { All=queryTypes });
                var invocationExpression = candidate.Parent as InvocationExpressionSyntax;
                var (entityQuery, dependency, scheduleMode) = GetArguments(context, isExtensionMethodUsed, invocationExpression, candidate.Name.Identifier.ValueText, generatedQueryField);

                var (syntax, name) =
                    CreateSchedulingMethod(jobEntityAssignments, jobEntityDesc.FullTypeName, scheduleMode, i, jobEntityDesc.HasEntityInQueryIndex ? ExtensionType.BatchIndex : ExtensionType.Batch);

                context.AddNewMember(syntax);

                // Generate code for callsite
                context.ReplaceNodeInMethod(invocationExpression,
                    SyntaxFactory.ParseExpression($"{name}({jobArgumentUsedInSchedulingMethod}, {entityQuery}, {dependency})"));
            }

            return success;
        }

        static ExpressionSyntax GetExpressionSyntax(MemberAccessExpressionSyntax candidate)
        {
            if (candidate.Expression is IdentifierNameSyntax identifierNameSyntax)
            {
                return identifierNameSyntax;
            }

            return candidate.Expression as ObjectCreationExpressionSyntax;
        }

        public bool ShouldRun(ParseOptions parseOptions) => parseOptions.PreprocessorSymbolNames.Contains("DOTS_EXPERIMENTAL");

        enum ArgumentType
        {
            EntityQuery,
            Dependency,
            JobEntity
        }

        static (string EntityQuery, string Dependency, ScheduleMode ScheduleMode) GetArguments(
            SystemGeneratorContext context, bool isExtensionMethodUsed, InvocationExpressionSyntax invocationExpression, string methodName, string defaultEntityQueryName)
        {
            string entityQueryArgument = defaultEntityQueryName;
            string dependencyArgument = "default(Unity.Jobs.JobHandle)";

            var arguments = invocationExpression.ChildNodes().OfType<ArgumentListSyntax>().SelectMany(list => list.Arguments).ToArray();

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var namedArgument = argument.ChildNodes().OfType<NameColonSyntax>().SingleOrDefault();

                var (type, value) =
                    namedArgument == null
                        ? ParseUnnamedArgument(argument, i, isExtensionMethodUsed, methodName == "Schedule" || methodName == "ScheduleByRef", context)
                        : ParseNamedArgument(argument, argumentName: namedArgument.Name.Identifier.ValueText);

                switch (type)
                {
                    case ArgumentType.EntityQuery:
                        entityQueryArgument = value;
                        continue;
                    case ArgumentType.Dependency:
                        dependencyArgument = value;
                        continue;
                }
            }

            ScheduleMode scheduleMode = methodName switch
            {
                "Run" => ScheduleMode.Run,
                "RunByRef" => ScheduleMode.RunByRef,
                "Schedule" => ScheduleMode.Schedule,
                "ScheduleByRef" => ScheduleMode.ScheduleByRef,
                "ScheduleParallel" => ScheduleMode.ScheduleParallel,
                "ScheduleParallelByRef" => ScheduleMode.ScheduleParallelByRef,
                _ => throw new ArgumentOutOfRangeException()
            };

            return (entityQueryArgument, dependencyArgument, scheduleMode);
        }

        static (ArgumentType Type, string Value) ParseNamedArgument(ArgumentSyntax argument, string argumentName)
        {
            switch (argumentName)
            {
                case "query":
                    return (ArgumentType.EntityQuery, GetArgument(argument.Expression));
                case "dependsOn":
                    return (ArgumentType.Dependency, GetArgument(argument.Expression));
                case "jobData":
                    return
                        (ArgumentType.JobEntity,
                            "Previously retrieved, so we are ignoring this value. We are addressing this case only for " +
                            "the purpose of avoiding the ArgumentOutOfRangeException at the end of this method.");
            }

            throw new ArgumentOutOfRangeException("The IJobEntityExtensions class does not contain methods accepting more than 4 arguments.");
        }

        static (ArgumentType Type, string Value) ParseUnnamedArgument(ArgumentSyntax argument, int argumentPosition, bool isExtensionMethodUsed, bool methodOverloadAcceptsDependencyAsOnlyArgument, SystemGeneratorContext context)
        {
            bool isFirstArgumentJobData = isExtensionMethodUsed;
            int position = isFirstArgumentJobData ? argumentPosition : argumentPosition + 1;

            switch (position)
            {
                case 0:
                {
                    return
                        (ArgumentType.JobEntity,
                        "Previously retrieved, so we are ignoring this value. We are addressing this case only for " +
                        "the purpose of avoiding the ArgumentOutOfRangeException at the end of this method.");
                }
                case 1: // Could be EntityQuery or dependsOn
                {
                    if (argument.Expression.IsKind(SyntaxKind.DefaultLiteralExpression))
                        return (methodOverloadAcceptsDependencyAsOnlyArgument ? ArgumentType.Dependency : ArgumentType.EntityQuery, "default");

                    var typeInfo = context.SemanticModel.GetTypeInfo(argument.Expression);
                    return
                        typeInfo.Type.ToFullName() == "Unity.Entities.EntityQuery"
                            ? (ArgumentType.EntityQuery, GetArgument(argument.Expression))
                            : (ArgumentType.Dependency, GetArgument(argument.Expression));
                }
                case 2: // dependsOn
                    return (ArgumentType.Dependency, GetArgument(argument.Expression));
            }

            throw new ArgumentOutOfRangeException("The IJobEntityExtensions class does not contain methods accepting more than 4 arguments.");
        }

        static string GetArgument(ExpressionSyntax argumentExpression)
        {
            switch (argumentExpression)
            {
                case DefaultExpressionSyntax defaultExpressionSyntax:
                    return defaultExpressionSyntax.ToString();
                case LiteralExpressionSyntax literalExpressionSyntax:
                    return literalExpressionSyntax.Token.ValueText;
                case ObjectCreationExpressionSyntax objectCreationExpressionSyntax:
                    return objectCreationExpressionSyntax.ToString();
                case IdentifierNameSyntax identifierNameSyntax:
                    return identifierNameSyntax.Identifier.ValueText;
            }

            return default;
        }

        static (MemberDeclarationSyntax Syntax, string Name) CreateSchedulingMethod(
            IReadOnlyCollection<(JobEntityParam, string)> assignments, string fullTypeName, ScheduleMode scheduleMode, int methodId, ExtensionType extensionType)
        {
            bool containsReturn = !(scheduleMode == ScheduleMode.Run || scheduleMode == ScheduleMode.RunByRef);
            string methodName = extensionType == ExtensionType.BatchIndex ? $"__ScheduleViaJobEntityBatchIndexExtension_{methodId}" : $"__ScheduleViaJobEntityBatchExtension_{methodId}";
            string staticExtensionsClass = extensionType == ExtensionType.BatchIndex ? "JobEntityBatchIndexExtensions" : "JobEntityBatchExtensions";
            string methodAndArguments = GetMethodAndArguments(scheduleMode);
            string returnType = containsReturn ? "Unity.Jobs.JobHandle" : "void";
            string returnExpression = containsReturn ? $"return Unity.Entities.{staticExtensionsClass}.{methodAndArguments};" : $"Unity.Entities.{staticExtensionsClass}.{methodAndArguments};";

            string method =
                $@"[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    {returnType} {methodName}({fullTypeName} job, Unity.Entities.EntityQuery entityQuery, Unity.Jobs.JobHandle dependency)
                    {{
                        {GenerateUpdateCalls(assignments).SeparateBySemicolonAndNewLine()};{Environment.NewLine}
                        {GenerateAssignments(assignments).SeparateBySemicolonAndNewLine()};{Environment.NewLine}
                        {returnExpression};
                    }}";

            return (Syntax: (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(method), Name: methodName);
        }

        static string GetMethodAndArguments(ScheduleMode scheduleMode)
        {
            switch (scheduleMode)
            {
                case ScheduleMode.Schedule:
                    return "Schedule(job, entityQuery, dependency)";
                case ScheduleMode.ScheduleByRef:
                    return "ScheduleByRef(ref job, entityQuery, dependency)";
                case ScheduleMode.ScheduleParallel:
                    return "ScheduleParallel(job, entityQuery, dependency)";
				case ScheduleMode.ScheduleParallelByRef:
                    return "ScheduleParallelByRef(ref job, entityQuery, dependency)";
                case ScheduleMode.Run:
                    return "Run(job, entityQuery)";
                case ScheduleMode.RunByRef:
                    return "RunByRef(ref job, entityQuery)";
            }

            throw new ArgumentOutOfRangeException();
        }

        static IEnumerable<string> GenerateUpdateCalls(IEnumerable<(JobEntityParam JobEntityFieldToAssignTo, string ComponentTypeField)> assignments)
        {
            foreach (var assignment in assignments)
            {
                if (assignment.JobEntityFieldToAssignTo.RequiresTypeHandleFieldInSystemBase)
                {
                    yield return $"{assignment.ComponentTypeField}.Update(this)";
                }
            }
        }

        static IEnumerable<string> GenerateAssignments(IEnumerable<(JobEntityParam JobEntityFieldToAssignTo, string AssignmentValue)> assignments)
        {
            if (assignments.Any(assignment => assignment.JobEntityFieldToAssignTo.RequiresEntityManagerAccess))
            {
                yield return "job.__EntityManager = EntityManager";
            }

            foreach (var assignment in assignments)
            {
                yield return $"job.{assignment.JobEntityFieldToAssignTo.FieldName} = {assignment.AssignmentValue}";
            }
        }
    }
}
