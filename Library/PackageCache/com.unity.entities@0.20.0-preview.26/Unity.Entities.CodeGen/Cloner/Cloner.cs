using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;
using Unity.Entities;
using Unity.Entities.CodeGen;

class Cloner : EntitiesILPostProcessor
{
    protected override bool PostProcessImpl(TypeDefinition[] componentSystemTypes)
    {
        var madeChange = false;
        foreach (var typeDef in componentSystemTypes)
        {
            var methodsToPatch =
                 typeDef.Methods
                        .Where(methodDef =>
                            methodDef.CustomAttributes.Any(attr =>
                                attr.AttributeType.Name == nameof(DOTSCompilerPatchedMethodAttribute)))
                        .ToArray();

            if (!methodsToPatch.Any())
                continue;

            var methodNameAndParamsToMethodDefs =
                typeDef.Methods.Where(methodDef => !methodsToPatch.Contains(methodDef)).ToDictionary(GetMethodNameAndParamsAsString, method => method);

            foreach (var method in methodsToPatch)
            {
                var attributeValue =
                    method.CustomAttributes
                        .First(attribute =>
                            attribute.AttributeType.Name == nameof(DOTSCompilerPatchedMethodAttribute))
                        .ConstructorArguments
                        .First()
                        .Value
                        .ToString();

                if (!methodNameAndParamsToMethodDefs.ContainsKey(attributeValue))
                    throw new InvalidOperationException(
                    $"Method Cloner ILPP: Cannot find method {attributeValue} in {typeDef.FullName}.  Method candidates are {string.Join(", ", methodNameAndParamsToMethodDefs.Keys)}");

                var destinationMethod = methodNameAndParamsToMethodDefs[attributeValue];
                foreach (var lambdaClass in destinationMethod.Body.Variables.Select(v => v.VariableType).OfType<TypeDefinition>().Where(IsDisplayClass))
                {
                    destinationMethod.DeclaringType.NestedTypes.Remove(lambdaClass);
                }

                destinationMethod.Body = method.Body;
                typeDef.Methods.Remove(method);

                var sequencePoints = destinationMethod.DebugInformation.SequencePoints;
                sequencePoints.Clear();

                foreach (var sp in method.DebugInformation.SequencePoints)
                    sequencePoints.Add(sp);

                destinationMethod.DebugInformation.Scope = method.DebugInformation.Scope;

                if (method.HasGenericParameters && destinationMethod.HasGenericParameters)
                {
                    destinationMethod.GenericParameters.Clear();
                    foreach (var genericParam in method.GenericParameters)
                    {
                        destinationMethod.GenericParameters.Add(genericParam);
                    }
                }
                madeChange = true;
            }
        }
        return madeChange;
    }

    static bool IsDisplayClass(TypeDefinition arg) => arg.Name.Contains("<>");

    protected override bool PostProcessUnmanagedImpl(TypeDefinition[] unmanagedComponentSystemTypes)
    {
        return false;
    }

    // Remove /& characters and `# for type arity
    static string CleanupTypeName(string typeName)
    {
        typeName = typeName.Replace('/', '.').Replace("&", "").Replace(" ", string.Empty);
        var indexOfArityStart = typeName.IndexOf('`');
        if (indexOfArityStart != -1)
        {
            var indexOfArityEnd = typeName.IndexOf('<');
            if (indexOfArityEnd != -1)
                return typeName.Remove(indexOfArityStart, indexOfArityEnd - indexOfArityStart);
        }

        return typeName;
    }

    static string GetMethodNameAndParamsAsString(MethodReference method)
    {
        var strBuilder = new StringBuilder();
        strBuilder.Append(method.Name);

        for (var typeIndex = 0; typeIndex < method.GenericParameters.Count; typeIndex++)
            strBuilder.Append($"_T{typeIndex}");

        foreach (var parameter in method.Parameters)
        {
            if (parameter.ParameterType.IsByReference)
            {
                if (parameter.IsIn)
                    strBuilder.Append($"_in");
                else if (parameter.IsOut)
                    strBuilder.Append($"_out");
                else
                    strBuilder.Append($"_ref");
            }


            strBuilder.Append($"_{CleanupTypeName(parameter.ParameterType.ToString())}");
        }

        return strBuilder.ToString();
    }
}
