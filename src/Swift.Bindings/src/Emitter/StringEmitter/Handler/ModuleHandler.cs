// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;
using Swift.Runtime;

namespace BindingsGeneration
{
    /// <summary>
    /// Factory class for creating instances of ModuleHandler.
    /// </summary>
    public class ModuleHandlerFactory : IFactory<BaseDecl, IModuleHandler>
    {
        /// <summary>
        /// Determines if the factory handles the specified declaration.
        /// </summary>
        /// <param name="decl">The base declaration.</param>
        public bool Handles(BaseDecl decl)
        {
            return decl is ModuleDecl;
        }

        /// <summary>
        /// Constructs a new instance of ModuleHandler.
        /// </summary>
        public IModuleHandler Construct()
        {
            return new ModuleHandler();
        }
    }

    /// <summary>
    /// Handler class for module declarations.
    /// </summary>
    public class ModuleHandler : BaseHandler, IModuleHandler
    {
        public ModuleHandler()
        {
        }

        /// <summary>
        /// Marshals the module declaration.
        /// </summary>
        /// <param name="moduleDecl">The module declaration.</param>
        /// <param name="typeDatabase">The type database instance.</param>
        public IEnvironment Marshal(BaseDecl decl, ITypeDatabase typeDatabase)
        {
            if (decl is not ModuleDecl moduleDecl)
            {
                throw new ArgumentException("The provided decl must be a ModuleDecl.", nameof(decl));
            }
            return new ModuleEnvironment(moduleDecl, typeDatabase);
        }

        /// <summary>
        /// Emits the code for the specified environment.
        /// </summary>
        /// <param name="csWriter">The IndentedTextWriter instance.</param>
        /// <param name="env">The environment.</param>
        /// <param name="conductor">The conductor instance.</param>
        /// <param name="typeDatabase">The type database instance.</param>
        public void Emit(CSharpWriter csWriter, SwiftWriter swiftWriter, IEnvironment env, Conductor conductor)
        {
            var moduleEnv = (ModuleEnvironment)env;
            var moduleDecl = moduleEnv.ModuleDecl;

            var generatedNamespace = $"Swift.{moduleDecl.Name}";

            csWriter.WriteLine($"using System;");
            csWriter.WriteLine($"using System.Runtime.CompilerServices;");
            csWriter.WriteLine($"using System.Runtime.InteropServices;");
            csWriter.WriteLine($"using System.Runtime.InteropServices.Swift;");
            csWriter.WriteLine($"using Swift;");
            csWriter.WriteLine($"using Swift.Runtime;");
            csWriter.WriteLine($"using Swift.Runtime.InteropServices;");
            csWriter.WriteLine();
            csWriter.WriteLine($"namespace {generatedNamespace}");
            csWriter.WriteLine("{");
            csWriter.Indent++;

            // Emit top-level fields and pinvokes
            if (moduleDecl.Methods.Any() || moduleDecl.Fields.Any())
            {
                csWriter.WriteLine($"public class {moduleDecl.Name}");
                csWriter.WriteLine("{");
                csWriter.Indent++;
                foreach (FieldDecl fieldDecl in moduleDecl.Fields)
                {
                    string accessModifier = fieldDecl.Visibility == Visibility.Public ? "public" : "private";
                    var fieldTypeRecord = moduleEnv.TypeDatabase.GetTypeRecordOrThrow(fieldDecl.SwiftTypeSpec);
                    csWriter.WriteLine($"{accessModifier} {fieldTypeRecord.CSTypeIdentifier} {fieldDecl.Name};");
                }
                csWriter.WriteLine();
                foreach (MethodDecl methodDecl in moduleDecl.Methods)
                {
                    if (conductor.TryGetMethodHandler(methodDecl, out var methodHandler))
                    {
                        var methodEnv = methodHandler.Marshal(methodDecl, env.TypeDatabase);
                        if (methodEnv != null)
                            methodHandler.Emit(csWriter, swiftWriter, methodEnv, conductor);
                    }
                    else
                    {
                        Console.WriteLine($"No handler found for method {methodDecl.Name}");
                    }
                    // EmitMethod(csWriter, swiftWriter, moduleDecl, moduleDecl, methodDecl);
                    csWriter.WriteLine();
                }
                csWriter.Indent--;
                csWriter.WriteLine("}");
                csWriter.WriteLine();
            }

            // Emit top-level types
            base.HandleBaseDecl(csWriter, swiftWriter, moduleDecl.Types, conductor, env.TypeDatabase);

            csWriter.Indent--;
            csWriter.WriteLine("}");

        }
    }
}
