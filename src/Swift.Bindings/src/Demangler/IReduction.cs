// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace BindingsGeneration.Demangling;

/// <summary>
/// Represents the result of reducing a Node or tree of nodes
/// </summary>
public interface IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    string Symbol { get; init; }
}

/// <summary>
/// Represents an error in an attempted reduction
/// </summary>
public class ReductionError : IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    public required string Symbol { get; init; }
    /// <summary>
    /// Returns an error message describing the error
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Returns the severity of the error
    /// </summary>
    public ReductionErrorSeverity Severity { get; set; } = ReductionErrorSeverity.Low;
}

/// <summary>
/// Represents a reduction that reduces to a single type
/// </summary>
public class TypeSpecReduction : IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    public required string Symbol { get; init; }
    /// <summary>
    /// Returns a TypeSpec for the type that this node represents
    /// </summary>
    public required TypeSpec TypeSpec { get; init; }
}

/// <summary>
/// Represents a reduction for a metadata acessor function.
/// </summary>
public class MetadataAccessorReduction : IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    public required string Symbol { get; init; }
    /// <summary>
    /// Returns a NameTypeSpec for the type hosting the metadata
    /// </summary>
    public required NamedTypeSpec TypeSpec { get; init; }
}

/// <summary>
/// Represents a reduction that reduces to a Swift function
/// </summary>
public class FunctionReduction : IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    public required string Symbol { get; init; }
    /// <summary>
    /// Returns a function that this type represents
    /// </summary>
    public required SwiftFunction Function { get; init; }
}

/// <summary>
/// Represents a dispatch thunk for a function.
/// While they are identical in terms of content, having this as a separate type will make it easy to locate
/// dispatch thunks.
/// </summary>
public class DispatchThunkFunctionReduction : FunctionReduction
{
    /// <summary>
    /// Factory method to create a DispatchThunkFunctionReduction from a FunctionReduction
    /// </summary>
    /// <param name="reduction">The FunctionReduction to copy</param>
    /// <returns>A DispatchThunkFunctionReduction that matches the give FunctionReduction</returns>
    public static DispatchThunkFunctionReduction FromFunctionReduction(FunctionReduction reduction)
    {
        return new DispatchThunkFunctionReduction() { Symbol = reduction.Symbol, Function = reduction.Function };
    }
}

/// <summary>
/// Represents a reduction to a protocol witness table
/// </summary>
public class ProtocolWitnessTableReduction : IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    public required string Symbol { get; init; }
    /// <summary>
    /// Returns the TypeSpec of the type that implements the protocol
    /// </summary>
    public required NamedTypeSpec ImplementingType { get; init; }
    /// <summary>
    /// Returns the TypeSpec of the protocol being implemented
    /// </summary>
    public required NamedTypeSpec ProtocolType { get; init; }
}

/// <summary>
/// Represents a reduction to a protocol conformance descriptor
/// </summary>
public class ProtocolConformanceDescriptorReduction : ProtocolWitnessTableReduction
{
    public required string Module { get; init; }
}

/// <summary>
/// Represents a reduction to a provenance
/// </summary>
public class ProvenanceReduction : IReduction
{
    /// <summary>
    /// Returns a the mangled symbol associated with a reduction
    /// </summary>
    public required string Symbol { get; init; }
    /// <summary>
    /// Returns the Provenance of reduction
    /// </summary>
    public required Provenance Provenance { get; init; }

    /// <summary>
    /// Factory method to construct a top-level provenance reduction
    /// </summary>
    public static ProvenanceReduction TopLevel(string symbol, string moduleName) =>
        new ProvenanceReduction() { Symbol = symbol, Provenance = Provenance.TopLevel(moduleName) };

    /// <summary>
    /// Factory method to construct an instance provenance reduction
    /// </summary>
    public static ProvenanceReduction Instance(string symbol, NamedTypeSpec instance) =>
        new ProvenanceReduction() { Symbol = symbol, Provenance = Provenance.Instance(instance) };

    /// <summary>
    /// Factory method to construct an extension provenance reduction
    /// </summary>
    public static ProvenanceReduction Extension(string symbol, NamedTypeSpec extensionOn) =>
        new ProvenanceReduction() { Symbol = symbol, Provenance = Provenance.Extension(extensionOn) };
}
