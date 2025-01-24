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
        public virtual IEnvironment? Marshal(BaseDecl decl, ITypeDatabase typeDatabase)
        {
            if (decl is not MethodDecl methodDecl)
            {
                throw new ArgumentException("The provided decl must be a MethodDecl.", nameof(decl));
            }

            if (methodDecl.GenericParameters.Any(x => x.Constraints.Count > 0))
            {
                Console.WriteLine($"Method {methodDecl.Name} has unsupported generic constraints");
                return null;
            }

            Dictionary<string, GenericParameterCSName> genericTypeMapping = NameProvider.GetGenericTypeMapping(methodDecl);
            PInvokeBuilder pInvokeBuilder = new PInvokeBuilder();
            WrapperBuilder wrapperBuilder = new WrapperBuilder();

            MethodEnvironment env = new MethodEnvironment(methodDecl, typeDatabase, genericTypeMapping, pInvokeBuilder, wrapperBuilder);

            // TODO: Fix circular dependency
            SignatureHandler signatureHandler = new SignatureHandler(env);

            if (signatureHandler.GetWrapperSignature().ContainsPlaceholder)
            {
                Console.WriteLine($"Method {env.MethodDecl.Name} has unsupported signature: ({signatureHandler.GetWrapperSignature().ParametersExpression()}) -> {signatureHandler.GetWrapperSignature().ReturnType}");
                return null;
            }

            env.MethodName = methodDecl.Name;
            env.ReturnType = signatureHandler.GetWrapperSignature().ReturnType;
            env.RequiresIndirectResult = MarshallingHelpers.MethodRequiresIndirectResult(methodDecl, typeDatabase);
            env.RequiresSwiftSelf = MarshallingHelpers.MethodRequiresSwiftSelf(methodDecl);
            env.RequiresSwiftError = methodDecl.Throws;
            env.RequiresSwiftAsync = methodDecl.IsAsync;
            return env;
        }

        /// <summary>
        /// Emits the method declaration.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        /// <param name="swiftWriter">The IndentedTextWriter instance.</param>
        /// <param name="env">The environment.</param>
        /// <param name="conductor">The conductor instance.</param>
        public virtual void Emit(CSharpWriter csWriter, SwiftWriter swiftWriter, IEnvironment env, Conductor conductor)
        {
            MethodEnvironment methodEnv = (MethodEnvironment)env;
            ModuleDecl moduleDecl = methodEnv.MethodDecl.ModuleDecl ?? throw new ArgumentNullException(nameof(methodEnv.MethodDecl.ModuleDecl));
            
            string pinvokeName = NameProvider.GetPInvokeName(methodEnv.MethodDecl);
            string libPath = methodEnv.TypeDatabase.GetLibraryPath(moduleDecl.Name);
            SignatureHandler signatureHandler = new SignatureHandler(methodEnv);
            Signature pinvokeSignature = signatureHandler.GetPInvokeSignature();
            Signature wrapperSignature = signatureHandler.GetWrapperSignature();
            PInvokeBuilder pInvokeBuilder = methodEnv.PInvokeBuilder;
            WrapperBuilder wrapperBuilder = methodEnv.WrapperBuilder;

            var staticKeyword = methodEnv.MethodDecl.MethodType == MethodType.Static || methodEnv.ParentDecl is ModuleDecl ? "static " : "";
            var unsafeKeyword = methodEnv.RequiresIndirectResult || methodEnv.MethodDecl.IsGeneric ? "unsafe " : "";
            string[] modifiers = new string[] { staticKeyword, unsafeKeyword }.Where(m => !string.IsNullOrEmpty(m)).ToArray();

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

            csWriter.WriteLine($$"""
                {{pInvokeBuilder.EmitPInvokeSignature(libPath, methodEnv.MethodDecl.MangledName, pinvokeSignature.ReturnType, pinvokeName, pinvokeSignature.ParametersExpression())}}

                {{wrapperBuilder.EmitMethodSignature(modifiers, methodEnv.ReturnType, methodEnv.MethodName, generics, wrapperSignature.ParametersExpression())}}
                {
                    {{wrapperBuilder.EmitDeclarationsForAllocations(payloadNames)}}
                    try
                    {
                        {{wrapperBuilder.EmitSwiftSelf(methodEnv, swiftSelf)}}
                        {{wrapperBuilder.EmitIndirectResultMethod(methodEnv, wrapperSignature.ReturnType)}}
                        {{wrapperBuilder.EmitGenericArguments(metadataNames, typeNames, argumentNames, payloadNames)}}
                        {{wrapperBuilder.EmitPInvokeCall(hasReturn, NameProvider.GetPInvokeName(methodEnv.MethodDecl), pinvokeSignature.ArgumentsExpression())}}
                        {{wrapperBuilder.EmitSwiftError(methodEnv, methodEnv.MethodDecl.Name)}}
                        {{wrapperBuilder.EmitReturnMethod(methodEnv, wrapperSignature.ReturnType)}}
                    }
                    finally { }
                }
            """);
        }
    }

    /// <summary>
    /// Represents a parameter.
    /// </summary>
    /// <param name="Type"></param>
    /// <param name="Name"></param>
    /// <param name="Modifier"></param>
    public record Parameter(string Type, string Name, string Modifier = "")
    {
        public string ParameterExpression() => $"{Modifier} {Type} {Name}";
        public string ArgumentExpression() => this switch
        {
            { Type: "SwiftHandle" } => $"{Name}.Payload",
            { Modifier: "out" } => $"out var {Name}",
            _ => Name
        };
    }

    /// <summary>
    /// Represents a signature.
    /// </summary>
    /// <param name="ReturnType"></param>
    /// <param name="Parameters"></param>
    public record Signature(string ReturnType, IReadOnlyList<Parameter> Parameters)
    {
        public bool ContainsPlaceholder => Parameters.Any(p => p.Type == "AnyType") || ReturnType == "AnyType";

        public string[] ParametersExpression() => Parameters.Select(p => p.ParameterExpression()).ToArray();
        public string[] ArgumentsExpression() => Parameters.Select(p => p.ArgumentExpression()).ToArray();
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
            if (!MarshallingHelpers.MethodRequiresIndirectResult(_env.MethodDecl, _env.TypeDatabase))
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
            if (MarshallingHelpers.MethodRequiresSwiftSelf(_env.MethodDecl))
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
    public class PInvokeBuilder
    {
        /// <summary>
        /// Emits the PInvoke signature.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        /// <param name="methodEnv">The method environment.</param>
        public string EmitPInvokeSignature(string libPath, string mangledName, string returnType, string methodName, string[] parameters)
        {
            return $$"""
            [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
            [DllImport("{{libPath}}", EntryPoint = "{{mangledName}}")]
            private static extern {{returnType}} {{methodName}}({{string.Join(", ", parameters)}});
            """;
        }
    }

    /// <summary>
    /// Provides methods for emitting wrappers.
    /// </summary>
    public partial class WrapperBuilder
    {
        /// <summary>
        /// Emits the declarations for allocations.
        /// </summary>
        /// <param name="names">The names of the allocations.</param>
        /// <returns>The emitted declarations.</returns>
        public string EmitDeclarationsForAllocations(string[] names)
        {
            return string.Join("\n", names.Select(name => $"IntPtr {name} = IntPtr.Zero;"));
        }

        /// <summary>
        /// Emits the SwiftSelf variable.
        /// </summary>
        /// <param name="env">The method environment.</param>
        /// <param name="name">The name of the SwiftSelf variable.</param>
        public string EmitSwiftSelf(MethodEnvironment env, string name)
        {
            if (!env.RequiresSwiftSelf)
                return "";

            if (string.IsNullOrEmpty(name))
            {
                return $"""
                var self = new SwiftSelf((void*)_payload);
                """;
            }
            else
            {
                return $"""
                var self = new SwiftSelf<{name}>(this);
                """;
            }
        }
        
        /// <summary>
        /// Emits the IndirectResult set up in constructor context.
        /// </summary>
        public string EmitIndirectResultConstructor()
        {
            return $$"""
            _payload = (SwiftHandle)NativeMemory.Alloc(_payloadSize);
            var swiftIndirectResult = new SwiftIndirectResult((void*)_payload);
            """;
        }

        /// <summary>
        /// Emits the IndirectResult set up in method context.
        /// </summary>
        /// <param name="env">The method environment.</param>
        /// <param name="returnType">The return type.</param>
        public virtual string EmitIndirectResultMethod(MethodEnvironment env, string returnType)
        {
            if (!env.RequiresIndirectResult)
                return "";

            return $$"""
            var returnMetadata = TypeMetadata.GetTypeMetadataOrThrow<{{returnType}}>();
            var payload = (SwiftHandle)NativeMemory.Alloc(returnMetadata.Size);
            var swiftIndirectResult = new SwiftIndirectResult((void*)payload);
            """;
        }

        /// <summary>
        /// Emits the generic arguments setup.
        /// </summary>
        /// <param name="metadataName">The names of the metadata.</param>
        /// <param name="typeName">The names of the types.</param>
        /// <param name="argumentName">The names of the arguments.</param>
        /// <param name="payloadName">The names of the payloads.</param>
        public string EmitGenericArguments(string[] metadataName, string[] typeName, string[] argumentName, string[] payloadName)
        {
            string text = "";
            for (int i = 0; i < metadataName.Length; i++)
            {
                text += $"""
                var {metadataName[i]} = TypeMetadata.GetTypeMetadataOrThrow<{typeName[i]}>();
                {payloadName[i]} = (IntPtr)NativeMemory.Alloc({metadataName[i]}.Size);
                SwiftMarshal.MarshalToSwift({argumentName[i]}, {payloadName[i]});
                """;
            }

            return text;
        }

        /// <summary>
        /// Emits the PInvoke call.
        /// </summary>
        /// <param name="hasReturn">Whether the method has a return value.</param>
        /// <param name="name">The name of the method.</param>
        /// <param name="parameters">The parameters of the method.</param>
        public string EmitPInvokeCall(bool hasReturn, string name, string[] parameters)
        {
            if (hasReturn)
                return $"""
                var result = {name}({string.Join(", ", parameters)});
                """;
            else
                return $"""
                {name}({string.Join(", ", parameters)});
                """;
        }

        /// <summary>
        /// Emits the SwiftError handling.
        /// </summary>
        /// <param name="env">The method environment.</param>
        /// <param name="methodName">The name of the method.</param>
        public string EmitSwiftError(MethodEnvironment env, string methodName)
        {
            if (!env.RequiresSwiftError)
                return "";

            return $$"""
            if (error.Value != null)
            {
                throw new SwiftRuntimeException("Call to Swift method {{methodName}} failed.");
            }
            """;
        }

        /// <summary>
        /// Emits the return statement for the method.
        /// </summary>
        /// <param name="env">The method environment.</param>
        /// <param name="returnType">The return type.</param>
        public virtual string EmitReturnMethod(MethodEnvironment env, string returnType)
        {
            if (env.RequiresIndirectResult)
            {
                return $"return SwiftMarshal.MarshalFromSwift<{returnType}>((SwiftHandle)swiftIndirectResult.Value);";
            }
            else
            {
                if (returnType == "void")
                {
                    return "return;";
                }
                else
                {
                    return "return result;";
                }
            }
        }

        /// <summary>
        /// Emits the method signature.
        /// </summary>
        /// <param name="modifiers">The modifiers.</param>
        /// <param name="returnType">The return type.</param>
        /// <param name="name">The name of the method.</param>
        /// <param name="generics">The generic parameters.</param>
        /// <param name="parameters">The parameters.</param>
        public string EmitMethodSignature(string[] modifiers, string returnType, string name, string[] generics, string[] parameters)
        {
            string genericsString = generics.Length > 0 ? $"<{string.Join(", ", generics)}>" : "";
            return $"""
            public {string.Join(" ", modifiers)}{returnType} {name}{genericsString}({string.Join(", ", parameters)})
            """;
        }
    }
}
