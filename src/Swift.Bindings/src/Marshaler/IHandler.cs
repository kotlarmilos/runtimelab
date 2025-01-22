// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;
using Swift.Runtime;

namespace BindingsGeneration
{
    /// <summary>
    /// Interface for handling various types of declarations.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Marshals the specified base declaration.
        /// </summary>
        /// <param name="baseDecl">The base declaration.</param>
        /// <returns>The environment corresponding to the base declaration.</returns>
        IEnvironment Marshal(BaseDecl baseDecl, ITypeDatabase typeDatabase);

        /// <summary>
        /// Emits the necessary code for the specified environment.
        /// </summary>
        /// <param name="writer">The IndentedTextWriter instance.</param>
        /// <param name="env">The environment.</param>
        /// <param name="conductor">The conductor instance.</param>
        void Emit(IndentedTextWriter csWriter, IndentedTextWriter swiftWriter, IEnvironment env, Conductor conductor);
    }

    /// <summary>
    /// Interface for handling module declarations.
    /// </summary>
    public interface IModuleHandler : IHandler
    {
    }

    /// <summary>
    /// Interface for handling type declarations.
    /// </summary>
    public interface ITypeHandler : IHandler
    {
    }

    /// <summary>
    /// Interface for handling field declarations.
    /// </summary>
    public interface IFieldHandler : IHandler
    {
    }

    /// <summary>
    /// Interface for handling method declarations.
    /// </summary>
    public interface IMethodHandler : IHandler
    {
    }

    /// <summary>
    /// Interface for handling argument declarations.
    /// </summary>
    public interface IArgumentHandler : IHandler
    {
    }

    /// <summary>
    /// Base class for handling declarations.
    /// </summary>
    public class BaseHandler
    {
        /// <summary>
        /// Handles a base declaration.
        /// </summary>
        /// <param name="writer">The IndentedTextWriter instance.</param>
        /// <param name="decl">The list of base declarations.</param>
        /// <param name="conductor">The conductor instance.</param>
        /// <param name="typeDatabase">The type database instance.</param>
        protected virtual void HandleBaseDecl(IndentedTextWriter csWriter, IndentedTextWriter swiftWriter, IEnumerable<BaseDecl> decl, Conductor conductor, ITypeDatabase typeDatabase)
        {
            foreach (var baseDecl in decl)
            {
                if (baseDecl is StructDecl structDecl)
                {
                    if (conductor.TryGetTypeHandler(structDecl, out var handler))
                    {
                        var env = handler.Marshal(structDecl, typeDatabase);
                        handler.Emit(csWriter, swiftWriter, env, conductor);
                    }
                    else
                    {
                        Console.WriteLine($"No handler found for method {structDecl.Name}");
                    }
                }
                else if (baseDecl is ClassDecl classDecl)
                {
                    if (conductor.TryGetTypeHandler(classDecl, out var handler))
                    {
                        var env = handler.Marshal(classDecl, typeDatabase);
                        handler.Emit(csWriter, swiftWriter, env, conductor);
                    }
                    else
                    {
                        Console.WriteLine($"No handler found for method {classDecl.Name}");
                    }
                }
                else if (baseDecl is MethodDecl methodDecl)
                {
                    if (conductor.TryGetMethodHandler(methodDecl, out var handler))
                    {
                        var env = handler.Marshal(methodDecl, typeDatabase);
                        handler.Emit(csWriter, swiftWriter, env, conductor);
                    }
                    else
                    {
                        Console.WriteLine($"No handler found for method {methodDecl.Name}");
                    }
                }
                else
                {
                    var declType = baseDecl?.GetType() ?? throw new ArgumentNullException(nameof(baseDecl));
                    throw new NotImplementedException($"Unsupported declaration type: {declType}");
                }

                csWriter.WriteLine();
            }
        }
    }
}
