// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;

namespace BindingsGeneration
{
    /// <summary>
    /// Represents a constructor method handler factory.
    /// </summary>
    public class MethodConstructorHandlerFactory : IFactory<BaseDecl, IMethodHandler>
    {
        /// <summary>
        /// Checks if the factory can handle the declaration.
        /// </summary>
        /// <param name="decl">The base declaration.</param>
        /// <returns></returns>
        public bool Handles(BaseDecl decl)
        {
            return decl is MethodDecl methodDecl && methodDecl.IsConstructor;
        }

        /// <summary>
        /// Constructs a handler.
        /// </summary>
        public IMethodHandler Construct()
        {
            return new MethodConstructorHandler();
        }
    }

    /// <summary>
    /// Represents a constructor method handler.
    /// </summary>
    public class MethodConstructorHandler : MethodHandler, IMethodHandler
    {
        public MethodConstructorHandler()
        {
        }

        /// <summary>
        /// Marshals the method declaration.
        /// </summary>
        /// <param name="methodDecl">The method declaration.</param>
        /// <param name="typeDatabase">The type database instance.</param>
        public override IEnvironment? Marshal(BaseDecl decl, ITypeDatabase typeDatabase)
        {
            MethodEnvironment? env = base.Marshal(decl, typeDatabase) as MethodEnvironment;
            if (env != null)
            {
                // TODO: Make better interface between builders and handlers
                env.MethodName = decl?.ParentDecl?.Name ?? throw new ArgumentNullException(nameof(MethodDecl));
                env.ReturnType = string.Empty;
                env.WrapperBuilder = new ConstructorWrapperBuilder();
            }
            return env;
        }
    }

    public partial class ConstructorWrapperBuilder : WrapperBuilder
    {
        public override string EmitReturnMethod(MethodEnvironment methodEnv, string returnType)
        {
            if (!methodEnv.RequiresIndirectResult)
            {
                return "this = result;";
            }

            return "";
        }

        public override string EmitIndirectResultMethod(MethodEnvironment methodEnv, string returnType)
        {
            if (!methodEnv.RequiresIndirectResult)
                return "";

            return $$"""
            _payload = (SwiftHandle)NativeMemory.Alloc(_payloadSize);
            var swiftIndirectResult = new SwiftIndirectResult((void*)_payload);
            """;
        }
    }
}
