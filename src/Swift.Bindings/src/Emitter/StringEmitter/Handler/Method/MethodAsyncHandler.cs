// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;

namespace BindingsGeneration
{
    /// <summary>
    /// Represents an async method handler factory.
    /// </summary>
    public class MethodAsyncHandlerFactory : IFactory<BaseDecl, IMethodHandler>
    {
        /// <summary>
        /// Checks if the factory can handle the declaration.
        /// </summary>
        /// <param name="decl">The base declaration.</param>
        /// <returns></returns>
        public bool Handles(BaseDecl decl)
        {
            return decl is MethodDecl methodDecl && methodDecl.IsAsync;
        }

        /// <summary>
        /// Constructs a handler.
        /// </summary>
        public IMethodHandler Construct()
        {
            return new AsyncMethodHandler();
        }
    }

    /// <summary>
    /// Represents an async method handler.
    /// </summary>
    public class AsyncMethodHandler : MethodHandler, IMethodHandler
    {
        public AsyncMethodHandler()
        {
        }

        /// <summary>
        /// Emits the method declaration.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        /// <param name="env">The environment.</param>
        /// <param name="conductor">The conductor instance.</param>
        public override void Emit(CSharpWriter csWriter, SwiftWriter swiftWriter, IEnvironment env, Conductor conductor)
        {
            // TODO: Simplify marshalling logic and move to Marshal method/Environment
            MethodEnvironment methodEnv = (MethodEnvironment)env;
            ModuleDecl moduleDecl = methodEnv.MethodDecl.ModuleDecl ?? throw new ArgumentNullException(nameof(methodEnv.MethodDecl.ModuleDecl));
            SignatureHandler signatureHandler = new SignatureHandler(methodEnv);
            PInvokeBuilder pinvokeBuilder = new PInvokeBuilder();
            WrapperBuilder wrapperBuilder = new WrapperBuilder();

            // TODO: Move this to Marshal method
            if (signatureHandler.GetWrapperSignature().ContainsPlaceholder)
            {
                Console.WriteLine($"Method {methodEnv.MethodDecl.Name} has unsupported signature: ({signatureHandler.GetWrapperSignature().ParametersExpression()}) -> {signatureHandler.GetWrapperSignature().ReturnType}");
                return;
            }

            string pinvokeName = NameProvider.GetPInvokeName(methodEnv.MethodDecl);
            string libPath = methodEnv.TypeDatabase.GetLibraryPath(moduleDecl.Name);
            Signature pinvokeSignature = signatureHandler.GetPInvokeSignature();
            Signature wrapperSignature = signatureHandler.GetWrapperSignature();

            var staticKeyword = methodEnv.MethodDecl.MethodType == MethodType.Static || methodEnv.ParentDecl is ModuleDecl ? "static " : "";
            var unsafeKeyword = "unsafe ";
            string[] modifiers = new string[] { unsafeKeyword, staticKeyword }.Where(m => !string.IsNullOrEmpty(m)).ToArray();

            string[] generics = methodEnv.MethodDecl.GenericParameters.Select(p => methodEnv.GenericTypeMapping[p.TypeName].PlaceholderName.ToString()).ToArray();

            string [] payloadNames = methodEnv.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric).Select(a => methodEnv.GenericTypeMapping[a.SwiftTypeSpec.ToString()].PayloadName).ToArray();
            string [] metadataNames = methodEnv.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric).Select(a => methodEnv.GenericTypeMapping[a.SwiftTypeSpec.ToString()].MetadataName).ToArray();
            string [] typeNames = methodEnv.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric).Select(a => methodEnv.GenericTypeMapping[a.SwiftTypeSpec.ToString()].PlaceholderName).ToArray();
            string [] argumentNames = methodEnv.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric).Select(a => a.Name).ToArray();

            string swiftSelf = string.Empty;
            if (methodEnv.ParentDecl is StructDecl structDecl && MarshallingHelpers.StructIsMarshalledAsCSStruct(structDecl))
                swiftSelf = methodEnv.ParentDecl.Name;

            var voidReturn = methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple;
            var hasReturn = !(methodEnv.RequiresIndirectResult || voidReturn);

            var pinvokeParameters = new[]
            {
                "IntPtr callback",
                "IntPtr context",
                "IntPtr task",
            }.Concat(pinvokeSignature.ParametersExpression()).ToArray();

            var pinvokeArguments = new[]
            {
                $"(IntPtr)s_{methodEnv.MethodDecl.Name}Callback",
                "IntPtr.Zero",
                "GCHandle.ToIntPtr(handle)",
            }.Concat(pinvokeSignature.ArgumentsExpression()).ToArray();

            csWriter.WriteLine($$"""
                {{pinvokeBuilder.EmitPInvokeSignature(libPath, $"{methodEnv.MethodDecl.Name}_async", pinvokeSignature.ReturnType, pinvokeName, pinvokeParameters)}}

                private static unsafe delegate* unmanaged[Cdecl]<{{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"{wrapperSignature.ReturnType}, ")}}IntPtr, void> s_{{methodEnv.MethodDecl.Name}}Callback = &{{methodEnv.MethodDecl.Name}}OnComplete;
                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
                private static void {{methodEnv.MethodDecl.Name}}OnComplete({{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"{wrapperSignature.ReturnType} result, ")}}IntPtr task)
                {
                    GCHandle handle = GCHandle.FromIntPtr(task);
                    try
                    {
                        if (handle.Target is TaskCompletionSource{{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{wrapperSignature.ReturnType}>")}} tcs)
                        {
                            tcs.TrySetResult({{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : "result")}});
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }

                {{wrapperBuilder.EmitMethodSignature(modifiers, $"Task{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{wrapperSignature.ReturnType}>")}", methodEnv.MethodDecl.Name, generics, wrapperSignature.ParametersExpression())}}
                {
                    TaskCompletionSource{{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{wrapperSignature.ReturnType}>")}} task = new TaskCompletionSource{{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{wrapperSignature.ReturnType}>")}}();
                    GCHandle handle = GCHandle.Alloc(task, GCHandleType.Normal);

                    {{wrapperBuilder.EmitDeclarationsForAllocations(payloadNames)}}
                    try
                    {
                        {{wrapperBuilder.EmitSwiftSelf(methodEnv, swiftSelf)}}
                        {{wrapperBuilder.EmitGenericArguments(metadataNames, typeNames, argumentNames, payloadNames)}}
                        {{wrapperBuilder.EmitPInvokeCall(false, NameProvider.GetPInvokeName(methodEnv.MethodDecl), pinvokeArguments)}}
                        {{wrapperBuilder.EmitSwiftError(methodEnv, methodEnv.MethodDecl.Name)}}
                        return task.Task;
                    }
                    finally { }
                }
            """);

            string parameters = string.Join(", ", new[] {$"callback: @escaping ({(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"{methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec}, ")}Int64) -> Void", $"task: Int64"}.Concat(methodEnv.MethodDecl.CSSignature.Skip(1).Select(p => p.Name + ": " + p.SwiftTypeSpec)));

            swiftWriter.WriteLine($$"""
            extension {{methodEnv.MethodDecl.ParentDecl!.Name}} {
                @_silgen_name("{{methodEnv.MethodDecl.Name}}_async")
                public {{(methodEnv.MethodDecl.MethodType == MethodType.Static ? "static " : "")}} func {{NameProvider.GetPInvokeName(methodEnv.MethodDecl)}}_async({{parameters}}) {
                    Task {
                        {{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : "let result = ")}}await {{(methodEnv.MethodDecl.MethodType == MethodType.Static ? $"{methodEnv.MethodDecl.ParentDecl.Name}." : "")}}{{methodEnv.MethodDecl.Name}}({{string.Join(", ", methodEnv.MethodDecl.CSSignature.Skip(1).Select(p => p.Name+": "+p.Name))}})
                        callback({{(methodEnv.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : "result, ")}}task)
                    }
                }
            }
            """);
        }
    }
}
