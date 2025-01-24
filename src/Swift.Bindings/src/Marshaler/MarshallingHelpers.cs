// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace BindingsGeneration
{
    public static class MarshallingHelpers // TODO: Find better place for those
    {
        public static bool MethodRequiresIndirectResult(MethodDecl decl, ITypeDatabase typeDatabase)
        {
            if (decl.IsConstructor && !(decl.ParentDecl is StructDecl structDecl && StructIsMarshalledAsCSStruct(structDecl))) return true;
            var returnType = decl.CSSignature.First();

            if (returnType.IsGeneric) return true;
            if (returnType.SwiftTypeSpec.IsEmptyTuple) return false;

            if (!ArgumentIsMarshalledAsCSStruct(returnType, typeDatabase)) return true;
            return false;
        }

        public static bool MethodRequiresSwiftSelf(MethodDecl decl)
        {
            if (decl.ParentDecl is ModuleDecl) return false; // global funcs
            if (decl.MethodType == MethodType.Static) return false;
            if (decl.IsConstructor) return false;

            return true;
        }

        public static bool StructIsMarshalledAsCSStruct(StructDecl decl)
        {
            return decl is StructDecl structDecl && structDecl.IsFrozen && structDecl.IsBlittable;
        }

        public static bool ArgumentIsMarshalledAsCSStruct(ArgumentDecl argumentDecl, ITypeDatabase typeDatabase)
        {
            var typeRecord = typeDatabase.GetTypeRecordOrThrow(argumentDecl.SwiftTypeSpec);
            return typeRecord.IsFrozen && typeRecord.IsBlittable;
        }
    }
}
