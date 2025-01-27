// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;

namespace BindingsGeneration
{

    /// <summary>
    /// Represents a method handler factory.
    /// </summary>
    public class MethodHandlerFactory : IFactory<BaseDecl, IMethodHandler>
    {
        /// <summary>
        /// Checks if the factory can handle the declaration.
        /// </summary>
        /// <param name="decl">The base declaration.</param>
        /// <returns></returns>
        public bool Handles(BaseDecl decl)
        {
            return decl is MethodDecl;
        }

        /// <summary>
        /// Constructs a handler.
        /// </summary>
        public IMethodHandler Construct()
        {
            return new MethodHandler();
        }
    }

    /// <summary>
    /// Represents a method handler.
    /// </summary>
    public class MethodHandler : BaseHandler, IMethodHandler
    {
        public MethodHandler()
        {
        }

        /// <summary>
        /// Marshals the method declaration.
        /// </summary>
        /// <param name="methodDecl">The method declaration.</param>
        /// <param name="typeDatabase">The type database instance.</param>
        public IEnvironment Marshal(BaseDecl decl, ITypeDatabase typeDatabase)
        {
            if (decl is not MethodDecl methodDecl)
            {
                throw new ArgumentException("The provided decl must be a MethodDecl.", nameof(decl));
            }
            return new MethodEnvironment(methodDecl, typeDatabase);
        }

        /// <summary>
        /// Emits the method declaration.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        /// <param name="env">The environment.</param>
        /// <param name="conductor">The conductor instance.</param>
        public void Emit(CSharpWriter csWriter, SwiftWriter swiftWriter, IEnvironment env, Conductor conductor)
        {
            var methodEnv = (MethodEnvironment)env;
            var signatureHandler = new SignatureHandler(methodEnv);

            if (methodEnv.MethodDecl.GenericParameters.Any(x => x.Constraints.Count > 0))
            {
                Console.WriteLine($"Method {methodEnv.MethodDecl.Name} has unsupported generic constraints");
                return;
            }

            if (signatureHandler.GetWrapperSignature().ContainsPlaceholder)
            {
                Console.WriteLine($"Method {methodEnv.MethodDecl.Name} has unsupported signature: ({signatureHandler.GetWrapperSignature().ParametersString()}) -> {signatureHandler.GetWrapperSignature().ReturnType}");
                return;
            }

            var wrapperEmitter = new WrapperEmitter(methodEnv, signatureHandler);
            wrapperEmitter.Emit(csWriter, swiftWriter);
            PInvokeEmitter.EmitPInvoke(csWriter, methodEnv, signatureHandler);
            csWriter.WriteLine();
        }
    }

    /// <summary>
    /// Represents a parameter.
    /// </summary>
    /// <param name="Type"></param>
    /// <param name="Name"></param>
    public record Parameter(string Type, string Name, string modifier = "")
    {
        public string CallString() => $"{Type} {Name}";
        public string SignatureString() => $"{modifier} {Type} {Name}";
    }

    /// <summary>
    /// Represents a signature.
    /// </summary>
    /// <param name="ReturnType"></param>
    /// <param name="Parameters"></param>
    public record Signature(string ReturnType, IReadOnlyList<Parameter> Parameters)
    {
        public bool ContainsPlaceholder => Parameters.Any(p => p.Type == "AnyType") || ReturnType == "AnyType";
        public string ParametersString() => string.Join(", ", Parameters.Select(p => p.SignatureString()));

        public string CallArgumentsString() => string.Join(", ", Parameters.Select(p => GetCallArgumentString(p)));

        public static string GetCallArgumentString(Parameter parameter)
        {
            return parameter switch
            {
                { Type: "SwiftHandle" } => $"{parameter.Name}.Payload",
                { modifier: "out" } => $"out var {parameter.Name}",
                _ => parameter.Name
            };
        }
    }

    public class WrapperSignatureBuilder
    {
        private string _returnType = "invalid";
        private readonly List<Parameter> _parameters = new();
        private readonly MethodEnvironment _env;

        public WrapperSignatureBuilder(MethodEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Handles the return type of the method.
        /// </summary>
        public void HandleReturnType()
        {
            var argument = _env.MethodDecl.CSSignature.First();
            if (argument.IsGeneric)
            {
                var placeholderName = _env.GenericTypeMapping[argument.SwiftTypeSpec.ToString()].PlaceholderName;
                SetReturnType(placeholderName);
            }
            else
            {
                var typeRecord = _env.TypeDatabase.GetTypeRecordOrAnyType(argument.SwiftTypeSpec);
                SetReturnType(typeRecord.CSTypeIdentifier);
            }
        }

        /// <summary>
        /// Handles the arguments of the method.
        /// </summary>
        public void HandleArguments()
        {
            foreach (var argument in _env.MethodDecl.CSSignature.Skip(1))
            {
                if (argument.IsGeneric)
                {
                    var placeholderName = _env.GenericTypeMapping[argument.SwiftTypeSpec.ToString()].PlaceholderName;
                    AddParameter(placeholderName, argument.Name);
                }
                else
                {
                    var typeRecord = _env.TypeDatabase.GetTypeRecordOrAnyType(argument.SwiftTypeSpec);
                    AddParameter(typeRecord.CSTypeIdentifier, argument.Name);
                }
            }
        }

        /// <summary>
        /// Builds the PInvoke signature.
        /// </summary>
        /// <returns>The PInvoke signature.</returns>
        public Signature Build()
        {
            return new Signature(_returnType, _parameters.ToArray());
        }


        /// <summary>
        /// Sets the return type of the method.
        /// </summary>
        /// <param name="returnType">The return type.</param>
        private void SetReturnType(string returnType)
        {
            _returnType = returnType;
        }

        /// <summary>
        /// Adds a parameter to the PInvoke signature.
        /// </summary>
        /// <param name="type">The parameter type.</param>
        /// <param name="name">The parameter name.</param>s
        private void AddParameter(string type, string name)
        {
            _parameters.Add(new Parameter(type, name));
        }
    }

    /// <summary>
    /// Represents a PInvoke signature builder.
    /// </summary>
    public class PInvokeSignatureBuilder
    {
        private string _returnType = "invalid";
        private readonly List<Parameter> _parameters = new();
        private readonly MethodEnvironment _env;

        /// <summary>
        /// Initializes a new instance of the <see cref="PInvokeSignatureBuilder"/> class.
        /// </summary>
        /// <param name="methodDecl">The method declaration.</param>
        /// <param name="parentDecl">The parent declaration.</param>
        /// <param name="typeDatabase">The type database.</param>
        public PInvokeSignatureBuilder(MethodEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Handles the return type of the method.
        /// </summary>
        public void HandleReturnType()
        {
            if (!MarshallingHelpers.MethodRequiresIndirectResult(_env))
            {
                var returnTypeRecord = _env.TypeDatabase.GetTypeRecordOrThrow(_env.MethodDecl.CSSignature.First().SwiftTypeSpec);
                SetReturnType(returnTypeRecord.CSTypeIdentifier);
            }
            else
            {
                AddParameter("SwiftIndirectResult", "swiftIndirectResult");
                SetReturnType("void");
            }
        }

        /// <summary>
        /// Handles the arguments of the method.
        /// </summary>
        public void HandleArguments()
        {
            foreach (var argument in _env.MethodDecl.CSSignature.Skip(1))
            {
                if (argument.IsGeneric)
                {
                    var payloadName = _env.GenericTypeMapping[argument.SwiftTypeSpec.ToString()].PayloadName;
                    AddParameter("IntPtr", payloadName);
                }
                else if (MarshallingHelpers.ArgumentIsMarshalledAsCSStruct(argument, _env.TypeDatabase))
                {
                    var argumentTypeRecord = _env.TypeDatabase.GetTypeRecordOrThrow(argument.SwiftTypeSpec);
                    AddParameter(argumentTypeRecord.CSTypeIdentifier, argument.Name);
                }
                else
                {
                    AddParameter("SwiftHandle", argument.Name);
                }
            }
        }

        /// <summary>
        /// Handles the metadata of generic arguments.
        /// </summary>
        public void HandleGenericMetadata()
        {
            foreach (var genericParameter in _env.MethodDecl.GenericParameters)
            {
                var metadataName = _env.GenericTypeMapping[genericParameter.TypeName].MetadataName;
                AddParameter("TypeMetadata", metadataName);
            }
        }

        /// <summary>
        /// Handles the SwiftSelf parameter of the method.
        /// </summary>
        public void HandleSwiftSelf()
        {
            if (MarshallingHelpers.MethodRequiresSwiftSelf(_env))
            {
                if (_env.ParentDecl is StructDecl structDecl && MarshallingHelpers.StructIsMarshalledAsCSStruct(structDecl))
                {
                    AddParameter($"SwiftSelf<{_env.ParentDecl.Name}>", "self");
                }
                else
                {
                    AddParameter("SwiftSelf", "self");
                }
            }
        }

        /// <summary>
        /// Handles the SwiftError parameter of the method.
        /// </summary>
        public void HandleSwiftError()
        {
            if (_env.MethodDecl.Throws)
            {
                AddParameter("SwiftError", "error", "out");
            }
        }

        /// <summary>
        /// Builds the PInvoke signature.
        /// </summary>
        /// <returns>The PInvoke signature.</returns>
        public Signature Build()
        {
            return new Signature(_returnType, _parameters.ToArray());
        }

        /// <summary>
        /// Sets the return type of the method.
        /// </summary>
        /// <param name="returnType">The return type.</param>
        private void SetReturnType(string returnType)
        {
            _returnType = returnType;
        }

        /// <summary>
        /// Adds a parameter to the PInvoke signature.
        /// </summary>
        /// <param name="type">The parameter type.</param>
        /// <param name="name">The parameter name.</param>s
        private void AddParameter(string type, string name, string modifier = "")
        {
            _parameters.Add(new Parameter(type, name, modifier));
        }
    }

    /// <summary>
    /// Provides methods for handling method signatures.
    /// </summary>
    public class SignatureHandler
    {
        private Signature? _pInvokeSignature;
        private Signature? _wrapperSignature;
        private readonly MethodEnvironment _env;

        public SignatureHandler(MethodEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Gets the PInvoke signature.
        /// </summary>
        /// <returns>The PInvoke signature.</returns>
        public Signature GetPInvokeSignature()
        {
            if (_pInvokeSignature == null)
            {
                var pInvokeSignature = new PInvokeSignatureBuilder(_env);
                pInvokeSignature.HandleReturnType();
                pInvokeSignature.HandleArguments();
                pInvokeSignature.HandleGenericMetadata();
                pInvokeSignature.HandleSwiftSelf();
                pInvokeSignature.HandleSwiftError();
                _pInvokeSignature = pInvokeSignature.Build();
            }
            return _pInvokeSignature;
        }

        /// <summary>
        /// Gets the wrapper method signature.
        /// </summary>
        /// <returns>The wrapper method signature.</returns>
        public Signature GetWrapperSignature()
        {
            if (_wrapperSignature == null)
            {
                var wrapperSignature = new WrapperSignatureBuilder(_env);
                wrapperSignature.HandleReturnType();
                wrapperSignature.HandleArguments();
                _wrapperSignature = wrapperSignature.Build();
            }
            return _wrapperSignature;
        }
    }

    /// <summary>
    /// Provides methods for emitting PInvoke signatures.
    /// </summary>
    internal static class PInvokeEmitter
    {
        /// <summary>
        /// Emits the PInvoke signature.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        /// <param name="methodEnv">The method environment.</param>
        public static void EmitPInvoke(CSharpWriter csWriter, MethodEnvironment methodEnv, SignatureHandler signatureHandler)
        {
            var methodDecl = (MethodDecl)methodEnv.MethodDecl;
            var moduleDecl = methodDecl.ModuleDecl ?? throw new ArgumentNullException(nameof(methodDecl.ModuleDecl));

            var pInvokeName = NameProvider.GetPInvokeName(methodDecl);
            var libPath = methodEnv.TypeDatabase.GetLibraryPath(moduleDecl.Name);

            var pInvokeSignature = signatureHandler.GetPInvokeSignature();

            csWriter.WriteLine("[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]");
            if (methodDecl.IsAsync)
            {
                string parameters = string.Join(", ",
                    new[]
                    {
                        "IntPtr callback",
                        "IntPtr context",
                        "IntPtr task",
                        pInvokeSignature.ParametersString()
                    }.Where(arg => !string.IsNullOrEmpty(arg))
                );
                csWriter.WriteLine($"[DllImport(\"{libPath}\", EntryPoint = \"{methodDecl.Name}_async\")]");
                csWriter.WriteLine($"private static extern void {pInvokeName}({parameters});");
            }
            else
            {
                csWriter.WriteLine($"[DllImport(\"{libPath}\", EntryPoint = \"{methodDecl.MangledName}\")]");
                csWriter.WriteLine($"private static extern {pInvokeSignature.ReturnType} {pInvokeName}({pInvokeSignature.ParametersString()});");
            }
        }
    }

    /// <summary>
    /// Provides methods for emitting wrappers.
    /// </summary>

    internal class WrapperEmitter
    {
        private readonly MethodEnvironment _env;
        private readonly Signature _wrapperSignature;
        private readonly Signature _pInvokeSignature;
        private readonly bool _requiresIndirectResult;
        private readonly bool _requiresSwiftSelf;
        private readonly bool _requiresSwiftError;
        private readonly bool _requiresSwiftAsync;

        internal WrapperEmitter(MethodEnvironment methodEnv, SignatureHandler signatureHandler)
        {
            _env = methodEnv;

            _wrapperSignature = signatureHandler.GetWrapperSignature();
            _pInvokeSignature = signatureHandler.GetPInvokeSignature();

            _requiresIndirectResult = MarshallingHelpers.MethodRequiresIndirectResult(methodEnv);
            _requiresSwiftSelf = MarshallingHelpers.MethodRequiresSwiftSelf(methodEnv);
            _requiresSwiftError = _env.MethodDecl.Throws;
            _requiresSwiftAsync = _env.MethodDecl.IsAsync;
        }

        /// <summary>
        /// Emits the wrapper.
        /// </summary>
        /// <param name="writer"></param>
        internal void Emit(CSharpWriter csWriter, SwiftWriter swiftWriter)
        {
            if (_env.MethodDecl.IsConstructor)
            {
                EmitConstructor(csWriter);
            }
            else
            {
                EmitMethod(csWriter, swiftWriter);
            }
        }

        /// <summary>
        /// Emits the constructor wrapper.
        /// </summary>
        /// <param name="writer">The IndentedTextWriter instance.</param>
        private void EmitConstructor(CSharpWriter csWriter)
        {
            EmitSignatureConstructor(csWriter);
            EmitBodyStart(csWriter);

            EmitDeclarationsForAllocations(csWriter);

            EmitTryBlockStart(csWriter);

            EmitSwiftSelf(csWriter);
            EmitIndirectResultConstructor(csWriter);
            EmitGenericArguments(csWriter);
            EmitPInvokeCall(csWriter);
            EmitSwiftError(csWriter);
            EmitReturnConstructor(csWriter);

            EmitTryBlockEnd(csWriter);

            EmitFinally(csWriter);

            EmitBodyEnd(csWriter);
        }

        /// <summary>
        /// Emits the declarations for allocations.
        /// </summary>
        private void EmitDeclarationsForAllocations(CSharpWriter csWriter)
        {
            foreach (var argument in _env.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric))
            {
                var (typeName, metadataName, payloadName) = _env.GenericTypeMapping[argument.SwiftTypeSpec.ToString()];
                csWriter.WriteLine($"IntPtr {payloadName} = IntPtr.Zero;");
            }
        }

        /// <summary>
        /// Emits the method wrapper.
        /// </summary>
        /// <param name="writer">The IndentedTextWriter instance.</param>
        private void EmitMethod(CSharpWriter csWriter, SwiftWriter swiftWriter)
        {
            EmitSignatureMethod(csWriter);
            EmitBodyStart(csWriter);
            EmitAsync(csWriter, swiftWriter);

            EmitDeclarationsForAllocations(csWriter);

            EmitTryBlockStart(csWriter);

            EmitSwiftSelf(csWriter);
            EmitIndirectResultMethod(csWriter);
            EmitGenericArguments(csWriter);
            EmitPInvokeCall(csWriter);
            EmitSwiftError(csWriter);
            EmitReturnMethod(csWriter);

            EmitTryBlockEnd(csWriter);

            EmitFinally(csWriter);

            EmitBodyEnd(csWriter);
        }

        /// <summary>
        /// Emits the SwiftSelf variable.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitSwiftSelf(CSharpWriter csWriter)
        {
            if (!_requiresSwiftSelf)
            {
                return;
            }

            if (_env.ParentDecl is StructDecl structDecl && MarshallingHelpers.StructIsMarshalledAsCSStruct(structDecl))
            {
                csWriter.WriteLine($"var self = new SwiftSelf<{_env.ParentDecl.Name}>(this);");
            }
            else
            {
                csWriter.WriteLine("var self = new SwiftSelf((void*)_payload);");
            }

            csWriter.WriteLine();
        }

        /// <summary>
        /// Emits the Async task.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitAsync(CSharpWriter csWriter, SwiftWriter swiftWriter)
        {
            if (!_requiresSwiftAsync)
                return;

            var text = $$"""
            TaskCompletionSource{{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{_wrapperSignature.ReturnType}>")}} task = new TaskCompletionSource{{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{_wrapperSignature.ReturnType}>")}}();
            GCHandle handle = GCHandle.Alloc(task, GCHandleType.Normal);
            """;
            csWriter.WriteLines(text);

            string parameters = string.Join(", ", new[] {$"callback: @escaping ({(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"{_env.MethodDecl.CSSignature.First().SwiftTypeSpec}, ")}Int64) -> Void", $"task: Int64"}.Concat(_env.MethodDecl.CSSignature.Skip(1).Select(p => p.Name + ": " + p.SwiftTypeSpec)));

            text = $$"""
            extension {{_env.MethodDecl.ParentDecl!.Name}} {
                @_silgen_name("{{_env.MethodDecl.Name}}_async")
                public {{(_env.MethodDecl.MethodType == MethodType.Static ? "static " : "")}} func {{NameProvider.GetPInvokeName(_env.MethodDecl)}}_async({{parameters}}) {
                    Task {
                        {{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : "let result = ")}}await {{(_env.MethodDecl.MethodType == MethodType.Static ? $"{_env.MethodDecl.ParentDecl.Name}." : "")}}{{_env.MethodDecl.Name}}({{string.Join(", ", _env.MethodDecl.CSSignature.Skip(1).Select(p => p.Name+": "+p.Name))}})
                        callback({{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : "result, ")}}task)
                    }
                }
            }
            """;
            swiftWriter.WriteLine(text);
        }

        /// <summary>
        /// Emits the IndirectResult set up in constructor context.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitIndirectResultConstructor(CSharpWriter csWriter)
        {
            if (!_requiresIndirectResult)
            {
                return;
            }

            var text = $$"""
            _payload = (SwiftHandle)NativeMemory.Alloc(_payloadSize);
            var swiftIndirectResult = new SwiftIndirectResult((void*)_payload);
            """;

            csWriter.WriteLines(text);
            csWriter.WriteLine();
        }


        /// <summary>
        /// Emits the IndirectResult set up in method context.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitIndirectResultMethod(CSharpWriter csWriter)
        {
            if (!_requiresIndirectResult)
            {
                return;
            }

            var text = $$"""
            var returnMetadata = TypeMetadata.GetTypeMetadataOrThrow<{{_wrapperSignature.ReturnType}}>();
            var payload = (SwiftHandle)NativeMemory.Alloc(returnMetadata.Size);
            var swiftIndirectResult = new SwiftIndirectResult((void*)payload);
            """;

            csWriter.WriteLines(text);
            csWriter.WriteLine();
        }

        /// <summary>
        /// Emits the generic arguments setup.
        /// </summary>
        private void EmitGenericArguments(CSharpWriter csWriter)
        {
            foreach (var argument in _env.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric))
            {
                var (typeName, metadataName, payloadName) = _env.GenericTypeMapping[argument.SwiftTypeSpec.ToString()];
                var text = $$"""
                var {{metadataName}} = TypeMetadata.GetTypeMetadataOrThrow<{{typeName}}>();
                {{payloadName}} = (IntPtr)NativeMemory.Alloc({{metadataName}}.Size);
                SwiftMarshal.MarshalToSwift({{argument.Name}}, {{payloadName}});
                """;
                csWriter.WriteLines(text);
            }
            csWriter.WriteLine();
        }

        /// <summary>
        /// Emits the PInvoke call.
        /// </summary>
        /// <param name="writer">The IndentedTextWriter instance.</param>
        private void EmitPInvokeCall(CSharpWriter csWriter)
        {
            if (_requiresSwiftAsync)
            {
                string parameters = string.Join(", ",
                    new[]
                    {
                        $"(IntPtr)s_{_env.MethodDecl.Name}Callback",
                        "IntPtr.Zero",
                        "GCHandle.ToIntPtr(handle)",
                        _pInvokeSignature.CallArgumentsString()
                    }.Where(arg => !string.IsNullOrEmpty(arg))
                );
                csWriter.WriteLine($"{NameProvider.GetPInvokeName(_env.MethodDecl)}({parameters});");
                csWriter.WriteLine();
            }
            else
            {
                var voidReturn = _env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple;
                var returnPrefix = (_requiresIndirectResult || voidReturn) ? "" : "var result = ";
                csWriter.WriteLine($"{returnPrefix}{NameProvider.GetPInvokeName(_env.MethodDecl)}({_pInvokeSignature.CallArgumentsString()});");
                csWriter.WriteLine();
            }
        }

        /// <summary>
        /// Emits the SwiftError handling.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitSwiftError(CSharpWriter csWriter)
        {
            if (!_requiresSwiftError)
            {
                return;
            }

            var text = $$"""
            if (error.Value != null)
            {
                throw new SwiftRuntimeException("Call to Swift method {{_env.MethodDecl.FullyQualifiedName}} failed.");
            }
            """;

            csWriter.WriteLines(text);
            csWriter.WriteLine();
        }

        /// <summary>
        /// Emits the return statement for the constructor.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitReturnConstructor(CSharpWriter csWriter)
        {
            if (!_requiresIndirectResult)
            {
                csWriter.WriteLine("this = result;");
            }
        }

        /// <summary>
        /// Emits the return statement for the method.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitReturnMethod(CSharpWriter csWriter)
        {
            if (_requiresIndirectResult)
            {
                csWriter.WriteLine($"return SwiftMarshal.MarshalFromSwift<{_wrapperSignature.ReturnType}>((SwiftHandle)swiftIndirectResult.Value);");
                return;
            }

            if (_requiresSwiftAsync)
            {
                csWriter.WriteLine("return task.Task;");
                return;
            }

            if (_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple)
            {
                csWriter.WriteLine("return;");
                return;
            }

            csWriter.WriteLine("return result;");
        }

        /// <summary>
        /// Emits the constructor signature.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitSignatureConstructor(CSharpWriter csWriter)
        {
            var genericParams = _env.MethodDecl.IsGeneric switch
            {
                true => $"<{string.Join(", ", _env.MethodDecl.GenericParameters.Select(p => _env.GenericTypeMapping[p.TypeName].PlaceholderName))}>",
                false => ""
            };
            csWriter.WriteLine($"public {_env.ParentDecl.Name}{genericParams}({_wrapperSignature.ParametersString()})");
        }

        /// <summary>
        /// Emits the method signature.
        /// </summary>
        /// <param name="writer">The IndentedTextWriter instance.</param>
        private void EmitSignatureMethod(CSharpWriter csWriter)
        {
            if (_requiresSwiftAsync)
            {
                var text = $$"""
                
                        private static unsafe delegate* unmanaged[Cdecl]<{{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"{_wrapperSignature.ReturnType}, ")}}IntPtr, void> s_{{_env.MethodDecl.Name}}Callback = &{{_env.MethodDecl.Name}}OnComplete;
                        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
                        private static void {{_env.MethodDecl.Name}}OnComplete({{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"{_wrapperSignature.ReturnType} result, ")}}IntPtr task)
                        {
                            GCHandle handle = GCHandle.FromIntPtr(task);
                            try
                            {
                                if (handle.Target is TaskCompletionSource{{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{_wrapperSignature.ReturnType}>")}} tcs)
                                {
                                    tcs.TrySetResult({{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : "result")}});
                                }
                            }
                            finally
                            {
                                handle.Free();
                            }
                        }
                """;
                csWriter.WriteLine(text);
            }
            var genericParams = _env.MethodDecl.IsGeneric switch
            {
                true => $"<{string.Join(", ", _env.MethodDecl.GenericParameters.Select(p => _env.GenericTypeMapping[p.TypeName].PlaceholderName))}>",
                false => ""
            };

            var staticKeyword = _env.MethodDecl.MethodType == MethodType.Static || _env.ParentDecl is ModuleDecl ? "static " : "";
            var unsafeKeyword = _requiresIndirectResult || _requiresSwiftAsync || _env.MethodDecl.IsGeneric ? "unsafe " : "";

            var returnType = _wrapperSignature.ReturnType;
            if (_requiresSwiftAsync)
            {
                returnType = $"Task{(_env.MethodDecl.CSSignature.First().SwiftTypeSpec.IsEmptyTuple ? "" : $"<{_wrapperSignature.ReturnType}>")}";
            }

            csWriter.WriteLine($"public {staticKeyword}{unsafeKeyword}{returnType} {_env.MethodDecl.Name}{genericParams}({_wrapperSignature.ParametersString()})");
        }

        /// <summary>
        /// Emits the finally block.
        /// </summary>
        private void EmitFinally(CSharpWriter csWriter)
        {
            csWriter.WriteLine("finally");
            EmitBodyStart(csWriter);

            foreach (var argument in _env.MethodDecl.CSSignature.Skip(1).Where(a => a.IsGeneric))
            {
                var (_, _, payloadName) = _env.GenericTypeMapping[argument.SwiftTypeSpec.ToString()];
                csWriter.WriteLine($"NativeMemory.Free((void*){payloadName});");
            }

            EmitBodyEnd(csWriter);
        }

        /// <summary>
        /// Emits the try block start.
        /// </summary>
        private void EmitTryBlockStart(CSharpWriter csWriter)
        {
            csWriter.WriteLine("try");
            EmitBodyStart(csWriter);
        }

        /// <summary>
        /// Emits the try block end.
        /// </summary>
        private void EmitTryBlockEnd(CSharpWriter csWriter)
        {
            EmitBodyEnd(csWriter);
        }

        /// <summary>
        /// Emits the body start.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitBodyStart(CSharpWriter csWriter)
        {
            csWriter.WriteLine("{");
            csWriter.Indent++;
        }

        /// <summary>
        /// Emits the body end.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        private void EmitBodyEnd(CSharpWriter csWriter)
        {
            csWriter.Indent--;
            csWriter.WriteLine("}");
            csWriter.WriteLine();
        }
    }
}
