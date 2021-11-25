using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGeneratorCommon
{
    public partial class JobEntityDescription
    {
        public readonly string FullTypeName;
        public readonly bool Valid;
        public readonly JobEntityParam[] UserExecuteMethodParams;
        public readonly IMethodSymbol[] UserExecuteMethods;
        public readonly string UserExecuteMethodSignature;
        public readonly IMethodSymbol[] ExecuteMethods;

        INamedTypeSymbol JobEntityType;
        string TypeName;
        INamespaceOrTypeSymbol[] Parents;

        public bool HasEntityInQueryIndex => UserExecuteMethodParams.Any(e => e is JobEntityParam_EntityInQueryIndex);

        public JobEntityDescription(BaseTypeDeclarationSyntax candidate, SemanticModel semanticModel) :
            this(semanticModel.GetDeclaredSymbol(candidate), semanticModel)
        {
        }

        public JobEntityDescription(INamedTypeSymbol jobEntityType, SemanticModel semanticModel)
        {
            JobEntityType = jobEntityType;

            // Extract Execute method arguments
            ExecuteMethods = JobEntityType.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name == "Execute").ToArray();
            FullTypeName = JobEntityType.ToFullName();

            // The full JobEntity struct can contain two Execute methods. The user written one, and our generated one.
            if (ExecuteMethods.Length == 0 || ExecuteMethods.Length > 2)
            {
                Valid = false;
                return;
            }

            UserExecuteMethods = ExecuteMethods.Where(m => !m.IsInterfaceImplementation()).ToArray();
            if (UserExecuteMethods.Length > 1)
            {
                Valid = false;
                return;
            }

            var userExecuteMethod = UserExecuteMethods.Single();
            UserExecuteMethodSignature = $"{userExecuteMethod.Name}({userExecuteMethod.Parameters.Select(p => $"{p.Type.Name} {p.Name}").SeparateByComma().TrimEnd('\n')})";
            UserExecuteMethodParams =
                userExecuteMethod
                    .Parameters
                    .Select(p => JobEntityParam.Create(p, semanticModel))
                    .ToArray();

            TypeName = JobEntityType.Name;
            FullTypeName = JobEntityType.ToFullName();
            Parents = JobEntityType.GetParentsFromMostToLeastNested().ToArray();

            Valid = CheckValidity();
        }

        bool CheckValidity()
        {
            int numValidEntityInQueryIndices = 0;

            foreach (var executeParam in UserExecuteMethodParams)
            {
                switch (executeParam)
                {
                    case JobEntityParam_EntityInQueryIndex entityInQueryIndex:
                    {
                        if (!entityInQueryIndex.IsInt)
                        {
                            return false;
                        }

                        if (numValidEntityInQueryIndices == 0)
                        {
                            numValidEntityInQueryIndices = 1;
                            continue;
                        }
                        return false;
                    }
                    case JobEntityParam_ValueTypesPassedWithDefaultArguments _:
                        return false;
                    case null:
                        return false;
                }
            }
            return true;
        }

        public void OutputErrors(SystemGeneratorContext systemTypeGeneratorContext)
        {
            if (ExecuteMethods.Length > 2 || ExecuteMethods.Length == 0)
            {
                JobEntityGeneratorWarningsAndErrors.IJE_DC0003(
                    systemTypeGeneratorContext,
                    JobEntityType.Locations.First(),
                    FullTypeName,
                    ExecuteMethods.Length);
                return;
            }

            if (UserExecuteMethods.Length > 1)
            {
                JobEntityGeneratorWarningsAndErrors.IJE_DC0006(
                    systemTypeGeneratorContext,
                    JobEntityType.Locations.First(),
                    FullTypeName,
                    ExecuteMethods.Length);
                return;
            }

            int validEntityInQueryIndexCount = 0;

            foreach (var param in UserExecuteMethodParams)
            {
                switch (param)
                {
                    case JobEntityParam_ValueTypesPassedWithDefaultArguments passedWithDefaultArguments:
                    {
                        JobEntityGeneratorWarningsAndErrors.IJE_DC0001(
                            systemTypeGeneratorContext,
                            passedWithDefaultArguments.ParameterSymbol.Locations.Single(),
                            FullTypeName,
                            UserExecuteMethodSignature,
                            passedWithDefaultArguments.ParameterSymbol.Name,
                            passedWithDefaultArguments.ParameterSymbol.GetSymbolTypeName());
                        break;
                    }
                    case JobEntityParam_EntityInQueryIndex entityInQueryIndex:
                    {
                        if (entityInQueryIndex.IsInt)
                        {
                            if (validEntityInQueryIndexCount == 0)
                            {
                                validEntityInQueryIndexCount = 1;
                                continue;
                            }
                            JobEntityGeneratorWarningsAndErrors.IJE_DC0005(
                                systemTypeGeneratorContext,
                                entityInQueryIndex.ParameterSymbol.Locations.Single(),
                                FullTypeName,
                                UserExecuteMethodSignature);
                            continue;
                        }

                        JobEntityGeneratorWarningsAndErrors.IJE_DC0004(
                            systemTypeGeneratorContext,
                            entityInQueryIndex.ParameterSymbol.Locations.Single(),
                            FullTypeName,
                            UserExecuteMethodSignature,
                            entityInQueryIndex.ParameterSymbol.Name);
                        continue;
                    }
                    default:
                        continue;
                }
            }
        }
    }

    public class JobEntityParam_SharedComponent : JobEntityParam
    {
        internal JobEntityParam_SharedComponent(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            VariableName = $"{char.ToLowerInvariant(TypeName[0])}{TypeName.Substring(1)}Data";
            RequiresEntityManagerAccess = true;
            RequiresTypeHandleFieldInSystemBase = false;

            VariableDeclarationText = $"var {VariableName} = batch.GetSharedComponentData({FieldName}, __EntityManager);";
            FieldText = $"{(IsReadOnly ? "[Unity.Collections.ReadOnly]" : "")}public Unity.Entities.SharedComponentTypeHandle<{FullyQualifiedTypeName}> {FieldName};";
            JobEntityFieldAssignment = $"GetSharedComponentTypeHandle<{FullyQualifiedTypeName}>()";

            ExecuteArgumentText = parameterSymbol.RefKind switch
            {
                RefKind.Ref => $"ref {VariableName}",
                RefKind.In => $"in {VariableName}",
                _ => VariableName
            };
        }
    }

    public class JobEntityParam_Entity : JobEntityParam
    {
        internal JobEntityParam_Entity(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            RequiresTypeHandleFieldInSystemBase = false;
            RequiresLocalCode = true;

            FieldName = "__EntityTypeHandle";
            VariableName = "entityPointer";

            const string localName = "entity";
            LocalCodeText = $"var {localName} = InternalCompilerInterface.UnsafeGetCopyOfNativeArrayPtrElement<Entity>({VariableName}, i);";

            VariableDeclarationText = $@"var {VariableName} = InternalCompilerInterface.UnsafeGetChunkEntityArrayIntPtr(batch, {FieldName});";
            FieldText = "[Unity.Collections.ReadOnly] public Unity.Entities.EntityTypeHandle __EntityTypeHandle;";
            ExecuteArgumentText = localName;

            JobEntityFieldAssignment = "GetEntityTypeHandle()";
        }
    }

    public class JobEntityParam_ValueTypesPassedWithDefaultArguments : JobEntityParam
    {
        internal JobEntityParam_ValueTypesPassedWithDefaultArguments(IParameterSymbol parameterSymbol) : base(parameterSymbol)
        {
            RequiresTypeHandleFieldInSystemBase = false;
            ExecuteArgumentText = "default";

            var localName = $"default{parameterSymbol.Name}";
            LocalCodeText = $"var {localName} = default({parameterSymbol.Type.Name});";
            ExecuteArgumentText = parameterSymbol.RefKind switch
            {
                RefKind.Ref => $"ref {localName}",
                RefKind.In => $"in {localName}",
                _ => localName
            };
        }
    }

    public class JobEntityParam_DynamicBuffer : JobEntityParam
    {
        public readonly ITypeSymbol BufferArgumentType;
        internal JobEntityParam_DynamicBuffer(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            BufferArgumentType = ((INamedTypeSymbol)TypeSymbol).TypeArguments.First();
            TypeSymbol = BufferArgumentType;

            VariableName = $"{parameterSymbol.Name}BufferAccessor";

            var localName = $"retrievedByIndexIn{VariableName}";
            LocalCodeText = $"var {localName} = {VariableName}[i];";
            FieldName = $"__{BufferArgumentType.ToFullName().Replace('.', '_')}TypeHandle";

            RequiresTypeHandleFieldInSystemBase = false;
            RequiresLocalCode = true;

            VariableDeclarationText = $"var {VariableName} = batch.GetBufferAccessor({FieldName});";
            FieldText = $"public Unity.Entities.BufferTypeHandle<{BufferArgumentType.ToFullName()}> {FieldName};";

            JobEntityFieldAssignment = $"GetBufferTypeHandle<{BufferArgumentType}>({(IsReadOnly ? "true" : "false")})";

            ExecuteArgumentText = parameterSymbol.RefKind switch
            {
                RefKind.Ref => $"ref {localName}",
                RefKind.In => $"in {localName}",
                _ => localName
            };
        }
    }

    public class JobEntityParam_ManagedComponent : JobEntityParam
    {
        internal JobEntityParam_ManagedComponent(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            RequiresTypeHandleFieldInSystemBase = false;
            RequiresEntityManagerAccess = true;
            RequiresLocalCode = true;
            VariableName = $"{parameterSymbol.Name}ManagedComponentAccessor";
            var localName = $"retrievedByIndexIn{VariableName}";
            LocalCodeText = $"var {localName} = {VariableName}[i];";

            VariableDeclarationText = $"var {VariableName} = batch.GetManagedComponentAccessor({FieldName}, __EntityManager);";
            FieldText = $"{(IsReadOnly ? "[Unity.Collections.ReadOnly]" : "")}public Unity.Entities.ComponentTypeHandle<{FullyQualifiedTypeName}> {FieldName};";

            JobEntityFieldAssignment = $"EntityManager.GetComponentTypeHandle<{FullyQualifiedTypeName}>({(IsReadOnly ? "true" : "false")})";

            ExecuteArgumentText = localName;

            // We do not allow managed components to be used by ref. See SourceGenerationErrors.DC0024.
            if(parameterSymbol.RefKind == RefKind.In)
            {
                ExecuteArgumentText = $"in {localName}";
            }
        }
    }

    public class JobEntityParam_ComponentData : JobEntityParam
    {
        internal JobEntityParam_ComponentData(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            VariableName = $"{char.ToLowerInvariant(TypeName[0])}{TypeName.Substring(1)}Data";
            RequiresTypeHandleFieldInSystemBase = true;
            RequiresLocalCode = true;
            FieldText = $"{(IsReadOnly ? "[Unity.Collections.ReadOnly]" : "")}public Unity.Entities.ComponentTypeHandle<{FullyQualifiedTypeName}> {FieldName};";

            if (IsReadOnly)
                VariableDeclarationText = $"var {VariableName} = InternalCompilerInterface.UnsafeGetChunkNativeArrayReadOnlyIntPtr<{FullyQualifiedTypeName}>(batch, {FieldName});";
            else
                VariableDeclarationText = $"var {VariableName} = InternalCompilerInterface.UnsafeGetChunkNativeArrayIntPtr<{FullyQualifiedTypeName}>(batch, {FieldName});";

            var localName = $"{VariableName}__ref";
            LocalCodeText = $"ref var {localName} = ref InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<{FullyQualifiedTypeName}>({VariableName}, i);";

            ExecuteArgumentText = parameterSymbol.RefKind switch
            {
                RefKind.Ref => $"ref {localName}",
                RefKind.In => $"in {localName}",
                _ => VariableName
            };
        }
    }

    class JobEntityParam_TagComponent : JobEntityParam
    {
        internal JobEntityParam_TagComponent(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            RequiresTypeHandleFieldInSystemBase = false;
            ExecuteArgumentText = "default";
        }
    }

    class JobEntityParam_EntityInQueryIndex : JobEntityParam
    {
        public bool IsInt => TypeSymbol.IsInt();

        internal JobEntityParam_EntityInQueryIndex(IParameterSymbol parameterSymbol)
            : base(parameterSymbol)
        {
            RequiresTypeHandleFieldInSystemBase = false;
            RequiresLocalCode = true;
            LocalCodeText = "var entityInQueryIndex = indexOfFirstEntityInQuery + i;";
            ExecuteArgumentText = "entityInQueryIndex";
            IsQueryableType = false;
        }
    }

    public class JobEntityParam
    {
        public bool RequiresEntityManagerAccess { get; protected set; }
        public bool RequiresLocalCode { get; protected set; }
        public bool RequiresTypeHandleFieldInSystemBase { get; protected set; } = true;
        public string FullyQualifiedTypeName { get; }
        public string TypeName { get; }

        public IParameterSymbol ParameterSymbol { get; }
        public ITypeSymbol TypeSymbol { get; protected set; }

        public string FieldName { get; protected set; }
        public string VariableName { get; protected set; }

        public bool IsReadOnly { get; }
        public string LocalCodeText { get; protected set; }
        public string FieldText { get; protected set; }
        public string VariableDeclarationText { get; protected set; }
        public string ExecuteArgumentText { get; protected set; }
        public string JobEntityFieldAssignment { get; protected set; }

        public bool IsQueryableType { get; protected set; } = true;

        internal JobEntityParam(IParameterSymbol parameterSymbol)
        {
            ParameterSymbol = parameterSymbol;
            TypeSymbol = parameterSymbol.Type;
            FullyQualifiedTypeName = TypeSymbol.GetSymbolTypeName();
            TypeName = TypeSymbol.Name;
            FieldName = $"__{TypeName}TypeHandle";
            IsReadOnly = parameterSymbol.IsReadOnly();
        }

        public static JobEntityParam Create(IParameterSymbol parameterSymbol, SemanticModel semanticModel)
        {
            var typeSymbol = parameterSymbol.Type;

            foreach (var attribute in parameterSymbol.GetAttributes())
            {
                if (attribute.AttributeClass.ToFullName() == "Unity.Entities.EntityInQueryIndex")
                {
                    return new JobEntityParam_EntityInQueryIndex(parameterSymbol);
                }
            }

            if (typeSymbol.InheritsFromInterface("Unity.Entities.ISharedComponentData"))
                return new JobEntityParam_SharedComponent(parameterSymbol);

            if (typeSymbol.Is("Unity.Entities.Entity"))
                return new JobEntityParam_Entity(parameterSymbol);

            if (typeSymbol.IsDynamicBuffer())
                return new JobEntityParam_DynamicBuffer(parameterSymbol);

            if (!typeSymbol.IsValueType
                && typeSymbol.InheritsFromInterface("Unity.Entities.IComponentData")
                | typeSymbol.InheritsFromType("UnityEngine.Behaviour"))
            {
                return new JobEntityParam_ManagedComponent(parameterSymbol);
            }

            if (typeSymbol.IsValueType)
            {
                if (typeSymbol.InheritsFromInterface("Unity.Entities.IComponentData"))
                {
                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Any())
                    {
                        //SourceGenerationErrors.DC0050(context, paramSyntax.GetLocation(), typeSymbol.Name);
                        return null;
                    }

                    if (typeSymbol.GetMembers().OfType<IFieldSymbol>().Any())
                    {
                        return new JobEntityParam_ComponentData(parameterSymbol);
                    }
                    return new JobEntityParam_TagComponent(parameterSymbol);
                }

                if (typeSymbol.InheritsFromInterface("Unity.Entities.IBufferElementData"))
                {
                    //SourceGenerationErrors.DC0033(context, paramSyntax.GetLocation(), paramSyntax.Identifier.ValueText, typeSymbol.Name);
                }
                else
                {
                    switch (typeSymbol)
                    {
                        case ITypeParameterSymbol _:
                            // SourceGenerationErrors.DC0050(context, paramSyntax.GetLocation(), typeSymbol.Name);
                            break;
                        case INamedTypeSymbol { IsValueType: true } :
                            return new JobEntityParam_ValueTypesPassedWithDefaultArguments(parameterSymbol);
                        default:
                            //SourceGenerationErrors.DC0021(context, paramSyntax.GetLocation(), paramSyntax.Identifier.ValueText, typeSymbol.Name);
                            break;
                    }
                }
                return null;
            }

            //SourceGenerationErrors.DC0069(context, paramSyntax.GetLocation(), typeSymbol.ToFullName(), paramSyntax.Identifier.ValueText);
            return null;
        }
    }
}
