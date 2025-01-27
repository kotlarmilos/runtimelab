// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;
using Swift.Runtime;

namespace BindingsGeneration
{
    /// <summary>
    /// Represents an string-based C# emitter.
    /// </summary>
    public partial class StringEmitter : IEmitter
    {
        // Private properties
        private readonly string _outputDirectory;
        private readonly ITypeDatabase _typeDatabase;
        private readonly int _verbose;
        private readonly Conductor _conductor;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringEmitter"/> class.
        /// </summary>
        public StringEmitter(string outputDirectory, ITypeDatabase typeDatabase, int verbose = 0)
        {
            _outputDirectory = outputDirectory;
            _typeDatabase = typeDatabase;
            _verbose = verbose;
            _conductor = new Conductor();
        }

        /// <summary>
        /// Emits a C# module based on the module declaration.
        /// </summary>
        /// <param name="moduleDecl">The module declaration.</param>
        public void EmitModule(ModuleDecl moduleDecl)
        {
            if (_conductor.TryGetModuleHandler(moduleDecl, out var moduleHandler))
            {
                var csStringWriter = new StringWriter();
                CSharpWriter csWriter = new(csStringWriter);
                var swiftStringWriter = new StringWriter();
                SwiftWriter swiftWriter = new(swiftStringWriter);
                var @namespace = $"Swift.{moduleDecl.Name}";

                var env = moduleHandler.Marshal(moduleDecl, _typeDatabase);
                moduleHandler.Emit(csWriter, swiftWriter, env, _conductor);

                string csOutputPath = Path.Combine(_outputDirectory, $"{@namespace}.cs");
                using (StreamWriter outputFile = new(csOutputPath))
                {
                    outputFile.Write(csStringWriter.ToString());
                }
                string swiftOutputPath = Path.Combine(_outputDirectory, $"{@namespace}.swift");
                using (StreamWriter outputFile = new(swiftOutputPath))
                {
                    outputFile.Write(swiftStringWriter.ToString());
                }
            }
            else
            {
                if (_verbose > 0)
                    Console.WriteLine($"No module handler found for {moduleDecl.Name}");
            }
        }
    }
}
