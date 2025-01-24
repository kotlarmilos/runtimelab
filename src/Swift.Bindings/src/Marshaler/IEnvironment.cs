// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Swift.Runtime;

namespace BindingsGeneration
{
    /// <summary>
    /// Represents an environment interface. It should contain data required to emit C# code.
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        /// Gets the TypeDatabase
        /// </summary>
        public ITypeDatabase TypeDatabase { get; }
    }

    /// <summary>
    /// Represents a module environment.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the ModuleEnvironment class.
    /// </remarks>
    /// <param name="moduleDecl">The module declaration.</param>
    /// <param name="typeDatabase">The type database instance.</param>
    public class ModuleEnvironment(ModuleDecl moduleDecl, ITypeDatabase typeDatabase) : IEnvironment
    {
        /// <summary>
        /// Gets the module declaration.
        /// </summary>
        public ModuleDecl ModuleDecl { get; private set; } = moduleDecl;

        /// <summary>
        /// Gets the TypeDatabase
        /// </summary>
        public ITypeDatabase TypeDatabase { get; } = typeDatabase;
    }

    /// <summary>
    /// Represents a type environment.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the TypeEnvironment class.
    /// </remarks>
    /// <param name="typeDecl">The type declaration.</param>
    /// <param name="typeDatabase">The type database instance.</param>
    public class TypeEnvironment(TypeDecl typeDecl, ITypeDatabase typeDatabase) : IEnvironment
    {
        /// <summary>
        /// Gets the type declaration.
        /// </summary>
        public TypeDecl TypeDecl { get; private set; } = typeDecl;

        /// <summary>
        /// Gets the TypeDatabase
        /// </summary>
        public ITypeDatabase TypeDatabase { get; } = typeDatabase;
    }

    /// <summary>
    /// Represents a method environment.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the MethodEnvironment class.
    /// </remarks>
    /// <param name="methodDecl">The method declaration.</param>
    /// <param name="typeDatabase">The type database instance.</param>
    /// <param name="GenericTypeMapping">The generic type mapping.</param>
    /// <param name="requiresIndirectResult">A value indicating whether the method requires an indirect result.</param>
    /// <param name="requiresSwiftSelf">A value indicating whether the method requires a Swift self parameter.</param>
    /// <param name="requiresSwiftError">A value indicating whether the method requires a Swift error parameter.</param>
    /// <param name="requiresSwiftAsync">A value indicating whether the method requires a Swift async parameter.</param>
    /// <param name="pInvokeBuilder">The PInvokeBuilder.</param>
    /// <param name="wrapperBuilder">The WrapperBuilder.</param>
    public class MethodEnvironment(MethodDecl methodDecl, ITypeDatabase typeDatabase, Dictionary<string, GenericParameterCSName> genericTypeMapping, bool requiresIndirectResult, bool requiresSwiftSelf, bool requiresSwiftError, bool requiresSwiftAsync, PInvokeBuilder pInvokeBuilder, WrapperBuilder wrapperBuilder) : IEnvironment
    {
        /// <summary>
        /// Gets the method declaration.
        /// </summary>
        public MethodDecl MethodDecl { get; init; } = methodDecl;

        /// <summary>
        /// Gets the parent declaration.
        /// </summary>
        public BaseDecl ParentDecl { get; } = methodDecl.ParentDecl ?? throw new ArgumentNullException($"Parent declaration on method {methodDecl.Name} is null.");

        /// <summary>
        /// Gets the TypeDatabase
        /// </summary>
        public ITypeDatabase TypeDatabase { get; } = typeDatabase;

        /// <summary>
        /// Gets the generic type mapping.
        /// </summary>
        public Dictionary<string, GenericParameterCSName> GenericTypeMapping { get; } = genericTypeMapping;

        /// <summary>
        /// Gets the method name.
        /// </summary>
        public string MethodName { get; set; } = methodDecl.Name;

        /// <summary>
        /// Gets the return type.
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the method requires an indirect result.
        /// </summary>
        public bool RequiresIndirectResult { get; } = requiresIndirectResult;

        /// <summary>
        /// Gets a value indicating whether the method requires a Swift self parameter.
        /// </summary>
        public bool RequiresSwiftSelf { get; } = requiresSwiftSelf;

        /// <summary>
        /// Gets a value indicating whether the method requires a Swift error parameter.
        /// </summary>
        public bool RequiresSwiftError { get; } = requiresSwiftError;

        /// <summary>
        /// Gets a value indicating whether the method requires a Swift async parameter.
        /// </summary>
        public bool RequiresSwiftAsync { get; } = requiresSwiftAsync;

        /// <summary>
        /// Gets the PInvokeBuilder.
        /// </summary>
        public PInvokeBuilder PInvokeBuilder { get; set; } = pInvokeBuilder;

        /// <summary>
        /// Gets the WrapperBuilder.
        /// </summary>
        public WrapperBuilder WrapperBuilder { get; set; } = wrapperBuilder;
    }
}
