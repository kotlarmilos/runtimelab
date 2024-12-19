// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Swift.Runtime;

namespace BindingsGeneration
{
    public static class MarshallingHelpers // TODO: Find better place for those
    {
        public static bool MethodRequiresIndirectResult(MethodDecl methodDecl, BaseDecl parentDecl, TypeDatabase typeDatabase)
        {
            if (methodDecl.IsConstructor && !(parentDecl is StructDecl structDecl && StructIsMarshalledAsCSStruct(structDecl))) return true;
            var returnType = methodDecl.CSSignature.First();

            if (returnType.Name == "") return false; // TODO: Void should be handled differently

            if (!ArgumentIsMarshalledAsCSStruct(returnType, typeDatabase)) return true;
            return false;
        }

        public static bool MethodRequiresSwiftSelf(MethodDecl methodDecl, BaseDecl parentDecl)
        {
            if (parentDecl is ModuleDecl) return false; // global funcs
            if (methodDecl.MethodType == MethodType.Static) return false;
            if (methodDecl.IsConstructor) return false;

            return true;
        }

        public static bool StructIsMarshalledAsCSStruct(StructDecl decl)
        {
            return decl is StructDecl structDecl && structDecl.IsFrozen && structDecl.IsBlittable;
        }

        public static bool ArgumentIsMarshalledAsCSStruct(ArgumentDecl argumentDecl, TypeDatabase typeDatabase)
        {
            var typeRecord = GetType(argumentDecl, typeDatabase.Registrar) ?? throw new NotImplementedException($"Type {argumentDecl.Name} not found in type database"); //TODO: Add information to typeRecord if thing is a struct
            return typeRecord.IsFrozen && typeRecord.IsBlittable;
        }

        public static TypeRecord? GetType(ArgumentDecl argumentDecl, TypeRegistrar typeRegistrar)
        {
            if (argumentDecl.SwiftTypeSpec is NamedTypeSpec namedTypeSpec)
            {
                var typeRecord = typeRegistrar.GetType(namedTypeSpec.Module, namedTypeSpec.NameWithoutModule);
                return typeRecord;
            }

            if (argumentDecl.SwiftTypeSpec is TupleTypeSpec tupleTypeSpec)
            {
                // Void is an empty tuple
                var typeRecord = typeRegistrar.GetType("Swift", tupleTypeSpec.ToString(true));
                return typeRecord;
            }

            throw new NotImplementedException($"{argumentDecl} is not a NamedTypeSpec nor TupleTypeSpec");
        }
    }
}
